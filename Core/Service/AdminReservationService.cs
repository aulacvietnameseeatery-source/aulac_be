using Core.DTO.Reservation;
using Core.Enum;
using Core.Interface.Repo;
using Core.Interface.Service;
using Core.Interface.Service.Entity;
using Core.Interface.Service.LookUp;
using Core.Interface.Service.Others;
using Microsoft.Extensions.Logging;
using Core.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hangfire;

namespace Core.Service
{
    public class AdminReservationService : IAdminReservationService
    {
        private readonly IReservationRepository _reservationRepository;
        private readonly ILogger<AdminReservationService> _logger;
        private readonly ITableRepository _tableRepository;
        private readonly ILookupResolver _lookupResolver;
        private readonly IRealtimeNotificationService _realtimeNotification;
        private readonly IUnitOfWork _uow;
        private readonly IJobSchedulerService _jobScheduler;

        public AdminReservationService(
            IReservationRepository reservationRepository,
            ITableRepository tableRepository,
            ILogger<AdminReservationService> logger,
            ILookupResolver lookupResolver,
            IRealtimeNotificationService realtimeNotification,
            IUnitOfWork uow,
            IJobSchedulerService jobScheduler)
        {
            _reservationRepository = reservationRepository;
            _tableRepository = tableRepository;
            _logger = logger;
            _lookupResolver = lookupResolver;
            _realtimeNotification = realtimeNotification;
            _uow = uow;
            _jobScheduler = jobScheduler;
        }

        public async Task<(List<ReservationManagementDto> Items, int TotalCount)> GetReservationsAsync(GetReservationsRequest request, CancellationToken cancellationToken = default)
        {
            try { return await _reservationRepository.GetReservationsAsync(request, cancellationToken); }
            catch (Exception ex) { _logger.LogError(ex, "Error getting reservation list"); throw; }
        }

        public async Task<List<ReservationStatusDto>> GetReservationStatusesAsync(CancellationToken cancellationToken = default)
        {
            var entities = await _reservationRepository.GetReservationStatusesAsync(cancellationToken);
            return entities.Select(x => new ReservationStatusDto { StatusId = x.ValueId, StatusName = x.ValueName, StatusCode = x.ValueCode }).ToList();
        }

        public async Task<ReservationDetailDto> GetReservationDetailAsync(long reservationId, CancellationToken cancellationToken = default)
        {
            var reservation = await _reservationRepository.GetByIdWithFullDetailsAsync(reservationId, cancellationToken);
            if (reservation == null) throw new KeyNotFoundException($"Reservation with ID {reservationId} not found.");

            return new ReservationDetailDto
            {
                ReservationId = reservation.ReservationId,
                CustomerName = reservation.CustomerName,
                Phone = reservation.Phone,
                Email = reservation.Email,
                PartySize = reservation.PartySize,
                ReservedTime = reservation.ReservedTime,
                CreatedAt = reservation.CreatedAt,
                StatusId = reservation.ReservationStatusLvId,
                StatusName = reservation.ReservationStatusLv.ValueName,
                StatusCode = reservation.ReservationStatusLv.ValueCode,
                SourceId = reservation.SourceLvId,
                SourceName = reservation.SourceLv.ValueName,
                SourceCode = reservation.SourceLv.ValueCode,
                Tables = reservation.Tables.Select(t => new ReservationTableDto
                {
                    TableId = t.TableId,
                    TableCode = t.TableCode,
                    Capacity = t.Capacity,
                    TableType = t.TableTypeLv.ValueName,
                    Zone = t.ZoneLv.ValueName
                }).ToList()
            };
        }

        // 1.  (CONFIRMED)
        public async Task AssignTableAndConfirmAsync(long reservationId, List<long> tableIds, CancellationToken cancellationToken = default)
        {
            var reservation = await _reservationRepository.GetByIdWithFullDetailsAsync(reservationId, cancellationToken);
            if (reservation == null) throw new KeyNotFoundException("Không tìm thấy đơn đặt bàn");

            var confirmedStatusId = await ReservationStatusCode.CONFIRMED.ToReservationStatusIdAsync(_lookupResolver, cancellationToken);
            var reservedTableStatusId = await TableStatusCode.RESERVED.ToTableStatusIdAsync(_lookupResolver, cancellationToken);

            await _uow.BeginTransactionAsync(cancellationToken);
            try
            {
                reservation.ReservationStatusLvId = confirmedStatusId;

                foreach (var tableId in tableIds)
                {
                    if (!reservation.Tables.Any(t => t.TableId == tableId))
                    {
                        var table = await _tableRepository.GetByIdAsync(tableId, cancellationToken);
                        if (table != null) reservation.Tables.Add(table);
                    }
                }

                // [ KHÓA BÀN TRƯỚC 2 TIẾNG]
                var lockTime = reservation.ReservedTime.AddHours(-2);
                var timeUntilLock = lockTime - DateTime.UtcNow;

                if (timeUntilLock <= TimeSpan.Zero)
                {
                    // Nếu thời gian đến lúc ăn < 2 tiếng -> Khóa bàn (RESERVED) ngay lập tức
                    foreach (var tableId in tableIds)
                    {
                        await _tableRepository.UpdateStatusAsync(tableId, reservedTableStatusId, cancellationToken);
                        await _realtimeNotification.NotifyTableStatusChangedAsync(tableId, "RESERVED");
                    }
                }
                else
                {
                    // Nếu thời gian đến lúc ăn còn > 2 tiếng -> Lập lịch khóa bàn sau khoảng thời gian tính được
                    _jobScheduler.ScheduleTableLock(reservation.ReservationId, timeUntilLock);
                }

                await _uow.SaveChangesAsync(cancellationToken);

                // Lập lịch Hangfire đếm ngược 15p No-Show
                TimeSpan delay = reservation.ReservedTime.AddMinutes(15) > DateTime.UtcNow
                    ? reservation.ReservedTime.AddMinutes(15) - DateTime.UtcNow
                    : TimeSpan.FromMinutes(1);
                _jobScheduler.ScheduleNoShowCheck(reservation.ReservationId, delay);

                await _uow.CommitAsync(cancellationToken);

                await _realtimeNotification.NotifyReservationUpdatedAsync(reservationId, "CONFIRMED");
            }
            catch (Exception)
            {
                await _uow.RollbackAsync(cancellationToken);
                throw;
            }
        }

        // 2. CẬP NHẬT TRẠNG THÁI (Lúc Khách Đến hoặc Khách Hủy)
        public async Task UpdateReservationStatusAsync(long reservationId, string newStatusCode, string? note = null, CancellationToken cancellationToken = default)
        {
            var reservation = await _reservationRepository.GetByIdWithFullDetailsAsync(reservationId, cancellationToken);
            if (reservation == null) throw new KeyNotFoundException("Không tìm thấy đơn đặt bàn");

            var newStatusId = await _lookupResolver.GetIdAsync((ushort)LookupType.ReservationStatus, newStatusCode, cancellationToken);

            await _uow.BeginTransactionAsync(cancellationToken);
            try
            {
                reservation.ReservationStatusLvId = newStatusId;

                if (!string.IsNullOrWhiteSpace(note))
                {
                    reservation.Notes = string.IsNullOrWhiteSpace(reservation.Notes) ? note.Trim() : $"{reservation.Notes} | {note.Trim()}";
                }

                if (reservation.Tables != null && reservation.Tables.Any())
                {
                    uint targetTableStatusId = 0;
                    string targetTableStatusCode = "";

                    if (newStatusCode == ReservationStatusCode.CHECKED_IN.ToString())
                    {
                        var occupiedTableStatusId = await TableStatusCode.OCCUPIED.ToTableStatusIdAsync(_lookupResolver, cancellationToken);

                        foreach (var table in reservation.Tables)
                        {
                            var currentTable = await _tableRepository.GetByIdAsync(table.TableId, cancellationToken);
                            if (currentTable != null && currentTable.TableStatusLvId == occupiedTableStatusId)
                            {
                                throw new InvalidOperationException($"Bàn {currentTable.TableCode} đã được xếp cho khách khác. Vui lòng gỡ bàn cũ và xếp bàn mới cho đơn này!");
                            }
                        }

                        targetTableStatusId = occupiedTableStatusId;
                        targetTableStatusCode = TableStatusCode.OCCUPIED.ToString();
                    }
                    else if (newStatusCode == ReservationStatusCode.CANCELLED.ToString() ||
                             newStatusCode == ReservationStatusCode.COMPLETED.ToString() ||
                             newStatusCode == ReservationStatusCode.NO_SHOW.ToString())
                    {
                        targetTableStatusId = await TableStatusCode.AVAILABLE.ToTableStatusIdAsync(_lookupResolver, cancellationToken);
                        targetTableStatusCode = TableStatusCode.AVAILABLE.ToString();
                    }
                    else if (newStatusCode == ReservationStatusCode.CONFIRMED.ToString())
                    {
                        // [ KHÓA BÀN TRƯỚC 2 TIẾNG CHO ĐƠN ONLINE]
                        var lockTime = reservation.ReservedTime.AddHours(-2);
                        var timeUntilLock = lockTime - DateTime.UtcNow;

                        if (timeUntilLock <= TimeSpan.Zero)
                        {
                            targetTableStatusId = await TableStatusCode.RESERVED.ToTableStatusIdAsync(_lookupResolver, cancellationToken);
                            targetTableStatusCode = TableStatusCode.RESERVED.ToString();
                        }
                        else
                        {
                            _jobScheduler.ScheduleTableLock(reservationId, timeUntilLock);
                            targetTableStatusId = 0; 
                        }

                        // Lập lịch Hangfire đếm ngược 15p No-Show
                        TimeSpan delay = reservation.ReservedTime.AddMinutes(15) > DateTime.UtcNow
                            ? reservation.ReservedTime.AddMinutes(15) - DateTime.UtcNow
                            : TimeSpan.FromMinutes(1);
                        _jobScheduler.ScheduleNoShowCheck(reservation.ReservationId, delay);
                    }

                    if (targetTableStatusId != 0)
                    {
                        foreach (var table in reservation.Tables)
                        {
                            await _tableRepository.UpdateStatusAsync(table.TableId, targetTableStatusId, cancellationToken);
                            await _realtimeNotification.NotifyTableStatusChangedAsync(table.TableId, targetTableStatusCode);
                        }
                    }
                }

                await _uow.SaveChangesAsync(cancellationToken);
                await _uow.CommitAsync(cancellationToken);

                await _realtimeNotification.NotifyReservationUpdatedAsync(reservationId, newStatusCode);
            }
            catch (Exception)
            {
                await _uow.RollbackAsync(cancellationToken);
                throw;
            }
        }

        // 3. HANGFIRE JOB: ĐÁNH DẤU NO-SHOW (Sau 15 phút)
        [AutomaticRetry(Attempts = 2)]
        public async Task CheckAndMarkNoShowAsync(long reservationId)
        {
            try
            {
                var reservation = await _reservationRepository.GetByIdWithFullDetailsAsync(reservationId, CancellationToken.None);
                if (reservation == null || reservation.ReservationStatusLv.ValueCode != "CONFIRMED") return;

                var noShowStatusId = await ReservationStatusCode.NO_SHOW.ToReservationStatusIdAsync(_lookupResolver, CancellationToken.None);
                var availableTableStatusId = await TableStatusCode.AVAILABLE.ToTableStatusIdAsync(_lookupResolver, CancellationToken.None);

                await _uow.BeginTransactionAsync(CancellationToken.None);

                reservation.ReservationStatusLvId = noShowStatusId;

                if (reservation.Tables != null)
                {
                    foreach (var table in reservation.Tables)
                    {
                        await _tableRepository.UpdateStatusAsync(table.TableId, availableTableStatusId, CancellationToken.None);
                    }
                }

                await _uow.SaveChangesAsync(CancellationToken.None);
                await _uow.CommitAsync(CancellationToken.None);

                await _realtimeNotification.NotifyReservationUpdatedAsync(reservationId, "NO_SHOW");
                if (reservation.Tables != null)
                {
                    foreach (var table in reservation.Tables)
                    {
                        await _realtimeNotification.NotifyTableStatusChangedAsync(table.TableId, "AVAILABLE");
                    }
                }
            }
            catch (Exception ex)
            {
                await _uow.RollbackAsync(CancellationToken.None);
                _logger.LogError(ex, "Lỗi khi chạy Hangfire Job No-Show cho Reservation {Id}", reservationId);
                throw;
            }
        }

        // 4. HANGFIRE JOB: TỰ ĐỘNG KHÓA BÀN (Trước 2 tiếng)
        [AutomaticRetry(Attempts = 2)]
        public async Task LockTablesForReservationAsync(long reservationId)
        {
            try
            {
                var reservation = await _reservationRepository.GetByIdWithFullDetailsAsync(reservationId, CancellationToken.None);

                // Chỉ khóa khi đơn vẫn còn giữ trạng thái CONFIRMED và giờ hiện tại thực sự đã đến gần (< 2 tiếng 5 phút để bù trừ delay)
                if (reservation == null || reservation.ReservationStatusLv.ValueCode != "CONFIRMED") return;

                if (reservation.ReservedTime.AddHours(-2) > DateTime.UtcNow.AddMinutes(5)) return;

                var reservedTableStatusId = await TableStatusCode.RESERVED.ToTableStatusIdAsync(_lookupResolver, CancellationToken.None);
                var availableTableStatusId = await TableStatusCode.AVAILABLE.ToTableStatusIdAsync(_lookupResolver, CancellationToken.None);

                await _uow.BeginTransactionAsync(CancellationToken.None);

                if (reservation.Tables != null)
                {
                    foreach (var table in reservation.Tables)
                    {
                        // Lấy trạng thái hiện tại của bàn dưới DB
                        var currentTable = await _tableRepository.GetByIdAsync(table.TableId, CancellationToken.None);

                        // Tránh tình trạng nhét đè lên bàn đang OCCUPIED của khách Walk-in chưa ăn xong.
                        if (currentTable != null && currentTable.TableStatusLvId == availableTableStatusId)
                        {
                            await _tableRepository.UpdateStatusAsync(table.TableId, reservedTableStatusId, CancellationToken.None);
                            await _realtimeNotification.NotifyTableStatusChangedAsync(table.TableId, "RESERVED");
                        }
                    }
                }

                await _uow.SaveChangesAsync(CancellationToken.None);
                await _uow.CommitAsync(CancellationToken.None);
            }
            catch (Exception ex)
            {
                await _uow.RollbackAsync(CancellationToken.None);
                _logger.LogError(ex, "Lỗi khi chạy Hangfire Job Lock Tables cho Reservation {Id}", reservationId);
                throw;
            }
        }

        public async Task UpdateReservationAsync(long id, UpdateReservationRequest request, CancellationToken ct = default)
        {
            var reservation = await _reservationRepository.GetByIdAsync(id, ct);
            if (reservation == null) throw new KeyNotFoundException("Không tìm thấy đơn đặt bàn");

            await _uow.BeginTransactionAsync(ct);
            try
            {
                reservation.CustomerName = request.CustomerName;
                reservation.Phone = request.Phone;
                reservation.Email = request.Email;
                reservation.PartySize = request.PartySize;
                reservation.ReservedTime = request.ReservedTime;
                reservation.Notes = request.Notes;

                if (request.StatusId.HasValue && request.StatusId.Value != reservation.ReservationStatusLvId)
                {
                    reservation.ReservationStatusLvId = request.StatusId.Value;
                }

                if (request.TableIds != null)
                {
                    var currentTableIds = reservation.Tables.Select(t => t.TableId).ToList();
                    var newTableIds = request.TableIds;

                    var tablesToRemove = reservation.Tables.Where(t => !newTableIds.Contains(t.TableId)).ToList();
                    var tableIdsToAdd = newTableIds.Where(id => !currentTableIds.Contains(id)).ToList();

                    var availableStatusId = await TableStatusCode.AVAILABLE.ToTableStatusIdAsync(_lookupResolver, ct);

                    foreach (var table in tablesToRemove)
                    {
                        reservation.Tables.Remove(table);
                        await _tableRepository.UpdateStatusAsync(table.TableId, availableStatusId, ct);
                        await _realtimeNotification.NotifyTableStatusChangedAsync(table.TableId, "AVAILABLE");
                    }

                    if (tableIdsToAdd.Any())
                    {
                        var reservationStatusCode = (await _reservationRepository.GetReservationStatusesAsync(ct))
                            .FirstOrDefault(s => s.ValueId == reservation.ReservationStatusLvId)?.ValueCode;

                        TableStatusCode targetTableStatus = TableStatusCode.RESERVED;
                        if (reservationStatusCode == ReservationStatusCode.CHECKED_IN.ToString())
                            targetTableStatus = TableStatusCode.OCCUPIED;

                        var targetTableStatusId = await targetTableStatus.ToTableStatusIdAsync(_lookupResolver, ct);

                        foreach (var tableId in tableIdsToAdd)
                        {
                            var table = await _tableRepository.GetByIdAsync(tableId, ct);
                            if (table != null)
                            {
                                reservation.Tables.Add(table);

                                // Nếu thời gian còn > 2 tiếng thì chưa khóa bàn 
                                if (targetTableStatus == TableStatusCode.RESERVED && reservation.ReservedTime.AddHours(-2) > DateTime.UtcNow)
                                {
                                }
                                else
                                {
                                    await _tableRepository.UpdateStatusAsync(tableId, targetTableStatusId, ct);
                                    await _realtimeNotification.NotifyTableStatusChangedAsync(tableId, targetTableStatus.ToString());
                                }
                            }
                        }
                    }
                }

                await _reservationRepository.UpdateAsync(reservation, ct);
                await _uow.CommitAsync(ct);

                var status = (await _reservationRepository.GetReservationStatusesAsync(ct)).FirstOrDefault(s => s.ValueId == reservation.ReservationStatusLvId);
                if (status != null) await _realtimeNotification.NotifyReservationUpdatedAsync(id, status.ValueCode);
            }
            catch (Exception) { await _uow.RollbackAsync(ct); throw; }
        }

        public async Task DeleteReservationAsync(long id, CancellationToken ct = default)
        {
            var reservation = await _reservationRepository.GetByIdAsync(id, ct);
            if (reservation == null) throw new KeyNotFoundException("Không tìm thấy đơn đặt bàn");

            await _uow.BeginTransactionAsync(ct);
            try
            {
                if (reservation.Tables != null && reservation.Tables.Any())
                {
                    var availableTableStatusId = await TableStatusCode.AVAILABLE.ToTableStatusIdAsync(_lookupResolver, ct);
                    foreach (var table in reservation.Tables)
                    {
                        await _tableRepository.UpdateStatusAsync(table.TableId, availableTableStatusId, ct);
                        await _realtimeNotification.NotifyTableStatusChangedAsync(table.TableId, TableStatusCode.AVAILABLE.ToString());
                    }
                }

                await _reservationRepository.DeleteAsync(reservation, ct);
                await _uow.CommitAsync(ct);
            }
            catch (Exception) { await _uow.RollbackAsync(ct); throw; }
        }
    }
}
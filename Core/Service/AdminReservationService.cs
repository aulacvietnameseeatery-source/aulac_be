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

        public async Task<(List<ReservationManagementDto> Items, int TotalCount)> GetReservationsAsync(
            GetReservationsRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                return await _reservationRepository.GetReservationsAsync(request, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting reservation list");
                throw;
            }
        }

        public async Task<List<ReservationStatusDto>> GetReservationStatusesAsync(CancellationToken cancellationToken = default)
        {
            var entities = await _reservationRepository.GetReservationStatusesAsync(cancellationToken);

            return entities.Select(x => new ReservationStatusDto
            {
                StatusId = x.ValueId,
                StatusName = x.ValueName,
                StatusCode = x.ValueCode
            }).ToList();
        }

        public async Task<ReservationDetailDto> GetReservationDetailAsync(long reservationId, CancellationToken cancellationToken = default)
        {
            var reservation = await _reservationRepository.GetByIdWithFullDetailsAsync(reservationId, cancellationToken);

            if (reservation == null)
            {
                throw new KeyNotFoundException($"Reservation with ID {reservationId} not found.");
            }

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

        // 1. DUYỆT ĐƠN & GÁN NHIỀU BÀN (CONFIRMED)
        public async Task AssignTableAndConfirmAsync(long reservationId, List<long> tableIds, CancellationToken cancellationToken = default)
        {
            var reservation = await _reservationRepository.GetByIdWithFullDetailsAsync(reservationId, cancellationToken);
            if (reservation == null) throw new KeyNotFoundException("Không tìm thấy đơn đặt bàn");

            var confirmedStatusId = await ReservationStatusCode.CONFIRMED.ToReservationStatusIdAsync(_lookupResolver, cancellationToken);
            var reservedTableStatusId = await TableStatusCode.RESERVED.ToTableStatusIdAsync(_lookupResolver, cancellationToken);

            await _uow.BeginTransactionAsync(cancellationToken);
            try
            {
                // 1. Đổi Status Đơn
                reservation.ReservationStatusLvId = confirmedStatusId;

                // 2. Gán các Bàn vào Đơn và đổi Status Bàn -> RESERVED
                foreach (var tableId in tableIds)
                {
                    if (!reservation.Tables.Any(t => t.TableId == tableId))
                    {
                        var table = await _tableRepository.GetByIdAsync(tableId, cancellationToken);
                        if (table != null) reservation.Tables.Add(table);
                    }
                    await _tableRepository.UpdateStatusAsync(tableId, reservedTableStatusId, cancellationToken);
                }

                // Không dùng _reservationRepository.UpdateAsync để tránh lỗi Tracking DB
                await _uow.SaveChangesAsync(cancellationToken);

                // 3. Kích hoạt Hangfire đếm ngược 15p No-Show
                TimeSpan delay = reservation.ReservedTime.AddMinutes(15) > DateTime.UtcNow
                    ? reservation.ReservedTime.AddMinutes(15) - DateTime.UtcNow
                    : TimeSpan.FromMinutes(1);
                _jobScheduler.ScheduleNoShowCheck(reservation.ReservationId, delay);

                await _uow.CommitAsync(cancellationToken);

                // 4. Bắn SignalR
                await _realtimeNotification.NotifyReservationUpdatedAsync(reservationId, "CONFIRMED");
                foreach (var tableId in tableIds)
                {
                    await _realtimeNotification.NotifyTableStatusChangedAsync(tableId, "RESERVED");
                }
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
                // 1. Đổi Status Đơn
                reservation.ReservationStatusLvId = newStatusId;

                // 2. Xử lý Ghi chú
                if (!string.IsNullOrWhiteSpace(note))
                {
                    reservation.Notes = string.IsNullOrWhiteSpace(reservation.Notes)
                        ? note.Trim()
                        : $"{reservation.Notes} | {note.Trim()}";
                }

                // 3. Xử lý Bàn theo Trạng thái (Quan trọng)
                if (reservation.Tables != null && reservation.Tables.Any())
                {
                    uint targetTableStatusId = 0;
                    string targetTableStatusCode = "";

                    if (newStatusCode == ReservationStatusCode.CHECKED_IN.ToString())
                    {
                        targetTableStatusId = await TableStatusCode.OCCUPIED.ToTableStatusIdAsync(_lookupResolver, cancellationToken);
                        targetTableStatusCode = TableStatusCode.OCCUPIED.ToString();
                    }
                    else if (newStatusCode == ReservationStatusCode.CANCELLED.ToString() || newStatusCode == ReservationStatusCode.COMPLETED.ToString())
                    {
                        targetTableStatusId = await TableStatusCode.AVAILABLE.ToTableStatusIdAsync(_lookupResolver, cancellationToken);
                        targetTableStatusCode = TableStatusCode.AVAILABLE.ToString();
                    }

                    // Đổi trạng thái tất cả các bàn đang được gán cho đơn này
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

                // 4. Bắn SignalR
                await _realtimeNotification.NotifyReservationUpdatedAsync(reservationId, newStatusCode);
            }
            catch (Exception)
            {
                await _uow.RollbackAsync(cancellationToken);
                throw;
            }
        }

        // 3. HANGFIRE - ĐÁNH DẤU NO-SHOW (Khách bùn)
        public async Task CheckAndMarkNoShowAsync(long reservationId)
        {
            try
            {
                var reservation = await _reservationRepository.GetByIdWithFullDetailsAsync(reservationId, CancellationToken.None);
                if (reservation == null) return;

                if (reservation.ReservationStatusLv.ValueCode != "CONFIRMED") return;

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

                // --- Xử lý cập nhật Bàn ---
                if (request.TableIds != null)
                {
                    var currentTableIds = reservation.Tables.Select(t => t.TableId).ToList();
                    var newTableIds = request.TableIds;

                    var tablesToRemove = reservation.Tables.Where(t => !newTableIds.Contains(t.TableId)).ToList();
                    var tableIdsToAdd = newTableIds.Where(id => !currentTableIds.Contains(id)).ToList();

                    var availableStatusId = await TableStatusCode.AVAILABLE.ToTableStatusIdAsync(_lookupResolver, ct);
                    
                    // 1. Giải phóng các bàn bị gỡ bỏ
                    foreach (var table in tablesToRemove)
                    {
                        reservation.Tables.Remove(table);
                        await _tableRepository.UpdateStatusAsync(table.TableId, availableStatusId, ct);
                        await _realtimeNotification.NotifyTableStatusChangedAsync(table.TableId, "AVAILABLE");
                    }

                    // 2. Thêm các bàn mới
                    if (tableIdsToAdd.Any())
                    {
                        var reservationStatusCode = (await _reservationRepository.GetReservationStatusesAsync(ct))
                            .FirstOrDefault(s => s.ValueId == reservation.ReservationStatusLvId)?.ValueCode;

                        // Xác định status bàn mới dựa trên status đơn
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
                                await _tableRepository.UpdateStatusAsync(tableId, targetTableStatusId, ct);
                                await _realtimeNotification.NotifyTableStatusChangedAsync(tableId, targetTableStatus.ToString());
                            }
                        }
                    }
                }

                await _reservationRepository.UpdateAsync(reservation, ct);
                await _uow.CommitAsync(ct);
                
                // Bắn thông báo cập nhật đơn
                var status = (await _reservationRepository.GetReservationStatusesAsync(ct))
                    .FirstOrDefault(s => s.ValueId == reservation.ReservationStatusLvId);
                if (status != null)
                {
                    await _realtimeNotification.NotifyReservationUpdatedAsync(id, status.ValueCode);
                }
            }
            catch (Exception)
            {
                await _uow.RollbackAsync(ct);
                throw;
            }
        }

        public async Task DeleteReservationAsync(long id, CancellationToken ct = default)
        {
            var reservation = await _reservationRepository.GetByIdAsync(id, ct);
            if (reservation == null) throw new KeyNotFoundException("Không tìm thấy đơn đặt bàn");

            await _uow.BeginTransactionAsync(ct);
            try
            {
                // Nếu Reservation đang gán bàn, cần giải phóng bàn trước
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
            catch (Exception)
            {
                await _uow.RollbackAsync(ct);
                throw;
            }
        }
    }
}
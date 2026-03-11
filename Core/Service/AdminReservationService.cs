using Core.DTO.Reservation;
using Core.Interface.Repo;
using Core.Interface.Service.Entity;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Service
{
    public class AdminReservationService : IAdminReservationService
    {
        private readonly IReservationRepository _reservationRepository;
        private readonly ILogger<AdminReservationService> _logger;

        public AdminReservationService(IReservationRepository reservationRepository, ILogger<AdminReservationService> logger)
        {
            _reservationRepository = reservationRepository;
            _logger = logger;
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
            // Lấy dữ liệu thô từ DB (LookupValues: ID 21, 22...)
            var entities = await _reservationRepository.GetReservationStatusesAsync(cancellationToken);

            // Map sang DTO để trả về Frontend
            return entities.Select(x => new ReservationStatusDto
            {
                StatusId = x.ValueId,       // 21, 22...
                StatusName = x.ValueName,   // "Pending", "Confirmed"...
                StatusCode = x.ValueCode    // "PENDING"...
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
    }
}

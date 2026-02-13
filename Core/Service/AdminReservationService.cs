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
    }
}

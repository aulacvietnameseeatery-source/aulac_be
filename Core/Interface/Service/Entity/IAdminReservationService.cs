using Core.DTO.Reservation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Interface.Service.Entity
{
    public interface IAdminReservationService
    {
        // Lấy danh sách đặt bàn (View List)
        Task<(List<ReservationManagementDto> Items, int TotalCount)> GetReservationsAsync(
            GetReservationsRequest request,
            CancellationToken cancellationToken = default);

        // Lấy danh sách Status từ DB để đổ vào Tabs trên Frontend
        Task<List<ReservationStatusDto>> GetReservationStatusesAsync(CancellationToken cancellationToken = default);
    }
}

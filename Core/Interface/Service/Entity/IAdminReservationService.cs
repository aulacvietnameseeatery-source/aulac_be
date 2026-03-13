using Core.DTO.Reservation;
using System;
using System.Collections.Generic;
using System.Threading;
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

        // Lấy chi tiết một đặt bàn theo ID
        Task<ReservationDetailDto> GetReservationDetailAsync(long reservationId, CancellationToken cancellationToken = default);

        // HANGFIRE: Kiểm tra và đánh dấu No-Show nếu khách không đến
        Task CheckAndMarkNoShowAsync(long reservationId);

        // ĐỔI TRẠNG THÁI (Dùng cho Checked_In, Cancelled...)
        Task UpdateReservationStatusAsync(long reservationId, string newStatusCode, string? note = null, CancellationToken cancellationToken = default);

        // DUYỆT ĐƠN VÀ GHÉP BÀN (Truyền vào 1 mảng ID bàn)
        Task AssignTableAndConfirmAsync(long reservationId, List<long> tableIds, CancellationToken cancellationToken = default);
    }
}
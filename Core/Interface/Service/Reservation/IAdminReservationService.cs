using Core.DTO.Reservation;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Interface.Service.Reservation
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

        // MANUAL FLOW: Lấy danh sách table options theo workflow availability
        Task<List<ManualTableOptionDto>> GetManualAvailableTablesAsync(
            DateTime? reservedTime,
            int? partySize,
            CancellationToken ct = default);

        // MANUAL FLOW: Tạo reservation trực tiếp từ back-office
        Task<ReservationResponseDto> CreateManualReservationAsync(
            CreateManualReservationRequest request,
            CancellationToken ct = default);

        // HANGFIRE: Kiểm tra và đánh dấu No-Show nếu khách không đến
        Task CheckAndMarkNoShowAsync(long reservationId);

        // ĐỔI TRẠNG THÁI (Dùng cho Checked_In, Cancelled...)
        Task UpdateReservationStatusAsync(long reservationId, string newStatusCode, string? note = null, CancellationToken cancellationToken = default);

        // MANUAL FLOW: Cập nhật status có context nhân viên (phục vụ check-in tạo order)
        Task<ReservationStatusResponseDTO> UpdateReservationStatusAsync(
            long reservationId,
            long staffId,
            UpdateReservationStatusRequest request,
            CancellationToken ct);

        // DUYỆT ĐƠN VÀ GHÉP BÀN (Truyền vào 1 mảng ID bàn)
        Task AssignTableAndConfirmAsync(long reservationId, List<long> tableIds, CancellationToken cancellationToken = default);

        // CẬP NHẬT THÔNG TIN ĐƠN (Sửa tên, sđt, party size...)
        Task UpdateReservationAsync(long id, UpdateReservationRequest request, CancellationToken ct = default);

        // XÓA ĐƠN
        Task DeleteReservationAsync(long id, CancellationToken ct = default);

        Task LockTablesForReservationAsync(long reservationId);

    }
}

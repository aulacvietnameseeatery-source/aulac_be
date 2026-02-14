using API.Models;
using Core.DTO.General; // Chứa ApiResponse, PagedResult
using Core.DTO.Reservation;
using Core.Interface.Service.Entity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Route("api/reservations")]
    [ApiController]
    public class ReservationController : ControllerBase
    {
        private readonly IAdminReservationService _reservationService;

        public ReservationController(IAdminReservationService reservationService)
        {
            _reservationService = reservationService;
        }

        // --- 1. GET LIST RESERVATIONS ---
        // URL: GET /api/reservations?pageIndex=1&pageSize=10&date=2024-02-15&statusId=21
        [HttpGet]
        // [HasPermission(Permissions.ViewReservation)] // Bỏ comment khi tích hợp Auth
        [ProducesResponseType(typeof(ApiResponse<PagedResult<ReservationManagementDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetReservations(
            [FromQuery] GetReservationsRequest request,
            CancellationToken cancellationToken)
        {
            // 1. Lấy dữ liệu từ Service
            var (items, totalCount) = await _reservationService.GetReservationsAsync(request, cancellationToken);

            // 2. Tính toán phân trang
            var totalPage = request.PageSize > 0
                ? (int)Math.Ceiling((double)totalCount / request.PageSize)
                : 0;

            // 3. Đóng gói kết quả
            var pagedResult = new PagedResult<ReservationManagementDto>
            {
                PageData = items,
                PageIndex = request.PageIndex,
                PageSize = request.PageSize,
                TotalCount = totalCount,
                TotalPage = totalPage
            };

            return Ok(new ApiResponse<PagedResult<ReservationManagementDto>>
            {
                Success = true,
                Code = 200,
                UserMessage = "Reservation list retrieved successfully.",
                Data = pagedResult,
                ServerTime = DateTimeOffset.UtcNow
            });
        }

        // --- 2. GET STATUSES FOR TABS ---
        // URL: GET /api/reservations/statuses
        [HttpGet("statuses")]
        [AllowAnonymous] // Cho phép gọi không cần token để render UI (hoặc cần Auth tùy nghiệp vụ)
        [ProducesResponseType(typeof(ApiResponse<List<ReservationStatusDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetStatuses(CancellationToken cancellationToken)
        {
            var data = await _reservationService.GetReservationStatusesAsync(cancellationToken);

            return Ok(new ApiResponse<List<ReservationStatusDto>>
            {
                Success = true,
                Code = 200,
                Data = data,
                ServerTime = DateTimeOffset.UtcNow
            });
        }
    }
}
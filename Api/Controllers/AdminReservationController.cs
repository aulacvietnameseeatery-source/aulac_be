using API.Models;
using Core.Attribute;
using Core.Data;
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

        // --- 3. GET RESERVATION DETAIL ---
        // URL: GET /api/reservations/{id}
        [HttpGet("{id:long}")]
        [HasPermission(Permissions.ViewReservation)]
        [ProducesResponseType(typeof(ApiResponse<ReservationDetailDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetReservationDetail(long id, CancellationToken cancellationToken)
        {
            try
            {
                var data = await _reservationService.GetReservationDetailAsync(id, cancellationToken);

                return Ok(new ApiResponse<ReservationDetailDto>
                {
                    Success = true,
                    Code = 200,
                    UserMessage = "Reservation detail retrieved successfully.",
                    Data = data,
                    ServerTime = DateTimeOffset.UtcNow
                });
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Code = 404,
                    UserMessage = $"Reservation with ID {id} not found.",
                    Data = null!,
                    ServerTime = DateTimeOffset.UtcNow
                });
            }
        }

        // URL: PATCH /api/reservations/{id}/status
        [HttpPatch("{id:long}/status")]
        // [HasPermission(Permissions.EditReservation)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> UpdateReservationStatus(
            long id,
            [FromBody] UpdateReservationStatusRequest payload,
            CancellationToken cancellationToken)
        {
            try
            {
                string statusCodeStr = payload.Status.ToString();
                await _reservationService.UpdateReservationStatusAsync(id, statusCodeStr, payload.Notes, cancellationToken);

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Code = 200,
                    UserMessage = "Cập nhật trạng thái đặt bàn thành công.",
                    Data = null!,
                    ServerTime = DateTimeOffset.UtcNow
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Code = 404,
                    UserMessage = ex.Message,
                    Data = null!,
                    ServerTime = DateTimeOffset.UtcNow
                });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<object>
                {
                    Success = false,
                    Code = 500,
                    UserMessage = "Đã xảy ra lỗi hệ thống khi cập nhật trạng thái.",
                    SystemMessage = ex.Message,
                    Data = null!,
                    ServerTime = DateTimeOffset.UtcNow
                });
            }
        }


        // --- 5. ASSIGN TABLE & CONFIRM RESERVATION ---
        // URL: PATCH /api/reservations/{id}/assign-and-confirm
        [HttpPatch("{id:long}/assign-and-confirm")]
        // [HasPermission(Permissions.EditReservation)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> AssignTableAndConfirm(
            long id,
            [FromBody] AssignTableRequest? payload,
            CancellationToken cancellationToken)
        {
            try
            {
                var selectedTableIds = payload?.TableIds ?? new List<long>();
                await _reservationService.AssignTableAndConfirmAsync(id, selectedTableIds, cancellationToken);

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Code = 200,
                    UserMessage = "Duyệt đơn thành công.",
                    Data = null!,
                    ServerTime = DateTimeOffset.UtcNow
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Code = 404,
                    UserMessage = ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<object>
                {
                    Success = false,
                    Code = 500,
                    UserMessage = "Đã xảy ra lỗi hệ thống khi xếp bàn.",
                    SystemMessage = ex.Message
                });
            }
        }

        // --- 6. UPDATE RESERVATION ---
        // URL: PUT /api/reservations/{id}
        [HttpPut("{id:long}")]
        // [HasPermission(Permissions.EditReservation)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> UpdateReservation(
            long id,
            [FromBody] UpdateReservationRequest payload,
            CancellationToken cancellationToken)
        {
            try
            {
                await _reservationService.UpdateReservationAsync(id, payload, cancellationToken);

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Code = 200,
                    UserMessage = "Cập nhật thông tin đặt bàn thành công.",
                    Data = null!,
                    ServerTime = DateTimeOffset.UtcNow
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Code = 404,
                    UserMessage = ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<object>
                {
                    Success = false,
                    Code = 500,
                    UserMessage = "Đã xảy ra lỗi hệ thống khi cập nhật thông tin đặt bàn.",
                    SystemMessage = ex.Message
                });
            }
        }

        // --- 7. DELETE RESERVATION ---
        // URL: DELETE /api/reservations/{id}
        [HttpDelete("{id:long}")]
        // [HasPermission(Permissions.DeleteReservation)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> DeleteReservation(long id, CancellationToken cancellationToken)
        {
            try
            {
                await _reservationService.DeleteReservationAsync(id, cancellationToken);

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Code = 200,
                    UserMessage = "Xóa đơn đặt bàn thành công.",
                    Data = null!,
                    ServerTime = DateTimeOffset.UtcNow
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Code = 404,
                    UserMessage = ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<object>
                {
                    Success = false,
                    Code = 500,
                    UserMessage = "Đã xảy ra lỗi hệ thống khi xóa đơn đặt bàn.",
                    SystemMessage = ex.Message
                });
            }
        }
    }
}

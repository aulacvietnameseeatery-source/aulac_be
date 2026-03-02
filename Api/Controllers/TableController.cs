using API.Models;
using Core.DTO.Table;
using Core.Interface.Service.Entity;
using Core.Interface.Service.Table;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    [Route("api/tables")]
    public class TableController : ControllerBase
    {
        private readonly ITableService _tableService;

        public TableController(ITableService tableService)
        {
            _tableService = tableService;
        }

        [HttpGet]
        public async Task<IActionResult> GetTablesManagement([FromQuery] GetTableManagementRequest request, CancellationToken ct)
        {
            var (items, totalCount) = await _tableService.GetTablesForManagementAsync(request, ct);

            var pagedResult = new PagedResult<TableManagementDto>
            {
                PageData = items,
                PageIndex = request.PageIndex > 0 ? request.PageIndex : 1,
                PageSize = request.PageSize > 0 ? request.PageSize : 30,
                TotalCount = totalCount,
                TotalPage = request.PageSize > 0 ? (int)Math.Ceiling((double)totalCount / (request.PageSize > 0 ? request.PageSize : 50)) : 1
            };

            return Ok(new ApiResponse<PagedResult<TableManagementDto>>
            {
                Success = true,
                Code = 200,
                UserMessage = "Lấy danh sách bàn thành công.",
                Data = pagedResult,
                ServerTime = DateTimeOffset.UtcNow
            });
        }

        [HttpGet("select")]
        public async Task<IActionResult> GetTablesForSelect(CancellationToken ct)
        {
            var result = await _tableService.GetTablesForSelectAsync(ct);

            return Ok(new ApiResponse<List<TableSelectDto>>
            {
                Success = true,
                Code = 200,
                UserMessage = $"Get All Table For Create Order",
                Data = result,
                ServerTime = DateTimeOffset.UtcNow
            });
        }
    }
}
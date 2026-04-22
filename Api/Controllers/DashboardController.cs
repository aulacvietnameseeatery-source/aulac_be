using Core.DTO.Dashboard;
using Core.Interface.Service;
using Core.Interface.Service.Others;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading;
using System.Threading.Tasks;

namespace Api.Controllers 
{
    [Route("api/dashboard")]
    [ApiController]
    [Authorize] 
    public class DashboardController : ControllerBase
    {
        private readonly IDashboardService _dashboardService;

        public DashboardController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        // 1. Summary
        [HttpGet("summary")]
        public async Task<IActionResult> GetSummary([FromQuery] DashboardFilterRequest request, CancellationToken ct)
        {
            var result = await _dashboardService.GetSummaryAsync(request, ct);
            return Ok(new { success = true, code = 200, data = result });
        }

        // 2. Revenue Chart
        [HttpGet("revenue-chart")]
        public async Task<IActionResult> GetRevenueChart([FromQuery] DashboardFilterRequest request, CancellationToken ct)
        {
            var result = await _dashboardService.GetRevenueChartAsync(request, ct);
            return Ok(new { success = true, code = 200, data = result });
        }

        // 3. Top Selling
        [HttpGet("top-selling")]
        public async Task<IActionResult> GetTopSelling([FromQuery] DashboardFilterRequest request, [FromQuery] int limit = 6, CancellationToken ct = default)
        {
            var result = await _dashboardService.GetTopSellingAsync(request, limit, ct);
            return Ok(new { success = true, code = 200, data = result });
        }

        // 4. Statistics
        [HttpGet("statistics")]
        public async Task<IActionResult> GetStatistics([FromQuery] DashboardFilterRequest request, CancellationToken ct)
        {
            var result = await _dashboardService.GetStatisticsAsync(request, ct);
            return Ok(new { success = true, code = 200, data = result });
        }

        [HttpGet("live-operations")]
        public async Task<IActionResult> GetLiveOperations([FromQuery] LiveOperationsSnapshotRequest request, CancellationToken ct)
        {
            var result = await _dashboardService.GetLiveOperationsSnapshotAsync(request, ct);
            return Ok(new { success = true, code = 200, data = result });
        }
    }
}
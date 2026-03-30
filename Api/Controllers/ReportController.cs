using API.Models; 
using Core.Attribute; 
using Core.Data; 
using Core.DTO.Common; 
using Core.DTO.Report;
using Core.Interface.Service.Report;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Api.Controllers;

/// <summary>
/// Report controller providing endpoints for generating business reports
/// </summary>
[ApiController]
[Route("api/reports")]
[Authorize] 
public class ReportController : ControllerBase
{
    private readonly IReportService _reportService;
    private readonly ILogger<ReportController> _logger;

    public ReportController(
        IReportService reportService,
        ILogger<ReportController> logger)
    {
        _reportService = reportService;
        _logger = logger;
    }

    // TAB 1: EARNINGS (TÀI CHÍNH)

    /// <summary>
    /// Gets the overall earning summary (gross, net, tax, discounts).
    /// </summary>
    /// <param name="request">Filter containing StartDate and EndDate.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Earning summary details.</returns>
    /// <response code="200">Summary retrieved successfully.</response>
    [HttpGet("earnings/summary")]
    // [HasPermission(Permissions.ViewReport)] // Uncomment khi hệ thống Auth đã sẵn sàng
    [ProducesResponseType(typeof(ApiResponse<EarningSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEarningSummary(
        [FromQuery] ReportFilterRequest request,
        CancellationToken ct)
    {
        var result = await _reportService.GetEarningSummaryAsync(request, ct);

        return Ok(new ApiResponse<EarningSummaryDto>
        {
            Success = true,
            Code = 200,
            UserMessage = "Earning summary retrieved successfully.",
            Data = result,
            ServerTime = DateTimeOffset.UtcNow
        });
    }

    /// <summary>
    /// Gets revenue breakdown by payment methods (Cash, Card, etc.).
    /// </summary>
    /// <param name="request">Filter containing StartDate and EndDate.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of revenue per payment method.</returns>
    /// <response code="200">Data retrieved successfully.</response>
    [HttpGet("earnings/payment-methods")]
    // [HasPermission(Permissions.ViewReport)]
    [ProducesResponseType(typeof(ApiResponse<List<PaymentMethodRevenueDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPaymentMethods(
        [FromQuery] ReportFilterRequest request,
        CancellationToken ct)
    {
        var result = await _reportService.GetPaymentMethodChartAsync(request, ct);

        return Ok(new ApiResponse<List<PaymentMethodRevenueDto>>
        {
            Success = true,
            Code = 200,
            UserMessage = "Payment methods breakdown retrieved successfully.",
            Data = result,
            ServerTime = DateTimeOffset.UtcNow
        });
    }

    /// <summary>
    /// Gets detailed earning list grouped by date with pagination.
    /// </summary>
    /// <param name="request">Filter containing StartDate and EndDate.</param>
    /// <param name="pageIndex">The page number (starts from 1).</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Paginated earning list.</returns>
    /// <response code="200">List retrieved successfully.</response>
    [HttpGet("earnings/table")]
    // [HasPermission(Permissions.ViewReport)]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<EarningTableItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEarningTable(
        [FromQuery] ReportFilterRequest request,
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var (items, totalCount) = await _reportService.GetEarningTableAsync(request, pageIndex, pageSize, ct);

        var pagedResult = new PagedResult<EarningTableItemDto>
        {
            PageData = items,
            PageIndex = pageIndex,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPage = pageSize > 0 ? (int)Math.Ceiling((double)totalCount / pageSize) : 0
        };

        return Ok(new ApiResponse<PagedResult<EarningTableItemDto>>
        {
            Success = true,
            Code = 200,
            UserMessage = "Earning table retrieved successfully.",
            Data = pagedResult,
            ServerTime = DateTimeOffset.UtcNow
        });
    }

    // TAB 2: ORDERS 

    /// <summary>
    /// Gets the overall order metrics (total, completed, cancelled, cancellation rate).
    /// </summary>
    /// <param name="request">Filter containing StartDate and EndDate.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Order metrics details.</returns>
    /// <response code="200">Metrics retrieved successfully.</response>
    [HttpGet("orders/metrics")]
    // [HasPermission(Permissions.ViewReport)]
    [ProducesResponseType(typeof(ApiResponse<OrderMetricsDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOrderMetrics(
        [FromQuery] ReportFilterRequest request,
        CancellationToken ct)
    {
        var result = await _reportService.GetOrderMetricsAsync(request, ct);

        return Ok(new ApiResponse<OrderMetricsDto>
        {
            Success = true,
            Code = 200,
            UserMessage = "Order metrics retrieved successfully.",
            Data = result,
            ServerTime = DateTimeOffset.UtcNow
        });
    }

    // TAB 3: SALES 

    /// <summary>
    /// Gets sales performance details for individual dishes with pagination.
    /// </summary>
    /// <param name="request">Filter containing StartDate and EndDate.</param>
    /// <param name="pageIndex">The page number (starts from 1).</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Paginated sales item list.</returns>
    /// <response code="200">Sales items retrieved successfully.</response>
    [HttpGet("sales/items")]
    // [HasPermission(Permissions.ViewReport)]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<SalesItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSalesItems(
        [FromQuery] ReportFilterRequest request,
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var (items, totalCount) = await _reportService.GetSalesItemsAsync(request, pageIndex, pageSize, ct);

        var pagedResult = new PagedResult<SalesItemDto>
        {
            PageData = items,
            PageIndex = pageIndex,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPage = pageSize > 0 ? (int)Math.Ceiling((double)totalCount / pageSize) : 0
        };

        return Ok(new ApiResponse<PagedResult<SalesItemDto>>
        {
            Success = true,
            Code = 200,
            UserMessage = "Sales items retrieved successfully.",
            Data = pagedResult,
            ServerTime = DateTimeOffset.UtcNow
        });
    }

    // TAB 4: CUSTOMERS

    /// <summary>
    /// Gets the list of top-spending customers with pagination.
    /// </summary>
    /// <param name="request">Filter containing StartDate and EndDate.</param>
    /// <param name="pageIndex">The page number (starts from 1).</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Paginated list of top spenders.</returns>
    /// <response code="200">List retrieved successfully.</response>
    [HttpGet("customers/top-spenders")]
    // [HasPermission(Permissions.ViewReport)]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<TopCustomerDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTopSpenders(
        [FromQuery] ReportFilterRequest request,
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var (items, totalCount) = await _reportService.GetTopSpendersAsync(request, pageIndex, pageSize, ct);

        var pagedResult = new PagedResult<TopCustomerDto>
        {
            PageData = items,
            PageIndex = pageIndex,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPage = pageSize > 0 ? (int)Math.Ceiling((double)totalCount / pageSize) : 0
        };

        return Ok(new ApiResponse<PagedResult<TopCustomerDto>>
        {
            Success = true,
            Code = 200,
            UserMessage = "Top spending customers retrieved successfully.",
            Data = pagedResult,
            ServerTime = DateTimeOffset.UtcNow
        });
    }
}
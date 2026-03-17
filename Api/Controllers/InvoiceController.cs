using Core.Attribute;
using Core.Data;
using Core.DTO.General;
using Core.DTO.Order;
using Core.Enum;
using Core.Interface.Service.Entity;
using API.Models;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    [Route("api/invoices")]
    [ApiController]
    public class InvoiceController : ControllerBase
    {
        private readonly ISaleInvoiceService _saleInvoiceService;

        public InvoiceController(ISaleInvoiceService saleInvoiceService)
        {
            _saleInvoiceService = saleInvoiceService;
        }

        /// <summary>
        /// Gets paginated sale invoice list with optional filters.
        /// </summary>
        /// <param name="query">Pagination and filter parameters</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Paginated list of sale invoices</returns>
        /// <response code="200">Invoices retrieved successfully</response>
        [HttpGet]
        [HasPermission(Permissions.ViewOrder)]
        [ProducesResponseType(typeof(ApiResponse<PagedResultDTO<SaleInvoiceListDTO>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetSaleInvoiceList(
            [FromQuery] SaleInvoiceListQueryDTO query,
            CancellationToken cancellationToken = default)
        {
            var result = await _saleInvoiceService.GetSaleInvoiceListAsync(query, cancellationToken);

            return Ok(new ApiResponse<PagedResultDTO<SaleInvoiceListDTO>>
            {
                Success = true,
                Code = 200,
                SubCode = 0,
                UserMessage = "Get sale invoice list successfully",
                Data = result,
                ServerTime = DateTimeOffset.Now
            });
        }

        /// <summary>
        /// Gets detailed information for a sale invoice (using order id).
        /// </summary>
        /// <param name="id">Order ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Sale Invoice View Model</returns>
        /// <response code="200">Invoice retrieved successfully</response>
        /// <response code="404">Invoice not found</response>
        [HttpGet("{id:long}")]
        [HasPermission(Permissions.ViewOrder)]
        [ProducesResponseType(typeof(ApiResponse<SaleInvoiceDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetSaleInvoiceDetail(
            long id,
            CancellationToken cancellationToken = default)
        {
            var result = await _saleInvoiceService.GetSaleInvoiceDetailAsync(id, cancellationToken);

            return Ok(new ApiResponse<SaleInvoiceDTO>
            {
                Success = true,
                Code = StatusCodes.Status200OK,
                SubCode = 0,
                UserMessage = "Get invoice detail successfully",
                Data = result,
                ServerTime = DateTimeOffset.UtcNow
            });
        }
    }
}

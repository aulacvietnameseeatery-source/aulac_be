using API.Models;
using API.Models.Requests;
using API.Attributes;
using Core.Data;
using Core.DTO.General;
using Core.DTO.Inventory;
using Core.Interface.Service.Entity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Api.Controllers;

/// <summary>
/// Inventory management controller — items, transactions, stock card, dashboard.
/// </summary>
[ApiController]
[Route("api/inventory")]
public class InventoryController : ControllerBase
{
    private readonly IInventoryService _inventoryService;
    private readonly ILogger<InventoryController> _logger;

    public InventoryController(
        IInventoryService inventoryService,
        ILogger<InventoryController> logger)
    {
        _inventoryService = inventoryService;
        _logger = logger;
    }

    private static List<MediaFileInput> MapFiles(IEnumerable<IFormFile>? files)
    {
        return files?.Select(file => new MediaFileInput
        {
            Stream = file.OpenReadStream(),
            FileName = file.FileName,
            ContentType = file.ContentType
        }).ToList() ?? [];
    }

    private long GetCurrentUserId()
    {
        var claim = User.FindFirst("user_id");
        if (claim == null || !long.TryParse(claim.Value, out var userId))
            throw new UnauthorizedAccessException("User ID not found in token.");
        return userId;
    }

    // ════════════════════════════════════════════════════════
    // ITEMS
    // ════════════════════════════════════════════════════════

    /// <summary>
    /// Get paginated list of inventory items (ingredients with category + stock info).
    /// </summary>
    [HttpGet("items")]
    [HasPermission(Permissions.ViewInventory)]
    [ProducesResponseType(typeof(ApiResponse<PagedResultDTO<InventoryItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetItems(
        [FromQuery] GetInventoryItemsFilterRequest filter,
        CancellationToken ct)
    {
        var result = await _inventoryService.GetItemsAsync(filter, ct);

        return Ok(new ApiResponse<PagedResultDTO<InventoryItemDto>>
        {
            Success = true,
            Code = 200,
            UserMessage = "Inventory items retrieved successfully.",
            Data = result,
            ServerTime = DateTimeOffset.UtcNow
        });
    }

    // ════════════════════════════════════════════════════════
    // TRANSACTIONS
    // ════════════════════════════════════════════════════════

    /// <summary>
    /// Get paginated list of inventory transactions.
    /// </summary>
    [HttpGet("transactions")]
    [HasPermission(Permissions.ViewInventory)]
    [ProducesResponseType(typeof(ApiResponse<PagedResultDTO<InventoryTransactionListDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTransactions(
        [FromQuery] GetTransactionsFilterRequest filter,
        CancellationToken ct)
    {
        var result = await _inventoryService.GetTransactionsAsync(filter, ct);

        return Ok(new ApiResponse<PagedResultDTO<InventoryTransactionListDto>>
        {
            Success = true,
            Code = 200,
            UserMessage = "Transactions retrieved successfully.",
            Data = result,
            ServerTime = DateTimeOffset.UtcNow
        });
    }

    /// <summary>
    /// Get transaction detail by ID (includes items and media).
    /// </summary>
    [HttpGet("transactions/{id}")]
    [HasPermission(Permissions.ViewInventory)]
    [ProducesResponseType(typeof(ApiResponse<InventoryTransactionDetailDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTransactionDetail(long id, CancellationToken ct)
    {
        var result = await _inventoryService.GetTransactionDetailAsync(id, ct);

        return Ok(new ApiResponse<InventoryTransactionDetailDto>
        {
            Success = true,
            Code = 200,
            UserMessage = "Transaction detail retrieved successfully.",
            Data = result,
            ServerTime = DateTimeOffset.UtcNow
        });
    }

    /// <summary>
    /// Create a new inventory transaction (starts as DRAFT).
    /// Accepts multipart/form-data with JSON payload and optional evidence files.
    /// </summary>
    [HttpPost("transactions")]
    [HasPermission(Permissions.CreateInventoryTx)]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ApiResponse<InventoryTransactionDetailDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> CreateTransaction(
        [FromForm] CreateInventoryTransactionFormRequest formRequest,
        CancellationToken ct)
    {
        var request = JsonSerializer.Deserialize<CreateInventoryTransactionRequest>(
            formRequest.RequestJson,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new ArgumentException("Invalid RequestJson payload.");

        var userId = GetCurrentUserId();
        var result = await _inventoryService.CreateTransactionAsync(request, userId, MapFiles(formRequest.EvidenceFiles), ct);

        _logger.LogInformation("User {UserId} created inventory transaction {TxCode}",
            userId, result.TransactionCode);

        return Ok(new ApiResponse<InventoryTransactionDetailDto>
        {
            Success = true,
            Code = 200,
            UserMessage = "Transaction created successfully.",
            Data = result,
            ServerTime = DateTimeOffset.UtcNow
        });
    }

    /// <summary>
    /// Submit a DRAFT transaction for approval (DRAFT → PENDING_APPROVAL).
    /// </summary>
    [HttpPost("transactions/{id}/submit")]
    [HasPermission(Permissions.CreateInventoryTx)]
    [ProducesResponseType(typeof(ApiResponse<InventoryTransactionDetailDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SubmitTransaction(
        long id,
        [FromBody] SubmitTransactionRequest? request,
        CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        var result = await _inventoryService.SubmitTransactionAsync(id, request, userId, ct);

        _logger.LogInformation("User {UserId} submitted inventory transaction {TxId}", userId, id);

        return Ok(new ApiResponse<InventoryTransactionDetailDto>
        {
            Success = true,
            Code = 200,
            UserMessage = "Transaction submitted for approval.",
            Data = result,
            ServerTime = DateTimeOffset.UtcNow
        });
    }

    /// <summary>
    /// Approve or reject a PENDING_APPROVAL transaction.
    /// </summary>
    [HttpPost("transactions/{id}/approve")]
    [HasPermission(Permissions.ApproveInventoryTx)]
    [ProducesResponseType(typeof(ApiResponse<InventoryTransactionDetailDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ApproveTransaction(
        long id,
        [FromBody] ApproveTransactionRequest request,
        CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        var result = await _inventoryService.ApproveTransactionAsync(id, request, userId, ct);

        var action = request.IsApproved ? "approved" : "rejected";
        _logger.LogInformation("User {UserId} {Action} inventory transaction {TxId}", userId, action, id);

        return Ok(new ApiResponse<InventoryTransactionDetailDto>
        {
            Success = true,
            Code = 200,
            UserMessage = $"Transaction {action} successfully.",
            Data = result,
            ServerTime = DateTimeOffset.UtcNow
        });
    }

    // ════════════════════════════════════════════════════════
    // STOCK CARD
    // ════════════════════════════════════════════════════════

    /// <summary>
    /// Get paginated stock movement history for a specific item.
    /// </summary>
    [HttpGet("items/{ingredientId}/stock-card")]
    [HasPermission(Permissions.ViewInventory)]
    [ProducesResponseType(typeof(ApiResponse<PagedResultDTO<StockCardDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStockCard(
        long ingredientId,
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _inventoryService.GetStockCardAsync(ingredientId, pageIndex, pageSize, ct);

        return Ok(new ApiResponse<PagedResultDTO<StockCardDto>>
        {
            Success = true,
            Code = 200,
            UserMessage = "Stock card retrieved successfully.",
            Data = result,
            ServerTime = DateTimeOffset.UtcNow
        });
    }

    // ════════════════════════════════════════════════════════
    // DASHBOARD
    // ════════════════════════════════════════════════════════

    /// <summary>
    /// Get inventory dashboard summary (totals, low stock, pending transactions).
    /// </summary>
    [HttpGet("dashboard")]
    [HasPermission(Permissions.ViewInventory)]
    [ProducesResponseType(typeof(ApiResponse<InventoryDashboardDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDashboard(CancellationToken ct)
    {
        var result = await _inventoryService.GetDashboardAsync(ct);

        return Ok(new ApiResponse<InventoryDashboardDto>
        {
            Success = true,
            Code = 200,
            UserMessage = "Dashboard data retrieved successfully.",
            Data = result,
            ServerTime = DateTimeOffset.UtcNow
        });
    }
}

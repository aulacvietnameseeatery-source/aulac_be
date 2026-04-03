using Core.Data;
using Core.DTO.General;
using Core.DTO.Inventory;
using Core.DTO.Notification;
using Core.Entity;
using Core.Enum;
using Core.Extensions;
using Core.Interface.Repo;
using Core.Interface.Service.Entity;
using Core.Interface.Service.FileStorage;
using Core.Interface.Service.Notification;

namespace Core.Service;

public class InventoryService : IInventoryService
{
    private readonly IInventoryRepository _repo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILookupResolver _lookupResolver;
    private readonly INotificationService _notificationService;
    private readonly IFileStorage _fileStorage;
    private readonly IMediaRepository _mediaRepository;

    // Bảng ánh xạ: Loại item nào được dùng lý do xuất nào
    // COOKING/SPOILED/EXPIRED → chỉ cho FOOD_INGREDIENT
    // BROKEN/LOST/WORN_OUT → chỉ cho KITCHEN_TOOL hoặc EQUIPMENT
    // DISPOSED → cho tất cả
    private static readonly Dictionary<ExportReasonCode, HashSet<InventoryCategoryCode>> _exportReasonCategoryMap = new()
    {
        [ExportReasonCode.COOKING]  = new() { InventoryCategoryCode.FOOD_INGREDIENT },
        [ExportReasonCode.SPOILED]  = new() { InventoryCategoryCode.FOOD_INGREDIENT },
        [ExportReasonCode.EXPIRED]  = new() { InventoryCategoryCode.FOOD_INGREDIENT },
        [ExportReasonCode.BROKEN]   = new() { InventoryCategoryCode.KITCHEN_TOOL, InventoryCategoryCode.EQUIPMENT },
        [ExportReasonCode.LOST]     = new() { InventoryCategoryCode.KITCHEN_TOOL, InventoryCategoryCode.EQUIPMENT, InventoryCategoryCode.CONSUMABLE_SUPPLY },
        [ExportReasonCode.DISPOSED] = new() { InventoryCategoryCode.FOOD_INGREDIENT, InventoryCategoryCode.KITCHEN_TOOL, InventoryCategoryCode.CONSUMABLE_SUPPLY, InventoryCategoryCode.EQUIPMENT },
        [ExportReasonCode.WORN_OUT] = new() { InventoryCategoryCode.KITCHEN_TOOL, InventoryCategoryCode.EQUIPMENT },
    };

    public InventoryService(
        IInventoryRepository repo,
        IUnitOfWork unitOfWork,
        ILookupResolver lookupResolver,
        INotificationService notificationService,
        IFileStorage fileStorage,
        IMediaRepository mediaRepository)
    {
        _repo = repo;
        _unitOfWork = unitOfWork;
        _lookupResolver = lookupResolver;
        _notificationService = notificationService;
        _fileStorage = fileStorage;
        _mediaRepository = mediaRepository;
    }

    // ──────────────────────────────────────────────────────────
    // Items
    // ──────────────────────────────────────────────────────────

    public async Task<PagedResultDTO<InventoryItemDto>> GetItemsAsync(
        GetInventoryItemsFilterRequest filter, CancellationToken ct = default)
    {
        return await _repo.GetItemsPagedAsync(filter, ct);
    }

    // ──────────────────────────────────────────────────────────
    // Create Transaction
    // ──────────────────────────────────────────────────────────

    public async Task<InventoryTransactionDetailDto> CreateTransactionAsync(
        CreateInventoryTransactionRequest request, long createdByUserId, IReadOnlyList<MediaFileInput>? evidenceFiles = null, CancellationToken ct = default)
    {
        var draftStatusId = await InventoryTxStatusCode.DRAFT.ToInventoryTxStatusIdAsync(_lookupResolver, ct);

        // Generate transaction code: {TYPE_CODE}-{YYYYMMDD}-{SEQ}
        var typeCode = await ResolveTypeCodeAsync(request.TypeLvId, ct);
        var transactionCode = await GenerateTransactionCodeAsync(typeCode, ct);

        var transaction = new InventoryTransaction
        {
            TransactionCode = transactionCode,
            TypeLvId = request.TypeLvId,
            StatusLvId = draftStatusId,
            CreatedBy = createdByUserId,
            CreatedAt = DateTime.UtcNow,
            SupplierId = request.SupplierId,
            ExportReasonLvId = request.ExportReasonLvId,
            StockCheckAreaNote = request.StockCheckAreaNote,
            Note = request.Note,
        };

        var items = request.Items.Select(i => new InventoryTransactionItem
        {
            IngredientId = i.IngredientId,
            Quantity = i.Quantity,
            UnitLvId = i.UnitLvId,
            UnitPrice = i.UnitPrice,
            Note = i.Note,
        }).ToList();

        var transactionId = await _repo.CreateTransactionAsync(transaction, items, ct);

        // ── Save evidence files (if any) ──
        if (evidenceFiles is { Count: > 0 })
        {
            await SaveEvidenceFilesAsync(transactionId, transaction, evidenceFiles, ct);
        }

        return await GetTransactionDetailAsync(transactionId, ct);
    }

    private async Task SaveEvidenceFilesAsync(
        long transactionId,
        InventoryTransaction transaction,
        IReadOnlyList<MediaFileInput> files,
        CancellationToken ct)
    {
        var imageTypeLvId = await _lookupResolver.GetIdAsync(
            (ushort)Enum.LookupType.MediaType, nameof(MediaTypeCode.IMAGE), ct);

        var uploadRequests = files.Select(f => new FileUploadRequest
        {
            Stream = f.Stream,
            FileName = f.FileName,
            ContentType = f.ContentType
        }).ToList();

        var uploadResults = await _fileStorage.SaveManyAsync(
            uploadRequests, "inventory-evidence", FileValidationOptions.ImageUpload, ct);

        foreach (var uploaded in uploadResults)
        {
            var asset = await _mediaRepository.AddMediaAsync(new MediaAsset
            {
                Url = uploaded.RelativePath,
                MimeType = files.First(f => f.FileName == uploaded.OriginalFileName).ContentType,
                MediaTypeLvId = imageTypeLvId,
                CreatedAt = DateTime.UtcNow
            }, ct);

            transaction.InventoryTransactionMedia.Add(new InventoryTransactionMedium
            {
                TransactionId = transactionId,
                MediaId = asset.MediaId,
            });
        }

        await _repo.UpdateTransactionAsync(transaction, ct);
    }

    // ──────────────────────────────────────────────────────────
    // Get Transactions
    // ──────────────────────────────────────────────────────────

    public async Task<InventoryTransactionDetailDto> GetTransactionDetailAsync(
        long transactionId, CancellationToken ct = default)
    {
        var entity = await _repo.GetTransactionByIdAsync(transactionId, ct);
        if (entity == null)
            throw new KeyNotFoundException($"Transaction {transactionId} not found.");

        return MapToDetailDto(entity);
    }

    public async Task<PagedResultDTO<InventoryTransactionListDto>> GetTransactionsAsync(
        GetTransactionsFilterRequest filter, CancellationToken ct = default)
    {
        return await _repo.GetTransactionsPagedAsync(filter, ct);
    }

    // ──────────────────────────────────────────────────────────
    // Submit (DRAFT → PENDING_APPROVAL)
    // ──────────────────────────────────────────────────────────

    public async Task<InventoryTransactionDetailDto> SubmitTransactionAsync(
        long transactionId, SubmitTransactionRequest? request, long userId, CancellationToken ct = default)
    {
        var entity = await _repo.GetTransactionByIdAsync(transactionId, ct);
        if (entity == null)
            throw new KeyNotFoundException($"Transaction {transactionId} not found.");

        var draftStatusId = await InventoryTxStatusCode.DRAFT.ToInventoryTxStatusIdAsync(_lookupResolver, ct);
        if (entity.StatusLvId != draftStatusId)
            throw new InvalidOperationException("Only DRAFT transactions can be submitted.");

        // Validate export reason scoping for OUT transactions
        var outTypeId = await InventoryTxTypeCode.OUT.ToInventoryTxTypeIdAsync(_lookupResolver, ct);
        if (entity.TypeLvId == outTypeId)
        {
            await ValidateExportReasonScopingAsync(entity, ct);
        }

        var pendingStatusId = await InventoryTxStatusCode.PENDING_APPROVAL.ToInventoryTxStatusIdAsync(_lookupResolver, ct);
        entity.StatusLvId = pendingStatusId;
        entity.SubmittedAt = DateTime.UtcNow;

        await _unitOfWork.BeginTransactionAsync(ct);
        try
        {
            // Attach media if provided
            if (request?.MediaIds != null && request.MediaIds.Count > 0)
            {
                foreach (var mediaId in request.MediaIds)
                {
                    // Tránh thêm trùng
                    if (!entity.InventoryTransactionMedia.Any(m => m.MediaId == mediaId))
                    {
                        entity.InventoryTransactionMedia.Add(new InventoryTransactionMedium
                        {
                            TransactionId = transactionId,
                            MediaId = mediaId,
                        });
                    }
                }
            }

            await _repo.UpdateTransactionAsync(entity, ct);
            await _unitOfWork.CommitAsync(ct);
        }
        catch
        {
            await _unitOfWork.RollbackAsync(ct);
            throw;
        }

        // Notify approvers
        await _notificationService.PublishAsync(new PublishNotificationRequest
        {
            Type = nameof(NotificationType.INVENTORY_TX_SUBMITTED),
            Title = "Inventory Transaction Submitted",
            Body = $"Transaction {entity.TransactionCode} has been submitted for approval.",
            Priority = nameof(NotificationPriority.Normal),
            ActionUrl = $"/dashboard/inventory/transactions/{transactionId}",
            EntityType = "InventoryTransaction",
            EntityId = transactionId.ToString(),
            Metadata = new Dictionary<string, object>
            {
                ["transactionCode"] = entity.TransactionCode ?? "",
                ["typeCode"] = entity.TypeLv?.ValueCode ?? "",
            },
            TargetPermissions = new List<string> { Permissions.ApproveInventoryTx },
        }, ct);

        return await GetTransactionDetailAsync(transactionId, ct);
    }

    // ──────────────────────────────────────────────────────────
    // Approve / Reject (PENDING_APPROVAL → COMPLETED or CANCELLED)
    // ──────────────────────────────────────────────────────────

    public async Task<InventoryTransactionDetailDto> ApproveTransactionAsync(
        long transactionId, ApproveTransactionRequest request, long approvedByUserId, CancellationToken ct = default)
    {
        var entity = await _repo.GetTransactionByIdAsync(transactionId, ct);
        if (entity == null)
            throw new KeyNotFoundException($"Transaction {transactionId} not found.");

        var pendingStatusId = await InventoryTxStatusCode.PENDING_APPROVAL.ToInventoryTxStatusIdAsync(_lookupResolver, ct);
        if (entity.StatusLvId != pendingStatusId)
            throw new InvalidOperationException("Only PENDING_APPROVAL transactions can be approved/rejected.");

        entity.ApprovedBy = approvedByUserId;
        entity.ApprovedAt = DateTime.UtcNow;

        if (request.IsApproved)
        {
            var completedStatusId = await InventoryTxStatusCode.COMPLETED.ToInventoryTxStatusIdAsync(_lookupResolver, ct);
            entity.StatusLvId = completedStatusId;

            if (!string.IsNullOrEmpty(request.Note))
                entity.Note = (entity.Note ?? "") + $" [Approval note: {request.Note}]";

            await _unitOfWork.BeginTransactionAsync(ct);
            try
            {
                await _repo.UpdateTransactionAsync(entity, ct);

                // Cập nhật stock dựa trên loại giao dịch
                await ApplyStockChangesAsync(entity, ct);

                await _unitOfWork.CommitAsync(ct);
            }
            catch
            {
                await _unitOfWork.RollbackAsync(ct);
                throw;
            }

            // Kiểm tra cảnh báo tồn kho thấp sau cập nhật
            await CheckLowStockAlertsAsync(entity, ct);

            // Notify creator
            await NotifyTransactionStatusAsync(entity, "approved", ct);
        }
        else
        {
            // Reject
            if (string.IsNullOrWhiteSpace(request.Note))
                throw new InvalidOperationException("A note is required when rejecting a transaction.");

            var cancelledStatusId = await InventoryTxStatusCode.CANCELLED.ToInventoryTxStatusIdAsync(_lookupResolver, ct);
            entity.StatusLvId = cancelledStatusId;
            entity.Note = (entity.Note ?? "") + $" [Rejection reason: {request.Note}]";

            await _repo.UpdateTransactionAsync(entity, ct);

            // Notify creator
            await NotifyTransactionStatusAsync(entity, "rejected", ct);
        }

        return await GetTransactionDetailAsync(transactionId, ct);
    }

    // ──────────────────────────────────────────────────────────
    // Stock Card
    // ──────────────────────────────────────────────────────────

    public async Task<PagedResultDTO<StockCardDto>> GetStockCardAsync(
        long ingredientId, int pageIndex, int pageSize, CancellationToken ct = default)
    {
        return await _repo.GetStockCardAsync(ingredientId, pageIndex, pageSize, ct);
    }

    // ──────────────────────────────────────────────────────────
    // Dashboard
    // ──────────────────────────────────────────────────────────

    public async Task<InventoryDashboardDto> GetDashboardAsync(CancellationToken ct = default)
    {
        return await _repo.GetDashboardAsync(ct);
    }

    // ══════════════════════════════════════════════════════════
    // Private Helpers
    // ══════════════════════════════════════════════════════════

    private async Task<string> ResolveTypeCodeAsync(uint typeLvId, CancellationToken ct)
    {
        // Resolve the type code from LookupValue
        var inTypeId = await InventoryTxTypeCode.IN.ToInventoryTxTypeIdAsync(_lookupResolver, ct);
        var outTypeId = await InventoryTxTypeCode.OUT.ToInventoryTxTypeIdAsync(_lookupResolver, ct);
        var adjustTypeId = await InventoryTxTypeCode.ADJUST.ToInventoryTxTypeIdAsync(_lookupResolver, ct);

        if (typeLvId == inTypeId) return "IN";
        if (typeLvId == outTypeId) return "OUT";
        if (typeLvId == adjustTypeId) return "ADJUST";

        throw new InvalidOperationException($"Unknown transaction type LvId: {typeLvId}");
    }

    private async Task<string> GenerateTransactionCodeAsync(string typeCode, CancellationToken ct)
    {
        var todayCount = await _repo.GetTodayTransactionCountByTypeAsync(typeCode, ct);
        var seq = (todayCount + 1).ToString("D3");
        return $"{typeCode}-{DateTime.UtcNow:yyyyMMdd}-{seq}";
    }

    /// <summary>
    /// Validate export reason scoping: certain reasons are only valid for certain categories.
    /// </summary>
    private Task ValidateExportReasonScopingAsync(InventoryTransaction entity, CancellationToken ct)
    {
        if (!entity.ExportReasonLvId.HasValue)
            throw new InvalidOperationException("Export reason is required for OUT transactions.");

        // Resolve the export reason code
        var exportReasonCode = entity.ExportReasonLv?.ValueCode;
        if (string.IsNullOrEmpty(exportReasonCode) || !System.Enum.TryParse<ExportReasonCode>(exportReasonCode, out var reasonEnum))
            return Task.CompletedTask; // Unknown reason — skip validation (let BE be flexible)

        if (!_exportReasonCategoryMap.TryGetValue(reasonEnum, out var allowedCategories))
            return Task.CompletedTask;

        // Check each item's category against the allowed categories
        foreach (var item in entity.InventoryTransactionItems)
        {
            var ingredient = item.Ingredient;
            if (ingredient?.CategoryLv == null) continue;

            if (!System.Enum.TryParse<InventoryCategoryCode>(ingredient.CategoryLv.ValueCode, out var categoryEnum))
                continue;

            if (!allowedCategories.Contains(categoryEnum))
            {
                throw new InvalidOperationException(
                    $"Export reason '{exportReasonCode}' is not allowed for category " +
                    $"'{ingredient.CategoryLv.ValueName}' (item: {ingredient.IngredientName}).");
            }
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Áp dụng thay đổi tồn kho khi giao dịch được duyệt (COMPLETED).
    /// IN → tăng stock, OUT → giảm stock, ADJUST → set stock = actual quantity
    /// </summary>
    private async Task ApplyStockChangesAsync(InventoryTransaction entity, CancellationToken ct)
    {
        var inTypeId = await InventoryTxTypeCode.IN.ToInventoryTxTypeIdAsync(_lookupResolver, ct);
        var outTypeId = await InventoryTxTypeCode.OUT.ToInventoryTxTypeIdAsync(_lookupResolver, ct);
        var adjustTypeId = await InventoryTxTypeCode.ADJUST.ToInventoryTxTypeIdAsync(_lookupResolver, ct);

        foreach (var item in entity.InventoryTransactionItems)
        {
            if (entity.TypeLvId == inTypeId)
            {
                // Nhập kho: tăng tồn kho
                await _repo.UpdateStockAsync(item.IngredientId, item.Quantity, ct);
            }
            else if (entity.TypeLvId == outTypeId)
            {
                // Xuất kho: giảm tồn kho
                await _repo.UpdateStockAsync(item.IngredientId, -item.Quantity, ct);
            }
            else if (entity.TypeLvId == adjustTypeId)
            {
                // Kiểm kho: set stock = actual quantity (Quantity stores the variance)
                // SystemQuantity = current stock, ActualQuantity = counted, Quantity = variance
                if (item.ActualQuantity.HasValue)
                {
                    var currentStock = await _repo.GetCurrentStockAsync(item.IngredientId, ct);
                    var currentQty = currentStock?.QuantityOnHand ?? 0;
                    var variance = item.ActualQuantity.Value - currentQty;
                    await _repo.UpdateStockAsync(item.IngredientId, variance, ct);
                }
            }
        }
    }

    /// <summary>
    /// Kiểm tra cảnh báo tồn kho thấp sau khi cập nhật stock.
    /// </summary>
    private async Task CheckLowStockAlertsAsync(InventoryTransaction entity, CancellationToken ct)
    {
        foreach (var item in entity.InventoryTransactionItems)
        {
            var stock = await _repo.GetCurrentStockAsync(item.IngredientId, ct);
            if (stock != null && stock.MinStockLevel > 0 && stock.QuantityOnHand <= stock.MinStockLevel)
            {
                var ingredient = item.Ingredient;
                var unitName = item.UnitLv?.ValueName ?? "";

                await _notificationService.PublishAsync(new PublishNotificationRequest
                {
                    Type = nameof(NotificationType.LOW_STOCK_ALERT),
                    Title = "Low Stock Alert",
                    Body = $"{ingredient?.IngredientName} stock is low: {stock.QuantityOnHand} {unitName} (min: {stock.MinStockLevel})",
                    Priority = nameof(NotificationPriority.High),
                    SoundKey = "notification_high",
                    ActionUrl = $"/dashboard/inventory/items/{item.IngredientId}",
                    EntityType = "Ingredient",
                    EntityId = item.IngredientId.ToString(),
                    Metadata = new Dictionary<string, object>
                    {
                        ["ingredientId"] = item.IngredientId.ToString(),
                        ["ingredientName"] = ingredient?.IngredientName ?? "",
                        ["currentStock"] = stock.QuantityOnHand.ToString(),
                        ["minStock"] = stock.MinStockLevel.ToString(),
                        ["unit"] = unitName,
                    },
                    TargetPermissions = new List<string> { Permissions.ViewInventory },
                }, ct);
            }
        }
    }

    /// <summary>
    /// Gửi thông báo cho người tạo giao dịch khi giao dịch được duyệt/từ chối.
    /// </summary>
    private async Task NotifyTransactionStatusAsync(InventoryTransaction entity, string action, CancellationToken ct)
    {
        if (!entity.CreatedBy.HasValue) return;

        var notificationType = action == "approved"
            ? nameof(NotificationType.INVENTORY_TX_APPROVED)
            : nameof(NotificationType.INVENTORY_TX_REJECTED);

        await _notificationService.PublishAsync(new PublishNotificationRequest
        {
            Type = notificationType,
            Title = $"Transaction {action.ToUpperInvariant()}",
            Body = $"Your transaction {entity.TransactionCode} has been {action}.",
            Priority = action == "rejected" ? nameof(NotificationPriority.High) : nameof(NotificationPriority.Normal),
            ActionUrl = $"/dashboard/inventory/transactions/{entity.TransactionId}",
            EntityType = "InventoryTransaction",
            EntityId = entity.TransactionId.ToString(),
            Metadata = new Dictionary<string, object>
            {
                ["transactionCode"] = entity.TransactionCode ?? "",
                ["action"] = action,
            },
            TargetUserIds = new List<long> { entity.CreatedBy.Value },
        }, ct);
    }

    // ──────────────────────────────────────────────────────────
    // DTO Mapper
    // ──────────────────────────────────────────────────────────

    private static InventoryTransactionDetailDto MapToDetailDto(InventoryTransaction entity)
    {
        return new InventoryTransactionDetailDto
        {
            TransactionId = entity.TransactionId,
            TransactionCode = entity.TransactionCode,
            TypeLvId = entity.TypeLvId,
            TypeCode = entity.TypeLv?.ValueCode,
            TypeName = entity.TypeLv?.ValueName,
            StatusLvId = entity.StatusLvId,
            StatusCode = entity.StatusLv?.ValueCode,
            StatusName = entity.StatusLv?.ValueName,
            ExportReasonLvId = entity.ExportReasonLvId,
            ExportReasonCode = entity.ExportReasonLv?.ValueCode,
            ExportReasonName = entity.ExportReasonLv?.ValueName,
            SupplierId = entity.SupplierId,
            SupplierName = entity.Supplier?.SupplierName,
            CreatedBy = entity.CreatedBy,
            CreatedByName = entity.CreatedByNavigation?.FullName,
            CreatedAt = entity.CreatedAt,
            SubmittedAt = entity.SubmittedAt,
            ApprovedBy = entity.ApprovedBy,
            ApprovedByName = entity.ApprovedByNavigation?.FullName,
            ApprovedAt = entity.ApprovedAt,
            Note = entity.Note,
            StockCheckAreaNote = entity.StockCheckAreaNote,
            Items = entity.InventoryTransactionItems.Select(ti => new TransactionItemDto
            {
                TransactionItemId = ti.TransactionItemId,
                IngredientId = ti.IngredientId,
                IngredientName = ti.Ingredient?.IngredientName,
                IngredientImageUrl = ti.Ingredient?.Image?.Url,
                CategoryLvId = ti.Ingredient?.CategoryLvId,
                CategoryCode = ti.Ingredient?.CategoryLv?.ValueCode,
                CategoryName = ti.Ingredient?.CategoryLv?.ValueName,
                Quantity = ti.Quantity,
                UnitLvId = ti.UnitLvId,
                UnitName = ti.UnitLv?.ValueName,
                UnitPrice = ti.UnitPrice,
                SystemQuantity = ti.SystemQuantity,
                ActualQuantity = ti.ActualQuantity,
                VarianceReasonLvId = ti.VarianceReasonLvId,
                VarianceReasonName = ti.VarianceReasonLv?.ValueName,
                Note = ti.Note,
            }).ToList(),
            Media = entity.InventoryTransactionMedia.Select(m => new TransactionMediaDto
            {
                MediaId = m.MediaId,
                Url = m.Media?.Url,
                MediaType = m.Media?.MimeType,
            }).ToList(),
        };
    }
}

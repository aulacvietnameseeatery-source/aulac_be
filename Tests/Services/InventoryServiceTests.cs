using Core.DTO.General;
using Core.DTO.Inventory;
using Core.DTO.Notification;
using Core.Entity;
using Core.Interface.Repo;
using Core.Interface.Service.Entity;
using Core.Interface.Service.FileStorage;
using Core.Interface.Service.Notification;
using Core.Service;
using FluentAssertions;
using Moq;

namespace Tests.Services;

/// <summary>
/// Unit Test — InventoryService operations.
/// Code Module : Core/Service/InventoryService.cs
/// Methods     : GetItemsAsync, CreateTransactionAsync, GetTransactionDetailAsync,
///               GetTransactionsAsync, SubmitTransactionAsync, ApproveTransactionAsync,
///               GetStockCardAsync, GetDashboardAsync
/// Created By  : quantm
/// Executed By : quantm
/// Test Req.   : Staff and managers create inventory transactions that flow through a lifecycle
///               (DRAFT → PENDING_APPROVAL → COMPLETED or CANCELLED), browse paginated items
///               and stock movement history, and view dashboard summaries.
/// </summary>
public class InventoryServiceTests
{
    // ── Mocks ──
    private readonly Mock<IInventoryRepository>   _repoMock                = new();
    private readonly Mock<IUnitOfWork>            _unitOfWorkMock          = new();
    private readonly Mock<ILookupResolver>        _lookupResolverMock      = new();
    private readonly Mock<INotificationService>   _notificationServiceMock = new();
    private readonly Mock<IFileStorage>           _fileStorageMock         = new();
    private readonly Mock<IMediaRepository>       _mediaRepoMock           = new();

    // ── Lookup ID constants used throughout tests ──
    private const uint DraftStatusId     = 1u;
    private const uint PendingStatusId   = 2u;
    private const uint CompletedStatusId = 3u;
    private const uint CancelledStatusId = 4u;
    private const uint InTypeId          = 10u;
    private const uint OutTypeId         = 11u;
    private const uint AdjustTypeId      = 12u;

    // ── Factory ──
    private InventoryService CreateService() => new(
        _repoMock.Object,
        _unitOfWorkMock.Object,
        _lookupResolverMock.Object,
        _notificationServiceMock.Object,
        _fileStorageMock.Object,
        _mediaRepoMock.Object);

    // ── Helpers ──
    private void SetupInventoryLookups()
    {
        // Transaction status IDs
        _lookupResolverMock.Setup(r => r.GetIdAsync((ushort)Core.Enum.LookupType.InventoryTxStatus, InventoryTxStatusCode.DRAFT,            It.IsAny<CancellationToken>())).ReturnsAsync(DraftStatusId);
        _lookupResolverMock.Setup(r => r.GetIdAsync((ushort)Core.Enum.LookupType.InventoryTxStatus, InventoryTxStatusCode.PENDING_APPROVAL, It.IsAny<CancellationToken>())).ReturnsAsync(PendingStatusId);
        _lookupResolverMock.Setup(r => r.GetIdAsync((ushort)Core.Enum.LookupType.InventoryTxStatus, InventoryTxStatusCode.COMPLETED,        It.IsAny<CancellationToken>())).ReturnsAsync(CompletedStatusId);
        _lookupResolverMock.Setup(r => r.GetIdAsync((ushort)Core.Enum.LookupType.InventoryTxStatus, InventoryTxStatusCode.CANCELLED,        It.IsAny<CancellationToken>())).ReturnsAsync(CancelledStatusId);
        // Transaction type IDs
        _lookupResolverMock.Setup(r => r.GetIdAsync((ushort)Core.Enum.LookupType.InventoryTxType, InventoryTxTypeCode.IN,     It.IsAny<CancellationToken>())).ReturnsAsync(InTypeId);
        _lookupResolverMock.Setup(r => r.GetIdAsync((ushort)Core.Enum.LookupType.InventoryTxType, InventoryTxTypeCode.OUT,    It.IsAny<CancellationToken>())).ReturnsAsync(OutTypeId);
        _lookupResolverMock.Setup(r => r.GetIdAsync((ushort)Core.Enum.LookupType.InventoryTxType, InventoryTxTypeCode.ADJUST, It.IsAny<CancellationToken>())).ReturnsAsync(AdjustTypeId);
    }

    private static InventoryTransaction MakeTransaction(
        long   id       = 1,
        uint   typeId   = InTypeId,
        uint   statusId = DraftStatusId,
        string code     = "IN-20260418-001") => new()
    {
        TransactionId    = id,
        TransactionCode  = code,
        TypeLvId         = typeId,
        StatusLvId       = statusId,
        CreatedBy        = 100L,
        CreatedAt        = DateTime.UtcNow,
        InventoryTransactionItems = new List<InventoryTransactionItem>(),
        InventoryTransactionMedia = new List<InventoryTransactionMedium>(),
    };

    // ═══════════════════════════════════════════════════════════════════════
    // GetItemsAsync
    // ═══════════════════════════════════════════════════════════════════════

    #region GetItemsAsync

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "GetItemsAsync")]
    public async Task GetItemsAsync_WhenItemsExist_ReturnsPagedResult()
    {
        // Arrange
        var filter = new GetInventoryItemsFilterRequest { PageIndex = 1, PageSize = 10 };
        var paged = new PagedResultDTO<InventoryItemDto>
        {
            PageData = new List<InventoryItemDto>
            {
                new() { IngredientId = 1, IngredientName = "Flour",  QuantityOnHand = 50m },
                new() { IngredientId = 2, IngredientName = "Sugar",  QuantityOnHand = 30m },
            },
            TotalCount = 2, PageIndex = 1, PageSize = 10
        };
        _repoMock.Setup(r => r.GetItemsPagedAsync(filter, It.IsAny<CancellationToken>())).ReturnsAsync(paged);

        var service = CreateService();

        // Act
        var result = await service.GetItemsAsync(filter);

        // Assert
        result.PageData.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "GetItemsAsync")]
    public async Task GetItemsAsync_WhenNoItems_ReturnsEmptyPagedResult()
    {
        // Arrange
        var filter = new GetInventoryItemsFilterRequest { PageIndex = 1, PageSize = 10 };
        var paged = new PagedResultDTO<InventoryItemDto>
        {
            PageData = new List<InventoryItemDto>(), TotalCount = 0, PageIndex = 1, PageSize = 10
        };
        _repoMock.Setup(r => r.GetItemsPagedAsync(filter, It.IsAny<CancellationToken>())).ReturnsAsync(paged);

        var service = CreateService();

        // Act
        var result = await service.GetItemsAsync(filter);

        // Assert
        result.PageData.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════════════
    // CreateTransactionAsync
    // ═══════════════════════════════════════════════════════════════════════

    #region CreateTransactionAsync

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "CreateTransactionAsync")]
    public async Task CreateTransactionAsync_WhenValidInRequest_CreatesDraftTransactionAndReturnsDetail()
    {
        // Arrange
        SetupInventoryLookups();
        _repoMock.Setup(r => r.GetTodayTransactionCountByTypeAsync("IN", It.IsAny<CancellationToken>())).ReturnsAsync(2);
        _repoMock.Setup(r => r.CreateTransactionAsync(It.IsAny<InventoryTransaction>(), It.IsAny<List<InventoryTransactionItem>>(), It.IsAny<CancellationToken>())).ReturnsAsync(1L);
        var entity = MakeTransaction(1, InTypeId, DraftStatusId, "IN-20260418-003");
        _repoMock.Setup(r => r.GetTransactionByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(entity);

        var request = new CreateInventoryTransactionRequest
        {
            TypeLvId = InTypeId,
            Items    = new List<TransactionItemRequest>
            {
                new() { IngredientId = 10, Quantity = 5m, UnitLvId = 1u, UnitPrice = 10_000m }
            }
        };

        var service = CreateService();

        // Act
        var result = await service.CreateTransactionAsync(request, createdByUserId: 100L);

        // Assert
        result.TransactionId.Should().Be(1);
        result.StatusLvId.Should().Be(DraftStatusId);
        _repoMock.Verify(r => r.CreateTransactionAsync(
            It.IsAny<InventoryTransaction>(),
            It.IsAny<List<InventoryTransactionItem>>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "CreateTransactionAsync")]
    public async Task CreateTransactionAsync_WhenUnknownTypeLvId_ThrowsInvalidOperationException()
    {
        // Arrange
        SetupInventoryLookups(); // IN=10, OUT=11, ADJUST=12
        // TypeLvId 99u doesn't match any known type
        var request = new CreateInventoryTransactionRequest
        {
            TypeLvId = 99u,
            Items    = new List<TransactionItemRequest>
            {
                new() { IngredientId = 10, Quantity = 5m, UnitLvId = 1u }
            }
        };
        var service = CreateService();

        // Act & Assert
        await service.Invoking(s => s.CreateTransactionAsync(request, 100L))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Unknown transaction type*");
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "CreateTransactionAsync")]
    public async Task CreateTransactionAsync_WhenFirstTransactionOfDay_GeneratesSequenceNumberOne()
    {
        // Arrange
        SetupInventoryLookups();
        _repoMock.Setup(r => r.GetTodayTransactionCountByTypeAsync("IN", It.IsAny<CancellationToken>())).ReturnsAsync(0); // first today
        _repoMock.Setup(r => r.CreateTransactionAsync(It.IsAny<InventoryTransaction>(), It.IsAny<List<InventoryTransactionItem>>(), It.IsAny<CancellationToken>())).ReturnsAsync(5L);
        var entity = MakeTransaction(5, InTypeId, DraftStatusId);
        _repoMock.Setup(r => r.GetTransactionByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(entity);

        var request = new CreateInventoryTransactionRequest
        {
            TypeLvId = InTypeId,
            Items    = new List<TransactionItemRequest> { new() { IngredientId = 10, Quantity = 1m, UnitLvId = 1u } }
        };
        var service = CreateService();

        // Act
        await service.CreateTransactionAsync(request, 200L);

        // Assert — the transaction code passed to the repo should end with "-001"
        _repoMock.Verify(r => r.CreateTransactionAsync(
            It.Is<InventoryTransaction>(t => t.TransactionCode!.EndsWith("-001")),
            It.IsAny<List<InventoryTransactionItem>>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════════════
    // GetTransactionDetailAsync
    // ═══════════════════════════════════════════════════════════════════════

    #region GetTransactionDetailAsync

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "GetTransactionDetailAsync")]
    public async Task GetTransactionDetailAsync_WhenTransactionExists_ReturnsDetailDto()
    {
        // Arrange
        var entity = MakeTransaction(1, InTypeId, PendingStatusId, "IN-20260418-001");
        _repoMock.Setup(r => r.GetTransactionByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(entity);

        var service = CreateService();

        // Act
        var result = await service.GetTransactionDetailAsync(1);

        // Assert
        result.TransactionId.Should().Be(1);
        result.TransactionCode.Should().Be("IN-20260418-001");
        result.StatusLvId.Should().Be(PendingStatusId);
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "GetTransactionDetailAsync")]
    public async Task GetTransactionDetailAsync_WhenTransactionNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        _repoMock.Setup(r => r.GetTransactionByIdAsync(999, It.IsAny<CancellationToken>())).ReturnsAsync((InventoryTransaction?)null);

        var service = CreateService();

        // Act & Assert
        await service.Invoking(s => s.GetTransactionDetailAsync(999))
            .Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*999*");
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "GetTransactionDetailAsync")]
    public async Task GetTransactionDetailAsync_WhenIdIsZero_ThrowsKeyNotFoundException()
    {
        // Arrange
        _repoMock.Setup(r => r.GetTransactionByIdAsync(0, It.IsAny<CancellationToken>())).ReturnsAsync((InventoryTransaction?)null);

        var service = CreateService();

        // Act & Assert
        await service.Invoking(s => s.GetTransactionDetailAsync(0))
            .Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*0*");
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════════════
    // GetTransactionsAsync
    // ═══════════════════════════════════════════════════════════════════════

    #region GetTransactionsAsync

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "GetTransactionsAsync")]
    public async Task GetTransactionsAsync_WhenTransactionsExist_ReturnsPagedResult()
    {
        // Arrange
        var filter = new GetTransactionsFilterRequest { PageIndex = 1, PageSize = 10 };
        var paged = new PagedResultDTO<InventoryTransactionListDto>
        {
            PageData = new List<InventoryTransactionListDto>
            {
                new() { TransactionId = 1, TransactionCode = "IN-20260418-001"  },
                new() { TransactionId = 2, TransactionCode = "OUT-20260418-001" },
            },
            TotalCount = 2, PageIndex = 1, PageSize = 10
        };
        _repoMock.Setup(r => r.GetTransactionsPagedAsync(filter, It.IsAny<CancellationToken>())).ReturnsAsync(paged);

        var service = CreateService();

        // Act
        var result = await service.GetTransactionsAsync(filter);

        // Assert
        result.PageData.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "GetTransactionsAsync")]
    public async Task GetTransactionsAsync_WhenNoTransactions_ReturnsEmptyPagedResult()
    {
        // Arrange
        var filter = new GetTransactionsFilterRequest { PageIndex = 1, PageSize = 10 };
        var paged = new PagedResultDTO<InventoryTransactionListDto>
        {
            PageData = new List<InventoryTransactionListDto>(), TotalCount = 0, PageIndex = 1, PageSize = 10
        };
        _repoMock.Setup(r => r.GetTransactionsPagedAsync(filter, It.IsAny<CancellationToken>())).ReturnsAsync(paged);

        var service = CreateService();

        // Act
        var result = await service.GetTransactionsAsync(filter);

        // Assert
        result.PageData.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════════════
    // SubmitTransactionAsync
    // ═══════════════════════════════════════════════════════════════════════

    #region SubmitTransactionAsync

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "SubmitTransactionAsync")]
    public async Task SubmitTransactionAsync_WhenDraftInTransaction_ChangeStatusToPendingAndNotifiesApprovers()
    {
        // Arrange
        SetupInventoryLookups();
        var draftEntity = MakeTransaction(1, InTypeId, DraftStatusId);
        _repoMock.Setup(r => r.GetTransactionByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(draftEntity);
        _unitOfWorkMock.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.UpdateTransactionAsync(It.IsAny<InventoryTransaction>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _notificationServiceMock.Setup(n => n.PublishAsync(It.IsAny<PublishNotificationRequest>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var request = new SubmitTransactionRequest { MediaIds = new List<long> { 101L } };
        var service  = CreateService();

        // Act
        var result = await service.SubmitTransactionAsync(1, request, userId: 100L);

        // Assert
        result.TransactionId.Should().Be(1);
        result.StatusLvId.Should().Be(PendingStatusId);
        _notificationServiceMock.Verify(n => n.PublishAsync(
            It.Is<PublishNotificationRequest>(r => r.Type == "INVENTORY_TX_SUBMITTED"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "SubmitTransactionAsync")]
    public async Task SubmitTransactionAsync_WhenTransactionNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        _repoMock.Setup(r => r.GetTransactionByIdAsync(999, It.IsAny<CancellationToken>())).ReturnsAsync((InventoryTransaction?)null);

        var service = CreateService();

        // Act & Assert
        await service.Invoking(s => s.SubmitTransactionAsync(999, null, 100L))
            .Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*999*");
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "SubmitTransactionAsync")]
    public async Task SubmitTransactionAsync_WhenTransactionAlreadyPending_ThrowsInvalidOperationException()
    {
        // Arrange
        SetupInventoryLookups();
        var pendingEntity = MakeTransaction(1, InTypeId, PendingStatusId); // not DRAFT
        _repoMock.Setup(r => r.GetTransactionByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(pendingEntity);

        var service = CreateService();

        // Act & Assert
        await service.Invoking(s => s.SubmitTransactionAsync(1, null, 100L))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*DRAFT*");
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "SubmitTransactionAsync")]
    public async Task SubmitTransactionAsync_WhenNullRequest_SubmitsSuccessfullyWithoutAttachingMedia()
    {
        // Arrange
        SetupInventoryLookups();
        var draftEntity = MakeTransaction(1, InTypeId, DraftStatusId);
        _repoMock.Setup(r => r.GetTransactionByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(draftEntity);
        _unitOfWorkMock.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.UpdateTransactionAsync(It.IsAny<InventoryTransaction>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _notificationServiceMock.Setup(n => n.PublishAsync(It.IsAny<PublishNotificationRequest>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var service = CreateService();

        // Act
        var result = await service.SubmitTransactionAsync(1, request: null, userId: 100L);

        // Assert
        result.TransactionId.Should().Be(1);
        result.StatusLvId.Should().Be(PendingStatusId);
        draftEntity.InventoryTransactionMedia.Should().BeEmpty(); // no media was attached
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════════════
    // ApproveTransactionAsync
    // ═══════════════════════════════════════════════════════════════════════

    #region ApproveTransactionAsync

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "ApproveTransactionAsync")]
    public async Task ApproveTransactionAsync_WhenApproved_MovesToCompletedAndNotifiesCreator()
    {
        // Arrange
        SetupInventoryLookups();
        var pendingEntity = MakeTransaction(1, InTypeId, PendingStatusId, "IN-20260418-001");
        // Empty items → ApplyStockChangesAsync and CheckLowStockAlertsAsync are no-ops
        _repoMock.Setup(r => r.GetTransactionByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(pendingEntity);
        _unitOfWorkMock.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.UpdateTransactionAsync(It.IsAny<InventoryTransaction>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _notificationServiceMock.Setup(n => n.PublishAsync(It.IsAny<PublishNotificationRequest>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var request = new ApproveTransactionRequest { IsApproved = true };
        var service  = CreateService();

        // Act
        var result = await service.ApproveTransactionAsync(1, request, approvedByUserId: 200L);

        // Assert
        result.TransactionId.Should().Be(1);
        result.StatusLvId.Should().Be(CompletedStatusId);
        pendingEntity.ApprovedBy.Should().Be(200L);
        _notificationServiceMock.Verify(n => n.PublishAsync(It.IsAny<PublishNotificationRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "ApproveTransactionAsync")]
    public async Task ApproveTransactionAsync_WhenRejectedWithNote_MovesToCancelledAndAppendNote()
    {
        // Arrange
        SetupInventoryLookups();
        var pendingEntity = MakeTransaction(2, InTypeId, PendingStatusId, "IN-20260418-002");
        _repoMock.Setup(r => r.GetTransactionByIdAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync(pendingEntity);
        _repoMock.Setup(r => r.UpdateTransactionAsync(It.IsAny<InventoryTransaction>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _notificationServiceMock.Setup(n => n.PublishAsync(It.IsAny<PublishNotificationRequest>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var request = new ApproveTransactionRequest { IsApproved = false, Note = "Quantity mismatch" };
        var service  = CreateService();

        // Act
        var result = await service.ApproveTransactionAsync(2, request, approvedByUserId: 200L);

        // Assert
        result.StatusLvId.Should().Be(CancelledStatusId);
        pendingEntity.Note.Should().Contain("Rejection reason: Quantity mismatch");
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "ApproveTransactionAsync")]
    public async Task ApproveTransactionAsync_WhenTransactionNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        _repoMock.Setup(r => r.GetTransactionByIdAsync(999, It.IsAny<CancellationToken>())).ReturnsAsync((InventoryTransaction?)null);

        var service = CreateService();

        // Act & Assert
        await service.Invoking(s => s.ApproveTransactionAsync(999, new ApproveTransactionRequest { IsApproved = true }, 200L))
            .Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*999*");
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "ApproveTransactionAsync")]
    public async Task ApproveTransactionAsync_WhenTransactionNotPendingApproval_ThrowsInvalidOperationException()
    {
        // Arrange
        SetupInventoryLookups();
        var draftEntity = MakeTransaction(1, InTypeId, DraftStatusId); // still DRAFT, not PENDING
        _repoMock.Setup(r => r.GetTransactionByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(draftEntity);

        var service = CreateService();

        // Act & Assert
        await service.Invoking(s => s.ApproveTransactionAsync(1, new ApproveTransactionRequest { IsApproved = true }, 200L))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*PENDING_APPROVAL*");
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════════════
    // GetStockCardAsync
    // ═══════════════════════════════════════════════════════════════════════

    #region GetStockCardAsync

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "GetStockCardAsync")]
    public async Task GetStockCardAsync_WhenIngredientHasMovements_ReturnsPagedStockCard()
    {
        // Arrange
        var paged = new PagedResultDTO<StockCardDto>
        {
            PageData = new List<StockCardDto>
            {
                new() { TransactionId = 1, QuantityChanged = 10m, TypeCode = "IN"  },
                new() { TransactionId = 2, QuantityChanged = -3m, TypeCode = "OUT" },
            },
            TotalCount = 2, PageIndex = 1, PageSize = 10
        };
        _repoMock.Setup(r => r.GetStockCardAsync(10, 1, 10, It.IsAny<CancellationToken>())).ReturnsAsync(paged);

        var service = CreateService();

        // Act
        var result = await service.GetStockCardAsync(ingredientId: 10, pageIndex: 1, pageSize: 10);

        // Assert
        result.PageData.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "GetStockCardAsync")]
    public async Task GetStockCardAsync_WhenIngredientIdIsZero_ReturnsEmptyResult()
    {
        // Arrange
        var paged = new PagedResultDTO<StockCardDto>
        {
            PageData = new List<StockCardDto>(), TotalCount = 0, PageIndex = 1, PageSize = 10
        };
        _repoMock.Setup(r => r.GetStockCardAsync(0, 1, 10, It.IsAny<CancellationToken>())).ReturnsAsync(paged);

        var service = CreateService();

        // Act
        var result = await service.GetStockCardAsync(ingredientId: 0, pageIndex: 1, pageSize: 10);

        // Assert
        result.PageData.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════════════
    // GetDashboardAsync
    // ═══════════════════════════════════════════════════════════════════════

    #region GetDashboardAsync

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "GetDashboardAsync")]
    public async Task GetDashboardAsync_WhenCalled_ReturnsDashboardSummary()
    {
        // Arrange
        var dashboard = new InventoryDashboardDto
        {
            TotalItems          = 50,
            LowStockItems       = 5,
            OutOfStockItems     = 2,
            PendingTransactions = 3,
            LowStockList        = new List<LowStockItemDto>
            {
                new() { IngredientId = 1, IngredientName = "Flour", QuantityOnHand = 2m, MinStockLevel = 10m }
            },
            RecentTransactions  = new List<RecentTransactionDto>
            {
                new() { TransactionId = 10, TransactionCode = "IN-20260418-001" }
            }
        };
        _repoMock.Setup(r => r.GetDashboardAsync(It.IsAny<CancellationToken>())).ReturnsAsync(dashboard);

        var service = CreateService();

        // Act
        var result = await service.GetDashboardAsync();

        // Assert
        result.TotalItems.Should().Be(50);
        result.LowStockItems.Should().Be(5);
        result.PendingTransactions.Should().Be(3);
        result.LowStockList.Should().HaveCount(1);
        result.RecentTransactions.Should().HaveCount(1);
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "GetDashboardAsync")]
    public async Task GetDashboardAsync_WhenNoData_ReturnsEmptyDashboard()
    {
        // Arrange
        var dashboard = new InventoryDashboardDto
        {
            TotalItems          = 0,
            LowStockItems       = 0,
            OutOfStockItems     = 0,
            PendingTransactions = 0,
            LowStockList        = new List<LowStockItemDto>(),
            RecentTransactions  = new List<RecentTransactionDto>()
        };
        _repoMock.Setup(r => r.GetDashboardAsync(It.IsAny<CancellationToken>())).ReturnsAsync(dashboard);

        var service = CreateService();

        // Act
        var result = await service.GetDashboardAsync();

        // Assert
        result.TotalItems.Should().Be(0);
        result.LowStockList.Should().BeEmpty();
        result.RecentTransactions.Should().BeEmpty();
    }

    #endregion
}

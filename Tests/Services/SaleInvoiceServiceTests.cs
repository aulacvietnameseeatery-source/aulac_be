using Core.DTO.General;
using Core.DTO.Order;
using Core.Entity;
using Core.Exceptions;
using Core.Interface.Repo;
using Core.Service;
using FluentAssertions;
using Moq;

namespace Tests.Services;

/// <summary>
/// Unit Test — SaleInvoiceService
/// Code Module : Core/Service/SaleInvoiceService.cs
/// Method      : GetSaleInvoiceDetailAsync, GetSaleInvoiceListAsync
/// Created By  : quantm
/// Executed By : quantm
/// Test Req.   : Kiểm tra logic tạo hoá đơn bán hàng từ đơn hàng, bao gồm mapping item,
///               tính tổng, xử lý trường hợp không tìm thấy đơn hàng, và phân trang danh sách.
/// </summary>
public class SaleInvoiceServiceTests
{
    // ── Mocks ──
    private readonly Mock<ISaleInvoiceRepository> _repoMock = new();

    // ── Factory ──
    private SaleInvoiceService CreateService() => new(_repoMock.Object);

    // ── Helpers ──
    private static Order MakeOrder(
        long orderId = 1,
        decimal subTotal = 200000,
        decimal totalAmount = 220000,
        decimal? tipAmount = 0,
        bool hasPaidPayment = true,
        int itemCount = 2)
    {
        var order = new Order
        {
            OrderId = orderId,
            CreatedAt = new DateTime(2025, 6, 1, 12, 0, 0),
            SubTotalAmount = subTotal,
            TotalAmount = totalAmount,
            TipAmount = tipAmount,
            SourceLv = new LookupValue { ValueName = "DINE_IN" },
            Table = new RestaurantTable { TableCode = "T001" },
            Staff = new StaffAccount { FullName = "Staff A" },
            Customer = new Customer { FullName = "Customer A", Phone = "0123456789" },
            Payments = hasPaidPayment
                ? new List<Payment>
                {
                    new Payment { MethodLv = new LookupValue { ValueName = "CASH" } }
                }
                : new List<Payment>(),
            OrderItems = Enumerable.Range(1, itemCount).Select(i => new OrderItem
            {
                OrderItemId = i,
                Quantity = 2,
                Price = 50000,
                Dish = new Dish { DishName = $"Dish {i}" },
                Note = null,
                ItemStatusLv = new LookupValue { ValueCode = "SERVED" }
            }).ToList()
        };

        return order;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // TEST CASES - GetSaleInvoiceDetailAsync
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "GetSaleInvoiceDetailAsync")]
    public async Task GetSaleInvoiceDetailAsync_WhenOrderExists_ReturnsInvoiceWithMappedItems()
    {
        // Arrange
        var order = MakeOrder(orderId: 1, subTotal: 200000, totalAmount: 220000, tipAmount: 10000);
        _repoMock
            .Setup(r => r.GetOrderForInvoiceAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
        _repoMock
            .Setup(r => r.GetTotalDiscountAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(5000m);

        var service = CreateService();

        // Act
        var result = await service.GetSaleInvoiceDetailAsync(1);

        // Assert
        result.OrderId.Should().Be(1);
        result.InvoiceCode.Should().Be("#INV0001");
        result.OrderType.Should().Be("DINE_IN");
        result.TableCode.Should().Be("T001");
        result.StaffName.Should().Be("Staff A");
        result.CustomerName.Should().Be("Customer A");
        result.CustomerPhone.Should().Be("0123456789");
        result.IsPaid.Should().BeTrue();
        result.PaymentMethod.Should().Be("CASH");
        result.Items.Should().HaveCount(2);
        result.SubTotal.Should().Be(200000);
        result.DiscountAmount.Should().Be(5000);
        result.TipAmount.Should().Be(10000);
        result.TotalAmount.Should().Be(220000);
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "GetSaleInvoiceDetailAsync")]
    public async Task GetSaleInvoiceDetailAsync_WhenOrderNotFound_ThrowsNotFoundException()
    {
        // Arrange
        _repoMock
            .Setup(r => r.GetOrderForInvoiceAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        var service = CreateService();

        // Act & Assert
        await service.Invoking(s => s.GetSaleInvoiceDetailAsync(999))
            .Should().ThrowAsync<NotFoundException>()
            .WithMessage("*999*");
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "GetSaleInvoiceDetailAsync")]
    public async Task GetSaleInvoiceDetailAsync_WhenUnpaid_ReturnsFalseIsPaidAndDash()
    {
        // Arrange
        var order = MakeOrder(orderId: 2, hasPaidPayment: false);
        _repoMock
            .Setup(r => r.GetOrderForInvoiceAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
        _repoMock
            .Setup(r => r.GetTotalDiscountAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0m);

        var service = CreateService();

        // Act
        var result = await service.GetSaleInvoiceDetailAsync(2);

        // Assert
        result.IsPaid.Should().BeFalse();
        result.PaymentMethod.Should().Be("-");
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "GetSaleInvoiceDetailAsync")]
    public async Task GetSaleInvoiceDetailAsync_WhenItemsHaveRejectedOrCancelled_FiltersThemOut()
    {
        // Arrange
        var order = MakeOrder(orderId: 3, itemCount: 0);
        order.OrderItems = new List<OrderItem>
        {
            new OrderItem
            {
                OrderItemId = 1, Quantity = 1, Price = 50000,
                Dish = new Dish { DishName = "Phở" },
                ItemStatusLv = new LookupValue { ValueCode = "SERVED" }
            },
            new OrderItem
            {
                OrderItemId = 2, Quantity = 1, Price = 30000,
                Dish = new Dish { DishName = "Bún" },
                ItemStatusLv = new LookupValue { ValueCode = "REJECTED" }
            },
            new OrderItem
            {
                OrderItemId = 3, Quantity = 1, Price = 20000,
                Dish = new Dish { DishName = "Cơm" },
                ItemStatusLv = new LookupValue { ValueCode = "CANCELLED" }
            }
        };
        _repoMock
            .Setup(r => r.GetOrderForInvoiceAsync(3, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
        _repoMock
            .Setup(r => r.GetTotalDiscountAsync(3, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0m);

        var service = CreateService();

        // Act
        var result = await service.GetSaleInvoiceDetailAsync(3);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items[0].ItemName.Should().Be("Phở");
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "GetSaleInvoiceDetailAsync")]
    public async Task GetSaleInvoiceDetailAsync_WhenNoItems_ReturnsEmptyItemsList()
    {
        // Arrange
        var order = MakeOrder(orderId: 4, subTotal: 0, totalAmount: 0, itemCount: 0);
        order.OrderItems = new List<OrderItem>();
        _repoMock
            .Setup(r => r.GetOrderForInvoiceAsync(4, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
        _repoMock
            .Setup(r => r.GetTotalDiscountAsync(4, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0m);

        var service = CreateService();

        // Act
        var result = await service.GetSaleInvoiceDetailAsync(4);

        // Assert
        result.Items.Should().BeEmpty();
        result.SubTotal.Should().Be(0);
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "GetSaleInvoiceDetailAsync")]
    public async Task GetSaleInvoiceDetailAsync_WhenSubTotalIsZero_FallsBackToItemSum()
    {
        // Arrange
        var order = MakeOrder(orderId: 5, subTotal: 0, totalAmount: 220000, itemCount: 2);
        _repoMock
            .Setup(r => r.GetOrderForInvoiceAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
        _repoMock
            .Setup(r => r.GetTotalDiscountAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0m);

        var service = CreateService();

        // Act
        var result = await service.GetSaleInvoiceDetailAsync(5);

        // Assert
        // SubTotal should fallback to sum of items: 50000*2 + 50000*2 = 200000
        result.SubTotal.Should().Be(200000);
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "GetSaleInvoiceDetailAsync")]
    public async Task GetSaleInvoiceDetailAsync_WhenNullNavigations_UsesDefaults()
    {
        // Arrange
        var order = new Order
        {
            OrderId = 6,
            CreatedAt = DateTime.UtcNow,
            SubTotalAmount = 100000,
            TotalAmount = 100000,
            TipAmount = null,
            SourceLv = null!,
            Table = null,
            Staff = null,
            Customer = null!,
            Payments = new List<Payment>(),
            OrderItems = new List<OrderItem>()
        };
        _repoMock
            .Setup(r => r.GetOrderForInvoiceAsync(6, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
        _repoMock
            .Setup(r => r.GetTotalDiscountAsync(6, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0m);

        var service = CreateService();

        // Act
        var result = await service.GetSaleInvoiceDetailAsync(6);

        // Assert
        result.OrderType.Should().Be("Unknown");
        result.TableCode.Should().Be("");
        result.StaffName.Should().Be("");
        result.CustomerName.Should().Be("");
        result.CustomerPhone.Should().Be("");
        result.TipAmount.Should().Be(0);
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "GetSaleInvoiceDetailAsync")]
    public async Task GetSaleInvoiceDetailAsync_WhenInvoiceCodeFormatting_PadsOrderIdTo4Digits()
    {
        // Arrange
        var order = MakeOrder(orderId: 42);
        _repoMock
            .Setup(r => r.GetOrderForInvoiceAsync(42, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
        _repoMock
            .Setup(r => r.GetTotalDiscountAsync(42, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0m);

        var service = CreateService();

        // Act
        var result = await service.GetSaleInvoiceDetailAsync(42);

        // Assert
        result.InvoiceCode.Should().Be("#INV0042");
    }

    // ═══════════════════════════════════════════════════════════════════════
    // TEST CASES - GetSaleInvoiceListAsync
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "GetSaleInvoiceListAsync")]
    public async Task GetSaleInvoiceListAsync_WhenDataExists_ReturnsPagedResult()
    {
        // Arrange
        var query = new SaleInvoiceListQueryDTO { PageIndex = 1, PageSize = 10 };
        var pagedResult = new PagedResultDTO<SaleInvoiceListDTO>
        {
            PageData = new List<SaleInvoiceListDTO>
            {
                new() { OrderId = 1, InvoiceCode = "#INV0001" },
                new() { OrderId = 2, InvoiceCode = "#INV0002" }
            },
            TotalCount = 2,
            PageIndex = 1,
            PageSize = 10
        };
        _repoMock
            .Setup(r => r.GetOrdersForInvoiceListAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        var service = CreateService();

        // Act
        var result = await service.GetSaleInvoiceListAsync(query);

        // Assert
        result.PageData.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "GetSaleInvoiceListAsync")]
    public async Task GetSaleInvoiceListAsync_WhenNoData_ReturnsEmptyResult()
    {
        // Arrange
        var query = new SaleInvoiceListQueryDTO { PageIndex = 1, PageSize = 10 };
        var pagedResult = new PagedResultDTO<SaleInvoiceListDTO>
        {
            PageData = new List<SaleInvoiceListDTO>(),
            TotalCount = 0,
            PageIndex = 1,
            PageSize = 10
        };
        _repoMock
            .Setup(r => r.GetOrdersForInvoiceListAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        var service = CreateService();

        // Act
        var result = await service.GetSaleInvoiceListAsync(query);

        // Assert
        result.PageData.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "GetSaleInvoiceListAsync")]
    public async Task GetSaleInvoiceListAsync_WhenQueryHasZeroPageSize_StillDelegatesToRepository()
    {
        // Arrange
        var query = new SaleInvoiceListQueryDTO { PageIndex = 0, PageSize = 0 };
        var pagedResult = new PagedResultDTO<SaleInvoiceListDTO>
        {
            PageData = new List<SaleInvoiceListDTO>(),
            TotalCount = 0,
            PageIndex = 0,
            PageSize = 0
        };
        _repoMock
            .Setup(r => r.GetOrdersForInvoiceListAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        var service = CreateService();

        // Act
        var result = await service.GetSaleInvoiceListAsync(query);

        // Assert
        result.PageData.Should().BeEmpty();
        _repoMock.Verify(r => r.GetOrdersForInvoiceListAsync(query, It.IsAny<CancellationToken>()), Times.Once);
    }
}

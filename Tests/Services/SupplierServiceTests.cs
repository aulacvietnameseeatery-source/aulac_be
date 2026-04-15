using Core.DTO.General;
using Core.DTO.Ingredient;
using Core.DTO.Supplier;
using Core.Entity;
using Core.Interface.Repo;
using Core.Service;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace Tests.Services;

/// <summary>
/// Unit Test — SupplierService
/// Code Module : Core/Service/SupplierService.cs
/// Method      : GetAllSuppliersAsync, GetSupplierDetailAsync, CreateSupplierAsync,
///               UpdateSupplierAsync, DeleteSupplierAsync
/// Created By  : quantm
/// Executed By : quantm
/// Test Req.   : Kiểm tra logic quản lý nhà cung cấp: CRUD operations, validate duplicate name,
///               kiểm tra ràng buộc phụ thuộc khi xóa, và liên kết nguyên liệu.
/// </summary>
public class SupplierServiceTests
{
    // ── Mocks ──
    private readonly Mock<ISupplierRepository> _repoMock = new();
    private readonly Mock<ILogger<SupplierService>> _loggerMock = new();

    // ── Factory ──
    private SupplierService CreateService() => new(_repoMock.Object, _loggerMock.Object);

    // ── Helpers ──
    private static Supplier MakeSupplier(
        long id = 1,
        string name = "Supplier A",
        string? phone = "0901234567",
        string? email = "supplier@test.com",
        string? address = "123 Main St",
        string? taxCode = "TAX001") => new()
    {
        SupplierId = id,
        SupplierName = name,
        Phone = phone,
        Email = email,
        Address = address,
        TaxCode = taxCode,
        IngredientSuppliers = new List<IngredientSupplier>(),
        InventoryTransactions = new List<InventoryTransaction>()
    };

    private static Supplier MakeSupplierWithIngredients(long id = 1) => new()
    {
        SupplierId = id,
        SupplierName = "Supplier A",
        Phone = "0901234567",
        Email = "supplier@test.com",
        Address = "123 Main St",
        TaxCode = "TAX001",
        IngredientSuppliers = new List<IngredientSupplier>
        {
            new()
            {
                IngredientSupplierId = 1,
                SupplierId = id,
                IngredientId = 10,
                Ingredient = new Ingredient { IngredientId = 10, IngredientName = "Salt", UnitLvId = 1 }
            },
            new()
            {
                IngredientSupplierId = 2,
                SupplierId = id,
                IngredientId = 20,
                Ingredient = new Ingredient { IngredientId = 20, IngredientName = "Sugar", UnitLvId = 2 }
            }
        }
    };

    private static CreateSupplierRequest MakeCreateRequest() => new()
    {
        SupplierName = "New Supplier",
        Phone = "0909999888",
        Email = "new@test.com",
        Address = "456 New St",
        TaxCode = "TAX002",
        IngredientIds = new List<long> { 10, 20 }
    };

    private static UpdateSupplierRequest MakeUpdateRequest() => new()
    {
        SupplierName = "Updated Supplier",
        Phone = "0908888777",
        Email = "updated@test.com",
        Address = "789 Updated St",
        TaxCode = "TAX003",
        IngredientIds = new List<long> { 30 }
    };

    // ═══════════════════════════════════════════════════════════════════════
    // TEST CASES - GetAllSuppliersAsync
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "GetAllSuppliersAsync")]
    public async Task GetAllSuppliersAsync_WhenDataExists_ReturnsPagedResult()
    {
        // Arrange
        var query = new SupplierListQueryDTO();
        var pagedResult = new PagedResultDTO<SupplierDto>
        {
            PageData = new List<SupplierDto>
            {
                new() { SupplierId = 1, SupplierName = "Supplier A" },
                new() { SupplierId = 2, SupplierName = "Supplier B" }
            },
            TotalCount = 2,
            PageIndex = 1,
            PageSize = 10
        };
        _repoMock
            .Setup(r => r.GetAllSuppliersAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        var service = CreateService();

        // Act
        var result = await service.GetAllSuppliersAsync(query);

        // Assert
        result.PageData.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "GetAllSuppliersAsync")]
    public async Task GetAllSuppliersAsync_WhenNoData_ReturnsEmptyResult()
    {
        // Arrange
        var query = new SupplierListQueryDTO();
        var pagedResult = new PagedResultDTO<SupplierDto>
        {
            PageData = new List<SupplierDto>(),
            TotalCount = 0,
            PageIndex = 1,
            PageSize = 10
        };
        _repoMock
            .Setup(r => r.GetAllSuppliersAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        var service = CreateService();

        // Act
        var result = await service.GetAllSuppliersAsync(query);

        // Assert
        result.PageData.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }
    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "GetAllSuppliersAsync")]
    public async Task GetAllSuppliersAsync_WhenQueryHasZeroPageSize_StillDelegatesToRepository()
    {
        // Arrange
        var query = new SupplierListQueryDTO();
        var pagedResult = new PagedResultDTO<SupplierDto>
        {
            PageData = new List<SupplierDto>(),
            TotalCount = 0,
            PageIndex = 0,
            PageSize = 0
        };
        _repoMock
            .Setup(r => r.GetAllSuppliersAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        var service = CreateService();

        // Act
        var result = await service.GetAllSuppliersAsync(query);

        // Assert
        result.PageData.Should().BeEmpty();
        _repoMock.Verify(r => r.GetAllSuppliersAsync(query, It.IsAny<CancellationToken>()), Times.Once);
    }
    // ═══════════════════════════════════════════════════════════════════════
    // TEST CASES - GetSupplierDetailAsync
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "GetSupplierDetailAsync")]
    public async Task GetSupplierDetailAsync_WhenExists_ReturnsDetailWithIngredients()
    {
        // Arrange
        var supplier = MakeSupplierWithIngredients(id: 1);
        _repoMock
            .Setup(r => r.GetByIdWithIngredientsAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(supplier);

        var service = CreateService();

        // Act
        var result = await service.GetSupplierDetailAsync(1);

        // Assert
        result.SupplierId.Should().Be(1);
        result.SupplierName.Should().Be("Supplier A");
        result.Phone.Should().Be("0901234567");
        result.Email.Should().Be("supplier@test.com");
        result.Ingredients.Should().HaveCount(2);
        result.Ingredients[0].IngredientName.Should().Be("Salt");
        result.Ingredients[1].IngredientName.Should().Be("Sugar");
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "GetSupplierDetailAsync")]
    public async Task GetSupplierDetailAsync_WhenNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        _repoMock
            .Setup(r => r.GetByIdWithIngredientsAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Supplier?)null);

        var service = CreateService();

        // Act & Assert
        await service.Invoking(s => s.GetSupplierDetailAsync(999))
            .Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*999*");
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "GetSupplierDetailAsync")]
    public async Task GetSupplierDetailAsync_WhenNoIngredients_ReturnsEmptyIngredientList()
    {
        // Arrange
        var supplier = MakeSupplier(id: 2);
        supplier.IngredientSuppliers = new List<IngredientSupplier>();
        _repoMock
            .Setup(r => r.GetByIdWithIngredientsAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(supplier);

        var service = CreateService();

        // Act
        var result = await service.GetSupplierDetailAsync(2);

        // Assert
        result.Ingredients.Should().BeEmpty();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // TEST CASES - CreateSupplierAsync
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "CreateSupplierAsync")]
    public async Task CreateSupplierAsync_WhenValidRequest_CreatesAndReturnsDto()
    {
        // Arrange
        var request = MakeCreateRequest();
        _repoMock
            .Setup(r => r.ExistsByNameAsync("New Supplier", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _repoMock
            .Setup(r => r.CreateAsync(It.IsAny<Supplier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Supplier s, CancellationToken _) =>
            {
                s.SupplierId = 10;
                return s;
            });
        _repoMock
            .Setup(r => r.UpdateSupplierIngredientsAsync(10, request.IngredientIds, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = CreateService();

        // Act
        var result = await service.CreateSupplierAsync(request);

        // Assert
        result.SupplierId.Should().Be(10);
        result.SupplierName.Should().Be("New Supplier");
        result.Phone.Should().Be("0909999888");
        result.Email.Should().Be("new@test.com");
        _repoMock.Verify(r => r.UpdateSupplierIngredientsAsync(10, request.IngredientIds, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "CreateSupplierAsync")]
    public async Task CreateSupplierAsync_WhenNameExists_ThrowsInvalidOperationException()
    {
        // Arrange
        var request = MakeCreateRequest();
        _repoMock
            .Setup(r => r.ExistsByNameAsync("New Supplier", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = CreateService();

        // Act & Assert
        await service.Invoking(s => s.CreateSupplierAsync(request))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already exists*");
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "CreateSupplierAsync")]
    public async Task CreateSupplierAsync_WhenNoIngredients_CreatesWithoutIngredientUpdate()
    {
        // Arrange
        var request = MakeCreateRequest();
        request.IngredientIds = new List<long>();
        _repoMock
            .Setup(r => r.ExistsByNameAsync("New Supplier", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _repoMock
            .Setup(r => r.CreateAsync(It.IsAny<Supplier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Supplier s, CancellationToken _) =>
            {
                s.SupplierId = 11;
                return s;
            });

        var service = CreateService();

        // Act
        var result = await service.CreateSupplierAsync(request);

        // Assert
        result.SupplierId.Should().Be(11);
        _repoMock.Verify(r => r.UpdateSupplierIngredientsAsync(It.IsAny<long>(), It.IsAny<List<long>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // TEST CASES - UpdateSupplierAsync
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "UpdateSupplierAsync")]
    public async Task UpdateSupplierAsync_WhenValidRequest_UpdatesAndReturnsDto()
    {
        // Arrange
        var existing = MakeSupplier(id: 5);
        var request = MakeUpdateRequest();
        _repoMock
            .Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);
        _repoMock
            .Setup(r => r.ExistsByNameAsync("Updated Supplier", 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _repoMock
            .Setup(r => r.UpdateAsync(It.IsAny<Supplier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Supplier s, CancellationToken _) => s);
        _repoMock
            .Setup(r => r.UpdateSupplierIngredientsAsync(5, request.IngredientIds, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = CreateService();

        // Act
        var result = await service.UpdateSupplierAsync(5, request);

        // Assert
        result.SupplierName.Should().Be("Updated Supplier");
        result.Phone.Should().Be("0908888777");
        result.Email.Should().Be("updated@test.com");
        _repoMock.Verify(r => r.UpdateSupplierIngredientsAsync(5, request.IngredientIds, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "UpdateSupplierAsync")]
    public async Task UpdateSupplierAsync_WhenNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        _repoMock
            .Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Supplier?)null);

        var service = CreateService();

        // Act & Assert
        await service.Invoking(s => s.UpdateSupplierAsync(999, MakeUpdateRequest()))
            .Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*999*");
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "UpdateSupplierAsync")]
    public async Task UpdateSupplierAsync_WhenNameConflict_ThrowsInvalidOperationException()
    {
        // Arrange
        var existing = MakeSupplier(id: 5);
        var request = MakeUpdateRequest();
        _repoMock
            .Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);
        _repoMock
            .Setup(r => r.ExistsByNameAsync("Updated Supplier", 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = CreateService();

        // Act & Assert
        await service.Invoking(s => s.UpdateSupplierAsync(5, request))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already exists*");
    }
    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "UpdateSupplierAsync")]
    public async Task UpdateSupplierAsync_WhenEmptyIngredientIds_UpdatesAndClearsIngredients()
    {
        // Arrange
        var existing = MakeSupplier(id: 6);
        var request = MakeUpdateRequest();
        request.IngredientIds = new List<long>(); // empty
        _repoMock
            .Setup(r => r.GetByIdAsync(6, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);
        _repoMock
            .Setup(r => r.ExistsByNameAsync("Updated Supplier", 6, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _repoMock
            .Setup(r => r.UpdateAsync(It.IsAny<Supplier>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Supplier s, CancellationToken _) => s);
        _repoMock
            .Setup(r => r.UpdateSupplierIngredientsAsync(6, It.IsAny<List<long>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = CreateService();

        // Act
        var result = await service.UpdateSupplierAsync(6, request);

        // Assert
        result.SupplierName.Should().Be("Updated Supplier");
        _repoMock.Verify(r => r.UpdateSupplierIngredientsAsync(6, It.Is<List<long>>(l => l.Count == 0), It.IsAny<CancellationToken>()), Times.Once);
    }
    // ═══════════════════════════════════════════════════════════════════════
    // TEST CASES - DeleteSupplierAsync
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "DeleteSupplierAsync")]
    public async Task DeleteSupplierAsync_WhenValid_DeletesSuccessfully()
    {
        // Arrange
        var supplier = MakeSupplier(id: 7);
        _repoMock
            .Setup(r => r.GetByIdAsync(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(supplier);
        _repoMock
            .Setup(r => r.HasDependenciesAsync(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _repoMock
            .Setup(r => r.DeleteAsync(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = CreateService();

        // Act
        var result = await service.DeleteSupplierAsync(7);

        // Assert
        result.Should().BeTrue();
        _repoMock.Verify(r => r.DeleteAsync(7, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "DeleteSupplierAsync")]
    public async Task DeleteSupplierAsync_WhenNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        _repoMock
            .Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Supplier?)null);

        var service = CreateService();

        // Act & Assert
        await service.Invoking(s => s.DeleteSupplierAsync(999))
            .Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*999*");
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "DeleteSupplierAsync")]
    public async Task DeleteSupplierAsync_WhenHasDependencies_ThrowsInvalidOperationException()
    {
        // Arrange
        var supplier = MakeSupplier(id: 8);
        _repoMock
            .Setup(r => r.GetByIdAsync(8, It.IsAny<CancellationToken>()))
            .ReturnsAsync(supplier);
        _repoMock
            .Setup(r => r.HasDependenciesAsync(8, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = CreateService();

        // Act & Assert
        await service.Invoking(s => s.DeleteSupplierAsync(8))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*related ingredients or inventory*");
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "DeleteSupplierAsync")]
    public async Task DeleteSupplierAsync_WhenDeleteReturnsFalse_ReturnsFalse()
    {
        // Arrange
        var supplier = MakeSupplier(id: 9);
        _repoMock
            .Setup(r => r.GetByIdAsync(9, It.IsAny<CancellationToken>()))
            .ReturnsAsync(supplier);
        _repoMock
            .Setup(r => r.HasDependenciesAsync(9, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _repoMock
            .Setup(r => r.DeleteAsync(9, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var service = CreateService();

        // Act
        var result = await service.DeleteSupplierAsync(9);

        // Assert
        result.Should().BeFalse();
    }
}

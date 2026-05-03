using Core.DTO.Tax;
using Core.Entity;
using Core.Exceptions;
using Core.Interface.Repo;
using Core.Interface.Service.Entity;
using Core.Service;
using FluentAssertions;
using Moq;

namespace Tests.Services;

/// <summary>
/// Unit Test — TaxService
/// Code Module : Core/Service/TaxService.cs
/// Methods     : GetAllTaxesAsync, GetTaxByIdAsync, GetDefaultTaxAsync,
///               CreateTaxAsync, UpdateTaxAsync, DeleteTaxAsync
/// </summary>
public class TaxServiceTests
{
    private readonly Mock<ITaxRepository> _taxRepoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();

    private TaxService CreateService() => new(
        _taxRepoMock.Object,
        _uowMock.Object);

    private static Tax MakeTax(
        long id = 1,
        string name = "VAT",
        decimal rate = 10m,
        string type = "EXCLUSIVE",
        bool isActive = true,
        bool isDefault = false) => new()
        {
            TaxId = id,
            TaxName = name,
            TaxRate = rate,
            TaxType = type,
            IsActive = isActive,
            IsDefault = isDefault,
            CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            UpdatedAt = new DateTime(2025, 6, 15, 0, 0, 0, DateTimeKind.Utc)
        };

    // ──────────────────────────────────────────────────────────────
    #region GetAllTaxesAsync
    // ──────────────────────────────────────────────────────────────

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "GetAllTaxesAsync")]
    public async Task GetAllTaxesAsync_OnlyActiveTrue_ReturnsMappedActiveTaxes()
    {
        // Arrange
        var taxes = new List<Tax>
        {
            MakeTax(id: 1, name: "VAT", rate: 10m),
            MakeTax(id: 2, name: "Service Tax", rate: 5m)
        };
        _taxRepoMock
            .Setup(r => r.GetAllAsync(true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(taxes);

        var svc = CreateService();

        // Act
        var result = await svc.GetAllTaxesAsync(true);

        // Assert
        result.Should().HaveCount(2);
        result[0].TaxId.Should().Be(1);
        result[0].TaxName.Should().Be("VAT");
        result[0].TaxRate.Should().Be(10m);
        result[1].TaxId.Should().Be(2);
        result[1].TaxName.Should().Be("Service Tax");
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "GetAllTaxesAsync")]
    public async Task GetAllTaxesAsync_OnlyActiveFalse_ReturnsAllTaxes()
    {
        // Arrange
        var taxes = new List<Tax>
        {
            MakeTax(id: 1, name: "VAT", rate: 10m, isActive: true),
            MakeTax(id: 2, name: "Old Tax", rate: 3m, isActive: false),
            MakeTax(id: 3, name: "Service Tax", rate: 5m, isActive: true)
        };
        _taxRepoMock
            .Setup(r => r.GetAllAsync(false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(taxes);

        var svc = CreateService();

        // Act
        var result = await svc.GetAllTaxesAsync(false);

        // Assert
        result.Should().HaveCount(3);
        result.Select(x => x.TaxName).Should()
            .BeEquivalentTo("VAT", "Old Tax", "Service Tax");
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "GetAllTaxesAsync")]
    public async Task GetAllTaxesAsync_WhenNoTaxesExist_ReturnsEmptyList()
    {
        // Arrange
        _taxRepoMock
            .Setup(r => r.GetAllAsync(true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Tax>());

        var svc = CreateService();

        // Act
        var result = await svc.GetAllTaxesAsync(true);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "GetAllTaxesAsync")]
    public async Task GetAllTaxesAsync_SingleTax_AllFieldsMappedCorrectly()
    {
        // Arrange
        var tax = MakeTax(id: 50, name: "GST", rate: 7.5m, type: "INCLUSIVE",
                          isActive: false, isDefault: true);
        _taxRepoMock
            .Setup(r => r.GetAllAsync(false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Tax> { tax });

        var svc = CreateService();

        // Act
        var result = await svc.GetAllTaxesAsync(false);

        // Assert
        result.Should().HaveCount(1);
        var dto = result[0];
        dto.TaxId.Should().Be(50);
        dto.TaxName.Should().Be("GST");
        dto.TaxRate.Should().Be(7.5m);
        dto.TaxType.Should().Be("INCLUSIVE");
        dto.IsActive.Should().BeFalse();
        dto.IsDefault.Should().BeTrue();
        dto.CreatedAt.Should().NotBeNull();
        dto.UpdatedAt.Should().NotBeNull();
    }

    #endregion

    // ──────────────────────────────────────────────────────────────
    #region GetTaxByIdAsync
    // ──────────────────────────────────────────────────────────────

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "GetTaxByIdAsync")]
    public async Task GetTaxByIdAsync_WhenTaxExists_ReturnsMappedDto()
    {
        // Arrange
        var tax = MakeTax(id: 7, name: "GST", rate: 8m, type: "EXCLUSIVE",
                          isActive: true, isDefault: false);
        _taxRepoMock
            .Setup(r => r.GetByIdAsync(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tax);

        var svc = CreateService();

        // Act
        var result = await svc.GetTaxByIdAsync(7);

        // Assert
        result.Should().NotBeNull();
        result!.TaxId.Should().Be(7);
        result.TaxName.Should().Be("GST");
        result.TaxRate.Should().Be(8m);
        result.TaxType.Should().Be("EXCLUSIVE");
        result.IsActive.Should().BeTrue();
        result.IsDefault.Should().BeFalse();
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "GetTaxByIdAsync")]
    public async Task GetTaxByIdAsync_WhenTaxExists_AllFieldsMapped()
    {
        // Arrange
        var tax = MakeTax(id: 100, name: "Import Duty", rate: 15.75m,
                          type: "INCLUSIVE", isActive: false, isDefault: true);
        _taxRepoMock
            .Setup(r => r.GetByIdAsync(100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tax);

        var svc = CreateService();

        // Act
        var result = await svc.GetTaxByIdAsync(100);

        // Assert
        result.Should().NotBeNull();
        result!.TaxId.Should().Be(100);
        result.TaxName.Should().Be("Import Duty");
        result.TaxRate.Should().Be(15.75m);
        result.TaxType.Should().Be("INCLUSIVE");
        result.IsActive.Should().BeFalse();
        result.IsDefault.Should().BeTrue();
        result.CreatedAt.Should().Be(new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        result.UpdatedAt.Should().Be(new DateTime(2025, 6, 15, 0, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "GetTaxByIdAsync")]
    public async Task GetTaxByIdAsync_WhenTaxNotFound_ReturnsNull()
    {
        // Arrange
        _taxRepoMock
            .Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Tax?)null);

        var svc = CreateService();

        // Act
        var result = await svc.GetTaxByIdAsync(999);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "GetTaxByIdAsync")]
    public async Task GetTaxByIdAsync_WhenIdIsZero_ReturnsNull()
    {
        // Arrange
        _taxRepoMock
            .Setup(r => r.GetByIdAsync(0, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Tax?)null);

        var svc = CreateService();

        // Act
        var result = await svc.GetTaxByIdAsync(0);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    // ──────────────────────────────────────────────────────────────
    #region GetDefaultTaxAsync
    // ──────────────────────────────────────────────────────────────

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "GetDefaultTaxAsync")]
    public async Task GetDefaultTaxAsync_WhenDefaultExists_ReturnsDto()
    {
        // Arrange
        var tax = MakeTax(id: 3, name: "Default VAT", rate: 10m, isDefault: true);
        _taxRepoMock
            .Setup(r => r.GetDefaultTaxAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(tax);

        var svc = CreateService();

        // Act
        var result = await svc.GetDefaultTaxAsync();

        // Assert
        result.Should().NotBeNull();
        result!.TaxId.Should().Be(3);
        result.TaxName.Should().Be("Default VAT");
        result.TaxRate.Should().Be(10m);
        result.IsDefault.Should().BeTrue();
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "GetDefaultTaxAsync")]
    public async Task GetDefaultTaxAsync_WhenDefaultExists_AllFieldsMapped()
    {
        // Arrange
        var tax = MakeTax(id: 20, name: "Standard GST", rate: 7m,
                          type: "INCLUSIVE", isActive: true, isDefault: true);
        _taxRepoMock
            .Setup(r => r.GetDefaultTaxAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(tax);

        var svc = CreateService();

        // Act
        var result = await svc.GetDefaultTaxAsync();

        // Assert
        result.Should().NotBeNull();
        result!.TaxId.Should().Be(20);
        result.TaxName.Should().Be("Standard GST");
        result.TaxRate.Should().Be(7m);
        result.TaxType.Should().Be("INCLUSIVE");
        result.IsActive.Should().BeTrue();
        result.IsDefault.Should().BeTrue();
        result.CreatedAt.Should().NotBeNull();
        result.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "GetDefaultTaxAsync")]
    public async Task GetDefaultTaxAsync_WhenNoDefaultExists_ReturnsNull()
    {
        // Arrange
        _taxRepoMock
            .Setup(r => r.GetDefaultTaxAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((Tax?)null);

        var svc = CreateService();

        // Act
        var result = await svc.GetDefaultTaxAsync();

        // Assert
        result.Should().BeNull();
    }

    #endregion

    // ──────────────────────────────────────────────────────────────
    #region CreateTaxAsync
    // ──────────────────────────────────────────────────────────────

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "CreateTaxAsync")]
    public async Task CreateTaxAsync_ValidExclusiveRequest_AddsEntityAndReturnsId()
    {
        // Arrange
        var request = new CreateTaxRequestDTO
        {
            TaxName = "VAT",
            TaxRate = 10m,
            TaxType = "EXCLUSIVE",
            IsActive = true,
            IsDefault = false
        };
        _taxRepoMock
            .Setup(r => r.AddAsync(It.IsAny<Tax>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var svc = CreateService();

        // Act
        var result = await svc.CreateTaxAsync(request);

        // Assert
        result.Should().Be(0); // TaxId defaults to 0 before DB assigns
        _taxRepoMock.Verify(r => r.AddAsync(
            It.Is<Tax>(t =>
                t.TaxName == "VAT" &&
                t.TaxRate == 10m &&
                t.TaxType == "EXCLUSIVE" &&
                t.IsActive == true &&
                t.IsDefault == false &&
                t.CreatedAt != null &&
                t.UpdatedAt != null),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "CreateTaxAsync")]
    public async Task CreateTaxAsync_InclusiveDefaultRequest_AddsCorrectEntity()
    {
        // Arrange
        var request = new CreateTaxRequestDTO
        {
            TaxName = "Service Charge",
            TaxRate = 5m,
            TaxType = "INCLUSIVE",
            IsActive = false,
            IsDefault = true
        };
        _taxRepoMock
            .Setup(r => r.AddAsync(It.IsAny<Tax>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var svc = CreateService();

        // Act
        var result = await svc.CreateTaxAsync(request);

        // Assert
        result.Should().Be(0);
        _taxRepoMock.Verify(r => r.AddAsync(
            It.Is<Tax>(t =>
                t.TaxName == "Service Charge" &&
                t.TaxRate == 5m &&
                t.TaxType == "INCLUSIVE" &&
                t.IsActive == false &&
                t.IsDefault == true),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "CreateTaxAsync")]
    public async Task CreateTaxAsync_TaxRateZero_CreatesSuccessfully()
    {
        // Arrange
        var request = new CreateTaxRequestDTO
        {
            TaxName = "Zero Tax",
            TaxRate = 0m,
            TaxType = "EXCLUSIVE",
            IsActive = true,
            IsDefault = false
        };
        _taxRepoMock
            .Setup(r => r.AddAsync(It.IsAny<Tax>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var svc = CreateService();

        // Act
        var result = await svc.CreateTaxAsync(request);

        // Assert
        result.Should().Be(0);
        _taxRepoMock.Verify(r => r.AddAsync(
            It.Is<Tax>(t => t.TaxRate == 0m && t.TaxName == "Zero Tax"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "CreateTaxAsync")]
    public async Task CreateTaxAsync_TaxRateHighValue_CreatesSuccessfully()
    {
        // Arrange
        var request = new CreateTaxRequestDTO
        {
            TaxName = "Luxury Tax",
            TaxRate = 99.99m,
            TaxType = "EXCLUSIVE",
            IsActive = true,
            IsDefault = false
        };
        _taxRepoMock
            .Setup(r => r.AddAsync(It.IsAny<Tax>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var svc = CreateService();

        // Act
        var result = await svc.CreateTaxAsync(request);

        // Assert
        result.Should().Be(0);
        _taxRepoMock.Verify(r => r.AddAsync(
            It.Is<Tax>(t => t.TaxRate == 99.99m && t.TaxName == "Luxury Tax"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    // ──────────────────────────────────────────────────────────────
    #region UpdateTaxAsync
    // ──────────────────────────────────────────────────────────────

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "UpdateTaxAsync")]
    public async Task UpdateTaxAsync_AllFieldsProvided_UpdatesAllFields()
    {
        // Arrange
        var tax = MakeTax(id: 11, name: "Old Tax", rate: 5m,
                          type: "INCLUSIVE", isActive: false, isDefault: false);
        var request = new UpdateTaxRequestDTO
        {
            TaxName = "Updated Tax",
            TaxRate = 12m,
            TaxType = "EXCLUSIVE",
            IsActive = true,
            IsDefault = true
        };
        _taxRepoMock
            .Setup(r => r.GetByIdAsync(11, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tax);
        _taxRepoMock
            .Setup(r => r.UpdateAsync(It.IsAny<Tax>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var svc = CreateService();

        // Act
        await svc.UpdateTaxAsync(11, request);

        // Assert
        _taxRepoMock.Verify(r => r.UpdateAsync(
            It.Is<Tax>(t =>
                t.TaxId == 11 &&
                t.TaxName == "Updated Tax" &&
                t.TaxRate == 12m &&
                t.TaxType == "EXCLUSIVE" &&
                t.IsActive == true &&
                t.IsDefault == true),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "UpdateTaxAsync")]
    public async Task UpdateTaxAsync_PartialUpdate_OnlyChangesProvidedFields()
    {
        // Arrange
        var tax = MakeTax(id: 15, name: "Original", rate: 8m,
                          type: "EXCLUSIVE", isActive: true, isDefault: false);
        var request = new UpdateTaxRequestDTO
        {
            TaxName = "Renamed Tax",
            TaxRate = 12.5m
            // TaxType, IsActive, IsDefault are null → not updated
        };
        _taxRepoMock
            .Setup(r => r.GetByIdAsync(15, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tax);
        _taxRepoMock
            .Setup(r => r.UpdateAsync(It.IsAny<Tax>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var svc = CreateService();

        // Act
        await svc.UpdateTaxAsync(15, request);

        // Assert
        _taxRepoMock.Verify(r => r.UpdateAsync(
            It.Is<Tax>(t =>
                t.TaxName == "Renamed Tax" &&
                t.TaxRate == 12.5m &&
                t.TaxType == "EXCLUSIVE" &&  // unchanged
                t.IsActive == true &&         // unchanged
                t.IsDefault == false),        // unchanged
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "UpdateTaxAsync")]
    public async Task UpdateTaxAsync_OnlyIsActive_ChangesIsActiveOnly()
    {
        // Arrange
        var tax = MakeTax(id: 25, name: "GST", rate: 7m,
                          type: "INCLUSIVE", isActive: true, isDefault: true);
        var request = new UpdateTaxRequestDTO { IsActive = false };
        _taxRepoMock
            .Setup(r => r.GetByIdAsync(25, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tax);
        _taxRepoMock
            .Setup(r => r.UpdateAsync(It.IsAny<Tax>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var svc = CreateService();

        // Act
        await svc.UpdateTaxAsync(25, request);

        // Assert
        _taxRepoMock.Verify(r => r.UpdateAsync(
            It.Is<Tax>(t =>
                t.TaxName == "GST" &&       // unchanged
                t.TaxRate == 7m &&           // unchanged
                t.TaxType == "INCLUSIVE" &&   // unchanged
                t.IsActive == false &&        // changed
                t.IsDefault == true),         // unchanged
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "UpdateTaxAsync")]
    public async Task UpdateTaxAsync_TaxNotFound_ThrowsNotFoundException()
    {
        // Arrange
        _taxRepoMock
            .Setup(r => r.GetByIdAsync(404, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Tax?)null);

        var svc = CreateService();

        // Act & Assert
        await svc.Invoking(s => s.UpdateTaxAsync(404, new UpdateTaxRequestDTO
            {
                TaxName = "Irrelevant"
            }))
            .Should().ThrowAsync<NotFoundException>()
            .WithMessage("*Tax with id 404 not found*");
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "UpdateTaxAsync")]
    public async Task UpdateTaxAsync_MaxIdNotFound_ThrowsNotFoundException()
    {
        // Arrange
        long maxId = long.MaxValue;
        _taxRepoMock
            .Setup(r => r.GetByIdAsync(maxId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Tax?)null);

        var svc = CreateService();

        // Act & Assert
        await svc.Invoking(s => s.UpdateTaxAsync(maxId, new UpdateTaxRequestDTO
            {
                TaxRate = 5m
            }))
            .Should().ThrowAsync<NotFoundException>()
            .WithMessage($"*Tax with id {maxId} not found*");
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "UpdateTaxAsync")]
    public async Task UpdateTaxAsync_AllFieldsNull_OnlyUpdatesTimestamp()
    {
        // Arrange
        var tax = MakeTax(id: 30, name: "GST", rate: 7m,
                          type: "EXCLUSIVE", isActive: true, isDefault: false);
        var originalUpdatedAt = tax.UpdatedAt;
        var request = new UpdateTaxRequestDTO(); // all fields null
        _taxRepoMock
            .Setup(r => r.GetByIdAsync(30, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tax);
        _taxRepoMock
            .Setup(r => r.UpdateAsync(It.IsAny<Tax>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var svc = CreateService();

        // Act
        await svc.UpdateTaxAsync(30, request);

        // Assert
        _taxRepoMock.Verify(r => r.UpdateAsync(
            It.Is<Tax>(t =>
                t.TaxName == "GST" &&           // unchanged
                t.TaxRate == 7m &&              // unchanged
                t.TaxType == "EXCLUSIVE" &&     // unchanged
                t.IsActive == true &&           // unchanged
                t.IsDefault == false &&         // unchanged
                t.UpdatedAt != originalUpdatedAt), // timestamp refreshed
            It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    // ──────────────────────────────────────────────────────────────
    #region DeleteTaxAsync
    // ──────────────────────────────────────────────────────────────

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "DeleteTaxAsync")]
    public async Task DeleteTaxAsync_ValidId_CallsRepositoryDelete()
    {
        // Arrange
        _taxRepoMock
            .Setup(r => r.DeleteAsync(21, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var svc = CreateService();

        // Act
        await svc.DeleteTaxAsync(21);

        // Assert
        _taxRepoMock.Verify(
            r => r.DeleteAsync(21, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "DeleteTaxAsync")]
    public async Task DeleteTaxAsync_IdZero_CallsRepositoryWithZero()
    {
        // Arrange
        _taxRepoMock
            .Setup(r => r.DeleteAsync(0, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var svc = CreateService();

        // Act
        await svc.DeleteTaxAsync(0);

        // Assert
        _taxRepoMock.Verify(
            r => r.DeleteAsync(0, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "DeleteTaxAsync")]
    public async Task DeleteTaxAsync_MaxId_CallsRepositoryWithMaxId()
    {
        // Arrange
        long maxId = long.MaxValue;
        _taxRepoMock
            .Setup(r => r.DeleteAsync(maxId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var svc = CreateService();

        // Act
        await svc.DeleteTaxAsync(maxId);

        // Assert
        _taxRepoMock.Verify(
            r => r.DeleteAsync(maxId, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion
}

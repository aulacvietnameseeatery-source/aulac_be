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
/// Unit Test - TaxService
/// Code Module : Core/Service/TaxService.cs
/// Method      : GetAllTaxesAsync, GetTaxByIdAsync, GetDefaultTaxAsync, CreateTaxAsync, UpdateTaxAsync, DeleteTaxAsync
/// Created By  : Automation
/// Executed By : Test Runner
/// Test Req.   : Test tax mapping, create/update behavior, not-found handling, and delete passthrough.
/// </summary>
public class TaxServiceTests
{
    private readonly Mock<ITaxRepository> _taxRepositoryMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();

    private TaxService CreateService() => new(
        _taxRepositoryMock.Object,
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
        CreatedAt = DateTime.UtcNow.AddDays(-1),
        UpdatedAt = DateTime.UtcNow
    };

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "GetAllTaxesAsync")]
    public async Task GetAllTaxesAsync_WhenCalled_ReturnsMappedTaxes()
    {
        var taxes = new List<Tax>
        {
            MakeTax(id: 1, name: "VAT", rate: 10m),
            MakeTax(id: 2, name: "Service Tax", rate: 5m)
        };

        _taxRepositoryMock
            .Setup(r => r.GetAllAsync(true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(taxes);

        var service = CreateService();

        var result = await service.GetAllTaxesAsync(true, CancellationToken.None);

        result.Should().HaveCount(2);
        result.Select(x => x.TaxName).Should().BeEquivalentTo(["VAT", "Service Tax"]);
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "GetTaxByIdAsync")]
    public async Task GetTaxByIdAsync_WhenTaxExists_ReturnsDto()
    {
        var tax = MakeTax(id: 7, name: "GST", rate: 8m);

        _taxRepositoryMock
            .Setup(r => r.GetByIdAsync(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tax);

        var service = CreateService();

        var result = await service.GetTaxByIdAsync(7, CancellationToken.None);

        result.Should().NotBeNull();
        result!.TaxId.Should().Be(7);
        result.TaxName.Should().Be("GST");
        result.TaxRate.Should().Be(8m);
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "GetTaxByIdAsync")]
    public async Task GetTaxByIdAsync_WhenTaxNotFound_ReturnsNull()
    {
        _taxRepositoryMock
            .Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Tax?)null);

        var service = CreateService();

        var result = await service.GetTaxByIdAsync(99, CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "GetDefaultTaxAsync")]
    public async Task GetDefaultTaxAsync_WhenDefaultExists_ReturnsDto()
    {
        var tax = MakeTax(id: 3, name: "Default Tax", rate: 7m, isDefault: true);

        _taxRepositoryMock
            .Setup(r => r.GetDefaultTaxAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(tax);

        var service = CreateService();

        var result = await service.GetDefaultTaxAsync(CancellationToken.None);

        result.Should().NotBeNull();
        result!.IsDefault.Should().BeTrue();
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "CreateTaxAsync")]
    public async Task CreateTaxAsync_WhenValidRequest_ReturnsCreatedTaxId()
    {
        var request = new CreateTaxRequestDTO
        {
            TaxName = "VAT",
            TaxRate = 10m,
            TaxType = "EXCLUSIVE",
            IsActive = true,
            IsDefault = false
        };

        _taxRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Tax>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = CreateService();

        var result = await service.CreateTaxAsync(request, CancellationToken.None);

        result.Should().Be(0);
        _taxRepositoryMock.Verify(r => r.AddAsync(
            It.Is<Tax>(t =>
                t.TaxName == "VAT" &&
                t.TaxRate == 10m &&
                t.TaxType == "EXCLUSIVE" &&
                t.IsActive &&
                !t.IsDefault),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "UpdateTaxAsync")]
    public async Task UpdateTaxAsync_WhenTaxExists_UpdatesProvidedFields()
    {
        var tax = MakeTax(id: 11, name: "Old Tax", rate: 5m, type: "INCLUSIVE", isActive: false, isDefault: false);
        var request = new UpdateTaxRequestDTO
        {
            TaxName = "Updated Tax",
            TaxRate = 12m,
            TaxType = "EXCLUSIVE",
            IsActive = true,
            IsDefault = true
        };

        _taxRepositoryMock
            .Setup(r => r.GetByIdAsync(11, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tax);

        _taxRepositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<Tax>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = CreateService();

        await service.UpdateTaxAsync(11, request, CancellationToken.None);

        _taxRepositoryMock.Verify(r => r.UpdateAsync(
            It.Is<Tax>(t =>
                t.TaxId == 11 &&
                t.TaxName == "Updated Tax" &&
                t.TaxRate == 12m &&
                t.TaxType == "EXCLUSIVE" &&
                t.IsActive &&
                t.IsDefault),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "UpdateTaxAsync")]
    public async Task UpdateTaxAsync_WhenTaxNotFound_ThrowsNotFoundException()
    {
        _taxRepositoryMock
            .Setup(r => r.GetByIdAsync(404, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Tax?)null);

        var service = CreateService();

        await service.Invoking(s => s.UpdateTaxAsync(404, new UpdateTaxRequestDTO
        {
            TaxName = "Name"
        }, CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>()
            .WithMessage("*Tax with id 404 not found.*");
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "DeleteTaxAsync")]
    public async Task DeleteTaxAsync_WhenCalled_DeletesTax()
    {
        _taxRepositoryMock
            .Setup(r => r.DeleteAsync(21, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = CreateService();

        await service.DeleteTaxAsync(21, CancellationToken.None);

        _taxRepositoryMock.Verify(r => r.DeleteAsync(21, It.IsAny<CancellationToken>()), Times.Once);
    }
}

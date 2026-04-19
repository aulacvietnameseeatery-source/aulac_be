using Core.DTO.Customer;
using Core.DTO.General;
using Core.Entity;
using Core.Exceptions;
using Core.Interface.Repo;
using Core.Service;
using FluentAssertions;
using Moq;

namespace Tests.Services;

/// <summary>
/// Unit Test — CustomerService
/// Code Module : Core/Service/CustomerService.cs
/// Method      : GetCustomersAsync, GetByPhoneAsync, GetByIdAsync, FindOrCreateCustomerIdAsync,
///               ResolveCustomerAsync, CreateCustomerAsync, UpdateCustomerAsync, DeleteCustomerAsync,
///               GetCustomerDetailAsync, GetCustomerOrdersAsync, GetCustomerOrderDetailAsync,
///               GetGuestCustomerIdAsync, SearchByPhoneAsync
/// Created By  : quantm
/// Executed By : quantm
/// Test Req.   : Verify customer management business logic including listing, lookup by phone/ID,
///               creating/updating/deleting customers, resolving customers for orders,
///               guest customer handling, and phone search functionality.
/// </summary>
public class CustomerServiceTests
{
    // ── Mocks ──
    private readonly Mock<ICustomerRepository> _repoMock = new();

    // ── Factory ──
    private CustomerService CreateService() => new(_repoMock.Object);

    // ── Helpers ──
    private static Customer MakeCustomer(
        long id = 1,
        string phone = "0901234567",
        string? fullName = "Nguyen Van A",
        string? email = "test@example.com",
        bool isMember = true,
        int loyaltyPoints = 100) => new()
        {
            CustomerId = id,
            Phone = phone,
            FullName = fullName,
            Email = email,
            IsMember = isMember,
            LoyaltyPoints = loyaltyPoints,
            CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        };

    private static Customer MakeGuestCustomer(long id = 999) => new()
    {
        CustomerId = id,
        Phone = "GUEST",
        FullName = "Guest Customer",
        Email = null,
        IsMember = false,
        LoyaltyPoints = 0,
        CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
    };

    // ══════════════════════════════════════════════════════════════════
    // GetCustomersAsync
    // ══════════════════════════════════════════════════════════════════

    #region GetCustomersAsync

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "GetCustomersAsync")]
    public async Task GetCustomersAsync_WhenValidQuery_ReturnsPagedResult()
    {
        // Arrange
        var query = new CustomerListQueryDTO { PageIndex = 1, PageSize = 10 };
        var expected = new PagedResultDTO<CustomerListDTO>
        {
            PageData = new List<CustomerListDTO>
            {
                new() { CustomerId = 1, Phone = "0901234567", FullName = "Nguyen Van A" }
            },
            PageIndex = 1,
            PageSize = 10,
            TotalCount = 1
        };
        _repoMock.Setup(r => r.GetCustomersAsync(query, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(expected);
        var service = CreateService();

        // Act
        var result = await service.GetCustomersAsync(query, CancellationToken.None);

        // Assert
        result.Should().BeSameAs(expected);
        _repoMock.Verify(r => r.GetCustomersAsync(query, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "GetCustomersAsync")]
    public async Task GetCustomersAsync_WhenNoCustomersExist_ReturnsEmptyPage()
    {
        // Arrange
        var query = new CustomerListQueryDTO { PageIndex = 1, PageSize = 10 };
        var expected = new PagedResultDTO<CustomerListDTO>
        {
            PageData = new List<CustomerListDTO>(),
            PageIndex = 1,
            PageSize = 10,
            TotalCount = 0
        };
        _repoMock.Setup(r => r.GetCustomersAsync(query, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(expected);
        var service = CreateService();

        // Act
        var result = await service.GetCustomersAsync(query, CancellationToken.None);

        // Assert
        result.PageData.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    #endregion

    // ══════════════════════════════════════════════════════════════════
    // GetByPhoneAsync
    // ══════════════════════════════════════════════════════════════════

    #region GetByPhoneAsync

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "GetByPhoneAsync")]
    public async Task GetByPhoneAsync_WhenCustomerExists_ReturnsCustomerDto()
    {
        // Arrange
        var customer = MakeCustomer();
        _repoMock.Setup(r => r.GetByPhoneAsync("0901234567"))
                 .ReturnsAsync(customer);
        var service = CreateService();

        // Act
        var result = await service.GetByPhoneAsync("0901234567");

        // Assert
        result.Should().NotBeNull();
        result!.CustomerId.Should().Be(1);
        result.Phone.Should().Be("0901234567");
        result.FullName.Should().Be("Nguyen Van A");
        result.Email.Should().Be("test@example.com");
        result.IsMember.Should().BeTrue();
        result.LoyaltyPoints.Should().Be(100);
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "GetByPhoneAsync")]
    public async Task GetByPhoneAsync_WhenCustomerNotFound_ReturnsNull()
    {
        // Arrange
        _repoMock.Setup(r => r.GetByPhoneAsync("0000000000"))
                 .ReturnsAsync((Customer?)null);
        var service = CreateService();

        // Act
        var result = await service.GetByPhoneAsync("0000000000");

        // Assert
        result.Should().BeNull();
    }

    #endregion

    // ══════════════════════════════════════════════════════════════════
    // GetByIdAsync
    // ══════════════════════════════════════════════════════════════════

    #region GetByIdAsync

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "GetByIdAsync")]
    public async Task GetByIdAsync_WhenCustomerExists_ReturnsCustomerDto()
    {
        // Arrange
        var customer = MakeCustomer(id: 5);
        _repoMock.Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(customer);
        var service = CreateService();

        // Act
        var result = await service.GetByIdAsync(5);

        // Assert
        result.Should().NotBeNull();
        result!.CustomerId.Should().Be(5);
        result.Phone.Should().Be("0901234567");
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "GetByIdAsync")]
    public async Task GetByIdAsync_WhenCustomerNotFound_ReturnsNull()
    {
        // Arrange
        _repoMock.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((Customer?)null);
        var service = CreateService();

        // Act
        var result = await service.GetByIdAsync(999);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    // ══════════════════════════════════════════════════════════════════
    // FindOrCreateCustomerIdAsync
    // ══════════════════════════════════════════════════════════════════

    #region FindOrCreateCustomerIdAsync

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "FindOrCreateCustomerIdAsync")]
    public async Task FindOrCreateCustomerIdAsync_WhenCalled_ReturnsCustomerId()
    {
        // Arrange
        var customer = MakeCustomer(id: 10);
        _repoMock.Setup(r => r.FindOrCreateAsync("0901234567", "Test", "test@test.com", It.IsAny<CancellationToken>()))
                 .ReturnsAsync(customer);
        var service = CreateService();

        // Act
        var result = await service.FindOrCreateCustomerIdAsync("0901234567", "Test", "test@test.com");

        // Assert
        result.Should().Be(10);
        _repoMock.Verify(r => r.FindOrCreateAsync("0901234567", "Test", "test@test.com", It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    // ══════════════════════════════════════════════════════════════════
    // ResolveCustomerAsync
    // ══════════════════════════════════════════════════════════════════

    #region ResolveCustomerAsync

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "ResolveCustomerAsync")]
    public async Task ResolveCustomerAsync_WhenCustomerDtoIsNull_ReturnsGuestId()
    {
        // Arrange
        var guest = MakeGuestCustomer();
        _repoMock.Setup(r => r.GetGuestCustomerAsync(It.IsAny<CancellationToken>()))
                 .ReturnsAsync(guest);
        var service = CreateService();

        // Act
        var result = await service.ResolveCustomerAsync(null, CancellationToken.None);

        // Assert
        result.Should().Be(999);
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "ResolveCustomerAsync")]
    public async Task ResolveCustomerAsync_WhenCustomerIdProvided_ReturnsExistingCustomerId()
    {
        // Arrange
        var customer = MakeCustomer(id: 5, fullName: "Old Name", email: "old@test.com");
        _repoMock.Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(customer);
        var dto = new OrderCustomerDto { CustomerId = 5 };
        var service = CreateService();

        // Act
        var result = await service.ResolveCustomerAsync(dto, CancellationToken.None);

        // Assert
        result.Should().Be(5);
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "ResolveCustomerAsync")]
    public async Task ResolveCustomerAsync_WhenCustomerIdProvidedWithNewName_UpdatesAndReturnsId()
    {
        // Arrange
        var customer = MakeCustomer(id: 5, fullName: "Old Name", email: "old@test.com");
        _repoMock.Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(customer);
        var dto = new OrderCustomerDto { CustomerId = 5, FullName = "New Name", Email = "new@test.com" };
        var service = CreateService();

        // Act
        var result = await service.ResolveCustomerAsync(dto, CancellationToken.None);

        // Assert
        result.Should().Be(5);
        customer.FullName.Should().Be("New Name");
        customer.Email.Should().Be("new@test.com");
        _repoMock.Verify(r => r.UpdateAsync(customer, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "ResolveCustomerAsync")]
    public async Task ResolveCustomerAsync_WhenCustomerIdNotFound_ThrowsNotFoundException()
    {
        // Arrange
        _repoMock.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((Customer?)null);
        var dto = new OrderCustomerDto { CustomerId = 999 };
        var service = CreateService();

        // Act
        Func<Task> act = () => service.ResolveCustomerAsync(dto, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
                 .WithMessage("Customer not found.");
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "ResolveCustomerAsync")]
    public async Task ResolveCustomerAsync_WhenPhoneMatchesExisting_ReturnsExistingId()
    {
        // Arrange
        var existing = MakeCustomer(id: 7, phone: "0912345678");
        _repoMock.Setup(r => r.GetByPhoneAsync("0912345678"))
                 .ReturnsAsync(existing);
        var dto = new OrderCustomerDto { Phone = "0912345678" };
        var service = CreateService();

        // Act
        var result = await service.ResolveCustomerAsync(dto, CancellationToken.None);

        // Assert
        result.Should().Be(7);
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "ResolveCustomerAsync")]
    public async Task ResolveCustomerAsync_WhenPhoneMatchesExistingWithNewInfo_UpdatesAndReturnsId()
    {
        // Arrange
        var existing = MakeCustomer(id: 7, phone: "0912345678", fullName: "Old", email: "old@test.com");
        _repoMock.Setup(r => r.GetByPhoneAsync("0912345678"))
                 .ReturnsAsync(existing);
        var dto = new OrderCustomerDto { Phone = "0912345678", FullName = "Updated", Email = "updated@test.com" };
        var service = CreateService();

        // Act
        var result = await service.ResolveCustomerAsync(dto, CancellationToken.None);

        // Assert
        result.Should().Be(7);
        existing.FullName.Should().Be("Updated");
        existing.Email.Should().Be("updated@test.com");
        _repoMock.Verify(r => r.UpdateAsync(existing, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "ResolveCustomerAsync")]
    public async Task ResolveCustomerAsync_WhenPhoneNotFoundCreatesNew_ReturnsNewId()
    {
        // Arrange
        _repoMock.Setup(r => r.GetByPhoneAsync("0999999999"))
                 .ReturnsAsync((Customer?)null);
        _repoMock.Setup(r => r.AddAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask)
                 .Callback<Customer, CancellationToken>((c, _) => c.CustomerId = 50);
        var dto = new OrderCustomerDto { Phone = "0999999999", FullName = "Brand New", Email = "new@test.com" };
        var service = CreateService();

        // Act
        var result = await service.ResolveCustomerAsync(dto, CancellationToken.None);

        // Assert
        result.Should().Be(50);
        _repoMock.Verify(r => r.AddAsync(It.Is<Customer>(c =>
            c.Phone == "0999999999" &&
            c.FullName == "Brand New" &&
            c.Email == "new@test.com" &&
            c.IsMember == true
        ), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "ResolveCustomerAsync")]
    public async Task ResolveCustomerAsync_WhenNoPhoneAndNoId_ReturnsGuestId()
    {
        // Arrange
        var guest = MakeGuestCustomer();
        _repoMock.Setup(r => r.GetGuestCustomerAsync(It.IsAny<CancellationToken>()))
                 .ReturnsAsync(guest);
        var dto = new OrderCustomerDto { Phone = null, FullName = "Some Name" };
        var service = CreateService();

        // Act
        var result = await service.ResolveCustomerAsync(dto, CancellationToken.None);

        // Assert
        result.Should().Be(999);
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "ResolveCustomerAsync")]
    public async Task ResolveCustomerAsync_WhenCustomerIdProvidedButNoChanges_DoesNotCallUpdate()
    {
        // Arrange
        var customer = MakeCustomer(id: 5, fullName: "Same Name", email: "same@test.com");
        _repoMock.Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(customer);
        var dto = new OrderCustomerDto { CustomerId = 5, FullName = "Same Name", Email = "same@test.com" };
        var service = CreateService();

        // Act
        var result = await service.ResolveCustomerAsync(dto, CancellationToken.None);

        // Assert
        result.Should().Be(5);
        _repoMock.Verify(r => r.UpdateAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    // ══════════════════════════════════════════════════════════════════
    // CreateCustomerAsync
    // ══════════════════════════════════════════════════════════════════

    #region CreateCustomerAsync

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "CreateCustomerAsync")]
    public async Task CreateCustomerAsync_WhenValidRequest_CreatesAndReturnsDto()
    {
        // Arrange
        _repoMock.Setup(r => r.GetByPhoneAsync("0901234567"))
                 .ReturnsAsync((Customer?)null);
        _repoMock.Setup(r => r.AddAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask)
                 .Callback<Customer, CancellationToken>((c, _) => c.CustomerId = 20);

        var request = new CreateCustomerRequest
        {
            Phone = "0901234567",
            FullName = "Nguyen Van A",
            Email = "test@example.com",
            IsMember = true
        };
        var service = CreateService();

        // Act
        var result = await service.CreateCustomerAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.CustomerId.Should().Be(20);
        result.Phone.Should().Be("0901234567");
        result.FullName.Should().Be("Nguyen Van A");
        result.Email.Should().Be("test@example.com");
        result.IsMember.Should().BeTrue();
        result.LoyaltyPoints.Should().Be(0);
        _repoMock.Verify(r => r.AddAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "CreateCustomerAsync")]
    public async Task CreateCustomerAsync_WhenPhoneAlreadyExists_ThrowsInvalidOperation()
    {
        // Arrange
        var existing = MakeCustomer(phone: "0901234567");
        _repoMock.Setup(r => r.GetByPhoneAsync("0901234567"))
                 .ReturnsAsync(existing);

        var request = new CreateCustomerRequest { Phone = "0901234567" };
        var service = CreateService();

        // Act
        Func<Task> act = () => service.CreateCustomerAsync(request);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
                 .WithMessage("*already exists*");
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "CreateCustomerAsync")]
    public async Task CreateCustomerAsync_WhenOptionalFieldsAreWhitespace_StoresNull()
    {
        // Arrange
        _repoMock.Setup(r => r.GetByPhoneAsync("0901234567"))
                 .ReturnsAsync((Customer?)null);
        _repoMock.Setup(r => r.AddAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);

        var request = new CreateCustomerRequest
        {
            Phone = "0901234567",
            FullName = "   ",
            Email = "   ",
            IsMember = false
        };
        var service = CreateService();

        // Act
        var result = await service.CreateCustomerAsync(request);

        // Assert
        result.FullName.Should().BeNull();
        result.Email.Should().BeNull();
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "CreateCustomerAsync")]
    public async Task CreateCustomerAsync_WhenPhoneHasLeadingTrailingSpaces_TrimsPhone()
    {
        // Arrange
        _repoMock.Setup(r => r.GetByPhoneAsync("0901234567"))
                 .ReturnsAsync((Customer?)null);
        _repoMock.Setup(r => r.AddAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);

        var request = new CreateCustomerRequest { Phone = "  0901234567  " };
        var service = CreateService();

        // Act
        var result = await service.CreateCustomerAsync(request);

        // Assert
        result.Phone.Should().Be("0901234567");
    }

    #endregion

    // ══════════════════════════════════════════════════════════════════
    // UpdateCustomerAsync
    // ══════════════════════════════════════════════════════════════════

    #region UpdateCustomerAsync

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "UpdateCustomerAsync")]
    public async Task UpdateCustomerAsync_WhenValidRequest_UpdatesAndReturnsDto()
    {
        // Arrange
        var customer = MakeCustomer(id: 1, phone: "0901234567");
        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(customer);
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);

        var request = new UpdateCustomerRequest
        {
            Phone = "0901234567",
            FullName = "Updated Name",
            Email = "updated@test.com",
            IsMember = true
        };
        var service = CreateService();

        // Act
        var result = await service.UpdateCustomerAsync(1, request);

        // Assert
        result.Should().NotBeNull();
        result.CustomerId.Should().Be(1);
        result.FullName.Should().Be("Updated Name");
        result.Email.Should().Be("updated@test.com");
        result.IsMember.Should().BeTrue();
        _repoMock.Verify(r => r.UpdateAsync(customer, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "UpdateCustomerAsync")]
    public async Task UpdateCustomerAsync_WhenCustomerNotFound_ThrowsKeyNotFound()
    {
        // Arrange
        _repoMock.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((Customer?)null);
        var request = new UpdateCustomerRequest { Phone = "0901234567" };
        var service = CreateService();

        // Act
        Func<Task> act = () => service.UpdateCustomerAsync(999, request);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
                 .WithMessage("*was not found*");
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "UpdateCustomerAsync")]
    public async Task UpdateCustomerAsync_WhenPhoneConflictsWithAnother_ThrowsInvalidOperation()
    {
        // Arrange
        var customer = MakeCustomer(id: 1, phone: "0901234567");
        var conflicting = MakeCustomer(id: 2, phone: "0909999999");
        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(customer);
        _repoMock.Setup(r => r.GetByPhoneAsync("0909999999"))
                 .ReturnsAsync(conflicting);

        var request = new UpdateCustomerRequest { Phone = "0909999999" };
        var service = CreateService();

        // Act
        Func<Task> act = () => service.UpdateCustomerAsync(1, request);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
                 .WithMessage("*already exists*");
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "UpdateCustomerAsync")]
    public async Task UpdateCustomerAsync_WhenPhoneChangedToAvailableNumber_UpdatesSuccessfully()
    {
        // Arrange
        var customer = MakeCustomer(id: 1, phone: "0901234567");
        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(customer);
        _repoMock.Setup(r => r.GetByPhoneAsync("0908888888"))
                 .ReturnsAsync((Customer?)null);
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);

        var request = new UpdateCustomerRequest
        {
            Phone = "0908888888",
            FullName = "New Name",
            IsMember = false
        };
        var service = CreateService();

        // Act
        var result = await service.UpdateCustomerAsync(1, request);

        // Assert
        result.Phone.Should().Be("0908888888");
        result.FullName.Should().Be("New Name");
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "UpdateCustomerAsync")]
    public async Task UpdateCustomerAsync_WhenPhoneUnchanged_SkipsUniquenessCheck()
    {
        // Arrange
        var customer = MakeCustomer(id: 1, phone: "0901234567");
        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(customer);
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);

        var request = new UpdateCustomerRequest
        {
            Phone = "0901234567",
            FullName = "Updated",
            IsMember = true
        };
        var service = CreateService();

        // Act
        var result = await service.UpdateCustomerAsync(1, request);

        // Assert
        result.FullName.Should().Be("Updated");
        _repoMock.Verify(r => r.GetByPhoneAsync(It.IsAny<string>()), Times.Never);
    }

    #endregion

    // ══════════════════════════════════════════════════════════════════
    // DeleteCustomerAsync
    // ══════════════════════════════════════════════════════════════════

    #region DeleteCustomerAsync

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "DeleteCustomerAsync")]
    public async Task DeleteCustomerAsync_WhenCustomerExistsNoDependencies_DeletesSuccessfully()
    {
        // Arrange
        var customer = MakeCustomer(id: 1);
        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(customer);
        _repoMock.Setup(r => r.HasOrdersOrReservationsAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(false);
        _repoMock.Setup(r => r.DeleteAsync(customer, It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);
        var service = CreateService();

        // Act
        await service.DeleteCustomerAsync(1);

        // Assert
        _repoMock.Verify(r => r.DeleteAsync(customer, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "DeleteCustomerAsync")]
    public async Task DeleteCustomerAsync_WhenCustomerNotFound_ThrowsKeyNotFound()
    {
        // Arrange
        _repoMock.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((Customer?)null);
        var service = CreateService();

        // Act
        Func<Task> act = () => service.DeleteCustomerAsync(999);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
                 .WithMessage("*was not found*");
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "DeleteCustomerAsync")]
    public async Task DeleteCustomerAsync_WhenCustomerHasDependencies_ThrowsInvalidOperation()
    {
        // Arrange
        var customer = MakeCustomer(id: 1);
        _repoMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(customer);
        _repoMock.Setup(r => r.HasOrdersOrReservationsAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(true);
        var service = CreateService();

        // Act
        Func<Task> act = () => service.DeleteCustomerAsync(1);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
                 .WithMessage("*existing orders or reservations*");
    }

    #endregion

    // ══════════════════════════════════════════════════════════════════
    // GetCustomerDetailAsync
    // ══════════════════════════════════════════════════════════════════

    #region GetCustomerDetailAsync

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "GetCustomerDetailAsync")]
    public async Task GetCustomerDetailAsync_WhenCalled_DelegatesToRepository()
    {
        // Arrange
        var expected = new CustomerDetailDTO();
        _repoMock.Setup(r => r.GetCustomerDetailAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(expected);
        var service = CreateService();

        // Act
        var result = await service.GetCustomerDetailAsync(1, CancellationToken.None);

        // Assert
        result.Should().BeSameAs(expected);
        _repoMock.Verify(r => r.GetCustomerDetailAsync(1, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "GetCustomerDetailAsync")]
    public async Task GetCustomerDetailAsync_WhenNotFound_ReturnsNull()
    {
        // Arrange
        _repoMock.Setup(r => r.GetCustomerDetailAsync(999, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((CustomerDetailDTO?)null);
        var service = CreateService();

        // Act
        var result = await service.GetCustomerDetailAsync(999, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    // ══════════════════════════════════════════════════════════════════
    // GetCustomerOrdersAsync
    // ══════════════════════════════════════════════════════════════════

    #region GetCustomerOrdersAsync

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "GetCustomerOrdersAsync")]
    public async Task GetCustomerOrdersAsync_WhenCalled_DelegatesToRepository()
    {
        // Arrange
        var query = new CustomerOrderQueryDTO();
        var expected = new PagedResultDTO<CustomerOrderDTO>();
        _repoMock.Setup(r => r.GetCustomerOrdersAsync(query, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(expected);
        var service = CreateService();

        // Act
        var result = await service.GetCustomerOrdersAsync(query, CancellationToken.None);

        // Assert
        result.Should().BeSameAs(expected);
    }

    #endregion

    // ══════════════════════════════════════════════════════════════════
    // GetCustomerOrderDetailAsync
    // ══════════════════════════════════════════════════════════════════

    #region GetCustomerOrderDetailAsync

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "GetCustomerOrderDetailAsync")]
    public async Task GetCustomerOrderDetailAsync_WhenCalled_DelegatesToRepository()
    {
        // Arrange
        var expected = new CustomerOrderDetailDTO();
        _repoMock.Setup(r => r.GetCustomerOrderDetailAsync(1, 10, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(expected);
        var service = CreateService();

        // Act
        var result = await service.GetCustomerOrderDetailAsync(1, 10, CancellationToken.None);

        // Assert
        result.Should().BeSameAs(expected);
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "GetCustomerOrderDetailAsync")]
    public async Task GetCustomerOrderDetailAsync_WhenNotFound_ReturnsNull()
    {
        // Arrange
        _repoMock.Setup(r => r.GetCustomerOrderDetailAsync(1, 999, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((CustomerOrderDetailDTO?)null);
        var service = CreateService();

        // Act
        var result = await service.GetCustomerOrderDetailAsync(1, 999, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    // ══════════════════════════════════════════════════════════════════
    // GetGuestCustomerIdAsync
    // ══════════════════════════════════════════════════════════════════

    #region GetGuestCustomerIdAsync

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "GetGuestCustomerIdAsync")]
    public async Task GetGuestCustomerIdAsync_WhenGuestExists_ReturnsExistingId()
    {
        // Arrange
        var guest = MakeGuestCustomer(id: 100);
        _repoMock.Setup(r => r.GetGuestCustomerAsync(It.IsAny<CancellationToken>()))
                 .ReturnsAsync(guest);
        var service = CreateService();

        // Act
        var result = await service.GetGuestCustomerIdAsync(CancellationToken.None);

        // Assert
        result.Should().Be(100);
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "GetGuestCustomerIdAsync")]
    public async Task GetGuestCustomerIdAsync_WhenGuestNotExists_CreatesAndReturnsNewId()
    {
        // Arrange
        _repoMock.Setup(r => r.GetGuestCustomerAsync(It.IsAny<CancellationToken>()))
                 .ReturnsAsync((Customer?)null);
        _repoMock.Setup(r => r.AddAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask)
                 .Callback<Customer, CancellationToken>((c, _) => c.CustomerId = 200);
        var service = CreateService();

        // Act
        var result = await service.GetGuestCustomerIdAsync(CancellationToken.None);

        // Assert
        result.Should().Be(200);
        _repoMock.Verify(r => r.AddAsync(It.Is<Customer>(c =>
            c.Phone == "GUEST" &&
            c.FullName == "Guest Customer" &&
            c.IsMember == false
        ), It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    // ══════════════════════════════════════════════════════════════════
    // SearchByPhoneAsync
    // ══════════════════════════════════════════════════════════════════

    #region SearchByPhoneAsync

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "SearchByPhoneAsync")]
    public async Task SearchByPhoneAsync_WhenMatchesFound_ReturnsMappedDtoList()
    {
        // Arrange
        var customers = new List<Customer>
        {
            MakeCustomer(id: 1, phone: "0901111111"),
            MakeCustomer(id: 2, phone: "0902222222")
        };
        _repoMock.Setup(r => r.SearchByPhoneAsync("090", 10, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(customers);
        var service = CreateService();

        // Act
        var result = await service.SearchByPhoneAsync("090", 10, CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        result[0].CustomerId.Should().Be(1);
        result[0].Phone.Should().Be("0901111111");
        result[1].CustomerId.Should().Be(2);
        result[1].Phone.Should().Be("0902222222");
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "SearchByPhoneAsync")]
    public async Task SearchByPhoneAsync_WhenNoMatches_ReturnsEmptyList()
    {
        // Arrange
        _repoMock.Setup(r => r.SearchByPhoneAsync("0000", 10, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new List<Customer>());
        var service = CreateService();

        // Act
        var result = await service.SearchByPhoneAsync("0000", 10, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }

    #endregion
}

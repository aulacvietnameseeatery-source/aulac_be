using Core.Data;
using Core.DTO.Account;
using Core.DTO.Auth;
using Core.DTO.Email;
using Core.DTO.EmailTemplate;
using Core.DTO.General;
using Core.Entity;
using Core.Exceptions;
using Core.Interface.Repo;
using Core.Interface.Service;
using Core.Interface.Service.Auth;
using Core.Interface.Service.Email;
using Core.Interface.Service.Entity;
using Core.Service;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace Tests.Services;

/// <summary>
/// Unit Test — AccountService account management operations.
/// Code Module : Core/Service/AccountService.cs
/// Methods     : CreateAccountAsync, UpdateAccountAsync, GetAccountDetailAsync,
///               ResetToDefaultPasswordAsync, ChangePasswordAsync, ChangePasswordForSelfAsync,
///               GetAccountByIdAsync, GetAccountsAsync
/// Created By  : quantm
/// Executed By : quantm
/// Test Req.   : Admin creates accounts with auto-generated credentials, updates profiles, views details,
///               resets passwords, and users change their own passwords through validated self-service flows.
/// </summary>
public class AccountServiceTests
{
    // ── Mocks ──
    private readonly Mock<IAccountRepository> _accountRepoMock = new();
    private readonly Mock<IRoleRepository> _roleRepoMock = new();
    private readonly Mock<IPasswordHasher> _passwordHasherMock = new();
    private readonly Mock<IPasswordGenerator> _passwordGeneratorMock = new();
    private readonly Mock<IUsernameGenerator> _usernameGeneratorMock = new();
    private readonly Mock<ISystemSettingService> _systemSettingServiceMock = new();
    private readonly Mock<ILookupResolver> _lookupResolverMock = new();
    private readonly Mock<IEmailQueue> _emailQueueMock = new();
    private readonly Mock<IEmailTemplateService> _emailTemplateServiceMock = new();
    private readonly Mock<IAuthSessionRepository> _sessionRepoMock = new();
    private readonly Mock<ILogger<AccountService>> _loggerMock = new();

    private const uint ActiveStatusId = 1u;
    private const uint InactiveStatusId = 2u;
    private const uint LockedStatusId = 3u;

    // ── Factory ──
    private AccountService CreateService() => new(
        _accountRepoMock.Object,
        _roleRepoMock.Object,
        _passwordHasherMock.Object,
        _passwordGeneratorMock.Object,
        _usernameGeneratorMock.Object,
        _systemSettingServiceMock.Object,
        _lookupResolverMock.Object,
        _emailQueueMock.Object,
        _emailTemplateServiceMock.Object,
        _sessionRepoMock.Object,
        _loggerMock.Object);

    // ── Test Data Helpers ──
    private static StaffAccount MakeActiveAccount(
        long accountId = 1,
        uint statusLvId = ActiveStatusId,
        bool isLocked = false,
        string roleCode = "STAFF") => new()
    {
        AccountId = accountId,
        Username = "nguyenvana",
        FullName = "Nguyễn Văn A",
        Email = "nguyen.van.a@example.com",
        Phone = "0901234567",
        PasswordHash = "hashed_password",
        AccountStatusLvId = statusLvId,
        IsLocked = isLocked,
        RoleId = 1,
        CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        Role = new Role
        {
            RoleId = 1,
            RoleName = roleCode == "ADMIN" ? "Quản trị viên" : "Nhân viên",
            RoleCode = roleCode,
            Permissions = new List<Permission>
            {
                new() { ScreenCode = "DASHBOARD", ActionCode = "VIEW" }
            }
        }
    };

    private static Role MakeRole(long roleId = 1, string roleCode = "STAFF") => new()
    {
        RoleId = roleId,
        RoleName = roleCode == "ADMIN" ? "Quản trị viên" : "Nhân viên",
        RoleCode = roleCode,
        Permissions = new List<Permission>()
    };

    private static EmailTemplateDto MakeEmailTemplate(string templateCode) => new()
    {
        TemplateCode = templateCode,
        TemplateName = templateCode,
        Subject = $"[AuLac] {templateCode}",
        BodyHtml = "Hello {{fullName}}, username: {{username}}, password: {{temporaryPassword}}, default: {{defaultPassword}}"
    };

    private void SetupAccountStatusLookups()
    {
        _lookupResolverMock
            .Setup(r => r.GetIdAsync(
                (ushort)Core.Enum.LookupType.AccountStatus,
                It.Is<System.Enum>(v => v.Equals(AccountStatusCode.ACTIVE)),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ActiveStatusId);

        _lookupResolverMock
            .Setup(r => r.GetIdAsync(
                (ushort)Core.Enum.LookupType.AccountStatus,
                It.Is<System.Enum>(v => v.Equals(AccountStatusCode.INACTIVE)),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(InactiveStatusId);

        _lookupResolverMock
            .Setup(r => r.GetIdAsync(
                (ushort)Core.Enum.LookupType.AccountStatus,
                It.Is<System.Enum>(v => v.Equals(AccountStatusCode.LOCKED)),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(LockedStatusId);
    }

    // ══════════════════════════════════════════════════════════════
    // CreateAccountAsync
    // ══════════════════════════════════════════════════════════════
    #region CreateAccountAsync

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "CreateAccountAsync")]
    public async Task CreateAccountAsync_WhenEmailUniqueAndRoleExists_CreatesAccountAndQueuesEmail()
    {
        // Arrange
        var request = new CreateAccountRequest
        {
            Email = "nguyen.van.a@example.com",
            FullName = "Nguyễn Văn A",
            Phone = "0901234567",
            RoleId = 1
        };

        var createdAccount = MakeActiveAccount(accountId: 10, statusLvId: LockedStatusId, isLocked: true);
        var template = MakeEmailTemplate("ACCOUNT_CREATED");

        _accountRepoMock
            .Setup(r => r.EmailExistsAsync("NGUYEN.VAN.A@EXAMPLE.COM", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _roleRepoMock
            .Setup(r => r.FindByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(MakeRole());
        _usernameGeneratorMock
            .Setup(g => g.GenerateUniqueUsernameAsync("Nguyễn Văn A", It.IsAny<CancellationToken>()))
            .ReturnsAsync("nguyenvana");
        _passwordGeneratorMock
            .Setup(g => g.GenerateTemporaryPassword(It.IsAny<int>(), It.IsAny<bool>()))
            .Returns("TempPass123!");
        _passwordHasherMock
            .Setup(h => h.HashPassword("TempPass123!"))
            .Returns("hashed_temp");
        SetupAccountStatusLookups();
        _accountRepoMock
            .Setup(r => r.CreateAsync(It.IsAny<StaffAccount>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdAccount);
        _emailTemplateServiceMock
            .Setup(s => s.GetByCodeAsync("ACCOUNT_CREATED", It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);
        _emailQueueMock
            .Setup(q => q.EnqueueAsync(It.IsAny<QueuedEmail>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = CreateService();

        // Act
        var result = await service.CreateAccountAsync(request);

        // Assert
        result.AccountId.Should().Be(10);
        result.Username.Should().Be("nguyenvana");
        result.AccountStatus.Should().Be("LOCKED");
        result.TemporaryPasswordSent.Should().BeTrue();
        _accountRepoMock.Verify(
            r => r.CreateAsync(
                It.Is<StaffAccount>(a =>
                    a.Username == "nguyenvana" &&
                    a.PasswordHash == "hashed_temp" &&
                    a.IsLocked == true),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _emailQueueMock.Verify(
            q => q.EnqueueAsync(It.IsAny<QueuedEmail>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "CreateAccountAsync")]
    public async Task CreateAccountAsync_WhenEmailDuplicate_ThrowsConflictException()
    {
        // Arrange
        var request = new CreateAccountRequest
        {
            Email = "existing@example.com",
            FullName = "Test User",
            RoleId = 1
        };

        _accountRepoMock
            .Setup(r => r.EmailExistsAsync("EXISTING@EXAMPLE.COM", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = CreateService();

        // Act
        Func<Task> act = () => service.CreateAccountAsync(request);

        // Assert
        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("*existing@example.com*already registered*");
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "CreateAccountAsync")]
    public async Task CreateAccountAsync_WhenRoleNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var request = new CreateAccountRequest
        {
            Email = "new@example.com",
            FullName = "New User",
            RoleId = 999
        };

        _accountRepoMock
            .Setup(r => r.EmailExistsAsync("NEW@EXAMPLE.COM", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _roleRepoMock
            .Setup(r => r.FindByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Role?)null);

        var service = CreateService();

        // Act
        Func<Task> act = () => service.CreateAccountAsync(request);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("*Role ID 999*not found*");
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "CreateAccountAsync")]
    public async Task CreateAccountAsync_WhenEmailTemplateNotFound_StillCreatesAccountWithEmailSentFalse()
    {
        // Arrange
        var request = new CreateAccountRequest
        {
            Email = "user@example.com",
            FullName = "User One",
            RoleId = 1
        };

        var createdAccount = MakeActiveAccount(accountId: 5, statusLvId: LockedStatusId, isLocked: true);

        _accountRepoMock
            .Setup(r => r.EmailExistsAsync("USER@EXAMPLE.COM", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _roleRepoMock
            .Setup(r => r.FindByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(MakeRole());
        _usernameGeneratorMock
            .Setup(g => g.GenerateUniqueUsernameAsync("User One", It.IsAny<CancellationToken>()))
            .ReturnsAsync("userone");
        _passwordGeneratorMock
            .Setup(g => g.GenerateTemporaryPassword(It.IsAny<int>(), It.IsAny<bool>()))
            .Returns("TempPass123!");
        _passwordHasherMock
            .Setup(h => h.HashPassword("TempPass123!"))
            .Returns("hashed_temp");
        SetupAccountStatusLookups();
        _accountRepoMock
            .Setup(r => r.CreateAsync(It.IsAny<StaffAccount>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdAccount);
        _emailTemplateServiceMock
            .Setup(s => s.GetByCodeAsync("ACCOUNT_CREATED", It.IsAny<CancellationToken>()))
            .ReturnsAsync((EmailTemplateDto?)null);

        var service = CreateService();

        // Act
        var result = await service.CreateAccountAsync(request);

        // Assert
        result.AccountId.Should().Be(5);
        result.TemporaryPasswordSent.Should().BeFalse();
        _emailQueueMock.Verify(
            q => q.EnqueueAsync(It.IsAny<QueuedEmail>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "CreateAccountAsync")]
    public async Task CreateAccountAsync_WhenEmailQueueThrows_StillReturnsSuccessResult()
    {
        // Arrange
        var request = new CreateAccountRequest
        {
            Email = "user2@example.com",
            FullName = "User Two",
            RoleId = 1
        };

        var createdAccount = MakeActiveAccount(accountId: 6, statusLvId: LockedStatusId, isLocked: true);
        var template = MakeEmailTemplate("ACCOUNT_CREATED");

        _accountRepoMock
            .Setup(r => r.EmailExistsAsync("USER2@EXAMPLE.COM", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _roleRepoMock
            .Setup(r => r.FindByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(MakeRole());
        _usernameGeneratorMock
            .Setup(g => g.GenerateUniqueUsernameAsync("User Two", It.IsAny<CancellationToken>()))
            .ReturnsAsync("usertwo");
        _passwordGeneratorMock
            .Setup(g => g.GenerateTemporaryPassword(It.IsAny<int>(), It.IsAny<bool>()))
            .Returns("TempPass456!");
        _passwordHasherMock
            .Setup(h => h.HashPassword("TempPass456!"))
            .Returns("hashed_temp2");
        SetupAccountStatusLookups();
        _accountRepoMock
            .Setup(r => r.CreateAsync(It.IsAny<StaffAccount>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdAccount);
        _emailTemplateServiceMock
            .Setup(s => s.GetByCodeAsync("ACCOUNT_CREATED", It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);
        _emailQueueMock
            .Setup(q => q.EnqueueAsync(It.IsAny<QueuedEmail>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Queue unavailable"));

        var service = CreateService();

        // Act
        var result = await service.CreateAccountAsync(request);

        // Assert
        result.AccountId.Should().Be(6);
        result.TemporaryPasswordSent.Should().BeFalse();
    }

    #endregion

    // ══════════════════════════════════════════════════════════════
    // UpdateAccountAsync
    // ══════════════════════════════════════════════════════════════
    #region UpdateAccountAsync

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "UpdateAccountAsync")]
    public async Task UpdateAccountAsync_WhenFullNameChanges_UpdatesAndReturnsDetail()
    {
        // Arrange
        var account = MakeActiveAccount();
        var request = new UpdateAccountRequest { FullName = "Nguyễn Văn B" };

        _accountRepoMock
            .Setup(r => r.FindByIdWithRoleAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);
        _accountRepoMock
            .Setup(r => r.UpdateAccountAsync(It.IsAny<StaffAccount>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        SetupAccountStatusLookups();

        var service = CreateService();

        // Act
        var result = await service.UpdateAccountAsync(1, request, requestingUserId: 99);

        // Assert
        result.Should().NotBeNull();
        _accountRepoMock.Verify(
            r => r.UpdateAccountAsync(
                It.Is<StaffAccount>(a => a.FullName == "Nguyễn Văn B"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "UpdateAccountAsync")]
    public async Task UpdateAccountAsync_WhenAccountNotFound_ThrowsNotFoundException()
    {
        // Arrange
        _accountRepoMock
            .Setup(r => r.FindByIdWithRoleAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((StaffAccount?)null);

        var service = CreateService();

        // Act
        Func<Task> act = () => service.UpdateAccountAsync(999, new UpdateAccountRequest(), requestingUserId: 1);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("*Account ID 999*not found*");
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "UpdateAccountAsync")]
    public async Task UpdateAccountAsync_WhenNewEmailAlreadyRegistered_ThrowsConflictException()
    {
        // Arrange
        var account = MakeActiveAccount();
        var request = new UpdateAccountRequest { Email = "taken@example.com" };

        _accountRepoMock
            .Setup(r => r.FindByIdWithRoleAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);
        _accountRepoMock
            .Setup(r => r.EmailExistsAsync("TAKEN@EXAMPLE.COM", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = CreateService();

        // Act
        Func<Task> act = () => service.UpdateAccountAsync(1, request, requestingUserId: 1);

        // Assert
        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("*taken@example.com*already registered*");
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "UpdateAccountAsync")]
    public async Task UpdateAccountAsync_WhenNonAdminAttemptsRoleChange_ThrowsForbiddenException()
    {
        // Arrange
        var account = MakeActiveAccount(accountId: 2);
        var requestingUser = MakeActiveAccount(accountId: 10, roleCode: "STAFF");
        var request = new UpdateAccountRequest { RoleId = 3 };

        _accountRepoMock
            .Setup(r => r.FindByIdWithRoleAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);
        _accountRepoMock
            .Setup(r => r.FindByIdWithRoleAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(requestingUser);

        var service = CreateService();

        // Act
        Func<Task> act = () => service.UpdateAccountAsync(2, request, requestingUserId: 10);

        // Assert
        await act.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("*Only administrators*");
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "UpdateAccountAsync")]
    public async Task UpdateAccountAsync_WhenNoFieldsChange_SkipsRepositoryUpdateCall()
    {
        // Arrange
        var account = MakeActiveAccount();
        // All fields null or same as existing
        var request = new UpdateAccountRequest
        {
            FullName = account.FullName,
            Phone = account.Phone
        };

        _accountRepoMock
            .Setup(r => r.FindByIdWithRoleAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);
        SetupAccountStatusLookups();

        var service = CreateService();

        // Act
        var result = await service.UpdateAccountAsync(1, request, requestingUserId: 99);

        // Assert
        result.Should().NotBeNull();
        _accountRepoMock.Verify(
            r => r.UpdateAccountAsync(It.IsAny<StaffAccount>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    // ══════════════════════════════════════════════════════════════
    // GetAccountDetailAsync
    // ══════════════════════════════════════════════════════════════
    #region GetAccountDetailAsync

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "GetAccountDetailAsync")]
    public async Task GetAccountDetailAsync_WhenAccountExists_ReturnsDetailDto()
    {
        // Arrange
        var account = MakeActiveAccount(statusLvId: ActiveStatusId);

        _accountRepoMock
            .Setup(r => r.FindByIdWithRoleAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);
        SetupAccountStatusLookups();

        var service = CreateService();

        // Act
        var result = await service.GetAccountDetailAsync(1);

        // Assert
        result.Should().NotBeNull();
        result!.AccountId.Should().Be(1);
        result.Username.Should().Be("nguyenvana");
        result.AccountStatus.Should().Be("ACTIVE");
        result.Role.Should().NotBeNull();
        result.Role!.RoleCode.Should().Be("STAFF");
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "GetAccountDetailAsync")]
    public async Task GetAccountDetailAsync_WhenAccountNotFound_ReturnsNull()
    {
        // Arrange
        _accountRepoMock
            .Setup(r => r.FindByIdWithRoleAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((StaffAccount?)null);

        var service = CreateService();

        // Act
        var result = await service.GetAccountDetailAsync(999);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "GetAccountDetailAsync")]
    public async Task GetAccountDetailAsync_WhenAccountIdIsZero_ReturnsNull()
    {
        // Arrange
        _accountRepoMock
            .Setup(r => r.FindByIdWithRoleAsync(0, It.IsAny<CancellationToken>()))
            .ReturnsAsync((StaffAccount?)null);

        var service = CreateService();

        // Act
        var result = await service.GetAccountDetailAsync(0);

        // Assert
        result.Should().BeNull();
        _accountRepoMock.Verify(
            r => r.FindByIdWithRoleAsync(0, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    // ══════════════════════════════════════════════════════════════
    // ResetToDefaultPasswordAsync
    // ══════════════════════════════════════════════════════════════
    #region ResetToDefaultPasswordAsync

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "ResetToDefaultPasswordAsync")]
    public async Task ResetToDefaultPasswordAsync_WhenValid_ResetsPasswordLocksAccountAndQueuesEmail()
    {
        // Arrange
        var account = MakeActiveAccount(statusLvId: ActiveStatusId);
        var template = MakeEmailTemplate("DEFAULT_PASSWORD_RESET");

        _systemSettingServiceMock
            .Setup(s => s.GetStringAsync("default_password", It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("DefaultPass123!");
        _accountRepoMock
            .Setup(r => r.FindByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);
        _passwordHasherMock
            .Setup(h => h.HashPassword("DefaultPass123!"))
            .Returns("hashed_default");
        SetupAccountStatusLookups();
        _accountRepoMock
            .Setup(r => r.UpdateAccountAsync(It.IsAny<StaffAccount>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _emailTemplateServiceMock
            .Setup(s => s.GetByCodeAsync("DEFAULT_PASSWORD_RESET", It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);
        _emailQueueMock
            .Setup(q => q.EnqueueAsync(It.IsAny<QueuedEmail>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = CreateService();

        // Act
        var result = await service.ResetToDefaultPasswordAsync(1);

        // Assert
        result.Success.Should().BeTrue();
        result.AccountId.Should().Be(1);
        _accountRepoMock.Verify(
            r => r.UpdateAccountAsync(
                It.Is<StaffAccount>(a => a.PasswordHash == "hashed_default" && a.IsLocked == true),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _emailQueueMock.Verify(
            q => q.EnqueueAsync(It.IsAny<QueuedEmail>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "ResetToDefaultPasswordAsync")]
    public async Task ResetToDefaultPasswordAsync_WhenAccountNotFound_ThrowsNotFoundException()
    {
        // Arrange
        _systemSettingServiceMock
            .Setup(s => s.GetStringAsync("default_password", It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("DefaultPass123!");
        _accountRepoMock
            .Setup(r => r.FindByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((StaffAccount?)null);

        var service = CreateService();

        // Act
        Func<Task> act = () => service.ResetToDefaultPasswordAsync(999);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("*Account*999*not found*");
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "ResetToDefaultPasswordAsync")]
    public async Task ResetToDefaultPasswordAsync_WhenDefaultPasswordNotConfigured_ThrowsInvalidOperationException()
    {
        // Arrange
        _systemSettingServiceMock
            .Setup(s => s.GetStringAsync("default_password", It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        var service = CreateService();

        // Act
        Func<Task> act = () => service.ResetToDefaultPasswordAsync(1);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Default password*not configured*");
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "ResetToDefaultPasswordAsync")]
    public async Task ResetToDefaultPasswordAsync_WhenEmailTemplateNotFound_StillResetsPassword()
    {
        // Arrange
        var account = MakeActiveAccount(statusLvId: ActiveStatusId);

        _systemSettingServiceMock
            .Setup(s => s.GetStringAsync("default_password", It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("DefaultPass123!");
        _accountRepoMock
            .Setup(r => r.FindByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);
        _passwordHasherMock
            .Setup(h => h.HashPassword("DefaultPass123!"))
            .Returns("hashed_default");
        SetupAccountStatusLookups();
        _accountRepoMock
            .Setup(r => r.UpdateAccountAsync(It.IsAny<StaffAccount>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _emailTemplateServiceMock
            .Setup(s => s.GetByCodeAsync("DEFAULT_PASSWORD_RESET", It.IsAny<CancellationToken>()))
            .ReturnsAsync((EmailTemplateDto?)null);

        var service = CreateService();

        // Act
        var result = await service.ResetToDefaultPasswordAsync(1);

        // Assert
        result.Success.Should().BeTrue();
        _accountRepoMock.Verify(
            r => r.UpdateAccountAsync(It.IsAny<StaffAccount>(), It.IsAny<CancellationToken>()),
            Times.Once);
        _emailQueueMock.Verify(
            q => q.EnqueueAsync(It.IsAny<QueuedEmail>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    // ══════════════════════════════════════════════════════════════
    // ChangePasswordAsync
    // ══════════════════════════════════════════════════════════════
    #region ChangePasswordAsync

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "ChangePasswordAsync")]
    public async Task ChangePasswordAsync_WhenLockedAccount_ChangesPasswordAndActivatesAccount()
    {
        // Arrange
        var account = MakeActiveAccount(statusLvId: LockedStatusId, isLocked: true);

        _accountRepoMock
            .Setup(r => r.FindByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);
        _passwordHasherMock
            .Setup(h => h.HashPassword("NewPass123!"))
            .Returns("new_hashed");
        SetupAccountStatusLookups();
        _accountRepoMock
            .Setup(r => r.UpdateAccountAsync(It.IsAny<StaffAccount>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = CreateService();

        // Act
        await service.ChangePasswordAsync(1, "NewPass123!");

        // Assert
        _accountRepoMock.Verify(
            r => r.UpdateAccountAsync(
                It.Is<StaffAccount>(a =>
                    a.PasswordHash == "new_hashed" &&
                    a.IsLocked == false &&
                    a.AccountStatusLvId == ActiveStatusId),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "ChangePasswordAsync")]
    public async Task ChangePasswordAsync_WhenAccountNotFound_ThrowsNotFoundException()
    {
        // Arrange
        _accountRepoMock
            .Setup(r => r.FindByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((StaffAccount?)null);

        var service = CreateService();

        // Act
        Func<Task> act = () => service.ChangePasswordAsync(999, "NewPass123!");

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("*Account*999*not found*");
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "ChangePasswordAsync")]
    public async Task ChangePasswordAsync_WhenPasswordIsEmpty_ThrowsArgumentException()
    {
        // Arrange
        var service = CreateService();

        // Act
        Func<Task> act = () => service.ChangePasswordAsync(1, "   ");

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Password cannot be empty*");
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "ChangePasswordAsync")]
    public async Task ChangePasswordAsync_WhenPasswordIsSevenChars_ThrowsArgumentException()
    {
        // Arrange
        var service = CreateService();

        // Act
        Func<Task> act = () => service.ChangePasswordAsync(1, "Pass12!");

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Password must be at least 8 characters*");
    }

    #endregion

    // ══════════════════════════════════════════════════════════════
    // ChangePasswordForSelfAsync
    // ══════════════════════════════════════════════════════════════
    #region ChangePasswordForSelfAsync

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "ChangePasswordForSelfAsync")]
    public async Task ChangePasswordForSelfAsync_WhenActiveAndCurrentPasswordCorrect_ChangesPassword()
    {
        // Arrange
        var account = MakeActiveAccount(statusLvId: ActiveStatusId, isLocked: false);

        _accountRepoMock
            .Setup(r => r.FindByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);
        _passwordHasherMock
            .Setup(h => h.VerifyPassword("CurrentPass1!", account.PasswordHash))
            .Returns(true);
        _passwordHasherMock
            .Setup(h => h.HashPassword("NewPass123!"))
            .Returns("new_hashed");
        _accountRepoMock
            .Setup(r => r.UpdateAccountAsync(It.IsAny<StaffAccount>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = CreateService();

        // Act
        var result = await service.ChangePasswordForSelfAsync(1, "CurrentPass1!", "NewPass123!");

        // Assert
        result.Should().BeTrue();
        _accountRepoMock.Verify(
            r => r.UpdateAccountAsync(
                It.Is<StaffAccount>(a => a.PasswordHash == "new_hashed"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "ChangePasswordForSelfAsync")]
    public async Task ChangePasswordForSelfAsync_WhenFirstTimeChange_ChangesWithoutCurrentPasswordAndActivates()
    {
        // Arrange
        var account = MakeActiveAccount(statusLvId: LockedStatusId, isLocked: true);

        _accountRepoMock
            .Setup(r => r.FindByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);
        _passwordHasherMock
            .Setup(h => h.HashPassword("NewPass123!"))
            .Returns("new_hashed");
        SetupAccountStatusLookups();
        _accountRepoMock
            .Setup(r => r.UpdateAccountAsync(It.IsAny<StaffAccount>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = CreateService();

        // Act
        var result = await service.ChangePasswordForSelfAsync(1, currentPassword: null, "NewPass123!");

        // Assert
        result.Should().BeTrue();
        _accountRepoMock.Verify(
            r => r.UpdateAccountAsync(
                It.Is<StaffAccount>(a =>
                    a.PasswordHash == "new_hashed" &&
                    a.IsLocked == false &&
                    a.AccountStatusLvId == ActiveStatusId),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "ChangePasswordForSelfAsync")]
    public async Task ChangePasswordForSelfAsync_WhenAccountNotFound_ThrowsNotFoundException()
    {
        // Arrange
        _accountRepoMock
            .Setup(r => r.FindByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((StaffAccount?)null);

        var service = CreateService();

        // Act
        Func<Task> act = () => service.ChangePasswordForSelfAsync(999, "OldPass1!", "NewPass123!");

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("*Account*999*not found*");
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "ChangePasswordForSelfAsync")]
    public async Task ChangePasswordForSelfAsync_WhenCurrentPasswordIsWrong_ThrowsValidationException()
    {
        // Arrange
        var account = MakeActiveAccount(statusLvId: ActiveStatusId, isLocked: false);

        _accountRepoMock
            .Setup(r => r.FindByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);
        _passwordHasherMock
            .Setup(h => h.VerifyPassword("WrongPass!", account.PasswordHash))
            .Returns(false);

        var service = CreateService();

        // Act
        Func<Task> act = () => service.ChangePasswordForSelfAsync(1, "WrongPass!", "NewPass123!");

        // Assert
        await act.Should().ThrowAsync<Core.Exceptions.ValidationException>()
            .WithMessage("*Current password is incorrect*");
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "ChangePasswordForSelfAsync")]
    public async Task ChangePasswordForSelfAsync_WhenActiveAccountAndNoCurrentPassword_ThrowsValidationException()
    {
        // Arrange
        var account = MakeActiveAccount(statusLvId: ActiveStatusId, isLocked: false);

        _accountRepoMock
            .Setup(r => r.FindByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        var service = CreateService();

        // Act
        Func<Task> act = () => service.ChangePasswordForSelfAsync(1, currentPassword: null, "NewPass123!");

        // Assert
        await act.Should().ThrowAsync<Core.Exceptions.ValidationException>()
            .WithMessage("*Current password is required*");
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "ChangePasswordForSelfAsync")]
    public async Task ChangePasswordForSelfAsync_WhenNewPasswordTooShort_ThrowsValidationException()
    {
        // Arrange
        var service = CreateService();

        // Act — 7 characters: boundary just below 8-character minimum
        Func<Task> act = () => service.ChangePasswordForSelfAsync(1, "OldPass1!", "Short7!");

        // Assert
        await act.Should().ThrowAsync<Core.Exceptions.ValidationException>()
            .WithMessage("*Password must be at least 8 characters*");
    }

    #endregion

    // ══════════════════════════════════════════════════════════════
    // GetAccountByIdAsync
    // ══════════════════════════════════════════════════════════════
    #region GetAccountByIdAsync

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "GetAccountByIdAsync")]
    public async Task GetAccountByIdAsync_WhenAccountExistsWithRole_ReturnsAccountDtoWithRole()
    {
        // Arrange
        var account = MakeActiveAccount(statusLvId: ActiveStatusId);

        _accountRepoMock
            .Setup(r => r.FindByIdWithRoleAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        var service = CreateService();

        // Act
        var result = await service.GetAccountByIdAsync(1, includeRole: true);

        // Assert
        result.Should().NotBeNull();
        result!.AccountId.Should().Be(1);
        result.Username.Should().Be("nguyenvana");
        result.Role.Should().NotBeNull();
        result.Role!.RoleCode.Should().Be("STAFF");
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "GetAccountByIdAsync")]
    public async Task GetAccountByIdAsync_WhenAccountNotFound_ReturnsNull()
    {
        // Arrange
        _accountRepoMock
            .Setup(r => r.FindByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((StaffAccount?)null);

        var service = CreateService();

        // Act
        var result = await service.GetAccountByIdAsync(999, includeRole: false);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "GetAccountByIdAsync")]
    public async Task GetAccountByIdAsync_WhenIncludeRoleFalse_ReturnsAccountDtoWithoutRole()
    {
        // Arrange
        var account = MakeActiveAccount(statusLvId: ActiveStatusId);
        // strip role for plain fetch
        account.Role = null!;

        _accountRepoMock
            .Setup(r => r.FindByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        var service = CreateService();

        // Act
        var result = await service.GetAccountByIdAsync(1, includeRole: false);

        // Assert
        result.Should().NotBeNull();
        result!.Role.Should().BeNull();
        _accountRepoMock.Verify(
            r => r.FindByIdWithRoleAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    // ══════════════════════════════════════════════════════════════
    // GetAccountsAsync
    // ══════════════════════════════════════════════════════════════
    #region GetAccountsAsync

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "GetAccountsAsync")]
    public async Task GetAccountsAsync_WhenCalled_DelegatesAndReturnsPagedResult()
    {
        // Arrange
        var query = new AccountListQueryDTO { PageIndex = 1, PageSize = 10 };
        var paged = new PagedResultDTO<AccountListDTO>
        {
            PageData = new List<AccountListDTO>
            {
                new() { AccountId = 1, FullName = "Nguyễn Văn A", RoleId = 1, RoleName = "Nhân viên", AccountStatus = 1, AccountStatusName = "ACTIVE" },
                new() { AccountId = 2, FullName = "Trần Thị B", RoleId = 1, RoleName = "Nhân viên", AccountStatus = 1, AccountStatusName = "ACTIVE" }
            },
            TotalCount = 2,
            PageIndex = 1,
            PageSize = 10
        };

        _accountRepoMock
            .Setup(r => r.GetAccountsAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(paged);

        var service = CreateService();

        // Act
        var result = await service.GetAccountsAsync(query);

        // Assert
        result.Should().NotBeNull();
        result.TotalCount.Should().Be(2);
        result.PageData.Should().HaveCount(2);
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "GetAccountsAsync")]
    public async Task GetAccountsAsync_WhenNoAccountsExist_ReturnsEmptyPagedResult()
    {
        // Arrange
        var query = new AccountListQueryDTO { PageIndex = 1, PageSize = 10 };
        var paged = new PagedResultDTO<AccountListDTO>
        {
            PageData = new List<AccountListDTO>(),
            TotalCount = 0,
            PageIndex = 1,
            PageSize = 10
        };

        _accountRepoMock
            .Setup(r => r.GetAccountsAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(paged);

        var service = CreateService();

        // Act
        var result = await service.GetAccountsAsync(query);

        // Assert
        result.TotalCount.Should().Be(0);
        result.PageData.Should().BeEmpty();
    }

    #endregion
}

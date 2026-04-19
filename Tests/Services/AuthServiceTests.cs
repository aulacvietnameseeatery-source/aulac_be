using Core.Data;
using Core.DTO.Auth;
using Core.DTO.Email;
using Core.DTO.EmailTemplate;
using Core.Entity;
using Core.Interface.Repo;
using Core.Interface.Service;
using Core.Interface.Service.Auth;
using Core.Interface.Service.Email;
using Core.Interface.Service.Entity;
using Core.Service;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Tests.Services;

/// <summary>
/// Unit Test — AuthService authentication workflows.
/// Code Module : Core/Service/AuthService.cs
/// Methods     : LoginAsync, RefreshTokenAsync, LogoutAsync, RequestPasswordResetAsync, ResetPasswordAsync, ValidateSessionAsync
/// Created By  : quantm
/// Executed By : quantm
/// Test Req.   : Staff signs in with credentials, rotates tokens, signs out, requests password reset links,
///                resets passwords, and the system validates active sessions for authenticated requests.
/// </summary>
public class AuthServiceTests
{
    private readonly Mock<ITokenService> _tokenServiceMock = new();
    private readonly Mock<IAuthSessionRepository> _sessionRepoMock = new();
    private readonly Mock<IAccountRepository> _accountRepoMock = new();
    private readonly Mock<IPasswordHasher> _passwordHasherMock = new();
    private readonly Mock<IPasswordResetTokenStore> _tokenStoreMock = new();
    private readonly Mock<IEmailQueue> _emailQueueMock = new();
    private readonly Mock<IEmailTemplateService> _emailTemplateServiceMock = new();
    private readonly Mock<ILookupResolver> _lookupResolverMock = new();
    private readonly Mock<ILogger<AuthService>> _loggerMock = new();

    private readonly IOptions<ForgotPasswordRulesOptions> _forgotOpt =
        Options.Create(new ForgotPasswordRulesOptions
        {
            TokenLengthBytes = 32,
            TokenLifetimeMinutes = 30
        });

    private readonly IOptions<BaseUrlOptions> _baseUrlOpt =
        Options.Create(new BaseUrlOptions
        {
            Client = "https://client.aulac.local",
            Api = "https://api.aulac.local"
        });

    private const uint ActiveStatusId = 1u;
    private const uint InactiveStatusId = 2u;
    private const uint LockedStatusId = 3u;

    private AuthService CreateService() => new(
        _tokenServiceMock.Object,
        _sessionRepoMock.Object,
        _accountRepoMock.Object,
        _passwordHasherMock.Object,
        _tokenStoreMock.Object,
        _emailQueueMock.Object,
        _emailTemplateServiceMock.Object,
        _lookupResolverMock.Object,
        _forgotOpt,
        _baseUrlOpt,
        _loggerMock.Object);

    private static StaffAccount MakeActiveAccount(uint statusLvId = ActiveStatusId, bool isLocked = false) => new()
    {
        AccountId = 1,
        FullName = "Admin User",
        RoleId = 1,
        Username = "admin",
        Email = "ADMIN@EXAMPLE.COM",
        PasswordHash = "hashed_password",
        IsLocked = isLocked,
        AccountStatusLvId = statusLvId,
        Role = new Role
        {
            RoleCode = "ADMIN",
            Permissions = new List<Permission>
            {
                new() { ScreenCode = "DASHBOARD", ActionCode = "VIEW" }
            }
        }
    };

    private static AuthSession MakeSession(long sessionId = 99, long userId = 1) => new()
    {
        SessionId = sessionId,
        UserId = userId,
        TokenHash = "stored_hash",
        ExpiresAt = DateTime.UtcNow.AddDays(7),
        Revoked = false
    };

    private static ClaimsPrincipal MakePrincipal(string? sessionId = "99", string? userId = "1")
    {
        var claims = new List<Claim>();

        if (sessionId != null)
        {
            claims.Add(new Claim("session_id", sessionId));
        }

        if (userId != null)
        {
            claims.Add(new Claim("user_id", userId));
        }

        claims.Add(new Claim(ClaimTypes.Name, "admin"));
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "unit-test"));
    }

    private static PasswordResetTokenRecord MakePasswordResetRecord(
        string rawToken,
        long userId = 1,
        string emailNormalized = "ADMIN@EXAMPLE.COM",
        DateTimeOffset? expiresAt = null)
    {
        return new PasswordResetTokenRecord(
            userId,
            emailNormalized,
            HashPasswordResetToken(rawToken),
            expiresAt ?? DateTimeOffset.UtcNow.AddMinutes(30),
            DateTimeOffset.UtcNow);
    }

    private static string HashPasswordResetToken(string token)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    private void SetupAccountStatusLookups()
    {
        _lookupResolverMock
            .Setup(r => r.GetIdAsync(
                (ushort)Core.Enum.LookupType.AccountStatus,
                It.Is<System.Enum>(value => value.Equals(AccountStatusCode.ACTIVE)),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ActiveStatusId);

        _lookupResolverMock
            .Setup(r => r.GetIdAsync(
                (ushort)Core.Enum.LookupType.AccountStatus,
                It.Is<System.Enum>(value => value.Equals(AccountStatusCode.INACTIVE)),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(InactiveStatusId);

        _lookupResolverMock
            .Setup(r => r.GetIdAsync(
                (ushort)Core.Enum.LookupType.AccountStatus,
                It.Is<System.Enum>(value => value.Equals(AccountStatusCode.LOCKED)),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(LockedStatusId);
    }

    private void SetupTokenAndSession()
    {
        _tokenServiceMock.Setup(t => t.GenerateRefreshToken()).Returns("raw_refresh_token");
        _tokenServiceMock.Setup(t => t.HashToken("raw_refresh_token")).Returns("hashed_refresh_token");
        _tokenServiceMock.Setup(t => t.RefreshTokenLifetime).Returns(TimeSpan.FromDays(7));
        _tokenServiceMock.Setup(t => t.AccessTokenLifetime).Returns(TimeSpan.FromMinutes(15));
        _tokenServiceMock
            .Setup(t => t.GenerateAccessToken(
                It.IsAny<long>(),
                It.IsAny<string>(),
                It.IsAny<long>(),
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<IEnumerable<string>?>()))
            .Returns("access_token_value");

        _sessionRepoMock
            .Setup(s => s.CreateSessionAsync(
                It.IsAny<long>(),
                It.IsAny<string>(),
                It.IsAny<DateTime>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AuthSession { SessionId = 99 });
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "LoginAsync")]
    public async Task LoginAsync_WhenAccountNotFound_ReturnsFailed()
    {
        _accountRepoMock
            .Setup(r => r.FindByUsernameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((StaffAccount?)null);

        _accountRepoMock
            .Setup(r => r.FindByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((StaffAccount?)null);

        var service = CreateService();
        var result = await service.LoginAsync(new LoginRequest("nonexistent_user", "any_password"));

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be("INVALID_CREDENTIALS");
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "LoginAsync")]
    public async Task LoginAsync_WhenPasswordIsWrong_ReturnsFailed()
    {
        var account = MakeActiveAccount();

        _accountRepoMock
            .Setup(r => r.FindByUsernameAsync("admin", It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        _passwordHasherMock
            .Setup(h => h.VerifyPassword("wrong_password", account.PasswordHash))
            .Returns(false);

        var service = CreateService();
        var result = await service.LoginAsync(new LoginRequest("admin", "wrong_password"));

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be("INVALID_CREDENTIALS");
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "LoginAsync")]
    public async Task LoginAsync_WhenFoundByEmail_AndPasswordCorrect_ReturnsSuccess()
    {
        var account = MakeActiveAccount(statusLvId: ActiveStatusId);

        _accountRepoMock
            .Setup(r => r.FindByUsernameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((StaffAccount?)null);

        _accountRepoMock
            .Setup(r => r.FindByEmailAsync("ADMIN@EXAMPLE.COM", It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        _passwordHasherMock
            .Setup(h => h.VerifyPassword("correct_pass", account.PasswordHash))
            .Returns(true);

        SetupAccountStatusLookups();
        SetupTokenAndSession();

        _accountRepoMock
            .Setup(r => r.UpdateLastLoginAsync(account.AccountId, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = CreateService();
        var result = await service.LoginAsync(new LoginRequest("admin@example.com", "correct_pass"));

        result.Success.Should().BeTrue();
        result.RequirePasswordChange.Should().BeFalse();
        result.AccessToken.Should().Be("access_token_value");
        result.RefreshToken.Should().Be("raw_refresh_token");
        result.Username.Should().Be("admin");
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "LoginAsync")]
    public async Task LoginAsync_WhenAccountIsLocked_ReturnsPasswordChangeRequired()
    {
        var account = MakeActiveAccount(statusLvId: LockedStatusId);

        _accountRepoMock
            .Setup(r => r.FindByUsernameAsync("admin", It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        _passwordHasherMock
            .Setup(h => h.VerifyPassword("correct_pass", account.PasswordHash))
            .Returns(true);

        SetupAccountStatusLookups();

        _tokenServiceMock
            .Setup(t => t.GenerateAccessToken(
                It.IsAny<long>(),
                It.IsAny<string>(),
                It.IsAny<long>(),
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<IEnumerable<string>?>()))
            .Returns("temp_access_token");

        _sessionRepoMock
            .Setup(s => s.CreateSessionAsync(
                It.IsAny<long>(),
                "temp_for_password_change",
                It.IsAny<DateTime>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AuthSession { SessionId = 50 });

        var service = CreateService();
        var result = await service.LoginAsync(new LoginRequest("admin", "correct_pass"));

        result.Success.Should().BeTrue();
        result.RequirePasswordChange.Should().BeTrue();
        result.AccessToken.Should().Be("temp_access_token");
        result.RefreshToken.Should().BeNull();
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "LoginAsync")]
    public async Task LoginAsync_WhenCredentialsValid_ReturnsSucceededWithTokens()
    {
        var account = MakeActiveAccount(statusLvId: ActiveStatusId);

        _accountRepoMock
            .Setup(r => r.FindByUsernameAsync("admin", It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        _passwordHasherMock
            .Setup(h => h.VerifyPassword("correct_pass", account.PasswordHash))
            .Returns(true);

        SetupAccountStatusLookups();
        SetupTokenAndSession();

        _accountRepoMock
            .Setup(r => r.UpdateLastLoginAsync(account.AccountId, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = CreateService();
        var result = await service.LoginAsync(new LoginRequest("admin", "correct_pass"));

        result.Success.Should().BeTrue();
        result.RequirePasswordChange.Should().BeFalse();
        result.AccessToken.Should().Be("access_token_value");
        result.RefreshToken.Should().Be("raw_refresh_token");
        result.SessionId.Should().Be(99);
        result.UserId.Should().Be(account.AccountId);
        result.Username.Should().Be("admin");
        result.Roles.Should().Contain("ADMIN");
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "LoginAsync")]
    public async Task LoginAsync_OnSuccess_ShouldCallUpdateLastLogin()
    {
        var account = MakeActiveAccount(statusLvId: ActiveStatusId);

        _accountRepoMock
            .Setup(r => r.FindByUsernameAsync("admin", It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        _passwordHasherMock
            .Setup(h => h.VerifyPassword("correct_pass", account.PasswordHash))
            .Returns(true);

        SetupAccountStatusLookups();
        SetupTokenAndSession();

        _accountRepoMock
            .Setup(r => r.UpdateLastLoginAsync(account.AccountId, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = CreateService();
        await service.LoginAsync(new LoginRequest("admin", "correct_pass"));

        _accountRepoMock.Verify(
            r => r.UpdateLastLoginAsync(account.AccountId, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "LoginAsync")]
    public async Task LoginAsync_WhenPasswordChangeRequired_ShouldNotCallUpdateLastLogin()
    {
        var account = MakeActiveAccount(statusLvId: LockedStatusId);

        _accountRepoMock
            .Setup(r => r.FindByUsernameAsync("admin", It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        _passwordHasherMock
            .Setup(h => h.VerifyPassword("correct_pass", account.PasswordHash))
            .Returns(true);

        SetupAccountStatusLookups();

        _tokenServiceMock
            .Setup(t => t.GenerateAccessToken(
                It.IsAny<long>(),
                It.IsAny<string>(),
                It.IsAny<long>(),
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<IEnumerable<string>?>()))
            .Returns("temp_token");

        _sessionRepoMock
            .Setup(s => s.CreateSessionAsync(
                It.IsAny<long>(),
                "temp_for_password_change",
                It.IsAny<DateTime>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AuthSession { SessionId = 50 });

        var service = CreateService();
        await service.LoginAsync(new LoginRequest("admin", "correct_pass"));

        _accountRepoMock.Verify(
            r => r.UpdateLastLoginAsync(It.IsAny<long>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "LoginAsync")]
    public async Task LoginAsync_WhenAccountIsInactive_ReturnsAccountDeactivated()
    {
        var account = MakeActiveAccount(statusLvId: InactiveStatusId);

        _accountRepoMock
            .Setup(r => r.FindByUsernameAsync("admin", It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        _passwordHasherMock
            .Setup(h => h.VerifyPassword("correct_pass", account.PasswordHash))
            .Returns(true);

        SetupAccountStatusLookups();

        var service = CreateService();
        var result = await service.LoginAsync(new LoginRequest("admin", "correct_pass"));

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be("ACCOUNT_DEACTIVATED");

        _sessionRepoMock.Verify(
            s => s.CreateSessionAsync(
                It.IsAny<long>(),
                It.IsAny<string>(),
                It.IsAny<DateTime>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "LoginAsync")]
    public async Task LoginAsync_WhenUsernameIsMaxLength_ReturnsFailedNotFound()
    {
        var maxLengthUsername = new string('a', 100);

        _accountRepoMock
            .Setup(r => r.FindByUsernameAsync(maxLengthUsername, It.IsAny<CancellationToken>()))
            .ReturnsAsync((StaffAccount?)null);

        _accountRepoMock
            .Setup(r => r.FindByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((StaffAccount?)null);

        var service = CreateService();
        var result = await service.LoginAsync(new LoginRequest(maxLengthUsername, "any_password"));

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be("INVALID_CREDENTIALS");
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "LoginAsync")]
    public async Task LoginAsync_WhenStatusIsExactlyLockedId_ReturnsPasswordChangeRequired()
    {
        var account = MakeActiveAccount(statusLvId: LockedStatusId);

        _accountRepoMock
            .Setup(r => r.FindByUsernameAsync("admin", It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        _passwordHasherMock
            .Setup(h => h.VerifyPassword("correct_pass", account.PasswordHash))
            .Returns(true);

        SetupAccountStatusLookups();

        _tokenServiceMock
            .Setup(t => t.GenerateAccessToken(
                It.IsAny<long>(),
                It.IsAny<string>(),
                It.IsAny<long>(),
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<IEnumerable<string>?>()))
            .Returns("temp_access_token");

        _sessionRepoMock
            .Setup(s => s.CreateSessionAsync(
                It.IsAny<long>(),
                "temp_for_password_change",
                It.IsAny<DateTime>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AuthSession { SessionId = 50 });

        var service = CreateService();
        var result = await service.LoginAsync(new LoginRequest("admin", "correct_pass"));

        result.RequirePasswordChange.Should().BeTrue();
        result.RefreshToken.Should().BeNull();
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "RefreshTokenAsync")]
    public async Task RefreshTokenAsync_WhenAccessTokenIsInvalid_ReturnsInvalidToken()
    {
        _tokenServiceMock
            .Setup(t => t.GetPrincipalFromExpiredToken("expired_access_token"))
            .Returns((ClaimsPrincipal?)null);

        var service = CreateService();
        var result = await service.RefreshTokenAsync(new RefreshTokenRequest("expired_access_token", "refresh_token"));

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be("INVALID_TOKEN");
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "RefreshTokenAsync")]
    public async Task RefreshTokenAsync_WhenSessionClaimMissing_ReturnsInvalidToken()
    {
        _tokenServiceMock
            .Setup(t => t.GetPrincipalFromExpiredToken("expired_access_token"))
            .Returns(MakePrincipal(sessionId: null, userId: "1"));

        var service = CreateService();
        var result = await service.RefreshTokenAsync(new RefreshTokenRequest("expired_access_token", "refresh_token"));

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be("INVALID_TOKEN");
        result.ErrorMessage.Should().Be("Session information not found in token.");
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "RefreshTokenAsync")]
    public async Task RefreshTokenAsync_WhenRefreshTokenIsInvalid_RevokesAllSessions()
    {
        _tokenServiceMock
            .Setup(t => t.GetPrincipalFromExpiredToken("expired_access_token"))
            .Returns(MakePrincipal());

        _tokenServiceMock
            .Setup(t => t.HashToken("stale_refresh_token"))
            .Returns("stale_refresh_hash");

        _sessionRepoMock
            .Setup(s => s.ValidateRefreshTokenAsync(99, "stale_refresh_hash", It.IsAny<CancellationToken>()))
            .ReturnsAsync((AuthSession?)null);

        _sessionRepoMock
            .Setup(s => s.RevokeAllUserSessionsAsync(1, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        var service = CreateService();
        var result = await service.RefreshTokenAsync(new RefreshTokenRequest("expired_access_token", "stale_refresh_token"));

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be("INVALID_REFRESH_TOKEN");

        _sessionRepoMock.Verify(
            s => s.RevokeAllUserSessionsAsync(1, null, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "RefreshTokenAsync")]
    public async Task RefreshTokenAsync_WhenAccountIsLocked_ReturnsAccountUnavailable()
    {
        var account = MakeActiveAccount(isLocked: true);

        _tokenServiceMock
            .Setup(t => t.GetPrincipalFromExpiredToken("expired_access_token"))
            .Returns(MakePrincipal());

        _tokenServiceMock
            .Setup(t => t.HashToken("refresh_token"))
            .Returns("stored_refresh_hash");

        _sessionRepoMock
            .Setup(s => s.ValidateRefreshTokenAsync(99, "stored_refresh_hash", It.IsAny<CancellationToken>()))
            .ReturnsAsync(MakeSession());

        _accountRepoMock
            .Setup(r => r.FindByIdWithRoleAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        _sessionRepoMock
            .Setup(s => s.RevokeSessionAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = CreateService();
        var result = await service.RefreshTokenAsync(new RefreshTokenRequest("expired_access_token", "refresh_token"));

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be("ACCOUNT_UNAVAILABLE");

        _sessionRepoMock.Verify(
            s => s.RevokeSessionAsync(99, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "RefreshTokenAsync")]
    public async Task RefreshTokenAsync_WhenAccountIsInactive_ReturnsAccountDeactivated()
    {
        var account = MakeActiveAccount(statusLvId: InactiveStatusId);

        _tokenServiceMock
            .Setup(t => t.GetPrincipalFromExpiredToken("expired_access_token"))
            .Returns(MakePrincipal());

        _tokenServiceMock
            .Setup(t => t.HashToken("refresh_token"))
            .Returns("stored_refresh_hash");

        _sessionRepoMock
            .Setup(s => s.ValidateRefreshTokenAsync(99, "stored_refresh_hash", It.IsAny<CancellationToken>()))
            .ReturnsAsync(MakeSession());

        _accountRepoMock
            .Setup(r => r.FindByIdWithRoleAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        _sessionRepoMock
            .Setup(s => s.RevokeAllUserSessionsAsync(1, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        SetupAccountStatusLookups();

        var service = CreateService();
        var result = await service.RefreshTokenAsync(new RefreshTokenRequest("expired_access_token", "refresh_token"));

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be("ACCOUNT_DEACTIVATED");

        _sessionRepoMock.Verify(
            s => s.RevokeAllUserSessionsAsync(1, null, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "RefreshTokenAsync")]
    public async Task RefreshTokenAsync_WhenRequestIsValid_RotatesTokensAndReturnsSuccess()
    {
        var account = MakeActiveAccount(statusLvId: ActiveStatusId);

        _tokenServiceMock
            .Setup(t => t.GetPrincipalFromExpiredToken("expired_access_token"))
            .Returns(MakePrincipal());

        _tokenServiceMock
            .Setup(t => t.HashToken("old_refresh_token"))
            .Returns("old_refresh_hash");

        _tokenServiceMock
            .Setup(t => t.GenerateRefreshToken())
            .Returns("new_refresh_token");

        _tokenServiceMock
            .Setup(t => t.HashToken("new_refresh_token"))
            .Returns("new_refresh_hash");

        _tokenServiceMock
            .Setup(t => t.RefreshTokenLifetime)
            .Returns(TimeSpan.FromDays(7));

        _tokenServiceMock
            .Setup(t => t.AccessTokenLifetime)
            .Returns(TimeSpan.FromMinutes(15));

        _tokenServiceMock
            .Setup(t => t.GenerateAccessToken(
                account.AccountId,
                account.Username,
                99,
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<IEnumerable<string>?>()))
            .Returns("rotated_access_token");

        _sessionRepoMock
            .Setup(s => s.ValidateRefreshTokenAsync(99, "old_refresh_hash", It.IsAny<CancellationToken>()))
            .ReturnsAsync(MakeSession());

        _sessionRepoMock
            .Setup(s => s.RotateRefreshTokenAsync(99, "new_refresh_hash", It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(MakeSession());

        _accountRepoMock
            .Setup(r => r.FindByIdWithRoleAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        SetupAccountStatusLookups();

        var service = CreateService();
        var result = await service.RefreshTokenAsync(new RefreshTokenRequest("expired_access_token", "old_refresh_token"));

        result.Success.Should().BeTrue();
        result.AccessToken.Should().Be("rotated_access_token");
        result.RefreshToken.Should().Be("new_refresh_token");
        result.SessionId.Should().Be(99);
        result.UserId.Should().Be(account.AccountId);
        result.Username.Should().Be(account.Username);
        result.Roles.Should().Contain("ADMIN");
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "LogoutAsync")]
    public async Task LogoutAsync_WhenSessionExists_ReturnsTrue()
    {
        _sessionRepoMock
            .Setup(s => s.RevokeSessionAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = CreateService();
        var result = await service.LogoutAsync(99);

        result.Should().BeTrue();
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "LogoutAsync")]
    public async Task LogoutAsync_WhenSessionDoesNotExist_ReturnsFalse()
    {
        _sessionRepoMock
            .Setup(s => s.RevokeSessionAsync(404, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var service = CreateService();
        var result = await service.LogoutAsync(404);

        result.Should().BeFalse();
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "RequestPasswordResetAsync")]
    public async Task RequestPasswordResetAsync_WhenEmailNotFound_DoesNotStoreTokenOrQueueEmail()
    {
        _accountRepoMock
            .Setup(r => r.FindByEmailAsync("MISSING@EXAMPLE.COM", It.IsAny<CancellationToken>()))
            .ReturnsAsync((StaffAccount?)null);

        var service = CreateService();
        await service.RequestPasswordResetAsync("missing@example.com", "127.0.0.1", "UnitTestBrowser/1.0");

        _tokenStoreMock.Verify(
            s => s.InvalidateUserAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()),
            Times.Never);

        _tokenStoreMock.Verify(
            s => s.StoreAsync(It.IsAny<PasswordResetTokenRecord>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()),
            Times.Never);

        _emailQueueMock.Verify(
            q => q.EnqueueAsync(It.IsAny<QueuedEmail>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "RequestPasswordResetAsync")]
    public async Task RequestPasswordResetAsync_WhenAccountIsLocked_DoesNotStoreTokenOrQueueEmail()
    {
        var account = MakeActiveAccount(isLocked: true);

        _accountRepoMock
            .Setup(r => r.FindByEmailAsync("ADMIN@EXAMPLE.COM", It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        var service = CreateService();
        await service.RequestPasswordResetAsync("admin@example.com", "127.0.0.1", "UnitTestBrowser/1.0");

        _tokenStoreMock.Verify(
            s => s.InvalidateUserAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()),
            Times.Never);

        _tokenStoreMock.Verify(
            s => s.StoreAsync(It.IsAny<PasswordResetTokenRecord>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()),
            Times.Never);

        _emailQueueMock.Verify(
            q => q.EnqueueAsync(It.IsAny<QueuedEmail>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "RequestPasswordResetAsync")]
    public async Task RequestPasswordResetAsync_WhenAccountExists_StoresTokenAndQueuesEmail()
    {
        var account = MakeActiveAccount();
        var template = new EmailTemplateDto
        {
            TemplateCode = "FORGOT_PASSWORD",
            TemplateName = "Forgot password",
            Subject = "Reset your password",
            BodyHtml = "Hello {{username}}, click {{resetLink}} within {{expiryMinutes}} minutes."
        };

        _accountRepoMock
            .Setup(r => r.FindByEmailAsync("ADMIN@EXAMPLE.COM", It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        _tokenStoreMock
            .Setup(s => s.InvalidateUserAsync(account.AccountId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _tokenStoreMock
            .Setup(s => s.StoreAsync(It.IsAny<PasswordResetTokenRecord>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _emailTemplateServiceMock
            .Setup(s => s.GetByCodeAsync("FORGOT_PASSWORD", It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);

        _emailQueueMock
            .Setup(q => q.EnqueueAsync(It.IsAny<QueuedEmail>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = CreateService();
        await service.RequestPasswordResetAsync("admin@example.com", "127.0.0.1", "UnitTestBrowser/1.0");

        _tokenStoreMock.Verify(
            s => s.InvalidateUserAsync(account.AccountId, It.IsAny<CancellationToken>()),
            Times.Once);

        _tokenStoreMock.Verify(
            s => s.StoreAsync(
                It.Is<PasswordResetTokenRecord>(record =>
                    record.UserId == account.AccountId &&
                    record.EmailNormalized == "ADMIN@EXAMPLE.COM" &&
                    record.TokenHash.Length == 64),
                It.Is<TimeSpan>(ttl => ttl == TimeSpan.FromMinutes(_forgotOpt.Value.TokenLifetimeMinutes)),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _emailQueueMock.Verify(
            q => q.EnqueueAsync(
                It.Is<QueuedEmail>(email =>
                    email.To == account.Email &&
                    email.Subject == template.Subject &&
                    email.HtmlBody.Contains(account.Username) &&
                    email.HtmlBody.Contains("https://client.aulac.local/reset-password?token=") &&
                    email.HtmlBody.Contains(_forgotOpt.Value.TokenLifetimeMinutes.ToString())),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "RequestPasswordResetAsync")]
    public async Task RequestPasswordResetAsync_WhenEmailHasWhitespace_NormalizesLookupAndSkipsQueueIfTemplateMissing()
    {
        var account = MakeActiveAccount();

        _accountRepoMock
            .Setup(r => r.FindByEmailAsync("ADMIN@EXAMPLE.COM", It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        _tokenStoreMock
            .Setup(s => s.InvalidateUserAsync(account.AccountId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _tokenStoreMock
            .Setup(s => s.StoreAsync(It.IsAny<PasswordResetTokenRecord>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _emailTemplateServiceMock
            .Setup(s => s.GetByCodeAsync("FORGOT_PASSWORD", It.IsAny<CancellationToken>()))
            .ReturnsAsync((EmailTemplateDto?)null);

        var service = CreateService();
        await service.RequestPasswordResetAsync("  admin@example.com  ", null, null);

        _accountRepoMock.Verify(
            r => r.FindByEmailAsync("ADMIN@EXAMPLE.COM", It.IsAny<CancellationToken>()),
            Times.Once);

        _tokenStoreMock.Verify(
            s => s.StoreAsync(
                It.Is<PasswordResetTokenRecord>(record => record.EmailNormalized == "ADMIN@EXAMPLE.COM"),
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _emailQueueMock.Verify(
            q => q.EnqueueAsync(It.IsAny<QueuedEmail>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "ResetPasswordAsync")]
    public async Task ResetPasswordAsync_WhenTokenIsBlank_ThrowsInvalidOperationException()
    {
        var service = CreateService();
        Func<Task> act = () => service.ResetPasswordAsync(" ", "NewPassword123!");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Invalid reset token.");
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "ResetPasswordAsync")]
    public async Task ResetPasswordAsync_WhenTokenRecordIsMissing_ThrowsInvalidOperationException()
    {
        var rawToken = "missing-reset-token";
        var tokenHash = HashPasswordResetToken(rawToken);

        _tokenStoreMock
            .Setup(s => s.GetByTokenHashAsync(tokenHash, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PasswordResetTokenRecord?)null);

        var service = CreateService();
        Func<Task> act = () => service.ResetPasswordAsync(rawToken, "NewPassword123!");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Invalid or expired reset token.");
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "ResetPasswordAsync")]
    public async Task ResetPasswordAsync_WhenTokenExpired_ConsumesTokenAndThrows()
    {
        var rawToken = "expired-reset-token";
        var tokenHash = HashPasswordResetToken(rawToken);
        var record = MakePasswordResetRecord(rawToken, expiresAt: DateTimeOffset.UtcNow.AddMinutes(-1));

        _tokenStoreMock
            .Setup(s => s.GetByTokenHashAsync(tokenHash, It.IsAny<CancellationToken>()))
            .ReturnsAsync(record);

        _tokenStoreMock
            .Setup(s => s.ConsumeAsync(tokenHash, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = CreateService();
        Func<Task> act = () => service.ResetPasswordAsync(rawToken, "NewPassword123!");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Reset token has expired.");

        _tokenStoreMock.Verify(
            s => s.ConsumeAsync(tokenHash, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "ResetPasswordAsync")]
    public async Task ResetPasswordAsync_WhenAccountIsLocked_ConsumesTokenAndThrows()
    {
        var rawToken = "locked-account-token";
        var tokenHash = HashPasswordResetToken(rawToken);
        var record = MakePasswordResetRecord(rawToken);
        var account = MakeActiveAccount(isLocked: true);

        _tokenStoreMock
            .Setup(s => s.GetByTokenHashAsync(tokenHash, It.IsAny<CancellationToken>()))
            .ReturnsAsync(record);

        _accountRepoMock
            .Setup(r => r.FindByIdAsync(record.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        _tokenStoreMock
            .Setup(s => s.ConsumeAsync(tokenHash, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = CreateService();
        Func<Task> act = () => service.ResetPasswordAsync(rawToken, "NewPassword123!");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("User account not found or is locked.");

        _accountRepoMock.Verify(
            r => r.UpdatePasswordAsync(It.IsAny<long>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "ResetPasswordAsync")]
    public async Task ResetPasswordAsync_WhenTokenIsValid_UpdatesPasswordConsumesTokenAndRevokesSessions()
    {
        var rawToken = "valid-reset-token";
        var tokenHash = HashPasswordResetToken(rawToken);
        var record = MakePasswordResetRecord(rawToken);
        var account = MakeActiveAccount();

        _tokenStoreMock
            .Setup(s => s.GetByTokenHashAsync(tokenHash, It.IsAny<CancellationToken>()))
            .ReturnsAsync(record);

        _accountRepoMock
            .Setup(r => r.FindByIdAsync(record.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        _passwordHasherMock
            .Setup(h => h.HashPassword("NewPassword123!"))
            .Returns("new_password_hash");

        _accountRepoMock
            .Setup(r => r.UpdatePasswordAsync(account.AccountId, "new_password_hash", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _tokenStoreMock
            .Setup(s => s.ConsumeAsync(tokenHash, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _sessionRepoMock
            .Setup(s => s.RevokeAllUserSessionsAsync(record.UserId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(3);

        var service = CreateService();
        await service.ResetPasswordAsync(rawToken, "NewPassword123!");

        _passwordHasherMock.Verify(h => h.HashPassword("NewPassword123!"), Times.Once);
        _accountRepoMock.Verify(
            r => r.UpdatePasswordAsync(account.AccountId, "new_password_hash", It.IsAny<CancellationToken>()),
            Times.Once);
        _tokenStoreMock.Verify(s => s.ConsumeAsync(tokenHash, It.IsAny<CancellationToken>()), Times.Once);
        _sessionRepoMock.Verify(
            s => s.RevokeAllUserSessionsAsync(record.UserId, null, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "ValidateSessionAsync")]
    public async Task ValidateSessionAsync_WhenSessionDoesNotExist_ReturnsFalse()
    {
        _sessionRepoMock
            .Setup(s => s.GetValidSessionAsync(404, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AuthSession?)null);

        var service = CreateService();
        var result = await service.ValidateSessionAsync(404);

        result.Should().BeFalse();
        _accountRepoMock.Verify(
            r => r.FindByIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "ValidateSessionAsync")]
    public async Task ValidateSessionAsync_WhenAccountDoesNotExist_ReturnsFalse()
    {
        _sessionRepoMock
            .Setup(s => s.GetValidSessionAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync(MakeSession());

        _accountRepoMock
            .Setup(r => r.FindByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync((StaffAccount?)null);

        var service = CreateService();
        var result = await service.ValidateSessionAsync(99);

        result.Should().BeFalse();
    }

    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "ValidateSessionAsync")]
    public async Task ValidateSessionAsync_WhenAccountIsInactive_RevokesSessionAndReturnsFalse()
    {
        var account = MakeActiveAccount(statusLvId: InactiveStatusId);

        _sessionRepoMock
            .Setup(s => s.GetValidSessionAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync(MakeSession());

        _accountRepoMock
            .Setup(r => r.FindByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        _sessionRepoMock
            .Setup(s => s.RevokeSessionAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        SetupAccountStatusLookups();

        var service = CreateService();
        var result = await service.ValidateSessionAsync(99);

        result.Should().BeFalse();
        _sessionRepoMock.Verify(s => s.RevokeSessionAsync(99, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "ValidateSessionAsync")]
    public async Task ValidateSessionAsync_WhenSessionAndAccountAreActive_ReturnsTrue()
    {
        var account = MakeActiveAccount(statusLvId: ActiveStatusId);

        _sessionRepoMock
            .Setup(s => s.GetValidSessionAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync(MakeSession());

        _accountRepoMock
            .Setup(r => r.FindByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        SetupAccountStatusLookups();

        var service = CreateService();
        var result = await service.ValidateSessionAsync(99);

        result.Should().BeTrue();
        _sessionRepoMock.Verify(
            s => s.RevokeSessionAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}

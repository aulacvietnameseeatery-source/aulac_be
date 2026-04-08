 using Core.Data;
using Core.DTO.Auth;
using Core.Entity;
using Core.Interface.Repo;
using Core.Interface.Service;
using Core.Interface.Service.Auth;
using Core.Interface.Service.Email;
using Core.Interface.Service.Entity;
using Core.Service;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using FluentAssertions;

namespace Tests.Services;

public class AuthServiceTests
{
    // ── Mocks ──────────────────────────────────────────────────────────────
    private readonly Mock<ITokenService>               _tokenServiceMock        = new();
    private readonly Mock<IAuthSessionRepository>      _sessionRepoMock         = new();
    private readonly Mock<IAccountRepository>          _accountRepoMock         = new();
    private readonly Mock<IPasswordHasher>             _passwordHasherMock      = new();
    private readonly Mock<IPasswordResetTokenStore>    _tokenStoreMock          = new();
    private readonly Mock<IEmailQueue>                 _emailQueueMock          = new();
    private readonly Mock<IEmailTemplateService>       _emailTemplateServiceMock= new();
    private readonly Mock<ILookupResolver>             _lookupResolverMock      = new();
    private readonly Mock<ILogger<AuthService>>        _loggerMock              = new();

    // IOptions sử dụng Options.Create để không cần mock
    private readonly IOptions<ForgotPasswordRulesOptions> _forgotOpt =
        Options.Create(new ForgotPasswordRulesOptions());
    private readonly IOptions<BaseUrlOptions> _baseUrlOpt =
        Options.Create(new BaseUrlOptions());

    // ID giả lập cho trạng thái LOCKED trong lookup table (typeId = 1)
    private const uint LockedStatusId = 3u;

    // ── Helper: tạo AuthService ────────────────────────────────────────────
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

    // ── Helper: tạo StaffAccount giả với Role đầy đủ ─────────────────────
    private static StaffAccount MakeActiveAccount(uint statusLvId = 1u) => new()
    {
        AccountId       = 1,
        Username        = "admin",
        Email           = "ADMIN@EXAMPLE.COM",
        PasswordHash    = "hashed_password",
        AccountStatusLvId = statusLvId,
        Role = new Role
        {
            RoleCode    = "ADMIN",
            Permissions = new List<Permission>
            {
                new() { ScreenCode = "DASHBOARD", ActionCode = "VIEW" }
            }
        }
    };

    // ── Helper: setup mock ILookupResolver trả về LockedStatusId ─────────
    private void SetupLookupLockedStatus()
    {
        _lookupResolverMock
            .Setup(r => r.GetIdAsync(
                (ushort)Core.Enum.LookupType.AccountStatus,
                It.IsAny<System.Enum>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(LockedStatusId);
    }

    // ── Helper: setup session và token mặc định ───────────────────────────
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

    // ═══════════════════════════════════════════════════════════════════════
    // TEST CASES
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// [Abnormal] Username không tồn tại và email cũng không tồn tại
    /// Precondition: Không có account nào trong hệ thống khớp với input
    /// Input: username = "nonexistent_user" (không hợp lệ — không tồn tại)
    /// Expected: Failed với ErrorCode = "INVALID_CREDENTIALS"
    /// </summary>
    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "LoginAsync")]
    public async Task LoginAsync_WhenAccountNotFound_ReturnsFailed()
    {
        // Arrange
        _accountRepoMock
            .Setup(r => r.FindByUsernameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((StaffAccount?)null);

        _accountRepoMock
            .Setup(r => r.FindByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((StaffAccount?)null);

        var service = CreateService();
        var request = new LoginRequest("nonexistent_user", "any_password");

        // Act
        var result = await service.LoginAsync(request);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be("INVALID_CREDENTIALS");
    }

    /// <summary>
    /// [Abnormal] Username tìm thấy nhưng password sai
    /// Precondition: Account "admin" tồn tại trong hệ thống
    /// Input: username = "admin" (hợp lệ), password = "wrong_password" (sai)
    /// Expected: Failed với ErrorCode = "INVALID_CREDENTIALS"
    /// </summary>
    [Fact]
    [Trait("Type", "Abnormal")]
    [Trait("Method", "LoginAsync")]
    public async Task LoginAsync_WhenPasswordIsWrong_ReturnsFailed()
    {
        // Arrange
        var account = MakeActiveAccount();

        _accountRepoMock
            .Setup(r => r.FindByUsernameAsync("admin", It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        _passwordHasherMock
            .Setup(h => h.VerifyPassword("wrong_password", account.PasswordHash))
            .Returns(false);

        var service = CreateService();
        var request = new LoginRequest("admin", "wrong_password");

        // Act
        var result = await service.LoginAsync(request);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be("INVALID_CREDENTIALS");
    }

    /// <summary>
    /// [Normal] Username không có nhưng tìm thấy qua email (case-insensitive), password đúng, tài khoản active
    /// Precondition: Account tồn tại với email "ADMIN@EXAMPLE.COM", trạng thái ACTIVE
    /// Input: username = "admin@example.com" (email hợp lệ), password = "correct_pass" (đúng)
    /// Expected: Success = true, có đầy đủ accessToken và refreshToken
    /// </summary>
    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "LoginAsync")]
    public async Task LoginAsync_WhenFoundByEmail_AndPasswordCorrect_ReturnsSuccess()
    {
        // Arrange
        var account = MakeActiveAccount(statusLvId: 1u); // active → không phải LockedStatusId

        _accountRepoMock
            .Setup(r => r.FindByUsernameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((StaffAccount?)null);

        _accountRepoMock
            .Setup(r => r.FindByEmailAsync("ADMIN@EXAMPLE.COM", It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        _passwordHasherMock
            .Setup(h => h.VerifyPassword("correct_pass", account.PasswordHash))
            .Returns(true);

        SetupLookupLockedStatus(); // LOCKED = 3, account là 1 → không cần đổi mật khẩu
        SetupTokenAndSession();

        _accountRepoMock
            .Setup(r => r.UpdateLastLoginAsync(account.AccountId, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = CreateService();
        var request = new LoginRequest("admin@example.com", "correct_pass");

        // Act
        var result = await service.LoginAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        result.RequirePasswordChange.Should().BeFalse();
        result.AccessToken.Should().Be("access_token_value");
        result.RefreshToken.Should().Be("raw_refresh_token");
        result.Username.Should().Be("admin");
    }

    /// <summary>
    /// [Normal] Tài khoản LOCKED (lần đầu đăng nhập), password đúng
    /// Precondition: Account "admin" tồn tại, trạng thái = LOCKED (AccountStatusLvId = 3)
    /// Input: username = "admin" (hợp lệ), password = "correct_pass" (đúng)
    /// Expected: Success = true, RequirePasswordChange = true, RefreshToken = null
    /// </summary>
    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "LoginAsync")]
    public async Task LoginAsync_WhenAccountIsLocked_ReturnsPasswordChangeRequired()
    {
        // Arrange: account có statusLvId = LockedStatusId (3)
        var account = MakeActiveAccount(statusLvId: LockedStatusId);

        _accountRepoMock
            .Setup(r => r.FindByUsernameAsync("admin", It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        _passwordHasherMock
            .Setup(h => h.VerifyPassword("correct_pass", account.PasswordHash))
            .Returns(true);

        // Lookup trả về LockedStatusId → account.AccountStatusLvId == LockedStatusId
        SetupLookupLockedStatus();

        // Token ngắn hạn cho phiên đổi mật khẩu
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
        var request = new LoginRequest("admin", "correct_pass");

        // Act
        var result = await service.LoginAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        result.RequirePasswordChange.Should().BeTrue();
        result.AccessToken.Should().Be("temp_access_token");
        result.RefreshToken.Should().BeNull("No refresh token for password-change session");
    }

    /// <summary>
    /// [Normal] Tài khoản active, username đúng, password đúng, đăng nhập bình thường
    /// Precondition: Account "admin" tồn tại, trạng thái = ACTIVE (AccountStatusLvId = 1)
    /// Input: username = "admin" (hợp lệ), password = "correct_pass" (đúng)
    /// Expected: Success = true, có đầy đủ accessToken, refreshToken, sessionId, roles
    /// </summary>
    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "LoginAsync")]
    public async Task LoginAsync_WhenCredentialsValid_ReturnsSucceededWithTokens()
    {
        // Arrange
        var account = MakeActiveAccount(statusLvId: 1u); // active, không phải LOCKED(3)

        _accountRepoMock
            .Setup(r => r.FindByUsernameAsync("admin", It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        _passwordHasherMock
            .Setup(h => h.VerifyPassword("correct_pass", account.PasswordHash))
            .Returns(true);

        SetupLookupLockedStatus();
        SetupTokenAndSession();

        _accountRepoMock
            .Setup(r => r.UpdateLastLoginAsync(account.AccountId, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = CreateService();
        var request = new LoginRequest("admin", "correct_pass");

        // Act
        var result = await service.LoginAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        result.RequirePasswordChange.Should().BeFalse();
        result.AccessToken.Should().Be("access_token_value");
        result.RefreshToken.Should().Be("raw_refresh_token");
        result.SessionId.Should().Be(99);
        result.UserId.Should().Be(account.AccountId);
        result.Username.Should().Be("admin");
        result.Roles.Should().Contain("ADMIN");
    }

    /// <summary>
    /// [Normal] Sau khi đăng nhập thành công, UpdateLastLoginAsync phải được gọi đúng 1 lần
    /// Precondition: Account "admin" tồn tại, trạng thái = ACTIVE
    /// Input: username = "admin" (hợp lệ), password = "correct_pass" (đúng)
    /// Expected: UpdateLastLoginAsync được gọi Times.Once với AccountId đúng
    /// </summary>
    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "LoginAsync")]
    public async Task LoginAsync_OnSuccess_ShouldCallUpdateLastLogin()
    {
        // Arrange
        var account = MakeActiveAccount(statusLvId: 1u);

        _accountRepoMock
            .Setup(r => r.FindByUsernameAsync("admin", It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        _passwordHasherMock
            .Setup(h => h.VerifyPassword("correct_pass", account.PasswordHash))
            .Returns(true);

        SetupLookupLockedStatus();
        SetupTokenAndSession();

        _accountRepoMock
            .Setup(r => r.UpdateLastLoginAsync(account.AccountId, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = CreateService();

        // Act
        await service.LoginAsync(new LoginRequest("admin", "correct_pass"));

        // Assert
        _accountRepoMock.Verify(
            r => r.UpdateLastLoginAsync(account.AccountId, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// [Normal] Khi tài khoản LOCKED đăng nhập (yêu cầu đổi mật khẩu),
    /// UpdateLastLoginAsync KHÔNG được gọi
    /// Precondition: Account "admin" tồn tại, trạng thái = LOCKED
    /// Input: username = "admin" (hợp lệ), password = "correct_pass" (đúng)
    /// Expected: UpdateLastLoginAsync không được gọi lần nào (Times.Never)
    /// </summary>
    [Fact]
    [Trait("Type", "Normal")]
    [Trait("Method", "LoginAsync")]
    public async Task LoginAsync_WhenPasswordChangeRequired_ShouldNotCallUpdateLastLogin()
    {
        // Arrange
        var account = MakeActiveAccount(statusLvId: LockedStatusId);

        _accountRepoMock
            .Setup(r => r.FindByUsernameAsync("admin", It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        _passwordHasherMock
            .Setup(h => h.VerifyPassword("correct_pass", account.PasswordHash))
            .Returns(true);

        SetupLookupLockedStatus();

        _tokenServiceMock
            .Setup(t => t.GenerateAccessToken(
                It.IsAny<long>(), It.IsAny<string>(), It.IsAny<long>(),
                It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>?>()))
            .Returns("temp_token");

        _sessionRepoMock
            .Setup(s => s.CreateSessionAsync(
                It.IsAny<long>(), "temp_for_password_change",
                It.IsAny<DateTime>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AuthSession { SessionId = 50 });

        var service = CreateService();

        // Act
        await service.LoginAsync(new LoginRequest("admin", "correct_pass"));

        // Assert
        _accountRepoMock.Verify(
            r => r.UpdateLastLoginAsync(It.IsAny<long>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // BOUNDARY TEST CASES
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// [Boundary] Username dài đúng 100 ký tự (giới hạn tối đa theo DB schema)
    /// Precondition: Không có account nào khớp
    /// Input: username = chuỗi 100 ký tự 'a' (boundary trên của độ dài)
    /// Expected: Failed với INVALID_CREDENTIALS (không tìm thấy account)
    /// </summary>
    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "LoginAsync")]
    public async Task LoginAsync_WhenUsernameIsMaxLength_ReturnsFailedNotFound()
    {
        // Arrange — boundary: đúng 100 ký tự (giới hạn max của cột username trong DB)
        var maxLengthUsername = new string('a', 100);

        _accountRepoMock
            .Setup(r => r.FindByUsernameAsync(maxLengthUsername, It.IsAny<CancellationToken>()))
            .ReturnsAsync((StaffAccount?)null);

        _accountRepoMock
            .Setup(r => r.FindByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((StaffAccount?)null);

        var service = CreateService();
        var request = new LoginRequest(maxLengthUsername, "any_password");

        // Act
        var result = await service.LoginAsync(request);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be("INVALID_CREDENTIALS");
    }

    /// <summary>
    /// [Boundary] AccountStatusLvId đúng bằng LockedStatusId (ranh giới giữa ACTIVE và LOCKED)
    /// Precondition: Account tồn tại với AccountStatusLvId == LockedStatusId (= 3), password đúng
    /// Input: username = "admin", password = "correct_pass"
    /// Expected: RequirePasswordChange = true (đúng ranh giới kích hoạt luồng đổi mật khẩu)
    /// </summary>
    [Fact]
    [Trait("Type", "Boundary")]
    [Trait("Method", "LoginAsync")]
    public async Task LoginAsync_WhenStatusIsExactlyLockedId_ReturnsPasswordChangeRequired()
    {
        // Arrange — boundary: statusLvId == LockedStatusId (= 3), không phải 2 hay 4
        var account = MakeActiveAccount(statusLvId: LockedStatusId);

        _accountRepoMock
            .Setup(r => r.FindByUsernameAsync("admin", It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        _passwordHasherMock
            .Setup(h => h.VerifyPassword("correct_pass", account.PasswordHash))
            .Returns(true);

        SetupLookupLockedStatus();

        _tokenServiceMock
            .Setup(t => t.GenerateAccessToken(
                It.IsAny<long>(), It.IsAny<string>(), It.IsAny<long>(),
                It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>?>()))
            .Returns("temp_access_token");

        _sessionRepoMock
            .Setup(s => s.CreateSessionAsync(
                It.IsAny<long>(), "temp_for_password_change",
                It.IsAny<DateTime>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AuthSession { SessionId = 50 });

        var service = CreateService();
        var request = new LoginRequest("admin", "correct_pass");

        // Act
        var result = await service.LoginAsync(request);

        // Assert
        result.RequirePasswordChange.Should().BeTrue(
            "AccountStatusLvId == LockedStatusId là ranh giới kích hoạt luồng đổi mật khẩu");
        result.RefreshToken.Should().BeNull();
    }
}

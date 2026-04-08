# Hướng Dẫn Viết Unit Test — AuLac Restaurant Backend

> Dự án: `AuLacRestaurant_BE` | Framework: `.NET 8` | Test Framework: `xUnit` + `Moq` + `FluentAssertions`

---

## Mục lục

1. [So sánh Guideline với thực tế](#1-so-sánh-guideline-với-thực-tế)
2. [Tổng quan kiến trúc test](#2-tổng-quan-kiến-trúc-test)
3. [Cấu trúc thư mục Tests](#3-cấu-trúc-thư-mục-tests)
4. [Các package đã cài đặt](#4-các-package-đã-cài-đặt)
5. [Quy tắc đặt tên test](#5-quy-tắc-đặt-tên-test)
6. [Phân loại Test Case: Normal / Boundary / Abnormal](#6-phân-loại-test-case-normal--boundary--abnormal)
7. [Metadata bắt buộc trong mỗi file test](#7-metadata-bắt-buộc-trong-mỗi-file-test)
8. [Cấu trúc 1 file test chuẩn](#8-cấu-trúc-1-file-test-chuẩn)
9. [Bước 1 — Khai báo Mocks](#bước-1--khai-báo-mocks)
10. [Bước 2 — Tạo instance Service cần test (Factory Method)](#bước-2--tạo-instance-service-cần-test-factory-method)
11. [Bước 3 — Tạo dữ liệu giả (Test Data Helpers)](#bước-3--tạo-dữ-liệu-giả-test-data-helpers)
12. [Bước 4 — Setup Mock (giả lập hành vi dependency)](#bước-4--setup-mock-giả-lập-hành-vi-dependency)
13. [Bước 5 — Viết Test Case theo mẫu AAA](#bước-5--viết-test-case-theo-mẫu-aaa)
14. [Bước 6 — Viết Assertion với FluentAssertions](#bước-6--viết-assertion-với-fluentassertions)
15. [Bước 7 — Verify Mock đã được gọi (Behavior Testing)](#bước-7--verify-mock-đã-được-gọi-behavior-testing)
16. [Chạy test](#16-chạy-test)
17. [Xử lý các tình huống đặc biệt](#17-xử-lý-các-tình-huống-đặc-biệt)
18. [Checklist trước khi commit](#18-checklist-trước-khi-commit)

---

## 1. So sánh Guideline với thực tế

Bảng dưới ánh xạ từng yêu cầu trong **Guideline** sang cách thực hiện trong code:

| Khái niệm trong Guideline | Cách thực hiện trong code | Trạng thái |
|---|---|---|
| Test cases based on functions | Mỗi file `*Tests.cs` = 1 Service; mỗi region = 1 method | ✅ Match |
| **Condition = Precondition + Input values** | `// Arrange` trong mỗi test | ✅ Match |
| Precondition | Mock setup giả lập trạng thái DB/dependency | ✅ Match |
| Normal values | Test happy-path, luồng bình thường | ✅ Match |
| **Boundary values** | `[Trait("Type", "Boundary")]` — test giá trị biên | ✅ Đã thêm |
| Abnormal values | `[Trait("Type", "Abnormal")]` — test input không hợp lệ | ✅ Match |
| **Confirmation** | `// Assert` với FluentAssertions | ✅ Match |
| **Type labeling** (Normal/Boundary/Abnormal) | `[Trait("Type", "...")]` trên mỗi `[Fact]` | ✅ Đã thêm |
| Test result P/F | xUnit tự động: Passed / Failed | ✅ Auto |
| **Code Module / Method** | XML doc `/// Code Module: ...` ở class level | ✅ Đã thêm |
| **Created By / Executed By** | XML doc `/// Created By: ...` ở class level | ✅ Đã thêm |
| **Test requirement** | XML doc `/// Test Req.: ...` ở class level | ✅ Đã thêm |
| Output log messages | Kiểm tra qua `Mock<ILogger>.Verify(...)` nếu cần | ℹ️ Optional |
| FunctionList (danh sách hàm) | Ghi trong README hoặc tài liệu dự án riêng | ℹ️ Ngoài code |
| TC count / KLOC | Tính thủ công từ số test ÷ LOC của method | ℹ️ Ngoài code |
| Test Report tổng hợp | Chạy `dotnet test` → output + coverage report | ℹ️ Auto |

---

## 2. Tổng quan kiến trúc test

```
Solution
├── Api/          ← Controller layer (HTTP)
├── Core/         ← Business logic: Service + Interface + Entity + DTO
├── Infa/         ← Infrastructure: Repository + EF Core + External services
└── Tests/        ← Unit test project (chỉ test Core layer)
```

**Unit test trong dự án này tập trung vào `Core/Service/`.**

Lý do: Service chứa toàn bộ business logic. Mọi dependency của Service đã được abstract qua Interface, nên có thể mock hoàn toàn mà không cần database hay HTTP.

**Nguyên tắc cốt lõi:**
- ✅ Test 1 class tại 1 thời điểm (unit = 1 class)
- ✅ Mock tất cả dependency (Repository, TokenService, Logger...)
- ✅ Không kết nối database thật
- ✅ Mỗi test độc lập, không phụ thuộc nhau
- ✅ Tên test phải mô tả rõ kịch bản và kết quả mong đợi

---

## 3. Cấu trúc thư mục Tests

```
Tests/
├── Tests.csproj
├── UNIT_TEST_GUIDE.md
└── Services/
    ├── AuthServiceTests.cs       ← ví dụ mẫu
    ├── DishServiceTests.cs
    ├── OrderServiceTests.cs
    └── ...
```

**Quy tắc tổ chức:**
- Mỗi Service trong `Core/Service/` có 1 file `*Tests.cs` tương ứng trong `Tests/Services/`
- Namespace: `Tests.Services`

---

## 4. Các package đã cài đặt

| Package | Version | Mục đích |
|---|---|---|
| `xunit` | 2.5.3 | Framework viết test (`[Fact]`, `[Theory]`) |
| `xunit.runner.visualstudio` | 2.5.3 | Tích hợp với VS Code Test Explorer |
| `Moq` | 4.20.72 | Tạo mock object từ Interface |
| `FluentAssertions` | 6.12.1 | Viết assertion dễ đọc, thông điệp lỗi rõ ràng |
| `Microsoft.Extensions.Logging` | 10.0.5 | Mock `ILogger<T>` |
| `Microsoft.Extensions.Options` | 10.0.5 | Dùng `Options.Create()` thay vì mock `IOptions<T>` |
| `coverlet.collector` | 6.0.0 | Thu thập code coverage |
| `Microsoft.NET.Test.Sdk` | 17.8.0 | Hạ tầng chạy test của .NET |

---

## 5. Quy tắc đặt tên test

**Format:** `[TênPhươngThức]_[Điều kiện/Kịch bản]_[Kết quả mong đợi]`

```csharp
// ✅ Đúng
LoginAsync_WhenAccountNotFound_ReturnsFailed()
LoginAsync_WhenPasswordIsWrong_ReturnsFailed()
LoginAsync_WhenCredentialsValid_ReturnsSucceededWithTokens()
LoginAsync_WhenAccountIsLocked_ReturnsPasswordChangeRequired()
LoginAsync_WhenUsernameIsMaxLength_ReturnsFailedNotFound()  // Boundary
LoginAsync_OnSuccess_ShouldCallUpdateLastLogin()

// ❌ Sai — quá ngắn, không rõ kịch bản
Test1()
LoginTest()
TestLogin()
```

---

## 6. Phân loại Test Case: Normal / Boundary / Abnormal

Theo guideline, mỗi test case phải được gán loại rõ ràng. Trong code dùng `[Trait("Type", "...")]`:

### 6.1 — Định nghĩa 3 loại

| Loại | Định nghĩa | Ví dụ với LoginAsync |
|---|---|---|
| **Normal** | Input hợp lệ, luồng chính | username đúng + password đúng → thành công |
| **Boundary** | Giá trị biên (min/max, ranh giới điều kiện) | username dài đúng 100 ký tự (max DB), status đúng bằng LockedId |
| **Abnormal** | Input không hợp lệ, luồng ngoại lệ | username không tồn tại, password sai |

### 6.2 — Cách gán loại bằng `[Trait]`

```csharp
[Fact]
[Trait("Type", "Normal")]     // hoặc "Boundary" hoặc "Abnormal"
[Trait("Method", "LoginAsync")]
public async Task LoginAsync_WhenCredentialsValid_ReturnsSucceededWithTokens()
{
    // ...
}
```

### 6.3 — Chạy test theo loại

```powershell
# Chỉ chạy Normal test cases
dotnet test --filter "Trait=Type,Normal"

# Chỉ chạy Boundary test cases
dotnet test --filter "Trait=Type,Boundary"

# Chỉ chạy Abnormal test cases
dotnet test --filter "Trait=Type,Abnormal"

# Chỉ chạy test của LoginAsync
dotnet test --filter "Trait=Method,LoginAsync"
```

### 6.4 — Xác định loại input theo giá trị biên

Ví dụ: Ràng buộc `username` có max length = 100 ký tự (theo DB schema)

```
Giá trị trong khoảng [1..99 ký tự]  → Normal values
Giá trị = 100 ký tự (đúng max)      → Boundary value (upper)
Giá trị = 101+ ký tự               → Abnormal value (vượt giới hạn)
Giá trị = "" hoặc null             → Abnormal value
```

Ví dụ: Phân loại `AccountStatusLvId` cho LoginAsync

```
StatusLvId < LockedStatusId   → Normal (tài khoản ACTIVE)
StatusLvId == LockedStatusId  → Boundary (ranh giới kích hoạt luồng đổi mật khẩu)
StatusLvId > LockedStatusId   → Abnormal (trạng thái không xác định)
```

---

## 7. Metadata bắt buộc trong mỗi file test

Theo guideline, mỗi function test cần ghi rõ: **Code Module**, **Method**, **Created By**, **Executed By**, **Test requirement**.

Trong code, đặt XML doc comment ở **class level**:

```csharp
/// <summary>
/// Unit Test — AuthService.LoginAsync
/// Code Module : Core/Service/AuthService.cs
/// Method      : LoginAsync(LoginRequest, deviceInfo, ipAddress, CancellationToken)
/// Created By  : Nguyễn Văn A
/// Executed By : Trần Thị B
/// Test Req.   : Xác thực người dùng bằng username/email + password.
///               Xử lý account LOCKED (yêu cầu đổi mật khẩu lần đầu).
///               Lưu lại thời gian đăng nhập cuối khi thành công.
/// </summary>
public class AuthServiceTests
{
    // ...
}
```

→ Xem ví dụ đầy đủ tại: [`Tests/Services/AuthServiceTests.cs`](Services/AuthServiceTests.cs)

---

## 8. Cấu trúc 1 file test chuẩn

```csharp
// 1. Import thư viện
using Core.Data;
using Core.DTO.Auth;
using Core.Entity;
using Core.Interface.Repo;
using Core.Interface.Service;
using Core.Interface.Service.Auth;
using Core.Interface.Service.Email;
using Core.Interface.Service.Entity;
using Core.Service;                    // namespace chứa class cần test
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using FluentAssertions;

namespace Tests.Services;

// 2. Metadata bắt buộc theo guideline (class level)
/// <summary>
/// Unit Test — [TênService].[TênMethod]
/// Code Module : Core/Service/[TênService].cs
/// Method      : [TênMethod]([danh sách tham số])
/// Created By  : [Tên người tạo]
/// Executed By : [Tên người chạy test]
/// Test Req.   : [Mô tả ngắn các yêu cầu được test]
/// </summary>
public class [TênService]Tests
{
    // 3. Khai báo Mocks (field-level)
    // 4. Tạo Factory Method CreateService()
    // 5. Tạo Test Data Helpers
    // 6. Test Cases Normal
    // 7. Test Cases Boundary
    // 8. Test Cases Abnormal
}
```

---

## Bước 1 — Khai báo Mocks (Bước 9)

Khai báo mock cho **tất cả dependency** của Service ở cấp field (không ở trong method).

**Tại sao dùng field?** Mỗi test method dùng chung một bộ mock mới (xUnit tạo instance mới cho mỗi test), đảm bảo test độc lập.

```csharp
public class AuthServiceTests
{
    // ── Khai báo mock cho từng dependency của AuthService ──────────────────
    private readonly Mock<ITokenService>             _tokenServiceMock         = new();
    private readonly Mock<IAuthSessionRepository>    _sessionRepoMock          = new();
    private readonly Mock<IAccountRepository>        _accountRepoMock          = new();
    private readonly Mock<IPasswordHasher>           _passwordHasherMock       = new();
    private readonly Mock<IPasswordResetTokenStore>  _tokenStoreMock           = new();
    private readonly Mock<IEmailQueue>               _emailQueueMock           = new();
    private readonly Mock<IEmailTemplateService>     _emailTemplateServiceMock = new();
    private readonly Mock<ILookupResolver>           _lookupResolverMock       = new();
    private readonly Mock<ILogger<AuthService>>      _loggerMock               = new();

    // ── IOptions<T>: dùng Options.Create() thay vì mock ──────────────────
    // Options.Create() tạo ra IOptions<T> thật từ object cụ thể
    private readonly IOptions<ForgotPasswordRulesOptions> _forgotOpt =
        Options.Create(new ForgotPasswordRulesOptions());

    private readonly IOptions<BaseUrlOptions> _baseUrlOpt =
        Options.Create(new BaseUrlOptions());
}
```

**Cách tra cứu dependency của 1 Service:**

Mở file `Core/Service/AuthService.cs`, xem constructor:

```csharp
public AuthService(
    ITokenService tokenService,            // → Mock<ITokenService>
    IAuthSessionRepository sessionRepository, // → Mock<IAuthSessionRepository>
    IAccountRepository accountRepository,  // → Mock<IAccountRepository>
    IPasswordHasher passwordHasher,        // → Mock<IPasswordHasher>
    IPasswordResetTokenStore tokenStore,   // → Mock<IPasswordResetTokenStore>
    IEmailQueue emailQueue,                // → Mock<IEmailQueue>
    IEmailTemplateService emailTemplateService, // → Mock<IEmailTemplateService>
    ILookupResolver lookupResolver,        // → Mock<ILookupResolver>
    IOptions<ForgotPasswordRulesOptions> forgotOpt, // → Options.Create(...)
    IOptions<BaseUrlOptions> baseUrlOpt,   // → Options.Create(...)
    ILogger<AuthService> logger)           // → Mock<ILogger<AuthService>>
```

---

## Bước 2 — Tạo instance Service cần test (Bước 10)

Tạo một private method để khởi tạo Service, truyền vào tất cả mock:

```csharp
private AuthService CreateService() => new(
    _tokenServiceMock.Object,           // .Object lấy instance thật từ mock
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
```

> **Lưu ý:** Gọi `CreateService()` **bên trong** mỗi test method (trong phần `// Arrange`), không phải trong constructor của class test. Điều này đảm bảo mỗi test có Service riêng với trạng thái mock riêng.

---

## Bước 3 — Tạo dữ liệu giả (Bước 11)

Tạo các `private static` method để sinh ra entity/DTO giả tái sử dụng:

```csharp
// ── Tạo StaffAccount giả với Role đầy đủ ─────────────────────────────────
private static StaffAccount MakeActiveAccount(uint statusLvId = 1u) => new()
{
    AccountId         = 1,
    Username          = "admin",
    Email             = "ADMIN@EXAMPLE.COM",   // luôn viết hoa để khớp logic normalize
    PasswordHash      = "hashed_password",
    AccountStatusLvId = statusLvId,            // tham số hóa để test nhiều trạng thái
    Role = new Role
    {
        RoleCode    = "ADMIN",
        Permissions = new List<Permission>
        {
            new() { ScreenCode = "DASHBOARD", ActionCode = "VIEW" }
        }
    }
};
```

**Nguyên tắc:**
- `static` vì không cần truy cập field của class
- Dùng tham số để tái sử dụng cho nhiều kịch bản (ví dụ: `statusLvId` để test vừa ACTIVE vừa LOCKED)
- Đặt giá trị sát với dữ liệu thực tế trong DB (độ dài string, kiểu dữ liệu)

---

## Bước 4 — Setup Mock (Bước 12)

Setup mock trong mỗi test hoặc nhóm thành helper method nếu tái sử dụng nhiều.

### 4.1 — Setup method trả về giá trị

```csharp
// Giả lập: FindByUsernameAsync("admin") → trả về account
_accountRepoMock
    .Setup(r => r.FindByUsernameAsync("admin", It.IsAny<CancellationToken>()))
    .ReturnsAsync(account);

// Giả lập: FindByUsernameAsync(bất kỳ string) → trả về null
_accountRepoMock
    .Setup(r => r.FindByUsernameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
    .ReturnsAsync((StaffAccount?)null);

// Giả lập: VerifyPassword với đúng input → true
_passwordHasherMock
    .Setup(h => h.VerifyPassword("correct_pass", "hashed_password"))
    .Returns(false);
```

### 4.2 — Setup property (không phải method)

```csharp
// Giả lập property RefreshTokenLifetime
_tokenServiceMock
    .Setup(t => t.RefreshTokenLifetime)
    .Returns(TimeSpan.FromDays(7));

_tokenServiceMock
    .Setup(t => t.AccessTokenLifetime)
    .Returns(TimeSpan.FromMinutes(15));
```

### 4.3 — Setup method Task (không trả về giá trị)

```csharp
// Giả lập: UpdateLastLoginAsync chạy xong không throw exception
_accountRepoMock
    .Setup(r => r.UpdateLastLoginAsync(
        account.AccountId,
        It.IsAny<DateTime>(),
        It.IsAny<CancellationToken>()))
    .Returns(Task.CompletedTask);
```

### 4.4 — Dùng It.IsAny<T>() khi không quan tâm giá trị cụ thể

```csharp
// It.IsAny<T>() — chấp nhận bất kỳ giá trị nào của kiểu T
It.IsAny<string>()
It.IsAny<long>()
It.IsAny<CancellationToken>()
It.IsAny<IEnumerable<string>>()

// It.Is<T>(predicate) — chỉ chấp nhận giá trị thỏa điều kiện
It.Is<string>(s => s.StartsWith("ADMIN"))
It.Is<long>(id => id > 0)
```

### 4.5 — Nhóm setup hay dùng thành Helper Method

```csharp
// Helper: setup token + session mặc định (tái sử dụng ở nhiều test)
private void SetupTokenAndSession()
{
    _tokenServiceMock.Setup(t => t.GenerateRefreshToken()).Returns("raw_refresh_token");
    _tokenServiceMock.Setup(t => t.HashToken("raw_refresh_token")).Returns("hashed_refresh_token");
    _tokenServiceMock.Setup(t => t.RefreshTokenLifetime).Returns(TimeSpan.FromDays(7));
    _tokenServiceMock.Setup(t => t.AccessTokenLifetime).Returns(TimeSpan.FromMinutes(15));
    _tokenServiceMock
        .Setup(t => t.GenerateAccessToken(
            It.IsAny<long>(), It.IsAny<string>(), It.IsAny<long>(),
            It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>?>()))
        .Returns("access_token_value");

    _sessionRepoMock
        .Setup(s => s.CreateSessionAsync(
            It.IsAny<long>(), It.IsAny<string>(), It.IsAny<DateTime>(),
            It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(new AuthSession { SessionId = 99 });
}

// Helper: setup LookupResolver trả về Id cho LOCKED status
private void SetupLookupLockedStatus()
{
    _lookupResolverMock
        .Setup(r => r.GetIdAsync(
            (ushort)Core.Enum.LookupType.AccountStatus,
            It.IsAny<System.Enum>(),
            It.IsAny<CancellationToken>()))
        .ReturnsAsync(3u); // LockedStatusId = 3
}
```

---

## Bước 5 — Viết Test Case theo mẫu AAA (Bước 13)

Mỗi test case tuân theo pattern **Arrange → Act → Assert**:

```csharp
[Fact]
public async Task LoginAsync_WhenAccountNotFound_ReturnsFailed()
{
    // ── Arrange (Chuẩn bị) ─────────────────────────────────────────────────
    // Setup mock để giả lập username không tồn tại
    _accountRepoMock
        .Setup(r => r.FindByUsernameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync((StaffAccount?)null);

    _accountRepoMock
        .Setup(r => r.FindByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync((StaffAccount?)null);

    var service = CreateService();                                 // tạo service
    var request = new LoginRequest("nonexistent_user", "pass");   // tạo input

    // ── Act (Thực thi) ─────────────────────────────────────────────────────
    var result = await service.LoginAsync(request);

    // ── Assert (Kiểm tra kết quả) ──────────────────────────────────────────
    result.Success.Should().BeFalse();
    result.ErrorCode.Should().Be("INVALID_CREDENTIALS");
}
```

**Quy tắc:**
- 1 test chỉ test 1 hành vi (1 kịch bản)
- Phần `Act` chỉ có đúng 1 dòng là lời gọi phương thức cần test
- Comment `// Arrange`, `// Act`, `// Assert` để dễ đọc

### Dùng `[Theory]` + `[InlineData]` khi test nhiều input cùng logic

```csharp
[Theory]
[InlineData("")]           // chuỗi rỗng
[InlineData("   ")]        // khoảng trắng
[InlineData(null)]         // null
public async Task LoginAsync_WhenUsernameIsInvalid_ReturnsFailed(string? username)
{
    // Arrange
    _accountRepoMock
        .Setup(r => r.FindByUsernameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync((StaffAccount?)null);
    _accountRepoMock
        .Setup(r => r.FindByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync((StaffAccount?)null);

    var service = CreateService();
    var request = new LoginRequest(username!, "any_pass");

    // Act
    var result = await service.LoginAsync(request);

    // Assert
    result.Success.Should().BeFalse();
}
```

---

## Bước 6 — Viết Assertion với FluentAssertions (Bước 14)

FluentAssertions cho phép viết assertion theo dạng câu tiếng Anh, thông điệp lỗi rõ hơn xUnit thuần.

### 6.1 — So sánh giá trị đơn

```csharp
result.Success.Should().BeTrue();
result.Success.Should().BeFalse();
result.ErrorCode.Should().Be("INVALID_CREDENTIALS");
result.AccessToken.Should().Be("access_token_value");
result.SessionId.Should().Be(99);
result.ExpiresIn.Should().BeGreaterThan(0);
```

### 6.2 — Kiểm tra null / not null

```csharp
result.RefreshToken.Should().BeNull();
result.RefreshToken.Should().NotBeNull();
result.AccessToken.Should().NotBeNullOrEmpty();
```

### 6.3 — Kiểm tra collection

```csharp
result.Roles.Should().Contain("ADMIN");
result.Roles.Should().HaveCount(1);
result.Roles.Should().NotBeEmpty();
result.Roles.Should().BeEmpty();
result.Roles.Should().BeEquivalentTo(new[] { "ADMIN", "MANAGER" });
```

### 6.4 — Kiểm tra exception (async)

```csharp
// Service ném exception khi gọi
var act = async () => await service.LoginAsync(request);

await act.Should().ThrowAsync<KeyNotFoundException>();
await act.Should().ThrowAsync<UnauthorizedAccessException>()
         .WithMessage("*credentials*");

// Service KHÔNG ném exception
await act.Should().NotThrowAsync();
```

### 6.5 — Thêm lý do vào assertion

```csharp
result.RefreshToken.Should().BeNull("No refresh token for password-change session");
result.RequirePasswordChange.Should().BeTrue("account is in LOCKED status");
```

---

## Bước 7 — Verify Mock đã được gọi (Bước 15)

Kiểm tra xem một phương thức của dependency có được gọi không, gọi bao nhiêu lần, với tham số gì.

### 7.1 — Verify đã được gọi đúng 1 lần

```csharp
// Sau khi login thành công, UpdateLastLoginAsync phải được gọi đúng 1 lần
_accountRepoMock.Verify(
    r => r.UpdateLastLoginAsync(
        account.AccountId,           // tham số cụ thể
        It.IsAny<DateTime>(),        // thời gian bất kỳ
        It.IsAny<CancellationToken>()),
    Times.Once);
```

### 7.2 — Verify KHÔNG được gọi

```csharp
// Khi account LOCKED, UpdateLastLoginAsync KHÔNG được gọi
_accountRepoMock.Verify(
    r => r.UpdateLastLoginAsync(
        It.IsAny<long>(),
        It.IsAny<DateTime>(),
        It.IsAny<CancellationToken>()),
    Times.Never);
```

### 7.3 — Các giá trị Times phổ biến

```csharp
Times.Once          // đúng 1 lần
Times.Never         // không lần nào
Times.Exactly(3)    // đúng 3 lần
Times.AtLeastOnce() // ít nhất 1 lần
Times.AtMost(2)     // tối đa 2 lần
```

---

## 16. Chạy test

### Chạy toàn bộ test

```powershell
cd d:\FPT_Document\SEP492\aulac_be\Tests
dotnet test
```

### Chạy với output chi tiết từng test

```powershell
dotnet test --logger "console;verbosity=normal"
```

### Chạy chỉ 1 class test

```powershell
dotnet test --filter "FullyQualifiedName~AuthServiceTests"
```

### Chạy chỉ 1 test method cụ thể

```powershell
dotnet test --filter "FullyQualifiedName~LoginAsync_WhenAccountNotFound_ReturnsFailed"
```

### Xem code coverage

```powershell
dotnet test --collect:"XPlat Code Coverage"
```

### Chạy từ VS Code

Mở **Testing** sidebar (icon ống nghiệm) → nhấn nút `Run All Tests` hoặc chạy từng test riêng lẻ.

---

## 17. Xử lý các tình huống đặc biệt

### 9.1 — Service dùng IOptions\<T\>

Dùng `Options.Create()` thay vì mock `IOptions<T>`:

```csharp
// ✅ Đúng
private readonly IOptions<ForgotPasswordRulesOptions> _forgotOpt =
    Options.Create(new ForgotPasswordRulesOptions
    {
        MaxAttempts        = 5,
        TokenLifetimeMinutes = 30
    });

// ❌ Không cần thiết — phức tạp hơn
var mockOptions = new Mock<IOptions<ForgotPasswordRulesOptions>>();
mockOptions.Setup(o => o.Value).Returns(new ForgotPasswordRulesOptions());
```

### 9.2 — Service dùng ILogger\<T\>

Mock ILogger nhưng thường không cần verify (logger không ảnh hưởng business logic):

```csharp
private readonly Mock<ILogger<AuthService>> _loggerMock = new();

// Truyền vào service, mock sẽ bỏ qua mọi lời gọi log
var service = new AuthService(..., _loggerMock.Object);
```

### 9.3 — Service dùng Extension Method với ILookupResolver

`AccountStatusCode.LOCKED.ToAccountStatusIdAsync(_lookupResolver)` là extension method gọi `_lookupResolver.GetIdAsync(typeId=1, enumValue)`.

Setup mock ở tầng thấp hơn (interface method, không phải extension method):

```csharp
_lookupResolverMock
    .Setup(r => r.GetIdAsync(
        (ushort)Core.Enum.LookupType.AccountStatus,   // typeId = 1
        It.IsAny<System.Enum>(),                       // bất kỳ enum nào
        It.IsAny<CancellationToken>()))
    .ReturnsAsync(3u);                                 // trả về LockedStatusId = 3
```

> **Quan trọng:** Nếu có 2 enum cùng tên (`Core.Entity.LookupType` và `Core.Enum.LookupType`), dùng fully qualified name: `Core.Enum.LookupType.AccountStatus`.

### 9.4 — Mock method trả về Task (void async)

```csharp
// Task (không có return value)
_accountRepoMock
    .Setup(r => r.UpdateLastLoginAsync(It.IsAny<long>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
    .Returns(Task.CompletedTask);

// Task<T> (có return value)
_sessionRepoMock
    .Setup(s => s.CreateSessionAsync(...))
    .ReturnsAsync(new AuthSession { SessionId = 99 });
```

### 9.5 — Test khi dependency throw exception

```csharp
[Fact]
public async Task LoginAsync_WhenRepositoryThrows_ShouldPropagateException()
{
    // Arrange
    _accountRepoMock
        .Setup(r => r.FindByUsernameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
        .ThrowsAsync(new InvalidOperationException("DB connection failed"));

    var service = CreateService();
    var request = new LoginRequest("admin", "pass");

    // Act
    var act = async () => await service.LoginAsync(request);

    // Assert
    await act.Should().ThrowAsync<InvalidOperationException>()
             .WithMessage("*DB connection failed*");
}
```

---

## 18. Checklist trước khi commit

Trước khi commit file test mới, kiểm tra:

- [ ] **Build thành công:** `dotnet build` không có error
- [ ] **Tất cả test pass:** `dotnet test` tất cả đều `Passed`
- [ ] **Tên test mô tả đủ kịch bản:** theo format `Method_Condition_ExpectedResult`
- [ ] **Metadata đầy đủ ở class level:** `Code Module`, `Method`, `Created By`, `Executed By`, `Test Req.`
- [ ] **Mỗi test có `[Trait("Type", "...")]`:** Normal / Boundary / Abnormal
- [ ] **Mỗi test có `[Trait("Method", "...")]`:** tên method đang test
- [ ] **Mỗi test có XML doc** mô tả Precondition, Input, Expected
- [ ] **Mỗi test có đúng 3 section:** `// Arrange`, `// Act`, `// Assert`
- [ ] **Phần Act chỉ có 1 dòng:** gọi duy nhất method cần test
- [ ] **Đủ 3 loại test case:** có ít nhất 1 Normal, 1 Boundary, 1 Abnormal cho mỗi method
- [ ] **Mock chỉ setup những gì cần:** không setup mock không dùng đến
- [ ] **Không có test phụ thuộc nhau:** mỗi test tự đủ, chạy độc lập

---

## Ví dụ đầy đủ — AuthServiceTests.cs

Xem file mẫu tại: [`Tests/Services/AuthServiceTests.cs`](Services/AuthServiceTests.cs)

Các kịch bản đã được test cho `AuthService.LoginAsync`:

| # | Test Method | Loại | Kịch bản | Kết quả mong đợi |
|---|---|---|---|---|
| 1 | `LoginAsync_WhenCredentialsValid_ReturnsSucceededWithTokens` | **Normal** | Username + password đúng, tài khoản ACTIVE | `Success=true`, có đầy đủ token |
| 2 | `LoginAsync_WhenFoundByEmail_AndPasswordCorrect_ReturnsSuccess` | **Normal** | Không có username, tìm thấy qua email | `Success=true`, có đầy đủ token |
| 3 | `LoginAsync_WhenAccountIsLocked_ReturnsPasswordChangeRequired` | **Normal** | Account LOCKED (lần đầu đăng nhập) | `RequirePasswordChange=true`, RefreshToken=null |
| 4 | `LoginAsync_OnSuccess_ShouldCallUpdateLastLogin` | **Normal** | Đăng nhập thành công | `UpdateLastLoginAsync` gọi đúng 1 lần |
| 5 | `LoginAsync_WhenPasswordChangeRequired_ShouldNotCallUpdateLastLogin` | **Normal** | Account LOCKED | `UpdateLastLoginAsync` không được gọi |
| 6 | `LoginAsync_WhenUsernameIsMaxLength_ReturnsFailedNotFound` | **Boundary** | Username dài đúng 100 ký tự (max DB) | `Success=false`, `INVALID_CREDENTIALS` |
| 7 | `LoginAsync_WhenStatusIsExactlyLockedId_ReturnsPasswordChangeRequired` | **Boundary** | `AccountStatusLvId` chính xác = LockedStatusId | `RequirePasswordChange=true` |
| 8 | `LoginAsync_WhenAccountNotFound_ReturnsFailed` | **Abnormal** | Username + email đều không tồn tại | `Success=false`, `INVALID_CREDENTIALS` |
| 9 | `LoginAsync_WhenPasswordIsWrong_ReturnsFailed` | **Abnormal** | Tìm thấy account nhưng sai password | `Success=false`, `INVALID_CREDENTIALS` |

# Unit Test Report Instructions — AuthService

> **Target Excel:** `Docs/Report5.1_Unit Test.xlsx`
> **Test file:** `Tests/Services/AuthServiceTests.cs`
> **Module:** AUTHENTICATION
> **Total tests:** 31  |  **Passed:** 31  |  **Failed:** 0

---

## 1. Sheet: MethodList (starting row 9)

Add these rows to the MethodList sheet — one per tested method:

| No | Module Name | Method Name | Sheet Name | Description | Pre-Condition |
|----|-------------|-------------|------------|-------------|---------------|
| (next) | AUTHENTICATION | LoginAsync | LoginAsync | Staff authenticates with username or email and receives access and refresh tokens, or a temporary token when the account requires a first-time password change | Account must exist for successful authentication scenarios |
| (next) | AUTHENTICATION | RefreshTokenAsync | RefreshTokenAsync | System rotates a refresh token and issues a new access token for an existing authenticated session, revoking sessions on security violations | Expired access token and refresh token must belong to an active session |
| (next) | AUTHENTICATION | LogoutAsync | LogoutAsync | Staff signs out by revoking the current authentication session and receives a boolean indicating whether the session existed | Session ID is available |
| (next) | AUTHENTICATION | ResetPasswordAsync | ResetPasswordAsync | User resets the account password using a one-time token received by email, consuming the token and revoking all active sessions on success | A valid password reset token must have been issued |
| (next) | AUTHENTICATION | ValidateSessionAsync | ValidateSessionAsync | System verifies that a session and its linked account are still active for incoming authenticated API requests, revoking the session when the account is deactivated | Session must exist in the store |

---

## 2. Sheet: Statistics (starting from the next available row after row 11)

| No | Function code | Passed | Failed | Untested | N | A | B | Total |
|----|---------------|--------|--------|----------|---|---|---|-------|
| (next) | LoginAsync | 10 | 0 | 0 | 5 | 3 | 2 | 10 |
| (next) | RefreshTokenAsync | 6 | 0 | 0 | 1 | 5 | 0 | 6 |
| (next) | LogoutAsync | 2 | 0 | 0 | 1 | 1 | 0 | 2 |
| (next) | ResetPasswordAsync | 5 | 0 | 0 | 1 | 4 | 0 | 5 |
| (next) | ValidateSessionAsync | 4 | 0 | 0 | 1 | 3 | 0 | 4 |
| **Sub total** | | **27** | **0** | **0** | **9** | **16** | **2** | **27** |

> Note: `RequestPasswordResetAsync` has 4 additional tests (1N/2A/1B) already documented. Grand total in file = 31 tests.

**Summary formulas:**
- Test coverage: `(31 + 0) / 31 × 100 = 100%`
- Test successful coverage: `31 / 31 × 100 = 100%`

---

## 3. Per-Method Sheets

---

### Sheet: LoginAsync

**Header (rows 1-5):**

| Field | Value |
|-------|-------|
| Code Module | Core/Service/AuthService.cs |
| Method | LoginAsync |
| Created By | quantm |
| Executed By | quantm |
| Test requirement | Staff authenticates with username or email, receives tokens on success, and is prompted for password change when the account status is LOCKED |
| Passed | 10 |
| Failed | 0 |
| Untested | 0 |
| N / A / B | 5 / 3 / 2 → Total = 10 |

**Test Case IDs (row 7, cols F+):** `UTCID01` – `UTCID10`

**Condition section:**

| Condition Group | Input Value | 01 | 02 | 03 | 04 | 05 | 06 | 07 | 08 | 09 | 10 |
|-----------------|-------------|----|----|----|----|----|----|----|----|----|----|
| username | `nonexistent_user` (not found by username or email) | O | | | | | | | | | |
| username | `admin` (exists) | | O | | O | O | O | O | O | | O |
| username | `admin@example.com` (email fallback) | | | O | | | | | | | |
| username | 100-character string (max length) | | | | | | | | | O | |
| password | Wrong password | | O | | | | | | | | |
| password | Correct password (`correct_pass`) | | | O | O | O | O | O | O | | O |
| accountStatus | ACTIVE | | O | O | | O | O | | | | |
| accountStatus | LOCKED (first-time login) | | | | O | | | O | | | O |
| accountStatus | INACTIVE | | | | | | | | O | | |

**Confirmation section:**

| Output | Expected Value | 01 | 02 | 03 | 04 | 05 | 06 | 07 | 08 | 09 | 10 |
|--------|---------------|----|----|----|----|----|----|----|----|----|----|
| Return | `Success=false`, `ErrorCode="INVALID_CREDENTIALS"` | O | O | | | | | | | O | |
| Return | `Success=true`, access token + refresh token returned | | | O | | O | | | | | |
| Return | `RequirePasswordChange=true`, `RefreshToken=null` | | | | O | | | | | | O |
| Verify | `UpdateLastLoginAsync` called once | | | | | | O | | | | |
| Verify | `UpdateLastLoginAsync` NOT called | | | | | | | O | | | |
| Return | `Success=false`, `ErrorCode="ACCOUNT_DEACTIVATED"` | | | | | | | | O | | |

**Result section:**

| UTCID | Type | P/F | Date |
|-------|------|-----|------|
| UTCID01 | A | P | 2026-04-16 |
| UTCID02 | A | P | 2026-04-16 |
| UTCID03 | N | P | 2026-04-16 |
| UTCID04 | N | P | 2026-04-16 |
| UTCID05 | N | P | 2026-04-16 |
| UTCID06 | N | P | 2026-04-16 |
| UTCID07 | N | P | 2026-04-16 |
| UTCID08 | A | P | 2026-04-16 |
| UTCID09 | B | P | 2026-04-16 |
| UTCID10 | B | P | 2026-04-16 |

---

### Sheet: RefreshTokenAsync

**Header:**
- Method: `RefreshTokenAsync`
- Test requirement: System rotates refresh tokens and issues a new access token for a valid session, blocking locked or inactive accounts and revoking all sessions on token reuse detection
- Passed: 6 | Failed: 0 | N/A/B: 1/5/0

**Test Case IDs:** `UTCID01` – `UTCID06`

**Condition section:**

| Condition Group | Input Value | UTCID01 | UTCID02 | UTCID03 | UTCID04 | UTCID05 | UTCID06 |
|-----------------|-------------|---------|---------|---------|---------|---------|---------|
| accessToken | Cannot extract principal (invalid token) | O | | | | | |
| accessToken | Principal exists but `session_id` claim is missing | | O | | | | |
| accessToken | Valid expired token (session_id=99, user_id=1) | | | O | O | O | O |
| refreshToken | Stale token (hash not in store) | | | O | | | |
| refreshToken | Valid token (hash matches session) | | | | O | O | O |
| accountState | Locked account (`IsLocked=true`) | | | | O | | |
| accountState | Inactive account (`accountStatus=INACTIVE`) | | | | | O | |
| accountState | Active account (`accountStatus=ACTIVE`) | | | | | | O |

**Confirmation section:**

| Output | Expected Value | UTCID01 | UTCID02 | UTCID03 | UTCID04 | UTCID05 | UTCID06 |
|--------|---------------|---------|---------|---------|---------|---------|---------|
| Return | `Success=false`, `ErrorCode="INVALID_TOKEN"` | O | O | | | | |
| Return | `Success=false`, `ErrorCode="INVALID_REFRESH_TOKEN"`; all sessions revoked | | | O | | | |
| Return | `Success=false`, `ErrorCode="ACCOUNT_UNAVAILABLE"`; current session revoked | | | | O | | |
| Return | `Success=false`, `ErrorCode="ACCOUNT_DEACTIVATED"`; all sessions revoked | | | | | O | |
| Return | New access token and new refresh token returned | | | | | | O |

**Result:**

| UTCID | Type | P/F | Date |
|-------|------|-----|------|
| UTCID01 | A | P | 2026-04-16 |
| UTCID02 | A | P | 2026-04-16 |
| UTCID03 | A | P | 2026-04-16 |
| UTCID04 | A | P | 2026-04-16 |
| UTCID05 | A | P | 2026-04-16 |
| UTCID06 | N | P | 2026-04-16 |

---

### Sheet: LogoutAsync

**Header:**
- Method: `LogoutAsync`
- Test requirement: Staff signs out by revoking the current session and receives a boolean indicating whether the session existed
- Passed: 2 | Failed: 0 | N/A/B: 1/1/0

**Test Case IDs:** `UTCID01`, `UTCID02`

**Condition section:**

| Condition Group | Input Value | UTCID01 | UTCID02 |
|-----------------|-------------|---------|---------|
| sessionId | `99` (existing active session) | O | |
| sessionId | `404` (non-existent session) | | O |

**Confirmation section:**

| Output | Expected Value | UTCID01 | UTCID02 |
|--------|---------------|---------|---------|
| Return | `true` (session found and revoked) | O | |
| Return | `false` (session not found) | | O |

**Result:**

| UTCID | Type | P/F | Date |
|-------|------|-----|------|
| UTCID01 | N | P | 2026-04-16 |
| UTCID02 | A | P | 2026-04-16 |

---

### Sheet: ResetPasswordAsync

**Header:**
- Method: `ResetPasswordAsync`
- Test requirement: User resets the account password using a one-time reset token, consuming the token and revoking all active sessions after a successful password update
- Passed: 5 | Failed: 0 | N/A/B: 1/4/0

**Test Case IDs:** `UTCID01` – `UTCID05`

**Condition section:**

| Condition Group | Input Value | UTCID01 | UTCID02 | UTCID03 | UTCID04 | UTCID05 |
|-----------------|-------------|---------|---------|---------|---------|---------|
| token | Blank / whitespace-only string | O | | | | |
| token | `missing-reset-token` (no stored record) | | O | | | |
| token | `expired-reset-token` (record exists but expired) | | | O | | |
| token | `locked-account-token` (account is locked) | | | | O | |
| token | `valid-reset-token` (valid record, active account) | | | | | O |
| newPassword | `NewPassword123!` | O | O | O | O | O |

**Confirmation section:**

| Output | Expected Value | UTCID01 | UTCID02 | UTCID03 | UTCID04 | UTCID05 |
|--------|---------------|---------|---------|---------|---------|---------|
| Exception | `InvalidOperationException("Invalid reset token.")` | O | | | | |
| Exception | `InvalidOperationException("Invalid or expired reset token.")` | | O | | | |
| Exception | `InvalidOperationException("Reset token has expired.")`; token consumed | | | O | | |
| Exception | `InvalidOperationException("User account not found or is locked.")`; token consumed | | | | O | |
| Verify | Password updated, token consumed, all sessions revoked | | | | | O |

**Result:**

| UTCID | Type | P/F | Date |
|-------|------|-----|------|
| UTCID01 | A | P | 2026-04-16 |
| UTCID02 | A | P | 2026-04-16 |
| UTCID03 | A | P | 2026-04-16 |
| UTCID04 | A | P | 2026-04-16 |
| UTCID05 | N | P | 2026-04-16 |

---

### Sheet: ValidateSessionAsync

**Header:**
- Method: `ValidateSessionAsync`
- Test requirement: System validates that a session and its linked account are both active, revoking the session when the account has been deactivated
- Passed: 4 | Failed: 0 | N/A/B: 1/3/0

**Test Case IDs:** `UTCID01` – `UTCID04`

**Condition section:**

| Condition Group | Input Value | UTCID01 | UTCID02 | UTCID03 | UTCID04 |
|-----------------|-------------|---------|---------|---------|---------|
| sessionId | `404` (session not found in store) | O | | | |
| sessionId | `99` (session found) | | O | O | O |
| account | Account not found (userId not in DB) | | O | | |
| account | Account status INACTIVE | | | O | |
| account | Account status ACTIVE | | | | O |

**Confirmation section:**

| Output | Expected Value | UTCID01 | UTCID02 | UTCID03 | UTCID04 |
|--------|---------------|---------|---------|---------|---------|
| Return | `false`; `FindByIdAsync` never called | O | | | |
| Return | `false`; session not revoked | | O | | |
| Return | `false`; session revoked immediately | | | O | |
| Return | `true`; `RevokeSessionAsync` never called | | | | O |

**Result:**

| UTCID | Type | P/F | Date |
|-------|------|-----|------|
| UTCID01 | A | P | 2026-04-16 |
| UTCID02 | A | P | 2026-04-16 |
| UTCID03 | A | P | 2026-04-16 |
| UTCID04 | N | P | 2026-04-16 |

---

## 4. Test Case ↔ C# Method Mapping

| UTCID | C# Test Method | Trait Type |
|-------|----------------|------------|
| LoginAsync UTCID01 | `LoginAsync_WhenAccountNotFound_ReturnsFailed` | A |
| LoginAsync UTCID02 | `LoginAsync_WhenPasswordIsWrong_ReturnsFailed` | A |
| LoginAsync UTCID03 | `LoginAsync_WhenFoundByEmail_AndPasswordCorrect_ReturnsSuccess` | N |
| LoginAsync UTCID04 | `LoginAsync_WhenAccountIsLocked_ReturnsPasswordChangeRequired` | N |
| LoginAsync UTCID05 | `LoginAsync_WhenCredentialsValid_ReturnsSucceededWithTokens` | N |
| LoginAsync UTCID06 | `LoginAsync_OnSuccess_ShouldCallUpdateLastLogin` | N |
| LoginAsync UTCID07 | `LoginAsync_WhenPasswordChangeRequired_ShouldNotCallUpdateLastLogin` | N |
| LoginAsync UTCID08 | `LoginAsync_WhenAccountIsInactive_ReturnsAccountDeactivated` | A |
| LoginAsync UTCID09 | `LoginAsync_WhenUsernameIsMaxLength_ReturnsFailedNotFound` | B |
| LoginAsync UTCID10 | `LoginAsync_WhenStatusIsExactlyLockedId_ReturnsPasswordChangeRequired` | B |
| RefreshTokenAsync UTCID01 | `RefreshTokenAsync_WhenAccessTokenIsInvalid_ReturnsInvalidToken` | A |
| RefreshTokenAsync UTCID02 | `RefreshTokenAsync_WhenSessionClaimMissing_ReturnsInvalidToken` | A |
| RefreshTokenAsync UTCID03 | `RefreshTokenAsync_WhenRefreshTokenIsInvalid_RevokesAllSessions` | A |
| RefreshTokenAsync UTCID04 | `RefreshTokenAsync_WhenAccountIsLocked_ReturnsAccountUnavailable` | A |
| RefreshTokenAsync UTCID05 | `RefreshTokenAsync_WhenAccountIsInactive_ReturnsAccountDeactivated` | A |
| RefreshTokenAsync UTCID06 | `RefreshTokenAsync_WhenRequestIsValid_RotatesTokensAndReturnsSuccess` | N |
| LogoutAsync UTCID01 | `LogoutAsync_WhenSessionExists_ReturnsTrue` | N |
| LogoutAsync UTCID02 | `LogoutAsync_WhenSessionDoesNotExist_ReturnsFalse` | A |
| ResetPasswordAsync UTCID01 | `ResetPasswordAsync_WhenTokenIsBlank_ThrowsInvalidOperationException` | A |
| ResetPasswordAsync UTCID02 | `ResetPasswordAsync_WhenTokenRecordIsMissing_ThrowsInvalidOperationException` | A |
| ResetPasswordAsync UTCID03 | `ResetPasswordAsync_WhenTokenExpired_ConsumesTokenAndThrows` | A |
| ResetPasswordAsync UTCID04 | `ResetPasswordAsync_WhenAccountIsLocked_ConsumesTokenAndThrows` | A |
| ResetPasswordAsync UTCID05 | `ResetPasswordAsync_WhenTokenIsValid_UpdatesPasswordConsumesTokenAndRevokesSessions` | N |
| ValidateSessionAsync UTCID01 | `ValidateSessionAsync_WhenSessionDoesNotExist_ReturnsFalse` | A |
| ValidateSessionAsync UTCID02 | `ValidateSessionAsync_WhenAccountDoesNotExist_ReturnsFalse` | A |
| ValidateSessionAsync UTCID03 | `ValidateSessionAsync_WhenAccountIsInactive_RevokesSessionAndReturnsFalse` | A |
| ValidateSessionAsync UTCID04 | `ValidateSessionAsync_WhenSessionAndAccountAreActive_ReturnsTrue` | N |

---

## Notes

- `RequestPasswordResetAsync` has 4 tests (1N/2A/1B) also present in `AuthServiceTests.cs`. They are documented in the existing sheet with UTCID01–UTCID04 and bring the file total to 31 tests.
- The `LoginAsync_WhenStatusIsExactlyLockedId_ReturnsPasswordChangeRequired` test (UTCID10) is a boundary case verifying that the exact LOCKED status ID triggers the password-change path.
- Run command: `dotnet test Tests/Tests.csproj --filter "FullyQualifiedName~AuthServiceTests"`


> **Hướng dẫn điền dữ liệu vào file `Docs/Report5.1_Unit Test.xlsx`**  
> Dựa trên kết quả chạy **31 test cases** từ file `Tests/Services/AuthServiceTests.cs`  
> Ngày chạy test: **2026-04-16**

---

## 1. Sheet: Cover

| Ô    | Nội dung |
|------|----------|
| B2   | `UNIT TEST DOCUMENT` |
| B4   | Project Name: `AuLac Restaurant` |
| B5   | Project Code: `AULAC_BE` |
| B6   | Document Code: `AULAC_BE_UnitTest_v1.0` |
| E4   | `quantm` |
| E5   | `2026-04-16` |
| E6   | `1.0` |

---

## 2. Sheet: MethodList

Add these rows to the MethodList sheet — one per tested method:

| No | Module Name | Method Name | Sheet Name | Description | Pre-Condition |
|----|-------------|-------------|------------|-------------|---------------|
| *n+1* | AUTHENTICATION | LoginAsync | LoginAsync | Staff authenticates with username or email and receives tokens or a password-change prompt depending on account status. | Account exists for successful authentication scenarios |
| *n+2* | AUTHENTICATION | RefreshTokenAsync | RefreshTokenAsync | System rotates a refresh token and issues a new access token for an existing authenticated session. | Expired access token and refresh token belong to a session |
| *n+3* | AUTHENTICATION | LogoutAsync | LogoutAsync | Staff signs out by revoking the current authentication session. | Session ID is available |
| *n+4* | AUTHENTICATION | RequestPasswordResetAsync | RequestPasswordResetAsync | User requests a password reset link by email without revealing whether the account exists. | N/A |
| *n+5* | AUTHENTICATION | ResetPasswordAsync | ResetPasswordAsync | User resets the password with a valid reset token and invalidates existing sessions. | Valid password reset token exists |
| *n+6* | AUTHENTICATION | ValidateSessionAsync | ValidateSessionAsync | System verifies that a session and its account are still active for authenticated API requests. | Session exists |

---

## 3. Sheet: Statistics

Thêm vào bảng thống kê:

| No | Function code | Passed | Failed | Untested | N | A | B | Total |
|----|---------------|--------|--------|----------|---|---|---|-------|
| *n+1* | LoginAsync | 10 | 0 | 0 | 5 | 3 | 2 | 10 |
| *n+2* | RefreshTokenAsync | 6 | 0 | 0 | 1 | 5 | 0 | 6 |
| *n+3* | LogoutAsync | 2 | 0 | 0 | 1 | 1 | 0 | 2 |
| *n+4* | RequestPasswordResetAsync | 4 | 0 | 0 | 1 | 2 | 1 | 4 |
| *n+5* | ResetPasswordAsync | 5 | 0 | 0 | 1 | 4 | 0 | 5 |
| *n+6* | ValidateSessionAsync | 4 | 0 | 0 | 1 | 3 | 0 | 4 |
| | **Sub Total** | **31** | **0** | **0** | **10** | **18** | **3** | **31** |

**Coverage:**
- Test coverage = (31 + 0) / 31 × 100 = **100%**
- Test successful coverage = 31 / 31 × 100 = **100%**

---

## 4. Per-Method Sheets

Mỗi method tạo 1 sheet riêng (copy từ sheet `Example`). Dưới đây là nội dung tương ứng.

---

### 4.1 Sheet: LoginAsync

**Header:**
- Code Module: AuthService
- Method: LoginAsync
- Test Req.: Staff authenticates with username or email, receives tokens on success, and is redirected to password change when the account requires it.
- Passed: 10 | Failed: 0 | Untested: 0 | N:5 A:3 B:2 → Total: 10

**Test Case IDs:**

| | UTCID01 | UTCID02 | UTCID03 | UTCID04 | UTCID05 | UTCID06 | UTCID07 | UTCID08 | UTCID09 | UTCID10 |
|--|---------|---------|---------|---------|---------|---------|---------|---------|---------|---------|
| **C# Method** | `LoginAsync_WhenAccountNotFound_ReturnsFailed` | `LoginAsync_WhenPasswordIsWrong_ReturnsFailed` | `LoginAsync_WhenFoundByEmail_AndPasswordCorrect_ReturnsSuccess` | `LoginAsync_WhenAccountIsLocked_ReturnsPasswordChangeRequired` | `LoginAsync_WhenCredentialsValid_ReturnsSucceededWithTokens` | `LoginAsync_OnSuccess_ShouldCallUpdateLastLogin` | `LoginAsync_WhenPasswordChangeRequired_ShouldNotCallUpdateLastLogin` | `LoginAsync_WhenAccountIsInactive_ReturnsAccountDeactivated` | `LoginAsync_WhenUsernameIsMaxLength_ReturnsFailedNotFound` | `LoginAsync_WhenStatusIsExactlyLockedId_ReturnsPasswordChangeRequired` |

**Condition matrix:**

| Condition | Input | 01 | 02 | 03 | 04 | 05 | 06 | 07 | 08 | 09 | 10 |
|-----------|-------|----|----|----|----|----|----|----|----|----|----|
| **username** | `nonexistent_user` | O | | | | | | | | | |
| **username** | `admin` | | O | | O | O | O | O | O | | O |
| **username** | `admin@example.com` | | | O | | | | | | | |
| **username** | 100-char string | | | | | | | | | O | |
| **password** | Wrong password | | O | | | | | | | | |
| **password** | Correct password | | | O | O | O | O | O | O | | O |
| **account lookup** | Account not found by username/email | O | | | | | | | | O | |
| **account status** | ACTIVE | | O | O | | O | O | | | | |
| **account status** | LOCKED | | | | O | | | O | | | O |
| **account status** | INACTIVE | | | | | | | | O | | |
| **lookup path** | Email fallback is used | | | O | | | | | | | |

**Confirm:**

| Output | Expected | 01 | 02 | 03 | 04 | 05 | 06 | 07 | 08 | 09 | 10 |
|--------|----------|----|----|----|----|----|----|----|----|----|----|
| **Return** | `Success=false`, `ErrorCode="INVALID_CREDENTIALS"` | O | O | | | | | | | O | |
| **Return** | `Success=true`, access token + refresh token returned | | | O | | O | | | | | |
| **Return** | `RequirePasswordChange=true`, refresh token is null | | | | O | | | | | | O |
| **Verify** | `UpdateLastLoginAsync` called once | | | | | | O | | | | |
| **Verify** | `UpdateLastLoginAsync` not called | | | | | | | O | | | |
| **Return** | `Success=false`, `ErrorCode="ACCOUNT_DEACTIVATED"` | | | | | | | | O | | |

**Result:**

| | UTCID01 | UTCID02 | UTCID03 | UTCID04 | UTCID05 | UTCID06 | UTCID07 | UTCID08 | UTCID09 | UTCID10 |
|--|---------|---------|---------|---------|---------|---------|---------|---------|---------|---------|
| Type | A | A | N | N | N | N | N | A | B | B |
| Passed/Failed | P | P | P | P | P | P | P | P | P | P |

Executed Date: `2026-04-16` for all cases. Defect ID: `—`.

---

### 4.2 Sheet: RefreshTokenAsync

**Header:**
- Code Module: AuthService
- Method: RefreshTokenAsync
- Test Req.: System rotates refresh tokens, blocks invalid or unavailable accounts, and issues a new access token only for a valid authenticated session.
- Passed: 6 | Failed: 0 | Untested: 0 | N:1 A:5 B:0 → Total: 6

**Test Case IDs:**

| | UTCID01 | UTCID02 | UTCID03 | UTCID04 | UTCID05 | UTCID06 |
|--|---------|---------|---------|---------|---------|---------|
| **C# Method** | `RefreshTokenAsync_WhenAccessTokenIsInvalid_ReturnsInvalidToken` | `RefreshTokenAsync_WhenSessionClaimMissing_ReturnsInvalidToken` | `RefreshTokenAsync_WhenRefreshTokenIsInvalid_RevokesAllSessions` | `RefreshTokenAsync_WhenAccountIsLocked_ReturnsAccountUnavailable` | `RefreshTokenAsync_WhenAccountIsInactive_ReturnsAccountDeactivated` | `RefreshTokenAsync_WhenRequestIsValid_RotatesTokensAndReturnsSuccess` |

**Condition matrix:**

| Condition | Input | 01 | 02 | 03 | 04 | 05 | 06 |
|-----------|-------|----|----|----|----|----|----|
| **expired access token** | Principal cannot be extracted | O | | | | | |
| **expired access token** | Principal exists but `session_id` claim missing | | O | | | | |
| **session** | Session `99` with stale refresh token | | | O | | | |
| **session** | Session `99` with valid refresh token | | | | O | O | O |
| **account state** | Locked account | | | | O | | |
| **account state** | Inactive account | | | | | O | |
| **account state** | Active account | | | | | | O |
| **refresh token** | Invalid refresh token | | | O | | | |
| **refresh token** | Valid refresh token | | | | O | O | O |

**Confirm:**

| Output | Expected | 01 | 02 | 03 | 04 | 05 | 06 |
|--------|----------|----|----|----|----|----|----|
| **Return** | `Success=false`, `ErrorCode="INVALID_TOKEN"` | O | O | | | | |
| **Return** | `Success=false`, `ErrorCode="INVALID_REFRESH_TOKEN"` and all sessions revoked | | | O | | | |
| **Return** | `Success=false`, `ErrorCode="ACCOUNT_UNAVAILABLE"` and current session revoked | | | | O | | |
| **Return** | `Success=false`, `ErrorCode="ACCOUNT_DEACTIVATED"` and all sessions revoked | | | | | O | |
| **Return** | New access token and new refresh token returned | | | | | | O |

**Result:**

| | UTCID01 | UTCID02 | UTCID03 | UTCID04 | UTCID05 | UTCID06 |
|--|---------|---------|---------|---------|---------|---------|
| Type | A | A | A | A | A | N |
| Passed/Failed | P | P | P | P | P | P |

Executed Date: `2026-04-16` for all cases. Defect ID: `—`.

---

### 4.3 Sheet: LogoutAsync

**Header:**
- Code Module: AuthService
- Method: LogoutAsync
- Test Req.: Staff signs out by revoking the current session and receives a boolean result indicating whether the session existed.
- Passed: 2 | Failed: 0 | Untested: 0 | N:1 A:1 B:0 → Total: 2

**Test Case IDs:**

| | UTCID01 | UTCID02 |
|--|---------|---------|
| **C# Method** | `LogoutAsync_WhenSessionExists_ReturnsTrue` | `LogoutAsync_WhenSessionDoesNotExist_ReturnsFalse` |

**Condition matrix:**

| Condition | Input | 01 | 02 |
|-----------|-------|----|----|
| **sessionId** | `99` (existing session) | O | |
| **sessionId** | `404` (missing session) | | O |

**Confirm:**

| Output | Expected | 01 | 02 |
|--------|----------|----|----|
| **Return** | `true` | O | |
| **Return** | `false` | | O |

**Result:**

| | UTCID01 | UTCID02 |
|--|---------|---------|
| Type | N | A |
| Passed/Failed | P | P |

Executed Date: `2026-04-16` for all cases. Defect ID: `—`.

---

### 4.4 Sheet: RequestPasswordResetAsync

**Header:**
- Code Module: AuthService
- Method: RequestPasswordResetAsync
- Test Req.: User requests a password reset link by email while the system avoids account enumeration and only queues email for an eligible account.
- Passed: 4 | Failed: 0 | Untested: 0 | N:1 A:2 B:1 → Total: 4

**Test Case IDs:**

| | UTCID01 | UTCID02 | UTCID03 | UTCID04 |
|--|---------|---------|---------|---------|
| **C# Method** | `RequestPasswordResetAsync_WhenEmailNotFound_DoesNotStoreTokenOrQueueEmail` | `RequestPasswordResetAsync_WhenAccountIsLocked_DoesNotStoreTokenOrQueueEmail` | `RequestPasswordResetAsync_WhenAccountExists_StoresTokenAndQueuesEmail` | `RequestPasswordResetAsync_WhenEmailHasWhitespace_NormalizesLookupAndSkipsQueueIfTemplateMissing` |

**Condition matrix:**

| Condition | Input | 01 | 02 | 03 | 04 |
|-----------|-------|----|----|----|----|
| **email** | `missing@example.com` | O | | | |
| **email** | `admin@example.com` | | O | O | |
| **email** | `  admin@example.com  ` | | | | O |
| **account state** | Account not found | O | | | |
| **account state** | Account locked | | O | | |
| **account state** | Active account | | | O | O |
| **template** | Reset template exists | | | O | |
| **template** | Reset template missing | | | | O |

**Confirm:**

| Output | Expected | 01 | 02 | 03 | 04 |
|--------|----------|----|----|----|----|
| **Verify** | No token invalidation, no token storage, no email queue | O | O | | |
| **Verify** | Token invalidated and new token stored | | | O | O |
| **Verify** | Reset email queued with rendered link | | | O | |
| **Verify** | Email normalized to `ADMIN@EXAMPLE.COM` and queue skipped | | | | O |

**Result:**

| | UTCID01 | UTCID02 | UTCID03 | UTCID04 |
|--|---------|---------|---------|---------|
| Type | A | A | N | B |
| Passed/Failed | P | P | P | P |

Executed Date: `2026-04-16` for all cases. Defect ID: `—`.

---

### 4.5 Sheet: ResetPasswordAsync

**Header:**
- Code Module: AuthService
- Method: ResetPasswordAsync
- Test Req.: User resets a password with a reset token, while the system rejects invalid tokens and revokes all active sessions after a successful reset.
- Passed: 5 | Failed: 0 | Untested: 0 | N:1 A:4 B:0 → Total: 5

**Test Case IDs:**

| | UTCID01 | UTCID02 | UTCID03 | UTCID04 | UTCID05 |
|--|---------|---------|---------|---------|---------|
| **C# Method** | `ResetPasswordAsync_WhenTokenIsBlank_ThrowsInvalidOperationException` | `ResetPasswordAsync_WhenTokenRecordIsMissing_ThrowsInvalidOperationException` | `ResetPasswordAsync_WhenTokenExpired_ConsumesTokenAndThrows` | `ResetPasswordAsync_WhenAccountIsLocked_ConsumesTokenAndThrows` | `ResetPasswordAsync_WhenTokenIsValid_UpdatesPasswordConsumesTokenAndRevokesSessions` |

**Condition matrix:**

| Condition | Input | 01 | 02 | 03 | 04 | 05 |
|-----------|-------|----|----|----|----|----|
| **token** | Blank token | O | | | | |
| **token** | `missing-reset-token` with no stored record | | O | | | |
| **token** | `expired-reset-token` with expired record | | | O | | |
| **token** | `locked-account-token` with locked user | | | | O | |
| **token** | `valid-reset-token` with active user | | | | | O |
| **new password** | `NewPassword123!` | O | O | O | O | O |

**Confirm:**

| Output | Expected | 01 | 02 | 03 | 04 | 05 |
|--------|----------|----|----|----|----|----|
| **Exception** | `InvalidOperationException("Invalid reset token.")` | O | | | | |
| **Exception** | `InvalidOperationException("Invalid or expired reset token.")` | | O | | | |
| **Exception** | `InvalidOperationException("Reset token has expired.")` and token consumed | | | O | | |
| **Exception** | `InvalidOperationException("User account not found or is locked.")` | | | | O | |
| **Verify** | Password updated, token consumed, all sessions revoked | | | | | O |

**Result:**

| | UTCID01 | UTCID02 | UTCID03 | UTCID04 | UTCID05 |
|--|---------|---------|---------|---------|---------|
| Type | A | A | A | A | N |
| Passed/Failed | P | P | P | P | P |

Executed Date: `2026-04-16` for all cases. Defect ID: `—`.

---

### 4.6 Sheet: ValidateSessionAsync

**Header:**
- Code Module: AuthService
- Method: ValidateSessionAsync
- Test Req.: System validates that a session exists, that the linked account still exists, and that inactive accounts cause the session to be revoked.
- Passed: 4 | Failed: 0 | Untested: 0 | N:1 A:3 B:0 → Total: 4

**Test Case IDs:**

| | UTCID01 | UTCID02 | UTCID03 | UTCID04 |
|--|---------|---------|---------|---------|
| **C# Method** | `ValidateSessionAsync_WhenSessionDoesNotExist_ReturnsFalse` | `ValidateSessionAsync_WhenAccountDoesNotExist_ReturnsFalse` | `ValidateSessionAsync_WhenAccountIsInactive_RevokesSessionAndReturnsFalse` | `ValidateSessionAsync_WhenSessionAndAccountAreActive_ReturnsTrue` |

**Condition matrix:**

| Condition | Input | 01 | 02 | 03 | 04 |
|-----------|-------|----|----|----|----|
| **session** | Missing session `404` | O | | | |
| **session** | Valid session `99` | | O | O | O |
| **account** | Account not found | | O | | |
| **account** | Inactive account | | | O | |
| **account** | Active account | | | | O |

**Confirm:**

| Output | Expected | 01 | 02 | 03 | 04 |
|--------|----------|----|----|----|----|
| **Return** | `false` because session does not exist | O | | | |
| **Return** | `false` because account does not exist | | O | | |
| **Return** | `false` and current session revoked | | | O | |
| **Return** | `true` and session remains active | | | | O |

**Result:**

| | UTCID01 | UTCID02 | UTCID03 | UTCID04 |
|--|---------|---------|---------|---------|
| Type | A | A | A | N |
| Passed/Failed | P | P | P | P |

Executed Date: `2026-04-16` for all cases. Defect ID: `—`.

---

## 5. Tổng kết

| Thông tin | Giá trị |
|-----------|---------|
| **Service** | AuthService |
| **File test** | `Tests/Services/AuthServiceTests.cs` |
| **Methods covered** | `LoginAsync`, `RefreshTokenAsync`, `LogoutAsync`, `RequestPasswordResetAsync`, `ResetPasswordAsync`, `ValidateSessionAsync` |
| **Tổng test** | 31 |
| **Passed** | 31 |
| **Failed** | 0 |
| **Normal (N)** | 10 |
| **Abnormal (A)** | 18 |
| **Boundary (B)** | 3 |
| **Test coverage** | 100% |
| **Run command** | `dotnet test Tests/Tests.csproj --filter "FullyQualifiedName~AuthServiceTests"` |

---

## 6. Ghi chú

- Bộ test đã được chạy thành công ngày `2026-04-16`: **31/31 PASSED**.
- Để tránh lỗi duplicate assembly attributes khi chạy project `Tests`, file `Tests/Tests.csproj` đã được thêm exclusion cho `AuthModuleTests/**` là artifact cũ nằm trong thư mục test.
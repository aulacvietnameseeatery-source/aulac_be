# Unit Test Report Instructions — AccountService

> **Target Excel:** `Docs/Report5.1_Unit Test.xlsx`
> **Test file:** `Tests/Services/AccountServiceTests.cs`
> **Module:** ACCOUNT
> **Total tests:** 32  |  **Passed:** 32  |  **Failed:** 0

---

## 1. Sheet: MethodList (starting row 9)

Add these rows to the MethodList sheet — one per tested method:

| No | Module Name | Method Name | Sheet Name | Description | Pre-Condition |
|----|-------------|-------------|------------|-------------|---------------|
| (next) | ACCOUNT | CreateAccountAsync | CreateAccountAsync | Admin creates a new staff account with an auto-generated username, a temporary password, and queues a welcome email containing the credentials | Email must be unique; role must exist |
| (next) | ACCOUNT | UpdateAccountAsync | UpdateAccountAsync | Admin or manager updates a staff account's profile information including email, full name, phone, and optionally role, with authorization enforced for role changes | Account must exist |
| (next) | ACCOUNT | GetAccountDetailAsync | GetAccountDetailAsync | Admin views the full details of a staff account including the resolved status label and assigned role information | N/A |
| (next) | ACCOUNT | ResetToDefaultPasswordAsync | ResetToDefaultPasswordAsync | Admin resets a staff account's password to the system default, locks the account, and notifies the user by email | Account must exist; default password must be configured |
| (next) | ACCOUNT | ChangePasswordAsync | ChangePasswordAsync | System changes a staff account's password and automatically activates a locked account after the first successful password change | Account must exist; new password must meet length requirement |
| (next) | ACCOUNT | ChangePasswordForSelfAsync | ChangePasswordForSelfAsync | Staff member changes their own password by providing the current password for verification, or skips that step when performing a first-time password change on a locked account | Account must exist; new password must meet length requirement |
| (next) | ACCOUNT | GetAccountByIdAsync | GetAccountByIdAsync | System retrieves a staff account by ID, with an option to include the role and permissions in the response | N/A |
| (next) | ACCOUNT | GetAccountsAsync | GetAccountsAsync | Admin retrieves a paginated list of staff accounts filtered by search term, role, or status | N/A |

---

## 2. Sheet: Statistics (starting from the next available row after row 11)

| No | Function code | Passed | Failed | Untested | N | A | B | Total |
|----|---------------|--------|--------|----------|---|---|---|-------|
| (next) | CreateAccountAsync | 5 | 0 | 0 | 1 | 2 | 2 | 5 |
| (next) | UpdateAccountAsync | 5 | 0 | 0 | 1 | 3 | 1 | 5 |
| (next) | GetAccountDetailAsync | 3 | 0 | 0 | 1 | 1 | 1 | 3 |
| (next) | ResetToDefaultPasswordAsync | 4 | 0 | 0 | 1 | 2 | 1 | 4 |
| (next) | ChangePasswordAsync | 4 | 0 | 0 | 1 | 2 | 1 | 4 |
| (next) | ChangePasswordForSelfAsync | 6 | 0 | 0 | 2 | 3 | 1 | 6 |
| (next) | GetAccountByIdAsync | 3 | 0 | 0 | 1 | 1 | 1 | 3 |
| (next) | GetAccountsAsync | 2 | 0 | 0 | 1 | 0 | 1 | 2 |
| **Sub total** | | **32** | **0** | **0** | **9** | **14** | **9** | **32** |

**Summary formulas:**
- Test coverage: `(32 + 0) / 32 × 100 = 100%`
- Test successful coverage: `32 / 32 × 100 = 100%`

---

## 3. Per-Method Sheets

---

### Sheet: CreateAccountAsync

**Header (rows 1-5):**

| Field | Value |
|-------|-------|
| Code Module | Core/Service/AccountService.cs |
| Method | CreateAccountAsync |
| Created By | quantm |
| Executed By | quantm |
| Test requirement | Admin creates a new staff account with an auto-generated username and temporary password, queues a welcome email, and returns the result including email delivery status |
| Passed | 5 |
| Failed | 0 |
| Untested | 0 |
| N / A / B | 1 / 2 / 2 → Total = 5 |

**Test Case IDs (row 7, cols F+):** `UTCID01`, `UTCID02`, `UTCID03`, `UTCID04`, `UTCID05`

**Condition section:**

| Condition Group | Input Value | UTCID01 | UTCID02 | UTCID03 | UTCID04 | UTCID05 |
|-----------------|-------------|---------|---------|---------|---------|---------|
| email | `nguyen.van.a@example.com` (unique) | O | | | O | |
| email | `existing@example.com` (already registered) | | O | | | |
| email | `new@example.com` (unique) | | | O | | |
| email | `user@example.com` (unique) | | | | | |
| email | `user2@example.com` (unique) | | | | | O |
| roleId | `1` (exists) | O | | | O | O |
| roleId | `999` (not found) | | | O | | |
| emailTemplate | ACCOUNT_CREATED template found | O | | | | O |
| emailTemplate | ACCOUNT_CREATED template not found | | | | O | |
| emailQueue | EnqueueAsync succeeds | O | | | | |
| emailQueue | EnqueueAsync throws exception | | | | | O |

**Confirmation section:**

| Output | Expected Value | UTCID01 | UTCID02 | UTCID03 | UTCID04 | UTCID05 |
|--------|---------------|---------|---------|---------|---------|---------|
| Return | `AccountId=10`, `TemporaryPasswordSent=true` | O | | | | |
| Exception | `ConflictException("*already registered*")` | | O | | | |
| Exception | `NotFoundException("*Role ID 999*not found*")` | | | O | | |
| Return | `AccountId=5`, `TemporaryPasswordSent=false` (no template) | | | | O | |
| Return | `AccountId=6`, `TemporaryPasswordSent=false` (queue failed) | | | | | O |

**Result section:**

| UTCID | Type | P/F | Date |
|-------|------|-----|------|
| UTCID01 | N | P | 2026-04-18 |
| UTCID02 | A | P | 2026-04-18 |
| UTCID03 | A | P | 2026-04-18 |
| UTCID04 | B | P | 2026-04-18 |
| UTCID05 | B | P | 2026-04-18 |

---

### Sheet: UpdateAccountAsync

**Header:**
- Method: `UpdateAccountAsync`
- Test requirement: Admin or manager updates a staff account's profile, with email uniqueness validation and role-change authorization enforced
- Passed: 5 | Failed: 0 | N/A/B: 1/3/1

**Test Case IDs:** `UTCID01` – `UTCID05`

**Condition section:**

| Condition Group | Input Value | UTCID01 | UTCID02 | UTCID03 | UTCID04 | UTCID05 |
|-----------------|-------------|---------|---------|---------|---------|---------|
| accountId | `1` (exists) | O | | O | O | O |
| accountId | `999` (not found) | | O | | | |
| request.FullName | `"Nguyễn Văn B"` (different from current) | O | | | | |
| request.FullName | Same as current (`"Nguyễn Văn A"`) | | | | | O |
| request.Email | `"taken@example.com"` (already registered) | | | O | | |
| request.RoleId | `3` (change role, requester is non-admin) | | | | O | |
| request.RoleId | not provided (no change) | O | O | O | | O |
| requesterRole | N/A (no role check needed) | O | O | O | | O |
| requesterRole | `STAFF` (non-admin attempting role change) | | | | O | |

**Confirmation section:**

| Output | Expected Value | UTCID01 | UTCID02 | UTCID03 | UTCID04 | UTCID05 |
|--------|---------------|---------|---------|---------|---------|---------|
| Verify | `UpdateAccountAsync` called with `FullName="Nguyễn Văn B"` | O | | | | |
| Exception | `NotFoundException("*Account ID 999*not found*")` | | O | | | |
| Exception | `ConflictException("*taken@example.com*already registered*")` | | | O | | |
| Exception | `ForbiddenException("*Only administrators*")` | | | | O | |
| Verify | `UpdateAccountAsync` NOT called (no changes detected) | | | | | O |

**Result:**

| UTCID | Type | P/F | Date |
|-------|------|-----|------|
| UTCID01 | N | P | 2026-04-18 |
| UTCID02 | A | P | 2026-04-18 |
| UTCID03 | A | P | 2026-04-18 |
| UTCID04 | A | P | 2026-04-18 |
| UTCID05 | B | P | 2026-04-18 |

---

### Sheet: GetAccountDetailAsync

**Header:**
- Method: `GetAccountDetailAsync`
- Test requirement: Admin retrieves the full detail of a staff account with the account status resolved to a string label and role information included
- Passed: 3 | Failed: 0 | N/A/B: 1/1/1

**Test Case IDs:** `UTCID01`, `UTCID02`, `UTCID03`

**Condition section:**

| Condition Group | Input Value | UTCID01 | UTCID02 | UTCID03 |
|-----------------|-------------|---------|---------|---------|
| accountId | `1` (exists, status=ACTIVE) | O | | |
| accountId | `999` (not found) | | O | |
| accountId | `0` (boundary: zero ID) | | | O |

**Confirmation section:**

| Output | Expected Value | UTCID01 | UTCID02 | UTCID03 |
|--------|---------------|---------|---------|---------|
| Return | `AccountDetailDto` with `AccountId=1`, `AccountStatus="ACTIVE"`, `Role != null` | O | | |
| Return | `null` (account not found) | | O | |
| Verify | `FindByIdWithRoleAsync(0)` called; returns `null` | | | O |

**Result:**

| UTCID | Type | P/F | Date |
|-------|------|-----|------|
| UTCID01 | N | P | 2026-04-18 |
| UTCID02 | A | P | 2026-04-18 |
| UTCID03 | B | P | 2026-04-18 |

---

### Sheet: ResetToDefaultPasswordAsync

**Header:**
- Method: `ResetToDefaultPasswordAsync`
- Test requirement: Admin resets a staff account's password to the system default, locks the account, and queues a notification email to the user
- Passed: 4 | Failed: 0 | N/A/B: 1/2/1

**Test Case IDs:** `UTCID01` – `UTCID04`

**Condition section:**

| Condition Group | Input Value | UTCID01 | UTCID02 | UTCID03 | UTCID04 |
|-----------------|-------------|---------|---------|---------|---------|
| defaultPassword | `"DefaultPass123!"` (configured) | O | O | | O |
| defaultPassword | `null` (not configured) | | | O | |
| accountId | `1` (exists) | O | | | O |
| accountId | `999` (not found) | | O | | |
| emailTemplate | `DEFAULT_PASSWORD_RESET` template found | O | | | |
| emailTemplate | `DEFAULT_PASSWORD_RESET` template not found | | | | O |

**Confirmation section:**

| Output | Expected Value | UTCID01 | UTCID02 | UTCID03 | UTCID04 |
|--------|---------------|---------|---------|---------|---------|
| Return | `Success=true`; account updated with `IsLocked=true`; email queued | O | | | |
| Exception | `NotFoundException("*Account*999*not found*")` | | O | | |
| Exception | `InvalidOperationException("*Default password*not configured*")` | | | O | |
| Return | `Success=true`; account updated; email NOT queued (no template) | | | | O |

**Result:**

| UTCID | Type | P/F | Date |
|-------|------|-----|------|
| UTCID01 | N | P | 2026-04-18 |
| UTCID02 | A | P | 2026-04-18 |
| UTCID03 | A | P | 2026-04-18 |
| UTCID04 | B | P | 2026-04-18 |

---

### Sheet: ChangePasswordAsync

**Header:**
- Method: `ChangePasswordAsync`
- Test requirement: System changes a staff account's password and activates the account if it was previously locked, enforcing a minimum length of 8 characters
- Passed: 4 | Failed: 0 | N/A/B: 1/2/1

**Test Case IDs:** `UTCID01` – `UTCID04`

**Condition section:**

| Condition Group | Input Value | UTCID01 | UTCID02 | UTCID03 | UTCID04 |
|-----------------|-------------|---------|---------|---------|---------|
| accountId | `1` (exists, LOCKED, `IsLocked=true`) | O | | | |
| accountId | `999` (not found) | | O | | |
| newPassword | `"NewPass123!"` (8+ chars, valid) | O | O | | |
| newPassword | whitespace only `"   "` (empty after trim) | | | O | |
| newPassword | `"Pass12!"` (7 chars, below minimum) | | | | O |

**Confirmation section:**

| Output | Expected Value | UTCID01 | UTCID02 | UTCID03 | UTCID04 |
|--------|---------------|---------|---------|---------|---------|
| Verify | Account updated: `PasswordHash="new_hashed"`, `IsLocked=false`, `AccountStatusLvId=ACTIVE` | O | | | |
| Exception | `NotFoundException("*Account*999*not found*")` | | O | | |
| Exception | `ArgumentException("*Password cannot be empty*")` | | | O | |
| Exception | `ArgumentException("*Password must be at least 8 characters*")` | | | | O |

**Result:**

| UTCID | Type | P/F | Date |
|-------|------|-----|------|
| UTCID01 | N | P | 2026-04-18 |
| UTCID02 | A | P | 2026-04-18 |
| UTCID03 | A | P | 2026-04-18 |
| UTCID04 | B | P | 2026-04-18 |

---

### Sheet: ChangePasswordForSelfAsync

**Header:**
- Method: `ChangePasswordForSelfAsync`
- Test requirement: Staff member changes their own password with current password verification for active accounts, or skips it for a first-time change on a locked account, then activates the account
- Passed: 6 | Failed: 0 | N/A/B: 2/3/1

**Test Case IDs:** `UTCID01` – `UTCID06`

**Condition section:**

| Condition Group | Input Value | UTCID01 | UTCID02 | UTCID03 | UTCID04 | UTCID05 | UTCID06 |
|-----------------|-------------|---------|---------|---------|---------|---------|---------|
| accountId | `1` (exists, ACTIVE, `IsLocked=false`) | O | | | O | O | |
| accountId | `1` (exists, LOCKED, `IsLocked=true`) | | O | | | | |
| accountId | `999` (not found) | | | O | | | |
| currentPassword | `"CurrentPass1!"` (correct) | O | | | | | |
| currentPassword | `null` (first-time change, locked account) | | O | | | | |
| currentPassword | `null` (active account, missing) | | | | | O | |
| currentPassword | `"WrongPass!"` (incorrect) | | | | O | | |
| newPassword | `"NewPass123!"` (8+ chars, valid) | O | O | O | O | O | |
| newPassword | `"Short7!"` (7 chars, below minimum) | | | | | | O |

**Confirmation section:**

| Output | Expected Value | UTCID01 | UTCID02 | UTCID03 | UTCID04 | UTCID05 | UTCID06 |
|--------|---------------|---------|---------|---------|---------|---------|---------|
| Return | `true`; account updated with new password | O | | | | | |
| Return | `true`; account activated (`IsLocked=false`, `status=ACTIVE`) | | O | | | | |
| Exception | `NotFoundException("*Account*999*not found*")` | | | O | | | |
| Exception | `ValidationException("*Current password is incorrect*")` | | | | O | | |
| Exception | `ValidationException("*Current password is required*")` | | | | | O | |
| Exception | `ValidationException("*Password must be at least 8 characters*")` | | | | | | O |

**Result:**

| UTCID | Type | P/F | Date |
|-------|------|-----|------|
| UTCID01 | N | P | 2026-04-18 |
| UTCID02 | N | P | 2026-04-18 |
| UTCID03 | A | P | 2026-04-18 |
| UTCID04 | A | P | 2026-04-18 |
| UTCID05 | A | P | 2026-04-18 |
| UTCID06 | B | P | 2026-04-18 |

---

### Sheet: GetAccountByIdAsync

**Header:**
- Method: `GetAccountByIdAsync`
- Test requirement: System retrieves a staff account by ID and optionally includes role and permissions, returning null when not found
- Passed: 3 | Failed: 0 | N/A/B: 1/1/1

**Test Case IDs:** `UTCID01`, `UTCID02`, `UTCID03`

**Condition section:**

| Condition Group | Input Value | UTCID01 | UTCID02 | UTCID03 |
|-----------------|-------------|---------|---------|---------|
| accountId | `1` (exists) | O | | O |
| accountId | `999` (not found) | | O | |
| includeRole | `true` (load role via `FindByIdWithRoleAsync`) | O | | |
| includeRole | `false` (load via `FindByIdAsync`, no role) | | O | O |

**Confirmation section:**

| Output | Expected Value | UTCID01 | UTCID02 | UTCID03 |
|--------|---------------|---------|---------|---------|
| Return | `AccountDto` with `AccountId=1`, `Role != null`, `RoleCode="STAFF"` | O | | |
| Return | `null` (account not found) | | O | |
| Return | `AccountDto` with `Role=null`; `FindByIdWithRoleAsync` never called | | | O |

**Result:**

| UTCID | Type | P/F | Date |
|-------|------|-----|------|
| UTCID01 | N | P | 2026-04-18 |
| UTCID02 | A | P | 2026-04-18 |
| UTCID03 | B | P | 2026-04-18 |

---

### Sheet: GetAccountsAsync

**Header:**
- Method: `GetAccountsAsync`
- Test requirement: Admin retrieves a paginated list of staff accounts, delegating directly to the repository with the query parameters
- Passed: 2 | Failed: 0 | N/A/B: 1/0/1

**Test Case IDs:** `UTCID01`, `UTCID02`

**Condition section:**

| Condition Group | Input Value | UTCID01 | UTCID02 |
|-----------------|-------------|---------|---------|
| query | `PageIndex=1, PageSize=10` (default paging) | O | O |
| repositoryReturn | 2 account records | O | |
| repositoryReturn | 0 records (empty result) | | O |

**Confirmation section:**

| Output | Expected Value | UTCID01 | UTCID02 |
|--------|---------------|---------|---------|
| Return | `PagedResultDTO` with `TotalCount=2`, `PageData.Count=2` | O | |
| Return | `PagedResultDTO` with `TotalCount=0`, `PageData` empty | | O |

**Result:**

| UTCID | Type | P/F | Date |
|-------|------|-----|------|
| UTCID01 | N | P | 2026-04-18 |
| UTCID02 | B | P | 2026-04-18 |

---

## 4. Test Case ↔ C# Method Mapping

| UTCID | C# Test Method | Trait Type |
|-------|----------------|------------|
| CreateAccountAsync UTCID01 | `CreateAccountAsync_WhenEmailUniqueAndRoleExists_CreatesAccountAndQueuesEmail` | N |
| CreateAccountAsync UTCID02 | `CreateAccountAsync_WhenEmailDuplicate_ThrowsConflictException` | A |
| CreateAccountAsync UTCID03 | `CreateAccountAsync_WhenRoleNotFound_ThrowsNotFoundException` | A |
| CreateAccountAsync UTCID04 | `CreateAccountAsync_WhenEmailTemplateNotFound_StillCreatesAccountWithEmailSentFalse` | B |
| CreateAccountAsync UTCID05 | `CreateAccountAsync_WhenEmailQueueThrows_StillReturnsSuccessResult` | B |
| UpdateAccountAsync UTCID01 | `UpdateAccountAsync_WhenFullNameChanges_UpdatesAndReturnsDetail` | N |
| UpdateAccountAsync UTCID02 | `UpdateAccountAsync_WhenAccountNotFound_ThrowsNotFoundException` | A |
| UpdateAccountAsync UTCID03 | `UpdateAccountAsync_WhenNewEmailAlreadyRegistered_ThrowsConflictException` | A |
| UpdateAccountAsync UTCID04 | `UpdateAccountAsync_WhenNonAdminAttemptsRoleChange_ThrowsForbiddenException` | A |
| UpdateAccountAsync UTCID05 | `UpdateAccountAsync_WhenNoFieldsChange_SkipsRepositoryUpdateCall` | B |
| GetAccountDetailAsync UTCID01 | `GetAccountDetailAsync_WhenAccountExists_ReturnsDetailDto` | N |
| GetAccountDetailAsync UTCID02 | `GetAccountDetailAsync_WhenAccountNotFound_ReturnsNull` | A |
| GetAccountDetailAsync UTCID03 | `GetAccountDetailAsync_WhenAccountIdIsZero_ReturnsNull` | B |
| ResetToDefaultPasswordAsync UTCID01 | `ResetToDefaultPasswordAsync_WhenValid_ResetsPasswordLocksAccountAndQueuesEmail` | N |
| ResetToDefaultPasswordAsync UTCID02 | `ResetToDefaultPasswordAsync_WhenAccountNotFound_ThrowsNotFoundException` | A |
| ResetToDefaultPasswordAsync UTCID03 | `ResetToDefaultPasswordAsync_WhenDefaultPasswordNotConfigured_ThrowsInvalidOperationException` | A |
| ResetToDefaultPasswordAsync UTCID04 | `ResetToDefaultPasswordAsync_WhenEmailTemplateNotFound_StillResetsPassword` | B |
| ChangePasswordAsync UTCID01 | `ChangePasswordAsync_WhenLockedAccount_ChangesPasswordAndActivatesAccount` | N |
| ChangePasswordAsync UTCID02 | `ChangePasswordAsync_WhenAccountNotFound_ThrowsNotFoundException` | A |
| ChangePasswordAsync UTCID03 | `ChangePasswordAsync_WhenPasswordIsEmpty_ThrowsArgumentException` | A |
| ChangePasswordAsync UTCID04 | `ChangePasswordAsync_WhenPasswordIsSevenChars_ThrowsArgumentException` | B |
| ChangePasswordForSelfAsync UTCID01 | `ChangePasswordForSelfAsync_WhenActiveAndCurrentPasswordCorrect_ChangesPassword` | N |
| ChangePasswordForSelfAsync UTCID02 | `ChangePasswordForSelfAsync_WhenFirstTimeChange_ChangesWithoutCurrentPasswordAndActivates` | N |
| ChangePasswordForSelfAsync UTCID03 | `ChangePasswordForSelfAsync_WhenAccountNotFound_ThrowsNotFoundException` | A |
| ChangePasswordForSelfAsync UTCID04 | `ChangePasswordForSelfAsync_WhenCurrentPasswordIsWrong_ThrowsValidationException` | A |
| ChangePasswordForSelfAsync UTCID05 | `ChangePasswordForSelfAsync_WhenActiveAccountAndNoCurrentPassword_ThrowsValidationException` | A |
| ChangePasswordForSelfAsync UTCID06 | `ChangePasswordForSelfAsync_WhenNewPasswordTooShort_ThrowsValidationException` | B |
| GetAccountByIdAsync UTCID01 | `GetAccountByIdAsync_WhenAccountExistsWithRole_ReturnsAccountDtoWithRole` | N |
| GetAccountByIdAsync UTCID02 | `GetAccountByIdAsync_WhenAccountNotFound_ReturnsNull` | A |
| GetAccountByIdAsync UTCID03 | `GetAccountByIdAsync_WhenIncludeRoleFalse_ReturnsAccountDtoWithoutRole` | B |
| GetAccountsAsync UTCID01 | `GetAccountsAsync_WhenCalled_DelegatesAndReturnsPagedResult` | N |
| GetAccountsAsync UTCID02 | `GetAccountsAsync_WhenNoAccountsExist_ReturnsEmptyPagedResult` | B |

---

## Notes

- Run command: `dotnet test Tests/Tests.csproj --filter "FullyQualifiedName~AccountServiceTests"`
- All 32 tests passed on 2026-04-18.
- `ChangePasswordAsync` is called internally by the system (e.g., after token-based password reset). `ChangePasswordForSelfAsync` is called by the user through the API.
- `GetAccountsAsync` is a thin delegation method; tests verify the delegation and empty-result boundary.

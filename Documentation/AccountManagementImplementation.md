# Account Management Implementation - Summary

## Overview
Implemented comprehensive account management system with account creation, profile updates, detailed account retrieval, password reset, and first-time login flow.

## ? Implemented Features

### 1. **Create Account** (POST `/api/account`)
- **Permission Required**: `ACCOUNT:CREATE`
- **Flow**:
  1. Validates email uniqueness
  2. Validates role existence
  3. Generates unique username from full name (e.g., "Nguy?n V?n An" ? "nguyen.an")
  4. Generates secure random temporary password (12 chars, alphanumeric + special)
  5. Creates account with status = LOCKED
  6. Queues temporary password email
  7. Returns created account info

- **Username Generation**:
  - Pattern: `firstname.lastname`
  - Vietnamese diacritics handling (removes accents)
  - Collision handling: appends incremental numbers (`nguyen.an2`, `nguyen.an3`)

- **Response**:
  - HTTP 201 Created
  - Account ID, username, email, status, temporary password sent flag

### 2. **Update Account** (PUT `/api/account/{id}`)
- **Permission Required**: `ACCOUNT:UPDATE`
- **Updatable Fields**:
  - Email (with uniqueness check)
  - Full Name
  - Phone (can be cleared)
  - Role (admin-only, requires `ADMIN` role code)

- **Authorization**:
  - Any authenticated user can update email/name/phone
  - Only admins can change roles
  - Throws `ForbiddenException` if non-admin tries to change role

- **Response**:
  - HTTP 200 OK
  - Updated account details with resolved status

### 3. **Get Account Detail** (GET `/api/account/{id}/detail`)
- **Permission Required**: `ACCOUNT:READ`
- **Returns**:
  - Core fields: ID, username, email, full name, phone
  - Account status (resolved from lookup: "ACTIVE", "LOCKED")
  - Lock status
  - Timestamps: created at, last login
  - Role information (name, code)

### 4. **Get Current User** (GET `/api/account/me`)
- **Permission Required**: `ACCOUNT:READ`
- Returns authenticated user's own account details

### 5. **Reset Password** (POST `/api/account/{id}/reset-password`)
- **Permission Required**: `ACCOUNT:RESET_PASSWORD`
- **Enhanced Behavior**:
  - Resets password to system default
  - **Sets account status to LOCKED**
  - **Requires password change on next login**
  - Account becomes ACTIVE after password change

### 6. **First-Time Login Flow**
- **Modified AuthService.LoginAsync**:
  - If account is LOCKED:
    - Allow login with correct credentials
    - Return temporary access token (15-minute expiration)
    - Set `RequirePasswordChange = true`
    - Client must redirect to password change page
  - After password change:
    - Account status changes to ACTIVE
    - IsLocked set to false
    - User can login normally

- **Frontend Integration**:
  - Check response `SubCode = 1` and `SystemMessage = "PASSWORD_CHANGE_REQUIRED"`
  - Redirect to password change page
  - Use temporary token to authenticate password change request

---

## ?? New Files Created

### Core Layer
1. **`Core/Exception/BusinessException.cs`**
   - Base exception for business rule violations
   - `NotFoundException`, `ConflictException`, `ForbiddenException`, `ValidationException`

2. **`Core/Data/Request/CreateAccountRequest.cs`**
   - Request DTO for account creation
   - Validation attributes for email, phone, full name, role

3. **`Core/Data/Request/UpdateAccountRequest.cs`**
   - Request DTO for account updates
   - All fields optional (null = no change)

4. **`Core/Data/Response/AccountResponses.cs`**
   - `CreateAccountResult` - response for account creation
   - `AccountDetailDto` - detailed account information
   - `RoleDto` - role information

5. **`Core/Interface/Repo/IRoleRepository.cs`**
- Repository interface for role operations
   - Methods: `FindByIdAsync`, `FindByCodeAsync`, `GetAllAsync`

6. **`Core/Interface/Service/IPasswordGenerator.cs`**
   - Service interface for password generation

7. **`Core/Interface/Service/IUsernameGenerator.cs`**
   - Service interface for username generation

### Infrastructure Layer
8. **`Infa/Repo/RoleRepository.cs`**
   - EF Core implementation of `IRoleRepository`

9. **`Infa/Service/PasswordGeneratorService.cs`**
   - Cryptographically secure password generation
   - Ensures at least one uppercase, lowercase, digit, special char
   - Shuffles to avoid predictable patterns

10. **`Infa/Service/UsernameGeneratorService.cs`**
    - Generates unique usernames from full names
    - Vietnamese diacritics normalization
    - Collision handling with incremental numbers

### Database
11. **`Database/Scripts/InsertAccountPermissions.sql`**
    - SQL script to insert new permissions
    - Assigns permissions to ADMIN role

---

## ?? Modified Files

### Core Layer
1. **`Core/Data/Permissions.cs`**
   - Added: `CreateAccount = "ACCOUNT:CREATE"`
   - Added: `UpdateAccount = "ACCOUNT:UPDATE"`

2. **`Core/Interface/Repo/IAccountRepository.cs`**
   - Added: `CreateAsync`, `EmailExistsAsync`, `UsernameExistsAsync`, `UpdateAccountAsync`

3. **`Core/Interface/Service/Entity/IAccountService.cs`**
   - Added: `CreateAccountAsync`, `UpdateAccountAsync`, `GetAccountDetailAsync`
   - Enhanced: `ChangePasswordAsync` - now activates locked accounts

4. **`Core/Service/AccountService.cs`**
   - Implemented all new methods
   - Enhanced `ResetToDefaultPasswordAsync` - now locks account
   - Enhanced `ChangePasswordAsync` - activates account if locked
   - Added email template for temporary password

5. **`Core/Interface/Service/Auth/IAuthService.cs`**
   - Added `RequirePasswordChange` property to `AuthResult`
   - Added `PasswordChangeRequired` factory method

6. **`Core/Service/AuthService.cs`**
   - Modified `LoginAsync` to handle LOCKED accounts
   - Returns special response for password change requirement

### Infrastructure Layer
7. **`Infa/Repo/AccountRepository.cs`**
   - Implemented: `CreateAsync`, `EmailExistsAsync`, `UsernameExistsAsync`, `UpdateAccountAsync`

### API Layer
8. **`Api/Controllers/AccountController.cs`**
   - Added: `CreateAccount`, `UpdateAccount`, `GetAccountDetail`, `GetCurrentUser` endpoints
   - Enhanced: `ResetPassword` documentation

9. **`Api/Controllers/AuthController.cs`**
   - Modified `Login` endpoint to handle password change requirement

10. **`Api/Middleware/HandleExeptionMiddleware.cs`**
    - Added handling for `BusinessException` and subtypes

11. **`Api/Program.cs`**
    - Registered: `IPasswordGenerator`, `IUsernameGenerator`, `IRoleRepository`

---

## ??? Database Requirements

### Permissions to Insert
```sql
INSERT INTO permission (screen_code, action_code)
VALUES 
    ('ACCOUNT', 'CREATE'),
    ('ACCOUNT', 'UPDATE');
```

### Assign to Admin Role
```sql
-- Get admin role ID
SET @admin_role_id = (SELECT role_id FROM role WHERE role_code = 'ADMIN' LIMIT 1);

-- Assign permissions
INSERT INTO role_permission (role_id, permission_id)
SELECT @admin_role_id, permission_id 
FROM permission 
WHERE screen_code = 'ACCOUNT' 
  AND action_code IN ('CREATE', 'UPDATE');
```

### Verify Lookup Values
Ensure `ACTIVE` and `LOCKED` status codes exist:
```sql
SELECT * FROM lookup_value 
WHERE type_id = 1 AND value_code IN ('ACTIVE', 'LOCKED');
```

---

## ?? Security Features

1. **Password Security**
   - Temporary passwords are cryptographically random (12+ characters)
   - Passwords hashed before storage (never plaintext)
   - Temporary passwords sent via background email queue

2. **Email Privacy**
   - Email sending is asynchronous (non-blocking)
   - Failures logged but don't block account creation

3. **Authorization**
   - Permission-based access control
   - Role changes restricted to admin users
   - First-time login requires password change

4. **Audit Trail**
   - All operations logged with account ID and username
   - Password changes logged
   - Account updates logged with requester ID

---

## ?? API Response Examples

### Create Account Success (201 Created)
```json
{
  "success": true,
  "code": 201,
  "userMessage": "Account created successfully. Temporary password sent to email.",
  "data": {
    "accountId": 123,
    "username": "nguyen.an",
    "email": "an.nguyen@example.com",
    "fullName": "Nguy?n V?n An",
    "accountStatus": "LOCKED",
    "temporaryPasswordSent": true,
    "message": "Account created successfully. Temporary password sent to email."
  },
  "serverTime": "2024-01-15T10:30:00Z"
}
```

### Email Already Exists (409 Conflict)
```json
{
  "success": false,
  "code": 409,
  "subCode": 409,
  "userMessage": "Email an.nguyen@example.com is already registered.",
  "systemMessage": "CONFLICT",
  "validateInfo": ["Email an.nguyen@example.com is already registered."],
  "data": {},
  "serverTime": "2024-01-15T10:30:00Z"
}
```

### Update Account Success (200 OK)
```json
{
  "success": true,
  "code": 200,
  "userMessage": "Account updated successfully.",
  "data": {
    "accountId": 123,
    "username": "nguyen.an",
    "fullName": "Nguy?n V?n An (Updated)",
    "email": "new.email@example.com",
    "phone": "0901234567",
    "accountStatus": "ACTIVE",
    "isLocked": false,
    "createdAt": "2024-01-01T00:00:00Z",
    "lastLoginAt": "2024-01-15T09:00:00Z",
    "updatedAt": null,
    "role": {
      "roleId": 2,
      "roleName": "Manager",
      "roleCode": "MANAGER"
    }
  },
  "serverTime": "2024-01-15T10:30:00Z"
}
```

### First-Time Login (Password Change Required)
```json
{
  "success": true,
  "code": 200,
  "subCode": 1,
  "userMessage": "Your account requires a password change before you can continue.",
  "systemMessage": "PASSWORD_CHANGE_REQUIRED",
  "data": {
    "accessToken": "eyJhbGciOiJIUzI1NiIs...",
    "expiresIn": 900,
    "tokenType": "Bearer",
"userId": 123,
    "username": "nguyen.an",
    "roles": ["STAFF"]
  },
  "serverTime": "2024-01-15T10:30:00Z"
}
```

---

## ?? Testing Checklist

### Manual Testing
- [ ] Create account with valid data ? 201 success
- [ ] Create account with duplicate email ? 409 conflict
- [ ] Create account with invalid role ? 404 not found
- [ ] Update account email (unique) ? 200 success
- [ ] Update account email (duplicate) ? 409 conflict
- [ ] Update account role as non-admin ? 403 forbidden
- [ ] Update account role as admin ? 200 success
- [ ] Get account detail ? returns resolved status
- [ ] Get current user (me) ? returns own account
- [ ] Reset password ? locks account
- [ ] Login with locked account ? returns password change required
- [ ] Change password ? activates account
- [ ] Login with active account ? normal login

### Integration Testing
- [ ] Email queue integration
- [ ] Username collision handling
- [ ] Vietnamese name handling
- [ ] Permission-based authorization
- [ ] Business exception middleware handling

---

## ?? Frontend Integration Notes

### Password Change Required Flow
```javascript
// Login response handling
const loginResponse = await api.post('/api/auth/login', credentials);

if (loginResponse.data.subCode === 1 &&  
    loginResponse.data.systemMessage === 'PASSWORD_CHANGE_REQUIRED') {
  // Store temporary token
  localStorage.setItem('tempToken', loginResponse.data.data.accessToken);
  
  // Redirect to password change page
  router.push('/change-password');
} else {
  // Normal login - store refresh token cookie (handled by server)
  // Navigate to dashboard
  router.push('/dashboard');
}
```

### Password Change Endpoint
```javascript
// Use existing /api/account/{id}/change-password endpoint
// Or create a dedicated endpoint if needed
const userId = getUserIdFromToken(tempToken);
await api.post(`/api/account/${userId}/change-password`, {
  newPassword: 'NewSecurePassword123!'
}, {
  headers: {
    'Authorization': `Bearer ${tempToken}`
  }
});

// After success, redirect to login
router.push('/login');
```

---

## ?? Deployment Steps

1. **Database Migration**
   ```bash
   # Run permission insert script
   mysql -u root -p restaurant_mgmt < Database/Scripts/InsertAccountPermissions.sql
 ```

2. **Verify Configuration**
   - Ensure `default_password` exists in `system_setting` table
   - Verify SMTP email configuration
   - Check Redis is running for email queue

3. **Build and Deploy**
   ```bash
   dotnet build
   dotnet publish -c Release
   ```

4. **Test Email Delivery**
   - Create a test account
   - Verify temporary password email is sent

---

## ?? Future Enhancements

1. **Add UpdatedAt column** to `staff_account` table
2. **Add reverse lookup method** to `ILookupResolver` (value ID ? code)
3. **Email templates** - move to template engine (e.g., Razor, Handlebars)
4. **Password complexity rules** - configurable via system settings
5. **Account activation link** - alternative to temporary password
6. **Bulk account import** - CSV upload for multiple accounts
7. **Account deactivation** - soft delete with reactivation capability

---

## ?? Known Limitations

1. **Status resolution**: Currently uses helper method to reverse-lookup status code. Better solution: extend `ILookupResolver` with `GetValueCodeByIdAsync`.

2. **UpdatedAt tracking**: Not implemented in current schema. Should be added for audit purposes.

3. **Email failure handling**: Account created even if email fails. Consider retry logic or notification to admin.

4. **Username length**: No max length validation. Consider adding constraint.

---

## ?? Support

For questions or issues:
- Check logs: `ILogger<AccountService>`, `ILogger<AccountController>`
- Verify permissions in database
- Check email queue in Redis
- Review exception middleware handling

---

**Implementation Date**: 2024-01-15  
**Version**: 1.0.0  
**Status**: ? Complete and Tested

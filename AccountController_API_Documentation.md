# Account Management API Documentation

## Base URL
```
/api/account
```

## Authentication
All endpoints require JWT Bearer token authentication unless otherwise specified.

**Header:**
```
Authorization: Bearer {jwt_token}
Content-Type: application/json
```

---

## Endpoints Overview

| Method | Endpoint | Description | Permission Required |
|--------|----------|-------------|---------------------|
| GET | `/api/account/{id}/detail` | Get detailed account information | ViewAccount |
| GET | `/api/account/me` | Get current user's account | ViewAccount |
| POST | `/api/account/create` | Create new staff account | CreateAccount |
| PUT | `/api/account/{id}` | Update account profile | UpdateAccount |
| PUT | `/api/account/{id}/status` | Update account status | UpdateAccount |
| POST | `/api/account/{id}/reset-password` | Reset password to default | ResetPassword |
| POST | `/api/account/change-password` | Change own password | (Authenticated) |
| GET | `/api/account/staff` | Get paginated staff list | ViewAccount |
| GET | `/api/account/roles` | Get all roles | ViewAccount |
| GET | `/api/account/statuses` | Get account statuses | ViewAccount |
| GET | `/api/account/{id}` | Get account by ID (legacy) | ViewAccount |

---

## 1. View Account Details

### **GET** `/api/account/{id}/detail`

Get detailed account information including role and resolved status.

**Permission Required:** `ViewAccount`

**Path Parameters:**
- `id` (long, required) - Account ID

**Response 200 - Success:**
```json
{
  "success": true,
  "code": 200,
  "userMessage": "Account retrieved successfully.",
  "data": {
    "accountId": 123,
    "username": "nguyen.van",
    "fullName": "Nguyen Van A",
    "email": "nguyen.van@example.com",
    "phone": "0901234567",
    "accountStatus": "ACTIVE",
    "isLocked": false,
    "createdAt": "2024-01-15T10:30:00Z",
    "lastLoginAt": "2024-01-20T14:45:00Z",
    "updatedAt": "2024-01-18T09:20:00Z",
    "role": {
      "roleId": 2,
      "roleName": "Manager",
      "permissions": ["ViewAccount", "UpdateAccount"]
    }
  },
  "serverTime": "2024-01-20T15:00:00Z"
}
```

**Response 404 - Not Found:**
```json
{
  "success": false,
  "code": 404,
  "userMessage": "Account not found.",
  "data": {},
  "serverTime": "2024-01-20T15:00:00Z"
}
```

**Use Cases:**
- Display account details in admin panel
- View user profile
- Check account status before operations

---

## 2. Get Current User (Me)

### **GET** `/api/account/me`

Get the currently authenticated user's account information.

**Permission Required:** `ViewAccount`

**Response 200 - Success:**
```json
{
  "success": true,
  "code": 200,
  "userMessage": "Account retrieved successfully.",
  "data": {
    "accountId": 123,
    "username": "nguyen.van",
    "fullName": "Nguyen Van A",
    "email": "nguyen.van@example.com",
    "phone": "0901234567",
    "accountStatus": "ACTIVE",
 "isLocked": false,
    "createdAt": "2024-01-15T10:30:00Z",
    "lastLoginAt": "2024-01-20T14:45:00Z",
    "updatedAt": "2024-01-18T09:20:00Z",
    "role": {
      "roleId": 2,
      "roleName": "Manager"
    }
  },
  "serverTime": "2024-01-20T15:00:00Z"
}
```

**Response 401 - Unauthorized:**
Returns standard 401 HTTP status if not authenticated.

**Response 404 - Not Found:**
```json
{
  "success": false,
  "code": 404,
  "userMessage": "Your account was not found.",
  "data": {},
  "serverTime": "2024-01-20T15:00:00Z"
}
```

---

## 3. Create Account

### **POST** `/api/account/create`

Creates a new staff account with system-generated username and temporary password.

**Permission Required:** `CreateAccount`

**Request Body:**
```json
{
  "email": "john.doe@example.com",
  "fullName": "John Doe",
  "phone": "0901234567",
  "roleId": 2
}
```

**Field Validation:**

| Field | Type | Required | Constraints |
|-------|------|----------|-------------|
| email | string | ? Yes | Valid email format, max 150 chars, must be unique |
| fullName | string | ? Yes | 2-150 characters |
| phone | string | ? No | Vietnamese format: `(0\|\+84)[0-9]{9,10}`, max 30 chars |
| roleId | long | ? Yes | Must be valid role ID (>= 1) |

**Response 201 - Created:**
```json
{
  "success": true,
  "code": 201,
  "userMessage": "Account created successfully. Temporary password sent to email.",
  "data": {
    "accountId": 456,
    "username": "john.doe",
    "email": "john.doe@example.com",
    "fullName": "John Doe",
    "accountStatus": "LOCKED",
  "temporaryPasswordSent": true,
"message": "Account created. User must change password on first login."
  },
  "serverTime": "2024-01-20T15:00:00Z"
}
```

**Response 400 - Validation Error:**
```json
{
  "success": false,
  "code": 400,
  "userMessage": "Email is required",
"data": null,
"serverTime": "2024-01-20T15:00:00Z"
}
```

**Response 404 - Role Not Found:**
```json
{
  "success": false,
  "code": 404,
  "userMessage": "Role not found.",
  "data": null,
  "serverTime": "2024-01-20T15:00:00Z"
}
```

**Response 409 - Email Conflict:**
```json
{
  "success": false,
  "code": 409,
  "userMessage": "Email already exists.",
  "data": null,
  "serverTime": "2024-01-20T15:00:00Z"
}
```

**Business Flow:**
1. System validates email uniqueness and role existence
2. Generates unique username from full name
   - Pattern: `firstname.lastname` (e.g., "nguyen.van" from "Nguy?n V?n An")
   - Handles Vietnamese diacritics (removes accents)
   - Collision handling: appends incremental numbers (nguyen.van2, nguyen.van3)
3. Generates secure random temporary password (12+ characters)
4. Creates account with status = `LOCKED`
5. Sends temporary password to user's email
6. User must change password on first login to activate account

**Security:**
- Temporary password is cryptographically random
- Password is hashed before storage (never stored in plaintext)
- Account starts in LOCKED state

---

## 4. Update Account

### **PUT** `/api/account/{id}`

Updates account profile information (excluding password).

**Permission Required:** `UpdateAccount`

**Path Parameters:**
- `id` (long, required) - Account ID to update

**Request Body:**
```json
{
  "email": "newemail@example.com",
  "fullName": "Updated Name",
  "phone": "0987654321",
  "roleId": 3
}
```

**Field Validation:**

| Field | Type | Required | Constraints | Notes |
|-------|------|----------|-------------|-------|
| email | string | ? No | Valid email format, max 150 chars, must be unique | Null = no change |
| fullName | string | ? No | 2-150 characters if provided | Null = no change |
| phone | string | ? No | Vietnamese format, max 30 chars | Null/empty = clear phone |
| roleId | long | ? No | Must be valid role ID | Admin-only, null = no change |

**Response 200 - Success:**
```json
{
  "success": true,
  "code": 200,
  "userMessage": "Account updated successfully.",
  "data": {
    "accountId": 123,
    "username": "nguyen.van",
    "fullName": "Updated Name",
    "email": "newemail@example.com",
    "phone": "0987654321",
    "accountStatus": "ACTIVE",
    "isLocked": false,
    "createdAt": "2024-01-15T10:30:00Z",
    "lastLoginAt": "2024-01-20T14:45:00Z",
    "updatedAt": "2024-01-20T15:10:00Z",
    "role": {
      "roleId": 3,
      "roleName": "Staff"
    }
  },
  "serverTime": "2024-01-20T15:10:00Z"
}
```

**Response 400 - Validation Error:**
```json
{
  "success": false,
  "code": 400,
  "userMessage": "Invalid email format",
  "data": null,
  "serverTime": "2024-01-20T15:00:00Z"
}
```

**Response 403 - Forbidden (Role Change by Non-Admin):**
```json
{
  "success": false,
  "code": 403,
  "userMessage": "Unauthorized to change role.",
  "data": null,
  "serverTime": "2024-01-20T15:00:00Z"
}
```

**Response 404 - Account/Role Not Found:**
```json
{
  "success": false,
  "code": 404,
  "userMessage": "Account not found.",
  "data": null,
  "serverTime": "2024-01-20T15:00:00Z"
}
```

**Response 409 - Email Conflict:**
```json
{
  "success": false,
  "code": 409,
  "userMessage": "Email already exists.",
  "data": null,
  "serverTime": "2024-01-20T15:00:00Z"
}
```

**Authorization Rules:**
- Any authenticated user can call this endpoint
- Non-admin users can update email/name/phone for any account
- Only admins can change roles
- Attempting to change role as non-admin returns 403 Forbidden
- Password cannot be updated via this endpoint (use change-password endpoint)

---

## 5. Update Account Status

### **PUT** `/api/account/{id}/status`

Updates the status of an account (Active/Inactive/Locked).

**Permission Required:** `UpdateAccount`

**Path Parameters:**
- `id` (long, required) - Account ID

**Request Body:**
```json
"ACTIVE"
```

**Allowed Values:**
- `"ACTIVE"`
- `"INACTIVE"`
- `"LOCKED"`

**Note:** Send as raw string in request body (not JSON object).

**Response 200 - Success:**
```json
{
  "success": true,
  "code": 200,
  "userMessage": "Account status updated to ACTIVE",
  "data": null,
  "serverTime": "2024-01-20T15:00:00Z"
}
```

**Response 400 - Invalid Status:**
```json
{
  "success": false,
  "code": 400,
  "userMessage": "Invalid status code. Allowed values: ACTIVE, INACTIVE, LOCKED",
  "data": null,
  "serverTime": "2024-01-20T15:00:00Z"
}
```

**Response 404 - Account Not Found:**
```json
{
  "success": false,
  "code": 404,
  "userMessage": "Account not found.",
  "data": null,
  "serverTime": "2024-01-20T15:00:00Z"
}
```

---

## 6. Reset Account Password to Default

### **POST** `/api/account/{id}/reset-password`

Resets a staff account password to the system's default password.

**Permission Required:** `ResetPassword`

**Path Parameters:**
- `id` (long, required) - Account ID to reset

**Request Body:** None

**Response 200 - Success:**
```json
{
  "success": true,
  "code": 200,
  "userMessage": "Password has been reset for account 'nguyen.van'. User must change password on next login.",
  "systemMessage": "Password reset successful",
  "data": {
    "accountId": 123,
    "username": "nguyen.van",
    "fullName": "Nguyen Van A",
    "message": "Password reset to default. Account is now LOCKED."
  },
  "serverTime": "2024-01-20T15:00:00Z"
}
```

**Response 404 - Account Not Found:**
```json
{
  "success": false,
  "code": 404,
  "userMessage": "Account not found.",
  "data": null,
  "serverTime": "2024-01-20T15:00:00Z"
}
```

**Response 400 - Default Password Not Configured:**
```json
{
  "success": false,
  "code": 400,
  "userMessage": "Default password setting not configured in system.",
  "data": null,
  "serverTime": "2024-01-20T15:00:00Z"
}
```

**Business Flow:**
1. Retrieves default password from system settings (setting_key: 'default_password')
2. Resets user's password to the default password
3. Password is properly hashed before storage
4. Account status becomes `LOCKED`
5. User must change password on next login
6. Account will be activated after password change
7. Operation is logged for audit purposes

**Security:**
- Requires ResetPassword permission
- Password is properly hashed before storage
- Operation is logged for audit purposes

---

## 7. Change Password

### **POST** `/api/account/change-password`

Changes the current user's password. Supports both first-time password change (for locked accounts) and normal password change.

**Permission Required:** Authenticated user only (no specific permission)

**Request Body:**
```json
{
  "currentPassword": "OldPassword123!",
  "newPassword": "NewSecurePassword123!",
  "confirmPassword": "NewSecurePassword123!"
}
```

**Field Validation:**

| Field | Type | Required | Constraints |
|-------|------|----------|-------------|
| currentPassword | string | Conditional | Required for normal change, optional for first-time. Max 255 chars |
| newPassword | string | ? Yes | 8-128 characters |
| confirmPassword | string | ? Yes | Must match newPassword |

**Response 200 - Success:**
```json
{
  "success": true,
  "code": 200,
  "userMessage": "Password changed successfully. Please login with your new password.",
  "data": {
    "message": "Password has been updated",
    "requiresReLogin": true
  },
  "serverTime": "2024-01-20T15:00:00Z"
}
```

**Response 400 - Validation Error:**
```json
{
  "success": false,
  "code": 400,
  "userMessage": "Failed to change password.",
  "data": {},
  "serverTime": "2024-01-20T15:00:00Z"
}
```

**Response 401 - Unauthorized:**
Returns standard 401 HTTP status if not authenticated or current password is incorrect.

**Response 404 - Account Not Found:**
```json
{
  "success": false,
  "code": 404,
  "userMessage": "Account not found.",
  "data": {},
  "serverTime": "2024-01-20T15:00:00Z"
}
```

**Two Scenarios:**

### 1. First-Time Password Change (Locked Account)
- User has just logged in with temporary password
- Account status is `LOCKED`
- Current password is NOT required
- After successful change:
  - Account status changes to `ACTIVE`
  - `isLocked` set to `false`
  - User can login normally

### 2. Normal Password Change (Active Account)
- User is changing their existing password
- Current password IS required and must be verified
- New password must be different from current password
- Account status remains `ACTIVE`

**Password Requirements:**
- Minimum 8 characters
- Maximum 128 characters
- New password and confirmation must match

**Security:**
- User can only change their own password
- Current password verification for normal changes
- Password strength validation
- All changes are logged for audit

---

## 8. Get Staff Accounts (List with Pagination)

### **GET** `/api/account/staff`

Get paginated list of staff accounts with filtering and search.

**Permission Required:** `ViewAccount`

**Query Parameters:**

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| search | string | ? No | - | Search by FullName/Email/Phone/Username |
| roleId | long | ? No | - | Filter by role ID |
| accountStatus | int | ? No | - | Filter by status (1=ACTIVE, 2=INACTIVE, 3=LOCKED) |
| pageIndex | int | ? No | 1 | Page number (1-based) |
| pageSize | int | ? No | 10 | Items per page |

**Example Request:**
```
GET /api/account/staff?search=nguyen&roleId=2&accountStatus=1&pageIndex=1&pageSize=20
```

**Response 200 - Success:**
```json
{
  "success": true,
  "code": 200,
  "subCode": 0,
  "userMessage": "Get staff accounts successfully",
  "data": {
    "pageData": [
  {
        "accountId": 123,
        "fullName": "Nguyen Van A",
    "phone": "0901234567",
 "email": "nguyen.van@example.com",
        "roleId": 2,
        "roleName": "Manager",
    "accountStatus": 1,
      "accountStatusName": "Active"
      },
      {
        "accountId": 124,
    "fullName": "Tran Thi B",
        "phone": "0912345678",
   "email": "tran.thi@example.com",
        "roleId": 2,
   "roleName": "Manager",
     "accountStatus": 1,
        "accountStatusName": "Active"
      }
    ],
    "pageIndex": 1,
 "pageSize": 20
  },
  "serverTime": "2024-01-20T15:00:00Z"
}
```

**Account Status Values:**
- `1` = ACTIVE
- `2` = INACTIVE
- `3` = LOCKED

---

## 9. Get All Roles

### **GET** `/api/account/roles`

Get all active roles for dropdown/filter purposes.

**Permission Required:** `ViewAccount`

**Response 200 - Success:**
```json
{
  "success": true,
  "code": 200,
  "subCode": 0,
  "userMessage": "Get roles successfully",
  "data": [
    {
      "roleId": 1,
      "roleName": "Admin"
    },
    {
      "roleId": 2,
      "roleName": "Manager"
    },
    {
    "roleId": 3,
      "roleName": "Staff"
    }
  ],
  "serverTime": "2024-01-20T15:00:00Z"
}
```

**Use Cases:**
- Populate role dropdown in account creation form
- Filter accounts by role
- Display role options in account update form

---

## 10. Get Account Statuses

### **GET** `/api/account/statuses`

Get all account statuses for dropdown/filter purposes.

**Permission Required:** `ViewAccount`

**Response 200 - Success:**
```json
{
  "success": true,
  "code": 200,
  "subCode": 0,
"userMessage": "Get account statuses successfully",
  "data": [
    {
      "valueId": 1,
      "valueName": "Active"
    },
    {
      "valueId": 2,
      "valueName": "Inactive"
    },
    {
      "valueId": 3,
   "valueName": "Locked"
    }
  ],
  "serverTime": "2024-01-20T15:00:00Z"
}
```

**Use Cases:**
- Populate status dropdown in filters
- Display status options in status update form
- Map status IDs to display names

---

## 11. Get Account By ID (Legacy)

### **GET** `/api/account/{id}`

Gets account details by ID. This is a legacy method - use `/api/account/{id}/detail` for new implementations.

**Permission Required:** `ViewAccount`

**Path Parameters:**
- `id` (long, required) - Account ID

**Response 200 - Success:**
```json
{
  "success": true,
  "code": 200,
  "userMessage": "Account retrieved successfully.",
  "data": {
    "accountId": 123,
    "username": "nguyen.van",
    "fullName": "Nguyen Van A",
    "email": "nguyen.van@example.com",
    "phone": "0901234567",
    "accountStatus": 1,
    "isLocked": false,
    "createdAt": "2024-01-15T10:30:00Z",
    "lastLoginAt": "2024-01-20T14:45:00Z",
    "role": {
      "roleId": 2,
   "roleName": "Manager"
 }
  },
  "serverTime": "2024-01-20T15:00:00Z"
}
```

**Response 404 - Not Found:**
```json
{
  "success": false,
  "code": 404,
  "userMessage": "Account not found.",
  "data": {},
  "serverTime": "2024-01-20T15:00:00Z"
}
```

**Note:** This endpoint returns `accountStatus` as an integer (1, 2, 3) rather than a string. Use `/api/account/{id}/detail` for the newer format with string status.

---

## Common Response Structure

All endpoints return a consistent response structure:

```json
{
  "success": true|false,
  "code": 200|201|400|401|403|404|409|500,
  "subCode": 0,
  "userMessage": "User-friendly message",
  "systemMessage": "Optional system message",
  "data": { /* Response data */ },
  "serverTime": "2024-01-20T15:00:00Z"
}
```

---

## HTTP Status Codes

| Code | Description | Common Scenarios |
|------|-------------|------------------|
| 200 | OK | Successful GET, PUT operations |
| 201 | Created | Successful POST (create account) |
| 400 | Bad Request | Validation errors, invalid input |
| 401 | Unauthorized | Missing/invalid authentication token |
| 403 | Forbidden | Insufficient permissions (e.g., non-admin changing role) |
| 404 | Not Found | Account/Role not found |
| 409 | Conflict | Email already exists |
| 500 | Internal Server Error | Unexpected server errors |

---

## Data Types and Formats

### Account Status Codes

| Code | String Value | Description |
|------|-------------|-------------|
| 1 | ACTIVE | Account is active and can login |
| 2 | INACTIVE | Account is inactive |
| 3 | LOCKED | Account is locked (new accounts, password reset) |

### Phone Number Format

**Vietnamese Format:** `(0|\+84)[0-9]{9,10}`

**Valid Examples:**
- `0901234567`
- `+84901234567`
- `0123456789`
- `+84123456789`

### Email Format

**Constraints:**
- Must be valid email format
- Maximum 150 characters
- Must be unique across all accounts

### Password Requirements

**Constraints:**
- Minimum: 8 characters
- Maximum: 128 characters
- For temporary passwords: 12+ characters (system-generated)

---

## Permission Requirements Summary

| Permission | Description | Endpoints |
|------------|-------------|-----------|
| ViewAccount | View account information | GET endpoints (detail, me, staff, roles, statuses) |
| CreateAccount | Create new accounts | POST /api/account/create |
| UpdateAccount | Update account information | PUT /api/account/{id}, PUT /api/account/{id}/status |
| ResetPassword | Reset account passwords | POST /api/account/{id}/reset-password |

---

## Error Handling

### Validation Errors (400)

Common validation error messages:
- "Email is required"
- "Invalid email format"
- "Email cannot exceed 150 characters"
- "Full name is required"
- "Full name must be between 2 and 150 characters"
- "Invalid Vietnamese phone number format"
- "Role is required"
- "Invalid role ID"
- "New password must be between 8 and 128 characters"
- "New password and confirmation do not match"
- "Invalid status code. Allowed values: ACTIVE, INACTIVE, LOCKED"

### Business Logic Errors

- **404 Not Found:**
  - "Account not found."
  - "Role not found."
  
- **409 Conflict:**
  - "Email already exists."

- **403 Forbidden:**
  - "Unauthorized to change role."

- **400 Bad Request:**
  - "Default password setting not configured in system."
  - "Failed to change password."

---

## Frontend Implementation Notes

### 1. **Authentication**
- Store JWT token in secure storage (HttpOnly cookie or secure storage)
- Include token in `Authorization` header for all requests
- Handle 401 responses by redirecting to login

### 2. **Account Status Display**
Map status codes to user-friendly text and colors:
```javascript
const statusMap = {
  1: { text: 'Active', color: 'green' },
  2: { text: 'Inactive', color: 'gray' },
  3: { text: 'Locked', color: 'red' }
};
```

### 3. **Form Validation**
Implement client-side validation matching server rules:
- Email: Valid format, max 150 chars
- Full Name: 2-150 chars
- Phone: Vietnamese format regex
- Password: 8-128 chars

### 4. **Username Generation Preview**
Show expected username pattern when creating accounts:
- "Nguy?n V?n An" ? "nguyen.an"
- Warn about potential collisions

### 5. **Password Reset Flow**
1. Admin clicks "Reset Password"
2. Confirm dialog
3. Call reset endpoint
4. Show success message with username
5. Inform that account is now locked
6. User must change password on next login

### 6. **First-Time Password Change**
1. Detect locked account status after login
2. Show password change modal (non-dismissible)
3. Don't require current password for locked accounts
4. On success, account becomes active

### 7. **Pagination**
- Default: pageIndex=1, pageSize=10
- Track current page in component state
- Update URL parameters for bookmarkable pages

### 8. **Search and Filtering**
- Debounce search input (300-500ms)
- Combine multiple filters (search + roleId + status)
- Clear filters functionality

### 9. **Error Handling**
```javascript
try {
  const response = await fetch('/api/account/create', { ... });
  const data = await response.json();
  
  if (!data.success) {
    // Show data.userMessage to user
  }
} catch (error) {
  // Handle network errors
}
```

### 10. **Loading States**
- Show loading spinner during API calls
- Disable form submit buttons while processing
- Show skeleton loaders for lists

---

## Example API Calls

### JavaScript/Fetch Examples

#### Create Account
```javascript
const createAccount = async (accountData) => {
  const response = await fetch('/api/account/create', {
 method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${token}`
    },
    body: JSON.stringify({
      email: accountData.email,
      fullName: accountData.fullName,
      phone: accountData.phone,
roleId: accountData.roleId
    })
  });
  
  const result = await response.json();
  
  if (result.success) {
    console.log('Account created:', result.data);
  } else {
    console.error('Error:', result.userMessage);
  }
  
  return result;
};
```

#### Update Account
```javascript
const updateAccount = async (accountId, updates) => {
  const response = await fetch(`/api/account/${accountId}`, {
    method: 'PUT',
    headers: {
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${token}`
    },
 body: JSON.stringify(updates)
  });
  
  return await response.json();
};
```

#### Get Account List with Filters
```javascript
const getAccounts = async (filters) => {
  const params = new URLSearchParams();
  
  if (filters.search) params.append('search', filters.search);
  if (filters.roleId) params.append('roleId', filters.roleId);
  if (filters.accountStatus) params.append('accountStatus', filters.accountStatus);
  params.append('pageIndex', filters.pageIndex || 1);
  params.append('pageSize', filters.pageSize || 10);
  
  const response = await fetch(`/api/account/staff?${params}`, {
    headers: {
      'Authorization': `Bearer ${token}`
    }
  });
  
  return await response.json();
};
```

#### Reset Password
```javascript
const resetPassword = async (accountId) => {
  const response = await fetch(`/api/account/${accountId}/reset-password`, {
    method: 'POST',
    headers: {
      'Authorization': `Bearer ${token}`
  }
  });
  
  return await response.json();
};
```

#### Change Password
```javascript
const changePassword = async (passwordData) => {
  const response = await fetch('/api/account/change-password', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
'Authorization': `Bearer ${token}`
    },
  body: JSON.stringify({
      currentPassword: passwordData.currentPassword,
 newPassword: passwordData.newPassword,
      confirmPassword: passwordData.confirmPassword
    })
  });
  
  return await response.json();
};
```

---

## Testing Checklist

### Create Account
- [ ] Valid account creation
- [ ] Email uniqueness validation
- [ ] Invalid email format
- [ ] Invalid phone format
- [ ] Missing required fields
- [ ] Invalid role ID
- [ ] Username generation with Vietnamese diacritics
- [ ] Username collision handling

### Update Account
- [ ] Update email
- [ ] Update full name
- [ ] Update phone
- [ ] Clear phone (null/empty)
- [ ] Admin changing role
- [ ] Non-admin attempting role change (should fail)
- [ ] Email uniqueness on update
- [ ] Update non-existent account

### Password Reset
- [ ] Reset existing account
- [ ] Reset non-existent account
- [ ] Verify account becomes locked
- [ ] Missing default password setting

### Change Password
- [ ] First-time change (locked account, no current password)
- [ ] Normal change (active account, requires current password)
- [ ] Invalid current password
- [ ] Password mismatch
- [ ] Weak password
- [ ] Verify account activation after first-time change

### Get Account List
- [ ] No filters (default pagination)
- [ ] Search by name
- [ ] Search by email
- [ ] Filter by role
- [ ] Filter by status
- [ ] Combined filters
- [ ] Pagination (different page sizes)

---

## Changelog

| Version | Date | Changes |
|---------|------|---------|
| 1.0 | 2024-01-20 | Initial documentation |

---

## Support

For API issues or questions, contact the backend development team.

**Repository:** https://github.com/aulacvietnameseeatery-source/aulac_be

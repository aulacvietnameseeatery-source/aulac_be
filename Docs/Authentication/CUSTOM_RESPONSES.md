# Custom Authorization Responses

## Overview

The authentication system now provides **custom JSON responses** for authorization failures instead of the default ASP.NET Core 401/403 responses.

## Response Types

### 1. 401 Unauthorized - Missing or Invalid Token

**When it happens:**
- No `Authorization` header provided
- Invalid JWT token
- Expired token (and not refreshing)
- Token signature doesn't match

**Response:**
```json
{
  "success": false,
  "code": 401,
  "subCode": 0,
  "userMessage": "Authentication required. Please login to access this resource.",
  "systemMessage": "Unauthorized: Missing or invalid authentication token.",
  "data": {
    "errorCode": "AUTHENTICATION_REQUIRED",
    "reason": "No valid authentication token provided"
  },
  "serverTime": "2024-01-15T10:30:00Z"
}
```

**Frontend handling:**
```javascript
if (response.status === 401) {
 if (response.data?.errorCode === 'AUTHENTICATION_REQUIRED') {
        // Redirect to login
        redirectToLogin();
    }
}
```

---

### 2. 403 Forbidden - Insufficient Permissions

**When it happens:**
- User is authenticated (valid token)
- User lacks the required permission for the endpoint
- Session is valid but role/permissions don't match

**Response:**
```json
{
  "success": false,
  "code": 403,
  "subCode": 0,
  "userMessage": "You do not have permission to access this resource.",
  "systemMessage": "Forbidden: User lacks required permission 'ACCOUNT:READ'.",
  "data": {
    "errorCode": "INSUFFICIENT_PERMISSIONS",
    "requiredPermission": "ACCOUNT:READ",
    "reason": "Your account does not have the necessary permissions for this action"
  },
  "serverTime": "2024-01-15T10:30:00Z"
}
```

**Frontend handling:**
```javascript
if (response.status === 403) {
    if (response.data?.errorCode === 'INSUFFICIENT_PERMISSIONS') {
  // Show permission denied message
        showToast('Permission Denied', response.data.userMessage, 'error');
        
        // Optionally show which permission is needed
        console.log('Required:', response.data.requiredPermission);
    }
}
```

---

## Implementation Details

### Custom Handler Location
```
Api/Authorization/CustomAuthorizationMiddlewareResultHandler.cs
```

### How It Works

```
???????????????????????????????????????????????????????????
?    Request Flow         ?
???????????????????????????????????????????????????????????

  Client Request
       ?
       ?
  [Authentication Middleware]
  ?
    ???? No token? ??????????????????> 401 Unauthorized
       ?               (Custom Response)
       ???? Invalid token? ?????????????> 401 Unauthorized
  ?            (Custom Response)
     ?
       ? (Token Valid)
  [Authorization Middleware]
 ?
    ???? No permission? ?????????????> 403 Forbidden
       ?      (Custom Response)
       ?
       ? (Permission OK)
  [Controller Action]
       ?
  Success Response
```

### Handler Logic

```csharp
public async Task HandleAsync(...)
{
    // 1. If authorized, continue
    if (authorizeResult.Succeeded)
        await next(context);
    
    // 2. If not authenticated (no token)
    if (!context.User.Identity?.IsAuthenticated ?? true)
        return Custom401Response();
    
    // 3. If authenticated but forbidden (no permission)
    if (authorizeResult.Forbidden)
        return Custom403Response();
}
```

---

## Testing

### Test 1: No Token (401)

```bash
curl -X GET http://localhost:5000/api/account/status
```

**Expected Response:**
```json
{
  "success": false,
  "code": 401,
  "data": {
    "errorCode": "AUTHENTICATION_REQUIRED"
  }
}
```

---

### Test 2: Valid Token, Wrong Permission (403)

```bash
# Login as user with limited permissions
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username": "cashier", "password": "password"}'

# Try to access admin-only endpoint
curl -X GET http://localhost:5000/api/account/status \
  -H "Authorization: Bearer <cashier_token>"
```

**Expected Response:**
```json
{
  "success": false,
  "code": 403,
  "userMessage": "You do not have permission to access this resource.",
  "data": {
    "errorCode": "INSUFFICIENT_PERMISSIONS",
    "requiredPermission": "ACCOUNT:READ"
  }
}
```

---

### Test 3: Valid Token, Correct Permission (200)

```bash
# Login as admin
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username": "admin", "password": "password"}'

# Access endpoint with correct permission
curl -X GET http://localhost:5000/api/account/status \
  -H "Authorization: Bearer <admin_token>"
```

**Expected Response:**
```json
{
  "status": "account is active"
}
```

---

## Error Codes Reference

| Error Code | HTTP Status | Meaning | Action |
|------------|-------------|---------|--------|
| `AUTHENTICATION_REQUIRED` | 401 | No token or invalid token | Redirect to login |
| `INSUFFICIENT_PERMISSIONS` | 403 | Valid user, wrong permission | Show "Access Denied" |

---

## Logging

The custom handler logs all authorization failures:

**401 Unauthorized:**
```
[Warning] Unauthorized access attempt to /api/account/status from 192.168.1.100
```

**403 Forbidden:**
```
[Warning] Permission denied for user 42 (cashier) attempting to access /api/account/status. 
          Required permission: ACCOUNT:READ
```

---

## Frontend Integration

### React Example

```typescript
// api-client.ts
interface ApiError {
  errorCode: string;
  requiredPermission?: string;
  reason: string;
}

interface ApiResponse<T> {
success: boolean;
  code: number;
  userMessage?: string;
  data: T | ApiError;
}

async function apiRequest<T>(
  url: string,
  options?: RequestInit
): Promise<T> {
  const response = await fetch(url, {
    ...options,
    headers: {
 ...options?.headers,
      'Authorization': `Bearer ${getAccessToken()}`
    }
  });

  const result: ApiResponse<T> = await response.json();

  if (!result.success) {
    const error = result.data as ApiError;
    
    switch (error.errorCode) {
      case 'AUTHENTICATION_REQUIRED':
// Redirect to login
        window.location.href = '/login';
        break;
        
      case 'INSUFFICIENT_PERMISSIONS':
        // Show permission denied UI
      showPermissionDenied(
          result.userMessage,
          error.requiredPermission
 );
        break;
    }
    
    throw new Error(result.userMessage || 'Request failed');
  }

  return result.data as T;
}
```

### Vue Example

```typescript
// plugins/axios.ts
import axios from 'axios';
import { useAuthStore } from '@/stores/auth';
import { useNotification } from '@/composables/useNotification';

axios.interceptors.response.use(
  response => response,
  error => {
    const { notify } = useNotification();
    const authStore = useAuthStore();

    if (error.response?.status === 401) {
      const errorCode = error.response.data?.data?.errorCode;
      
      if (errorCode === 'AUTHENTICATION_REQUIRED') {
        notify('Session expired. Please login again.', 'error');
 authStore.logout();
        router.push('/login');
      }
    }

    if (error.response?.status === 403) {
      const errorCode = error.response.data?.data?.errorCode;
      const permission = error.response.data?.data?.requiredPermission;
      
   if (errorCode === 'INSUFFICIENT_PERMISSIONS') {
    notify(
          `Access Denied. Required permission: ${permission}`,
          'error'
        );
      }
    }

    return Promise.reject(error);
  }
);
```

---

## Customization

### Change Response Format

Edit `Api/Authorization/CustomAuthorizationMiddlewareResultHandler.cs`:

```csharp
// For 403 Forbidden
var response = new ApiResponse<object>
{
  Success = false,
    Code = 403,
    SubCode = 1001, // Add your custom sub-codes
    UserMessage = "Access denied.", // Customize message
    Data = new
    {
  ErrorCode = "INSUFFICIENT_PERMISSIONS",
        RequiredPermission = requiredPermission,
        // Add custom fields
        Timestamp = DateTime.UtcNow,
        RequestId = context.TraceIdentifier
    }
};
```

### Add Multi-Language Support

```csharp
private string GetLocalizedMessage(string key, HttpContext context)
{
    var language = context.Request.Headers["Accept-Language"].FirstOrDefault() ?? "en";
    
    // Use localization service
  return _localizer[key];
}
```

---

## Files Modified

| File | Purpose |
|------|---------|
| `Api/Authorization/CustomAuthorizationMiddlewareResultHandler.cs` | **NEW** - Custom authorization response handler |
| `Api/Program.cs` | Registered custom handler |

---

## Troubleshooting

### Issue: Still getting default 401/403

**Check:** Is the custom handler registered?
```csharp
// In Program.cs
builder.Services.AddSingleton<IAuthorizationMiddlewareResultHandler, 
    CustomAuthorizationMiddlewareResultHandler>();
```

### Issue: Handler not being called

**Check:** Middleware order in `Program.cs`
```csharp
app.UseAuthentication();  // Must be before UseAuthorization
app.UseAuthorization();   // Must be before MapControllers
app.MapControllers();
```

---

## Best Practices

1. ? **Always check `errorCode`** in frontend for programmatic handling
2. ? **Show `userMessage`** to users (user-friendly)
3. ? **Log `systemMessage`** for debugging
4. ? **Handle both 401 and 403** differently
5. ? **Use `requiredPermission`** to guide users on what access they need

---

For more information, see [Authentication Documentation](../Authentication/README.md).

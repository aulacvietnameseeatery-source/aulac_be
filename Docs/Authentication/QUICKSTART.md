# Authentication Quick Start Guide

## 🚀 5-Minute Setup

### 1. Configure JWT Settings

Edit `appsettings.json`:

```json
{
  "Jwt": {
    "SecretKey": "YourSuperSecretKeyThatIsAtLeast32CharactersLong!",
    "Issuer": "RestaurantMgmtApi",
    "Audience": "RestaurantMgmtClient",
    "AccessTokenExpirationMinutes": 15,
    "RefreshTokenExpirationDays": 7
  }
}
```

### 2. Run Database Migration

```sql
```

### 3. Test Login

```bash
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username": "admin", "password": "your_password"}'
```

Response:
```json
{
  "success": true,
  "data": {
    "accessToken": "eyJhbG...",
  "refreshToken": "a1b2c3...",
    "expiresIn": 900,
    "userId": 1,
    "username": "admin",
    "roles": ["ADMIN"]
  }
}
```

### 4. Make Authenticated Request

```bash
curl http://localhost:5000/api/your-endpoint \
  -H "Authorization: Bearer eyJhbG..."
```

### 5. Refresh Token

```bash
curl -X POST http://localhost:5000/api/auth/refresh \
  -H "Content-Type: application/json" \
  -d '{
 "accessToken": "eyJhbG...",
    "refreshToken": "a1b2c3..."
  }'
```

### 6. Logout

```bash
curl -X POST http://localhost:5000/api/auth/logout \
  -H "Authorization: Bearer eyJhbG..."
```

---

## 📝 Protect Your Endpoints

### Require Authentication

```csharp
[ApiController]
[Route("api/orders")]
[Authorize]  // Add this
public class OrdersController : ControllerBase
{
    // All endpoints require authentication
}
```

### Require Specific Role

```csharp
[HttpDelete("{id}")]
[Authorize(Roles = "ADMIN")]
public async Task<IActionResult> Delete(long id) { ... }
```

### Require Multiple Roles (OR)

```csharp
[Authorize(Roles = "ADMIN,MANAGER")]  // ADMIN OR MANAGER
public async Task<IActionResult> Action() { ... }
```

### Get Current User Info

```csharp
[HttpGet("me")]
[Authorize]
public IActionResult GetCurrentUser()
{
    var userId = User.FindFirst("user_id")?.Value;
    var sessionId = User.FindFirst("session_id")?.Value;
    var roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value);
    var permissions = User.FindAll("permission").Select(c => c.Value);
    
    return Ok(new { userId, sessionId, roles, permissions });
}
```

---

## 🔐 Add Permission-Based Authorization

### Step 1: Add Policy in Program.cs

```csharp
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("CanViewOrders", policy =>
        policy.RequireClaim("permission", "ORDERS:VIEW"));
    
    options.AddPolicy("CanEditOrders", policy =>
      policy.RequireClaim("permission", "ORDERS:EDIT"));
});
```

### Step 2: Use Policy

```csharp
[HttpGet]
[Authorize(Policy = "CanViewOrders")]
public async Task<IActionResult> GetOrders() { ... }
```

---

## 🌐 Frontend Integration (JavaScript)

```javascript
class AuthService {
    accessToken = null;
    refreshToken = null;

    async login(username, password) {
     const response = await fetch('/api/auth/login', {
            method: 'POST',
   headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ username, password })
        });
        
        const result = await response.json();
   
        if (result.success) {
   this.accessToken = result.data.accessToken;
        this.refreshToken = result.data.refreshToken;
   }
   
        return result;
    }

    async refresh() {
        const response = await fetch('/api/auth/refresh', {
            method: 'POST',
       headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({
    accessToken: this.accessToken,
      refreshToken: this.refreshToken
       })
     });
        
        const result = await response.json();
        
        if (result.success) {
            this.accessToken = result.data.accessToken;
            this.refreshToken = result.data.refreshToken;
            return true;
        }
        
        return false;
    }

    async fetchWithAuth(url, options = {}) {
        let response = await fetch(url, {
            ...options,
        headers: {
              ...options.headers,
            'Authorization': `Bearer ${this.accessToken}`
            }
        });
  
     if (response.status === 401) {
        const refreshed = await this.refresh();
         if (refreshed) {
     return this.fetchWithAuth(url, options);
            }
            // Redirect to login
      window.location.href = '/login';
        }
        
        return response;
    }

    async logout() {
        await fetch('/api/auth/logout', {
            method: 'POST',
    headers: {
       'Authorization': `Bearer ${this.accessToken}`
   }
        });
        
     this.accessToken = null;
        this.refreshToken = null;
    }
}

// Usage
const auth = new AuthService();

await auth.login('admin', 'password');
const orders = await auth.fetchWithAuth('/api/orders');
await auth.logout();
```

---

## 📊 Token Lifetimes

| Token | Default | Recommendation |
|-------|---------|----------------|
| Access Token | 15 min | 5-15 min |
| Refresh Token | 7 days | 1-30 days |

**High Security:** Use shorter lifetimes
**Better UX:** Use longer lifetimes

---

## ⚠️ Common Errors

| Error | Cause | Solution |
|-------|-------|----------|
| 401 Unauthorized | Token expired/invalid | Refresh or login again |
| `INVALID_CREDENTIALS` | Wrong username/password | Check credentials |
| `ACCOUNT_LOCKED` | Account disabled | Contact admin |
| `INVALID_REFRESH_TOKEN` | Token reused/expired | Login again |

---

## 📁 Files Created

```
Core/
├── Interface/Auth/
│   ├── ITokenService.cs
│   ├── IAuthSessionRepository.cs
│   ├── IAuthService.cs
│   ├── IAccountRepository.cs
│   └── IPasswordHasher.cs
└── Entity/
    └── AuthSession.cs (updated)

Infa/
└── Auth/
    ├── JwtSettings.cs
    ├── JwtTokenService.cs
 ├── BcryptPasswordHasher.cs
    ├── AuthSessionRepository.cs
    ├── AccountRepository.cs
    ├── AuthService.cs
    └── AuthServiceExtensions.cs

Api/
├── Controllers/
│└── AuthController.cs
├── Models/Auth/
│   └── AuthDtos.cs
└── Program.cs (updated)
```

---

For detailed documentation, see [README.md](README.md).

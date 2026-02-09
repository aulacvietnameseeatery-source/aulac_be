# Conditional Redis Configuration

## Overview

This project now supports **conditional Redis usage** based on environment configuration:

- ? **Development**: Uses **Redis** for caching, queues, and token storage
- ? **Production**: Uses **In-Memory caching** (no Redis dependency)

---

## Configuration

### Development Environment (`appsettings.json`)

```json
{
  "UseRedis": true,
  "ConnectionStrings": {
    "Redis": "localhost:6379"
  }
}
```

### Production Environment (`appsettings.Production.json`)

```json
{
  "UseRedis": false
}
```

---

## Architecture Changes

### 1. **Cache Service Abstraction**

All Redis-specific implementations now use the generic `ICacheService` interface:

| Component | Old Name | New Name | Description |
|-----------|----------|----------|-------------|
| Email Queue | `RedisEmailQueue` | `CacheEmailQueue` | Works with any cache |
| Dead Letter Queue | `RedisDeadLetterSink` | `CacheDeadLetterSink` | Works with any cache |
| Password Reset Tokens | `RedisPasswordResetTokenStore` | `CachePasswordResetTokenStore` | Works with any cache |

### 2. **Cache Service Implementations**

| Implementation | When Used | Description |
|----------------|-----------|-------------|
| `RedisCacheService` | `UseRedis = true` | Distributed cache using Redis |
| `InMemoryCacheService` | `UseRedis = false` | In-memory cache using `IMemoryCache` |

---

## How It Works

### Program.cs Registration Logic

```csharp
var useRedis = builder.Configuration.GetValue<bool>("UseRedis", false);

if (useRedis)
{
    // Redis Configuration (Development)
    builder.Services.AddSingleton<IConnectionMultiplexer>(...);
    builder.Services.AddSingleton<ICacheService, RedisCacheService>();
}
else
{
    // In-Memory Configuration (Production)
    builder.Services.AddMemoryCache();
    builder.Services.AddSingleton<ICacheService, InMemoryCacheService>();
}
```

---

## Benefits

### Development (with Redis)
- ? Persistent queues (emails survive app restarts)
- ? Distributed caching (multi-instance testing)
- ? Real-world production simulation

### Production (without Redis)
- ? **No external dependencies** (easier deployment)
- ? **Lower infrastructure cost** (no Redis server needed)
- ? **Simpler deployment** (single-server setup)
- ? **Nginx handles HTTP caching** (reverse proxy)

---

## Deployment Instructions

### For Development

1. Ensure Redis is running:
   ```bash
   docker run -d -p 6379:6379 redis
   ```

2. Set `UseRedis = true` in `appsettings.json`

3. Run the application:
   ```bash
   dotnet run
   ```

### For Production

1. Set `UseRedis = false` in `appsettings.Production.json`

2. Deploy as usual (no Redis required)

3. Configure Nginx for HTTP response caching (optional)

---

## Testing

### Test Redis Mode (Development)

```bash
# Set environment
export ASPNETCORE_ENVIRONMENT=Development

# Run app
dotnet run

# Verify logs
# Output: ?? Using Redis for caching (Development mode)
```

### Test In-Memory Mode (Production)

```bash
# Set environment
export ASPNETCORE_ENVIRONMENT=Production

# Run app
dotnet run

# Verify logs
# Output: ?? Using In-Memory caching (Production mode)
```

---

## Limitations of In-Memory Cache

?? **Important Notes for Production**:

1. **Not Persistent**: Data lost on app restart
   - Email queue cleared on restart
   - Password reset tokens cleared on restart

2. **Single-Server Only**: Does not share state across multiple instances
   - Do NOT use with load balancers (multiple app instances)
   - Use Redis if you need horizontal scaling

3. **Memory Usage**: All cached data stored in application memory
   - Monitor memory usage
   - Set cache size limits if needed

---

## NuGet Packages

### Added Packages

```xml
<!-- Infa.csproj -->
<PackageReference Include="Microsoft.Extensions.Caching.Memory" Version="10.0.2" />
```

### Existing Packages (Still Required)

```xml
<PackageReference Include="StackExchange.Redis" Version="2.10.1" />
```

> **Note**: Redis package is still included for development mode compatibility.

---

## Migration Guide

### From Old Code (Redis-only)

```csharp
// Old (Redis-specific)
builder.Services.AddSingleton<IEmailQueue, RedisEmailQueue>();
builder.Services.AddSingleton<IDeadLetterSink, RedisDeadLetterSink>();
```

### To New Code (Cache-agnostic)

```csharp
// New (Works with any ICacheService)
builder.Services.AddSingleton<IEmailQueue, CacheEmailQueue>();
builder.Services.AddSingleton<IDeadLetterSink, CacheDeadLetterSink>();
```

---

## FAQ

### Q: Can I use Redis in production?
**A**: Yes! Just set `UseRedis = true` in `appsettings.Production.json` and provide a Redis connection string.

### Q: What happens to queued emails on app restart (production)?
**A**: With in-memory cache, queued emails are lost. For production, consider:
- Using Redis (set `UseRedis = true`)
- Implementing database-backed queue
- Using external queue service (Azure Service Bus, RabbitMQ)

### Q: Can I use multiple app instances in production?
**A**: Only if you enable Redis (`UseRedis = true`). In-memory cache does not support distributed scenarios.

---

## Contact

For questions about this implementation, contact your teacher or team lead.

using Api.Background;
using Api.Middleware;
using API.Middleware;
using API.Models;
using Core.Data;
using Core.Interface.Repo;
using Core.Interface.Service;
using Core.Interface.Service.Auth;
using Core.Interface.Service.Email;
using Core.Interface.Service.Entity;
using Core.Interface.Service.Others;
using Core.Interface.Service.Role;
using Core.Service;
using Infa.Auth;
using Infa.Data;
using Infa.Email;
using Infa.Others;
using Infa.Repo;
using Infa.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using System.Net;
using System.Reflection;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Api.Hubs;
using Api.SignalR;

var builder = WebApplication.CreateBuilder(args);

// Do NOT stop the whole API if a BackgroundService throws
builder.Services.Configure<HostOptions>(options =>
{
    options.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.Ignore;
});

#region Controllers + JSON + Model Validation Response

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Serialize enums as strings in JSON responses
        options.JsonSerializerOptions.Converters.Add(
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));

        options.JsonSerializerOptions.Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
    })
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var errors = context.ModelState
                .Where(x => x.Value?.Errors.Count > 0)
                .SelectMany(kvp => kvp.Value!.Errors.Select(e =>
                    string.IsNullOrWhiteSpace(kvp.Key)
                        ? e.ErrorMessage
                        : $"{kvp.Key}: {e.ErrorMessage}"
                ))
                .ToList();

            var api = new ApiResponse<object>
            {
                Success = false,
                Code = (int)HttpStatusCode.BadRequest,
                SubCode = 1,
                UserMessage = "Input values are not correct.",
                SystemMessage = "Validation failed",
                ValidateInfo = errors,
                Data = new { },
                GetLastData = false,
                ServerTime = DateTimeOffset.UtcNow
            };

            return new BadRequestObjectResult(api);
        };
    });

#endregion

#region Options (Bind from configuration)

builder.Services.Configure<ForgotPasswordRulesOptions>(
    builder.Configuration.GetSection("ForgotPasswordRules"));

builder.Services.Configure<BaseUrlOptions>(
    builder.Configuration.GetSection("BaseUrl"));

builder.Services.Configure<SmtpOptions>(
    builder.Configuration.GetSection("Smtp"));

#endregion

#region Database (DbContext)

var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("Missing Default connection string.");

builder.Services.AddDbContext<RestaurantMgmtContext>(options =>
{
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
});

#endregion

#region Cache Service (Three Modes: Redis / In-Memory / No Cache)

var cacheMode = builder.Configuration.GetValue<string>("CacheMode", "None")?.ToLower();

switch (cacheMode)
{
    case "redis":
        Console.WriteLine($"Cache Mode: Redis ({Environment.MachineName})");

        // Redis Configuration
        builder.Services.AddSingleton(sp =>
        {
            var cs = builder.Configuration.GetConnectionString("Redis")
                ?? throw new InvalidOperationException("Missing Redis connection string when CacheMode=Redis.");

            return new Lazy<IConnectionMultiplexer>(() =>
            {
                var options = ConfigurationOptions.Parse(cs);
                options.AbortOnConnectFail = false;
                options.ConnectRetry = 3;
                options.ReconnectRetryPolicy = new ExponentialRetry(1000);
                return ConnectionMultiplexer.Connect(options);
            });
        });

        builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
       sp.GetRequiredService<Lazy<IConnectionMultiplexer>>().Value);

        builder.Services.AddSingleton<ICacheService, RedisCacheService>();
        break;

    case "memory":
        Console.WriteLine($"Cache Mode: In-Memory ({Environment.MachineName})");

        // In-Memory Cache Configuration
        builder.Services.AddMemoryCache();
        builder.Services.AddSingleton<ICacheService, InMemoryCacheService>();
        break;

    case "none":
    default:
        Console.WriteLine($"Cache Mode: Disabled ({Environment.MachineName})");
        Console.WriteLine("Warning: Disabling cache may impact performance and disable certain features.");
        Console.WriteLine("Warning: Forgotten password functionality will not work.");


        // No caching - simplest production setup
        builder.Services.AddSingleton<ICacheService, NoCacheService>();
        break;
}

#endregion

#region Authentication / Authorization

builder.Services.AddAuthInfrastructure(builder.Configuration);
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddSingleton<IAuthorizationMiddlewareResultHandler, CustomAuthorizationMiddlewareResultHandler>();

builder.Services.AddAuthorization(options =>
{
    var permissionFields = typeof(Permissions)
        .GetFields(BindingFlags.Public | BindingFlags.Static);

    foreach (var field in permissionFields)
    {
        var permission = field.GetValue(null)!.ToString()!;

        options.AddPolicy(permission, policy =>
        {
            policy.RequireClaim("permission", permission);
        });
    }
});

#endregion

#region Business Services

builder.Services.AddScoped<ISystemSettingService, SystemSettingService>();
builder.Services.AddScoped<IAccountService, AccountService>();
// Forgot password token store uses cache
builder.Services.AddSingleton<IPasswordResetTokenStore, CachePasswordResetTokenStore>();

builder.Services.AddScoped<IDishService, DishService>();
builder.Services.AddScoped<IDishCategoryService, DishCategoryService>();

builder.Services.AddScoped<IPasswordGenerator, PasswordGeneratorService>();
builder.Services.AddScoped<IUsernameGenerator, UsernameGeneratorService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IPublicReservationService, PublicReservationService>();
builder.Services.AddScoped<IRoleService, RoleService>();

builder.Services.AddScoped<IAdminReservationService, AdminReservationService>();


#endregion

#region Lookup System

builder.Services.AddScoped<ILookupRepo, LookupRepo>();
builder.Services.AddSingleton<ILookupResolver, LookupResolver>();

#endregion

#region Repositories

builder.Services.AddScoped<IAccountRepository, AccountRepository>();
builder.Services.AddScoped<IRoleRepository, RoleRepository>();
builder.Services.AddScoped<ISystemSettingRepository, SystemSettingRepository>();
builder.Services.AddScoped<IDishRepository, DishRepository>();
builder.Services.AddScoped<IDishCategoryRepository, DishCategoryRepository>();
builder.Services.AddScoped<IReservationRepository, ReservationRepository>();
builder.Services.AddScoped<ITableRepository, TableRepository>();

#endregion

#region Email Services + Background Worker

// Email queue implementation based on cache mode
var emailQueueCacheMode = builder.Configuration.GetValue<string>("CacheMode", "None")?.ToLower();

if (emailQueueCacheMode == "none")
{
    // Direct email sending (synchronous) - no background worker needed
    builder.Services.AddSingleton<IEmailQueue, DirectEmailQueue>();
    // Note: EmailBackgroundService will NOT be registered
}
else
{
    // Cache-based email queue (async with background worker)
    builder.Services.AddSingleton<IEmailQueue, CacheEmailQueue>();
    // Register Background Service for async email processing
    builder.Services.AddHostedService<EmailBackgroundService>();
}

// Other email services (work with all modes)
builder.Services.AddSingleton<IDeadLetterSink, CacheDeadLetterSink>();
builder.Services.AddSingleton<IEmailSender, SmtpEmailSender>();


#endregion

#region SignalR

builder.Services.AddSignalR();
builder.Services.AddScoped<IReservationBroadcastService, SignalRReservationBroadcastService>();
builder.Services.AddScoped<IDishRepository, Infa.Repo.DishRepository>();
builder.Services.AddScoped<IDishService, Core.Service.DishService>();

#endregion

#region Swagger

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    // Add JWT security definition for Swagger
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Enter 'Bearer' followed by a space and your JWT token."
    });

    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

#endregion

#region CORS

// Get allowed origins from configuration (supports multiple environments)
var allowedOrigins = builder.Configuration
    .GetSection("AllowedOrigins")
    .Get<string[]>()
    ?? new[] { "http://localhost:3000" }; // Fallback to localhost if not configured

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(allowedOrigins)
        .AllowAnyMethod()
        .AllowAnyHeader()
        .AllowCredentials();
    });
});

#endregion

var app = builder.Build();

#region Startup Checks / Warmups

// Database connection check (non-blocking)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<RestaurantMgmtContext>();

    try
    {
        if (db.Database.CanConnect())
        {
            Console.WriteLine($"Database connection successful on {app.Environment.EnvironmentName} - {Environment.MachineName}.");

            // Warm up lookup resolver cache
            var lookupResolver = scope.ServiceProvider.GetRequiredService<ILookupResolver>();
            try
            {
                await lookupResolver.WarmUpAsync();
                Console.WriteLine("Lookup resolver cache warmed up.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lookup warmup failed (startup continues): {ex.Message}");
            }
        }
        else
        {
            Console.WriteLine("Database connection failed. Application will start but database operations will fail.");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Database check failed: {ex.Message}");
        Console.WriteLine("Application will start, but database operations may fail.");
    }
}

#endregion

#region Middleware Pipeline
// Register global exception handling middleware
// Must be first in the pipeline to catch all exceptions
app.UseMiddleware<HandleExceptionMiddleware>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowFrontend");
app.UseHttpsRedirection();

// Authentication & Authorization Middleware
// Must be before UseAuthorization
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<ReservationHub>("/hubs/reservation");

#endregion

app.Run();
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
using Core.Service;
using Infa.Auth;
using Infa.Data;
using Infa.Email;
using Infa.Others;
using Infa.Repo;
using Infa.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using System.Net;
using System.Reflection;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

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

#region Redis (NON-BLOCKING STARTUP)

builder.Services.AddSingleton(sp =>
{
    var cs = builder.Configuration.GetConnectionString("Redis");

    return new Lazy<IConnectionMultiplexer>(() =>
    {
        if (string.IsNullOrWhiteSpace(cs))
            throw new InvalidOperationException("Missing Redis connection string.");

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
builder.Services.AddScoped<IPasswordGenerator, PasswordGeneratorService>();
builder.Services.AddScoped<IUsernameGenerator, UsernameGeneratorService>();

#endregion

#region Lookup System

builder.Services.AddScoped<ILookupRepo, LookupRepo>();
builder.Services.AddSingleton<ILookupResolver, LookupResolver>();

#endregion

#region Repositories

builder.Services.AddScoped<IAccountRepository, AccountRepository>();
builder.Services.AddScoped<IRoleRepository, RoleRepository>();
builder.Services.AddScoped<ISystemSettingRepository, SystemSettingRepository>();

#endregion

#region Email Services + Background Worker

builder.Services.AddSingleton<IEmailQueue, RedisEmailQueue>();
builder.Services.AddSingleton<IDeadLetterSink, RedisDeadLetterSink>();
builder.Services.AddSingleton<IEmailSender, SmtpEmailSender>();
builder.Services.AddSingleton<IPasswordResetTokenStore, RedisPasswordResetTokenStore>();
builder.Services.AddHostedService<EmailBackgroundService>();

#endregion

#region Dish Management

builder.Services.AddScoped<IDishRepository, Infa.Repo.DishRepository>();
builder.Services.AddScoped<IDishService, Core.Service.DishService>();

#endregion

#region Swagger

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
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

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:3000")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

#endregion

var app = builder.Build();

#region Startup Checks / Warmups

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<RestaurantMgmtContext>();

    try
    {
        if (!db.Database.CanConnect())
        {
            throw new Exception("Database connection failed.");
        }

        Console.WriteLine("Database connection successful.");

        var lookupResolver = scope.ServiceProvider.GetRequiredService<ILookupResolver>();
        try
        {
            await lookupResolver.WarmUpAsync();
            Console.WriteLine("Lookup resolver cache warmed up.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Lookup warmup skipped (startup continues): {ex.Message}");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Startup error: {ex.Message}");
        throw;
    }
}

#endregion

#region Middleware Pipeline

app.UseMiddleware<HandleExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowFrontend");
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

#endregion

app.Run();
using Api.Background;
using Api.Middleware;
using API.Middleware;
using API.Models;
using Core.Data;
using Core.Interface.Repo;
using Core.Interface.Service;
using Core.Interface.Service.Auth;
using Core.Interface.Service.Email;
using Core.Interface.Service.Others;
using Core.Service;
using Infa.Auth;
using Infa.Data;
using Infa.Email;
using Infa.Others;
using Infa.Repo;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using System;
using System.Net;
using System.Reflection;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Serialize enums as strings in JSON responses (e.g., "Active" instead of 1)
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        
        // Keep existing Unicode handling
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
    }); ;

// DI Configuration:

builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
    ConnectionMultiplexer.Connect(builder.Configuration.GetConnectionString("Redis")!)
);

// Cache service (single entry point)
builder.Services.AddSingleton<ICacheService, RedisCacheService>();

// Authentication Infrastructure
// Register auth services (token service, session repository, password hasher, etc.)
builder.Services.AddAuthInfrastructure(builder.Configuration);

// Services

// Repositories
builder.Services.AddScoped<IAccountRepository, AccountRepository>();

// Email services
builder.Services.AddSingleton<IEmailQueue, RedisEmailQueue>();
builder.Services.AddSingleton<IDeadLetterSink, RedisDeadLetterSink>();
builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();

// Forgot password token store uses cache
builder.Services.AddSingleton<IPasswordResetTokenStore, RedisPasswordResetTokenStore>();


// Register Background Service
builder.Services.AddHostedService<EmailBackgroundService>();


// Load Options from configuration
builder.Services.Configure<ForgotPasswordRulesOptions>(
    builder.Configuration.GetSection("ForgotPasswordRules"));

builder.Services.Configure<BaseUrlOptions>(
    builder.Configuration.GetSection("BaseUrl"));

builder.Services.Configure<SmtpOptions>(builder.Configuration.GetSection("Smtp"));

// Configure DbContext with connection string per environment
var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("Missing Default connection string.");

builder.Services.AddDbContext<RestaurantMgmtContext>(options =>
{
    options.UseMySql(
        connectionString,
        ServerVersion.AutoDetect(connectionString)
    );
});


// Configure JWT Bearer authentication with session validation
builder.Services.AddJwtAuthentication(builder.Configuration);

//Register Custom Authorization Handler
builder.Services.AddSingleton<IAuthorizationMiddlewareResultHandler, CustomAuthorizationMiddlewareResultHandler>();
    
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
            {Reference = new Microsoft.OpenApi.Models.OpenApiReference
            {
                Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Add CORS services
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:3000") // Frontend URL
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials();
    });
});

// Dynamic Permission-Based Authorization permissions from Permissions class
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


var app = builder.Build();



// Kiểm tra kết nối database khi khởi động ứng dụng
// tý xóa
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
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Database connection error: {ex.Message}");
        throw; // stop application
    }
}



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

app.Run();

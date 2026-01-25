using Api.Background;
using Api.Middleware;
using API.Middleware;
using Core.Data;
using Core.Interface.Repo;
using Core.Interface.Service;
using Core.Interface.Service.Email;
using Core.Service;
using Infa.Auth;
using Infa.Data;
using Infa.Email;
using Infa.Repo;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using System;
using System.Reflection;
using System.Text.Encodings.Web;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// DI Configuration:

builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
    ConnectionMultiplexer.Connect(builder.Configuration.GetConnectionString("Redis")!)
);

builder.Services.AddSingleton<IEmailQueue, RedisEmailQueue>();
builder.Services.AddSingleton<IDeadLetterSink, RedisDeadLetterSink>();

builder.Services.Configure<SmtpOptions>(builder.Configuration.GetSection("Smtp"));
builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();



// Register Background Service
builder.Services.AddHostedService<EmailBackgroundService>();



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

// Authentication Infrastructure
// Register auth services (token service, session repository, password hasher, etc.)
builder.Services.AddAuthInfrastructure(builder.Configuration);

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

// Configure JSON to not escape Unicode characters
builder.Services.Configure<JsonOptions>(options =>
{
    options.SerializerOptions.Encoder =
        JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
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

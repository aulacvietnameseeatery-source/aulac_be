
using API.Middleware;
using Core.Interface.Repo;
using Core.Interface.Service;
using Core.Service;
using Infa.Data;
using Infra.Repo;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.EntityFrameworkCore;
using System;
using System.Text.Encodings.Web;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// Cấu Hình DI:
builder.Services.AddControllers();



// Cấu hình DbContext với chuỗi kết nối theo môi trường
var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("Missing Default connection string.");

builder.Services.AddDbContext<RestaurantMgmtContext>(options =>
{
    options.UseMySql(
        connectionString,
        ServerVersion.AutoDetect(connectionString)
    );
});



// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Cấu hình JSON để không escape ký tự Unicode
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


var app = builder.Build();



// Kiểm tra kết nối database khi khởi động ứng dụng
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



// *** QUAN TRỌNG: Đăng ký middleware xử lý exception toàn cục ***
// Phải đặt đầu tiên trong pipeline để bắt được tất cả exception
app.UseMiddleware<HandleExceptionMiddleware>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}


app.UseCors("AllowFrontend");
app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

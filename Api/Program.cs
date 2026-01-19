
using Core.Interface.Repo;
using Core.Interface.Service;
using Core.Middleware;
using Core.Service;
using Infra.Repo;
using Microsoft.AspNetCore.Http.Json;
using System.Text.Encodings.Web;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// Cấu Hình DI:
builder.Services.AddControllers();


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

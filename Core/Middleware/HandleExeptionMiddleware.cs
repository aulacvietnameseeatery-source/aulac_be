using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;

namespace Core.Middleware
{
    /* Middleware xử lý exception toàn cục và trả về ApiResponse chuẩn.
    * Tất cả lỗi đều được format theo ApiResponse với thông báo tiếng Việt thân thiện.
    * Created By DatND (18/1/2026)
    */
    public class HandleExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<HandleExceptionMiddleware> _logger;

        public HandleExceptionMiddleware(RequestDelegate next, ILogger<HandleExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Đã xảy ra lỗi: {Message}", ex.Message);
                await HandleExceptionAsync(context, ex);
            }
        }

        private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";
            var response = context.Response;

            // Khởi tạo error response
            ErrorApiResponse errorResponse;

            // Xử lý các loại exception khác nhau
            switch (exception)
            {
                // Custom Exceptions
                

                // Exceptions thông thường
                case ArgumentNullException argNullEx:
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                    errorResponse = new ErrorApiResponse
                    {
                        Success = false,
                        Code = (int)HttpStatusCode.BadRequest,
                        SubCode = 5,
                        UserMessage = "Dữ liệu đầu vào không hợp lệ. Vui lòng kiểm tra lại.",
                        SystemMessage = $"Tham số '{argNullEx.ParamName}' không được để trống",
                        ValidateInfo = new List<string> { $"Tham số '{argNullEx.ParamName}' là bắt buộc" },
                        Data = null,
                        GetLastData = false,
                        ServerTime = DateTimeOffset.UtcNow
                    };
                    break;

                case ArgumentException argEx:
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                    errorResponse = new ErrorApiResponse
                    {
                        Success = false,
                        Code = (int)HttpStatusCode.BadRequest,
                        SubCode = 6,
                        UserMessage = argEx.Message.Contains("không")
                      ? argEx.Message
                     : "Dữ liệu đầu vào không đúng định dạng. Vui lòng kiểm tra lại.",
                        SystemMessage = argEx.Message,
                        ValidateInfo = new List<string> { argEx.Message },
                        Data = null,
                        GetLastData = false,
                        ServerTime = DateTimeOffset.UtcNow
                    };
                    break;

                case InvalidOperationException invalidOpEx:
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                    errorResponse = new ErrorApiResponse
                    {
                        Success = false,
                        Code = (int)HttpStatusCode.BadRequest,
                        SubCode = 7,
                        UserMessage = "Thao tác không hợp lệ. Vui lòng kiểm tra lại dữ liệu.",
                        SystemMessage = invalidOpEx.Message,
                        ValidateInfo = new List<string> { invalidOpEx.Message },
                        Data = null,
                        GetLastData = false,
                        ServerTime = DateTimeOffset.UtcNow
                    };
                    break;

                case TimeoutException timeoutEx:
                    response.StatusCode = (int)HttpStatusCode.RequestTimeout;
                    errorResponse = new ErrorApiResponse
                    {
                        Success = false,
                        Code = (int)HttpStatusCode.RequestTimeout,
                        SubCode = 10,
                        UserMessage = "Yêu cầu xử lý quá lâu. Vui lòng thử lại.",
                        SystemMessage = timeoutEx.Message,
                        ValidateInfo = new List<string>(),
                        Data = null,
                        GetLastData = false,
                        ServerTime = DateTimeOffset.UtcNow
                    };
                    break;

                case JsonException jsonEx:
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                    errorResponse = new ErrorApiResponse
                    {
                        Success = false,
                        Code = (int)HttpStatusCode.BadRequest,
                        SubCode = 11,
                        UserMessage = "Dữ liệu JSON không hợp lệ. Vui lòng kiểm tra định dạng.",
                        SystemMessage = jsonEx.Message,
                        ValidateInfo = new List<string> { "Định dạng JSON không đúng" },
                        Data = null,
                        GetLastData = false,
                        ServerTime = DateTimeOffset.UtcNow
                    };
                    break;

                default:
                    response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    errorResponse = new ErrorApiResponse
                    {
                        Success = false,
                        Code = (int)HttpStatusCode.InternalServerError,
                        SubCode = 999,
                        UserMessage = "Đã xảy ra lỗi hệ thống. Vui lòng liên hệ quản trị viên hoặc thử lại sau.",
                        SystemMessage = exception.Message,
                        ValidateInfo = new List<string>(),
                        Data = null,
                        GetLastData = false,
                        ServerTime = DateTimeOffset.UtcNow
                    };
                    break;
            }

            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            };

            var result = JsonSerializer.Serialize(errorResponse, options);
            await response.WriteAsync(result);
        }
    }

    /// <summary>
    /// Error response class matching ApiResponse structure
    /// </summary>
    internal class ErrorApiResponse
    {
        public bool Success { get; set; }
        public int Code { get; set; }
        public int SubCode { get; set; }
        public string? UserMessage { get; set; }
        public string? SystemMessage { get; set; }
        public List<string> ValidateInfo { get; set; } = new List<string>();
        public object? Data { get; set; }
        public bool GetLastData { get; set; }
        public DateTimeOffset ServerTime { get; set; }
    }
}

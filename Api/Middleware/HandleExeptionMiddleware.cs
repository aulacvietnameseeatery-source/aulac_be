using API.Models;
using Core.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;

namespace API.Middleware
{
    /* 
     * Middleware xử lý exception toàn cục và trả về ApiResponse chuẩn.
     * Tất cả lỗi đều được format theo ApiResponse với thông báo thân thiện.
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
            ApiResponse<object> errorResponse;

            // Xử lý các loại exception khác nhau
            switch (exception)
            {
                // Business Exceptions (Custom)
                case BusinessException businessEx:
                    response.StatusCode = businessEx.StatusCode;
                    errorResponse = new ApiResponse<object>
                    {
                        Success = false,
                        Code = businessEx.StatusCode,
                        SubCode = GetSubCodeForBusinessException(businessEx.ErrorCode),
                        UserMessage = businessEx.Message,
                        SystemMessage = businessEx.ErrorCode,
                        ValidateInfo = new List<string> { businessEx.Message },
                        Data = new { },
                        GetLastData = false,
                        ServerTime = DateTimeOffset.UtcNow
                    };
                    break;

                // Exceptions thông thường
                case KeyNotFoundException keyNotFoundEx:
                    response.StatusCode = (int)HttpStatusCode.NotFound;
                    errorResponse = new ApiResponse<object>
                    {
                        Success = false,
                        Code = (int)HttpStatusCode.NotFound,
                        SubCode = 404,
                        UserMessage = keyNotFoundEx.Message,
                        SystemMessage = "Resource not found",
                        ValidateInfo = new List<string> { keyNotFoundEx.Message },
                        Data = new { },
                        GetLastData = false,
                        ServerTime = DateTimeOffset.UtcNow
                    };
                    break;

                case ArgumentNullException argNullEx:
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                    errorResponse = new ApiResponse<object>
                    {
                        Success = false,
                        Code = (int)HttpStatusCode.BadRequest,
                        SubCode = 5,
                        UserMessage = "Dữ liệu đầu vào không hợp lệ. Vui lòng kiểm tra lại.",
                        SystemMessage = $"Tham số '{argNullEx.ParamName}' không được để trống",
                        ValidateInfo = new List<string> { $"Tham số '{argNullEx.ParamName}' là bắt buộc" },
                        Data = new { },
                        GetLastData = false,
                        ServerTime = DateTimeOffset.UtcNow
                    };
                    break;

                case ArgumentException argEx:
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                    errorResponse = new ApiResponse<object>
                    {
                        Success = false,
                        Code = (int)HttpStatusCode.BadRequest,
                        SubCode = 6,
                        UserMessage = argEx.Message.Contains("không")
                      ? argEx.Message
                     : "Dữ liệu đầu vào không đúng định dạng. Vui lòng kiểm tra lại.",
                        SystemMessage = argEx.Message,
                        ValidateInfo = new List<string> { argEx.Message },
                        Data = new { },
                        GetLastData = false,
                        ServerTime = DateTimeOffset.UtcNow
                    };
                    break;

                case InvalidOperationException invalidOpEx:
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                    errorResponse = new ApiResponse<object>
                    {
                        Success = false,
                        Code = (int)HttpStatusCode.BadRequest,
                        SubCode = 7,
                        UserMessage = "Thao tác không hợp lệ. Vui lòng kiểm tra lại dữ liệu.",
                        SystemMessage = invalidOpEx.Message,
                        ValidateInfo = new List<string> { invalidOpEx.Message },
                        Data = new { },
                        GetLastData = false,
                        ServerTime = DateTimeOffset.UtcNow
                    };
                    break;

                case TimeoutException timeoutEx:
                    response.StatusCode = (int)HttpStatusCode.RequestTimeout;
                    errorResponse = new ApiResponse<object>
                    {
                        Success = false,
                        Code = (int)HttpStatusCode.RequestTimeout,
                        SubCode = 10,
                        UserMessage = "Yêu cầu xử lý quá lâu. Vui lòng thử lại.",
                        SystemMessage = timeoutEx.Message,
                        ValidateInfo = new List<string>(),
                        Data = new { },
                        GetLastData = false,
                        ServerTime = DateTimeOffset.UtcNow
                    };
                    break;

                case JsonException jsonEx:
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                    errorResponse = new ApiResponse<object>
                    {
                        Success = false,
                        Code = (int)HttpStatusCode.BadRequest,
                        SubCode = 11,
                        UserMessage = "Dữ liệu JSON không hợp lệ. Vui lòng kiểm tra định dạng.",
                        SystemMessage = jsonEx.Message,
                        ValidateInfo = new List<string> { "Định dạng JSON không đúng" },
                        Data = new { },
                        GetLastData = false,
                        ServerTime = DateTimeOffset.UtcNow
                    };
                    break;

                default:
                    response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    errorResponse = new ApiResponse<object>
                    {
                        Success = false,
                        Code = (int)HttpStatusCode.InternalServerError,
                        SubCode = 999,
                        UserMessage = "Đã xảy ra lỗi hệ thống. Vui lòng liên hệ quản trị viên hoặc thử lại sau.",
                        SystemMessage = exception.Message,
                        ValidateInfo = new List<string>(),
                        Data = new { },
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

        private static int GetSubCodeForBusinessException(string errorCode)
        {
            return errorCode switch
            {
                "NOT_FOUND" => 404,
                "CONFLICT" => 409,
                "FORBIDDEN" => 403,
                "VALIDATION_ERROR" => 400,
                _ => 400
            };
        }
    }
}

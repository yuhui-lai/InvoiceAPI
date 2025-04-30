using InvoiceAPI.Exceptions;
using InvoiceAPI.Models.Common;
using Microsoft.AspNetCore.Diagnostics;
using System.Text.Json;

namespace InvoiceAPI.Extensions
{
    public static class WelldoneExceptionHandler
    {
        public static void UseWelldoneExceptHandler(this IApplicationBuilder app, ILoggerFactory loggerFactory)
        {
            app.UseExceptionHandler(new ExceptionHandlerOptions
            {
                ExceptionHandler = async context =>
                {
                    var exceptionFeature = context.Features.Get<IExceptionHandlerFeature>();
                    if (exceptionFeature?.Error == null) return;

                    // 創建服務範圍並解析所需服務
                    using var scope = context.RequestServices.CreateScope();
                    var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
                    var logger = loggerFactory.CreateLogger("WelldoneExceptionHandler");
                    logger.LogError(exceptionFeature.Error, exceptionFeature.Error.Message);

                    // 記錄異常到資料庫
                    //await LogExceptionToDatabase(context, exceptionFeature.Error, logger, scope.ServiceProvider);

                    // 根據請求類型處理回應
                    await HandleApiResponse(context, exceptionFeature.Error);
                }
            });
        }

        private static async Task HandleApiResponse(HttpContext context, Exception error)
        {
            context.Response.ContentType = "application/json";

            // 根據異常類型設置狀態碼和錯誤訊息
            (int statusCode, string message) = error switch
            {
                BusinessException _ => (StatusCodes.Status400BadRequest, error.Message ?? "業務邏輯錯誤"),
                ArgumentException _ => (StatusCodes.Status400BadRequest, error.Message ?? "無效的輸入參數"),
                UnauthorizedAccessException _ => (StatusCodes.Status401Unauthorized, "未授權的訪問"),
                KeyNotFoundException _ => (StatusCodes.Status404NotFound, "請求的資源不存在"),
                TimeoutException _ => (StatusCodes.Status504GatewayTimeout, "請求超時，請稍後重試"),
                _ => (StatusCodes.Status500InternalServerError, "系統內部錯誤，請稍後重試")
            };

            context.Response.StatusCode = statusCode;

            var result = JsonSerializer.Serialize(new CommonAPIModel<string>
            {
                success = false,
                msg = message,
                data = null // 避免暴露詳細錯誤資訊
            });

            await context.Response.WriteAsync(result);
        }
    }
}

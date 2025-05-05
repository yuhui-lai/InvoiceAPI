using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace InvoiceAPI.Lib.Extensions
{
    public class ActionLogMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ActionLogMiddleware> _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        public ActionLogMiddleware(RequestDelegate next, ILogger<ActionLogMiddleware> logger, JsonSerializerOptions jsonOptions)
        {
            _next = next;
            _logger = logger;
            _jsonOptions = jsonOptions;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // 生成唯一的 session GUID
            var sessionGuid = Guid.NewGuid().ToString();
            var action = context.Request.Path; // 請求路徑作為 action

            // 記錄請求
            await LogRequest(context, action, sessionGuid);

            // 啟用回應緩衝以便讀取回應內容
            var originalBodyStream = context.Response.Body;
            using var responseBody = new MemoryStream();
            context.Response.Body = responseBody;

            try
            {
                // 繼續處理請求
                await _next(context);

                // 記錄回應
                await LogResponse(context, action, sessionGuid, responseBody);
            }
            finally
            {
                // 將回應內容寫回原始流
                responseBody.Seek(0, SeekOrigin.Begin);
                await responseBody.CopyToAsync(originalBodyStream);
                context.Response.Body = originalBodyStream;
            }
        }

        private async Task LogRequest(HttpContext context, string action, string sessionGuid)
        {
            // 確保請求流可以重複讀取
            context.Request.EnableBuffering();

            // 讀取請求內容
            using var reader = new StreamReader(
                context.Request.Body,
                encoding: System.Text.Encoding.UTF8,
                detectEncodingFromByteOrderMarks: false,
                leaveOpen: true);
            var requestBody = await reader.ReadToEndAsync();
            context.Request.Body.Position = 0; // 重置流位置

            // 序列化並記錄請求
            using (_logger.BeginScope(new Dictionary<string, object>
            {
                ["action"] = action,
                ["remark"] = "request",
                ["session"] = sessionGuid
            }))
            {
                _logger.LogInformation(requestBody);
            }
        }

        private async Task LogResponse(HttpContext context, string action, string sessionGuid, MemoryStream responseBody)
        {
            // 讀取回應內容
            responseBody.Seek(0, SeekOrigin.Begin);
            var responseContent = await new StreamReader(responseBody).ReadToEndAsync();
            responseBody.Seek(0, SeekOrigin.Begin); // 重置流位置

            // 序列化並記錄回應
            using (_logger.BeginScope(new Dictionary<string, object>
            {
                ["action"] = action,
                ["remark"] = "response",
                ["session"] = sessionGuid
            }))
            {
                _logger.LogInformation(responseContent);
            }
        }
    }
}
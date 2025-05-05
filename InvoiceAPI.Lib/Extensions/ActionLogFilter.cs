using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace InvoiceAPI.Lib.Extensions
{
    public class ActionLogFilter : IActionFilter
    {
        private readonly ILogger<ActionLogFilter> logger;
        private readonly JsonSerializerOptions jsonOptions;
        private readonly string guid;

        public ActionLogFilter(ILogger<ActionLogFilter> logger, JsonSerializerOptions jsonOptions)
        {
            this.logger = logger;
            this.jsonOptions=jsonOptions;
            guid = Guid.NewGuid().ToString();
        }

        public void OnActionExecuting(ActionExecutingContext context)
        {

            var request = context.HttpContext.Request;
            var action = request.Path; // 使用請求路徑作為 action
            var reqBody = JsonSerializer.Serialize(context.ActionArguments.Values, jsonOptions);
            using (logger.BeginScope(new Dictionary<string, object>
            {
                ["action"] = action,
                ["remark"] = "request",
                ["session"] = guid
            }))
            {
                logger.LogInformation(reqBody);
            }
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            var request = context.HttpContext.Request;
            var action = request.Path; // 使用請求路徑作為 action
            if (context.Result is ObjectResult res)
            {
                var resBody = JsonSerializer.Serialize(res.Value, jsonOptions);

                using (logger.BeginScope(new Dictionary<string, object>
                {
                    ["action"] = action,
                    ["remark"] = "response",
                    ["session"] = guid
                }))
                {
                    logger.LogInformation(resBody);
                }
            }
        }
    }
}

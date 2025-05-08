using Microsoft.Extensions.Configuration;
using System.Reflection;
using System.Text;

namespace InvoiceAPI.Lib.Utils
{
    public static class AppSettingUtil
    {
        private static IConfiguration _configuration;

        public static IConfiguration GetConfiguration()
        {
            if (_configuration != null)
                return _configuration;

            var environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";

            _configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory()) // 明確指定基底路徑
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true) // 要求必須存在
                .AddJsonFile($"appsettings.{environmentName}.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

            return _configuration;
        }
    }
}
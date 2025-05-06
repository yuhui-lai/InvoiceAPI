using Microsoft.AspNetCore.Builder;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.MSSqlServer;
using System.Data;

namespace InvoiceAPI.Lib.Extensions
{
    public static class SerilogExtensions
    {
        public static void ConfigureSerilog(this WebApplicationBuilder builder, string connectionString)
        {
            // Configure MSSqlServer Sink options
            var sinkOpts = new MSSqlServerSinkOptions
            {
                TableName = "welldone_common_log", // 指定表格名稱
                AutoCreateSqlTable = false        // 表格已存在，不自動建立
            };

            // Define custom column mappings
            var columnOpts = new ColumnOptions
            {
                AdditionalColumns = new List<SqlColumn>
                {
                    new SqlColumn { ColumnName = "project", DataType = SqlDbType.NVarChar, DataLength = 100, AllowNull = false },
                    new SqlColumn { ColumnName = "action", DataType = SqlDbType.NVarChar, DataLength = 256, AllowNull = true },
                    new SqlColumn { ColumnName = "user_id", DataType = SqlDbType.NVarChar, DataLength = 100, AllowNull = true },
                    new SqlColumn { ColumnName = "remark", DataType = SqlDbType.NVarChar, DataLength = 1000, AllowNull = true },
                    new SqlColumn { ColumnName = "ip_address", DataType = SqlDbType.VarChar, DataLength = 45, AllowNull = true },
                    new SqlColumn { ColumnName = "session", DataType = SqlDbType.VarChar, DataLength = 36, AllowNull = true },
                }
            };

            // Remove the default columns you’re not using and adjust names
            columnOpts.Store.Remove(StandardColumn.MessageTemplate);
            columnOpts.Store.Remove(StandardColumn.Exception);
            columnOpts.Store.Remove(StandardColumn.Properties);
            columnOpts.Store.Remove(StandardColumn.LogEvent);
            columnOpts.Message.ColumnName = "log_content";
            columnOpts.Level.ColumnName = "log_level";         // 將 Level 寫入 log_level
            columnOpts.TimeStamp.ColumnName = "create_date";      // 將 TimeStamp 寫入 create_date

            // Configure Serilog
            Log.Logger = new LoggerConfiguration()
                .Enrich.WithProperty("project", builder.Configuration["ProjectName"]) // 靜態設置 project 值
                .MinimumLevel.Information() // 設定應用程式本身的最低 Log 層級
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning) // 忽略 Microsoft Log 除非是 Warning 或更高層級
                .MinimumLevel.Override("System", LogEventLevel.Warning)    // 忽略 System Log
                .WriteTo.Console()
                .AuditTo.MSSqlServer(
                    connectionString: connectionString,
                    sinkOptions: sinkOpts,
                    columnOptions: columnOpts
                )
                .CreateLogger();

            // Register Serilog with dependency injection
            builder.Services.AddSerilog();
        }
    }
}
using InvoiceAPI.Lib.Entity;
using InvoiceAPI.Lib.Extensions;
using InvoiceAPI.Lib.Interfaces;
using InvoiceAPI.Lib.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;

var builder = WebApplication.CreateBuilder(args);

// Get connection string from environment variable
var connectionString = Environment.GetEnvironmentVariable("WelldoneConnection")
    ?? throw new InvalidOperationException("Connection string 'WelldoneConnection' not found.");

// 設定serilog
builder.ConfigureSerilog(connectionString);
// Add DbContext configuration
builder.Services.AddDbContext<WelldoneContext>(options =>
    options.UseSqlServer(connectionString));
// 配置 System.Text.Json 的序列化選項，防止中文轉碼
// 註冊 JsonSerializerOptions 作為單例
builder.Services.AddSingleton(new JsonSerializerOptions
{
    Encoder = JavaScriptEncoder.Create(UnicodeRanges.BasicLatin, UnicodeRanges.CjkUnifiedIdeographs),
});
//編碼器將基本拉丁字元與中日韓字元納入允許範圍不做轉碼
builder.Services.AddSingleton(HtmlEncoder.Create(allowedRanges: new[] { UnicodeRanges.All }));
builder.Services.AddControllers(options => options.Filters.Add<ActionLogFilter>());
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Invoice API", Version = "v1" });
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    c.IncludeXmlComments(xmlPath);
});
builder.Services.AddScoped<IInvoiceService, InvoiceService>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{ }
var loggerFactory = app.Services.GetRequiredService<ILoggerFactory>();
app.UseWelldoneExceptHandler(loggerFactory);

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

try
{
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "程式異常中斷");
}
finally
{
    Log.CloseAndFlush();
}
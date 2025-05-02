using InvoiceAPI.Entity;
using InvoiceAPI.Extensions;
using InvoiceAPI.Interfaces;
using InvoiceAPI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using System.Text.Encodings.Web;
using System.Text.Unicode;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// Get connection string from environment variable
var connectionString = Environment.GetEnvironmentVariable("WelldoneConnection")
    ?? throw new InvalidOperationException("Connection string 'WelldoneConnection' not found.");

// Add DbContext configuration
builder.Services.AddDbContext<WelldoneContext>(options =>
    options.UseSqlServer(connectionString));
//編碼器將基本拉丁字元與中日韓字元納入允許範圍不做轉碼
builder.Services.AddSingleton(HtmlEncoder.Create(allowedRanges: new[] { UnicodeRanges.All }));
builder.Services.AddControllers();
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
else
{
    var loggerFactory = app.Services.GetRequiredService<ILoggerFactory>();
    app.UseWelldoneExceptHandler(loggerFactory);
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

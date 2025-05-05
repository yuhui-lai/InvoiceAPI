using InvoiceAPI.Lib.Utils;
using InvoiceScheduler;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

Console.WriteLine("Hello, World!");

try
{
    IConfiguration configuration = AppSettingUtil.GetConfiguration();

    // 1. 建立依賴注入的容器
    var service = new ServiceCollection();
    // 2. 註冊服務
    service.AddTransient<App>();
    service.AddSingleton(configuration);

    // 建立依賴服務提供者
    var serviceProvider = service.BuildServiceProvider();

    // 3. 執行主服務
    await serviceProvider.GetRequiredService<App>().Run();
}
catch (Exception ex)
{
    Console.WriteLine("\n\n系統錯誤\n\n");
    Console.WriteLine(ex.Message);
}

using InvoiceAPI.Lib.Services;
using Microsoft.Extensions.Configuration;

namespace InvoiceScheduler
{
    public class App
    {
        private readonly IConfiguration config;
        private readonly InvoiceSchedulerService invoiceSchedulerService;

        public App(IConfiguration config, InvoiceSchedulerService invoiceSchedulerService)
        {
            this.config = config;
            this.invoiceSchedulerService = invoiceSchedulerService;
        }

        public async Task Run()
        {
            Console.WriteLine($"Env: {config["Env"]}");
            Console.WriteLine($"Env: {config["Env"]}");
            Console.WriteLine($"Env: {config["Env"]}");

            await invoiceSchedulerService.CreateF0401();
        }
    }
}

using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InvoiceScheduler
{
    public class App
    {
        private readonly IConfiguration config;

        public App(IConfiguration config)
        {
            this.config = config;

            
        }

        public async Task Run()
        {
            Console.WriteLine($"Env: {config["Env"]}");
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using ZooKeeprDistributedLock;

namespace ZooKeeprDistributedLockSample
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddZooKeeprDistributedLock(hostContext.Configuration);
                    //services.AddHostedService<Worker>();
                    services.AddHostedService<Worker2>();
                });
    }
}

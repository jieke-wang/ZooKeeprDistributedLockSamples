using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ZooKeeprDistributedLock
{
    public static class ServiceCollectionExtentions
    {
        public static IServiceCollection AddZooKeeprDistributedLock(this IServiceCollection services, IConfiguration configuration, string optionKey = ZooKeeprOptions.OptionKey)
        {
            services
                .AddOptions<ZooKeeprOptions>()
                .Bind(configuration.GetSection(optionKey));
            services.AddTransient<ZooKeeprDistributedLock>();
            return services;
        }

        public static IServiceCollection AddZooKeeprDistributedLock(this IServiceCollection services, IConfiguration configuration, Action<ZooKeeprOptions> configureOptions, string optionKey = ZooKeeprOptions.OptionKey)
        {
            services
                .AddOptions<ZooKeeprOptions>()
                .Bind(configuration.GetSection(optionKey))
                .Configure(configureOptions);

            services.AddTransient<ZooKeeprDistributedLock>();
            return services;
        }
    }
}

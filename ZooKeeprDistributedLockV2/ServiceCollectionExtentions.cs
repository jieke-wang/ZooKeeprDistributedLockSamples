using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using org.apache.zookeeper;

namespace ZooKeeprDistributedLockV2
{
    public static class ServiceCollectionExtentions
    {
        public static IServiceCollection AddZooKeeprDistributedLock(this IServiceCollection services, IConfiguration configuration, string optionKey = ZooKeeprOptions.OptionKey)
        {
            services
                .AddOptions<ZooKeeprOptions>()
                .Bind(configuration.GetSection(optionKey));

            ZooKeeper.LogToFile = false;
            ZooKeeper.LogToTrace = false;

            services.AddTransient<ZooKeeprDistributedLock>();
            services.AddHostedService<InitWorker>();
            services.AddSingleton(sp =>
            {
                return new ZooKeeper(sp.GetRequiredService<IOptions<ZooKeeprOptions>>().Value.ConnectionString, 50000, new DefaultWatcher()) { };
            });

            return services;
        }

        public static IServiceCollection AddZooKeeprDistributedLock(this IServiceCollection services, IConfiguration configuration, Action<ZooKeeprOptions> configureOptions, string optionKey = ZooKeeprOptions.OptionKey)
        {
            services
                .AddOptions<ZooKeeprOptions>()
                .Bind(configuration.GetSection(optionKey))
                .Configure(configureOptions);

            ZooKeeper.LogToFile = false;
            ZooKeeper.LogToTrace = false;

            services.AddTransient<ZooKeeprDistributedLock>();
            services.AddHostedService<InitWorker>();
            services.AddSingleton(sp =>
            {
                return new ZooKeeper(sp.GetRequiredService<IOptions<ZooKeeprOptions>>().Value.ConnectionString, 50000, new DefaultWatcher());
            });

            return services;
        }

        public static IServiceCollection AddZooKeeprDistributedLockV2(this IServiceCollection services, IConfiguration configuration, string optionKey = ZooKeeprOptions.OptionKey)
        {
            services
                .AddOptions<ZooKeeprOptions>()
                .Bind(configuration.GetSection(optionKey));

            ZooKeeper.LogToFile = false;
            ZooKeeper.LogToTrace = false;

            services.AddTransient<ZooKeeprDistributedLockV2>();
            services.AddHostedService<InitWorker>();
            services.AddSingleton(sp =>
            {
                return new ZooKeeper(sp.GetRequiredService<IOptions<ZooKeeprOptions>>().Value.ConnectionString, 50000, new DefaultWatcher()) { };
            });

            return services;
        }

        public static IServiceCollection AddZooKeeprDistributedLockV2(this IServiceCollection services, IConfiguration configuration, Action<ZooKeeprOptions> configureOptions, string optionKey = ZooKeeprOptions.OptionKey)
        {
            services
                .AddOptions<ZooKeeprOptions>()
                .Bind(configuration.GetSection(optionKey))
                .Configure(configureOptions);

            ZooKeeper.LogToFile = false;
            ZooKeeper.LogToTrace = false;

            services.AddTransient<ZooKeeprDistributedLockV2>();
            services.AddHostedService<InitWorker>();
            services.AddSingleton(sp =>
            {
                return new ZooKeeper(sp.GetRequiredService<IOptions<ZooKeeprOptions>>().Value.ConnectionString, 50000, new DefaultWatcher());
            });

            return services;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using org.apache.zookeeper;

using ZooKeeprDistributedLock;

namespace ZooKeeprDistributedLockSample
{
    public class Worker2 : BackgroundService
    {
        private readonly ILogger<Worker2> _logger;
        IOptions<ZooKeeprOptions> _zkOptions;
        private ILoggerFactory _loggerFactory;
        private ZooKeeper _zooKeeper;
        private IConfiguration _configuration;
        const string DATA_PATH = "/product-stock";
        const int PRODUCT_STOCK = 30;

        public Worker2(ILogger<Worker2> logger, IOptions<ZooKeeprOptions> zkOptions, IConfiguration configuration, ILoggerFactory loggerFactory)
        {
            _logger = logger;
            _zkOptions = zkOptions;
            _configuration = configuration;
            _loggerFactory = loggerFactory;
        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            _zooKeeper = new ZooKeeper(_zkOptions.Value.ConnectionString, 50000, new DefaultWatcher());
            if ((await _zooKeeper.existsAsync(DATA_PATH)) == null)
            {
                await _zooKeeper.createAsync(DATA_PATH, BitConverter.GetBytes(PRODUCT_STOCK), ZooDefs.Ids.OPEN_ACL_UNSAFE, CreateMode.PERSISTENT);
            }
            else
            {
                await _zooKeeper.setDataAsync(DATA_PATH, BitConverter.GetBytes(PRODUCT_STOCK));
            }

            await base.StartAsync(cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            int minute = int.Parse(_configuration["minute"]);
            Console.WriteLine($"在{minute}分0秒正式开启秒杀！");
            DateTime now = DateTime.Now;
            await Task.Delay(new DateTime(now.Year, now.Month, now.Day, now.Hour, minute, 0) - now);

            int maxCustomer = 30; // 1 30
            Task[] tasks = new Task[maxCustomer];
            for (int i = 0; i < maxCustomer; i++)
            {
                int index = i;
                tasks[i] = SeckillAsync(index);
            }

            await Task.WhenAll(tasks);
            DataResult dataResult = await _zooKeeper.getDataAsync(DATA_PATH);
            int productStock = BitConverter.ToInt32(dataResult.Data, 0);

            _logger.LogInformation($"\n\n\n当前库存: {productStock}");
        }

        private async Task SeckillAsync(int index)
        {
            using ZooKeeprDistributedLock.ZooKeeprDistributedLock distributedLock = new ZooKeeprDistributedLock.ZooKeeprDistributedLock(_zkOptions, _loggerFactory.CreateLogger<ZooKeeprDistributedLock.ZooKeeprDistributedLock>());
            distributedLock.LockName = "Product-xxxxx";

            if (await distributedLock.TryLockAsync(TimeSpan.FromSeconds(10)))
            {
                DataResult dataResult = await _zooKeeper.getDataAsync(DATA_PATH);
                int productStock = BitConverter.ToInt32(dataResult.Data, 0);
                _logger.LogInformation($"\n当前库存: {productStock}, {index} - {Thread.CurrentThread.ManagedThreadId}\n");

                if (productStock > 0)
                {
                    productStock--;
                    await _zooKeeper.setDataAsync(DATA_PATH, BitConverter.GetBytes(productStock));

                    _logger.LogInformation($"\n{index} - 秒杀成功, {Thread.CurrentThread.ManagedThreadId} - {DateTime.Now:yyyy-MM-dd HH:mm:ss.fffff}\n");
                }
                else
                {
                    _logger.LogWarning($"\n{index} - 已售罄, {Thread.CurrentThread.ManagedThreadId} - {DateTime.Now:yyyy-MM-dd HH:mm:ss.fffff}\n");
                }
            }
            else
            {
                _logger.LogWarning($"\n{index} - 秒杀失败, {Thread.CurrentThread.ManagedThreadId} - {DateTime.Now:yyyy-MM-dd HH:mm:ss.fffff}\n");
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            await _zooKeeper.closeAsync();
            await base.StopAsync(cancellationToken);
        }
    }
}

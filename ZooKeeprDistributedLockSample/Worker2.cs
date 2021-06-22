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
        const int PRODUCT_STOCK = 10;

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
            if ((await _zooKeeper.existsAsync(DATA_PATH).ConfigureAwait(false)) == null)
            {
                await _zooKeeper.createAsync(DATA_PATH, BitConverter.GetBytes(PRODUCT_STOCK), ZooDefs.Ids.OPEN_ACL_UNSAFE, CreateMode.PERSISTENT).ConfigureAwait(false);
            }
            else
            {
                await _zooKeeper.setDataAsync(DATA_PATH, BitConverter.GetBytes(PRODUCT_STOCK)).ConfigureAwait(false);
            }

            await base.StartAsync(cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            //int minute = int.Parse(_configuration["minute"]);
            //Console.WriteLine($"在{minute}分0秒正式开启秒杀！");
            //DateTime now = DateTime.Now;
            //await Task.Delay(new DateTime(now.Year, now.Month, now.Day, now.Hour, minute, 0) - now);

            DateTime now = DateTime.Now;
            DateTime startTime = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, 0).AddMinutes(1);
            TimeSpan waitTime = startTime - now;
            int waitSecond = (int)Math.Ceiling(waitTime.TotalSeconds);
            Console.WriteLine($"秒杀开始时间: {startTime}, 等待时间: {waitSecond}s");
            await Task.Delay(waitSecond * 1000, stoppingToken);
            Console.WriteLine("开始秒杀");

            int maxCustomer = PRODUCT_STOCK;
            Task[] tasks = new Task[maxCustomer];
            for (int i = 0; i < maxCustomer; i++)
            {
                int index = i;
                tasks[i] = SeckillAsync(index);
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);
            DataResult dataResult = await _zooKeeper.getDataAsync(DATA_PATH).ConfigureAwait(false);
            int productStock = BitConverter.ToInt32(dataResult.Data, 0);

            _logger.LogInformation($"\n\n\n当前库存: {productStock}");
        }

        private async Task SeckillAsync(int index)
        {
            using ZooKeeprDistributedLock.ZooKeeprDistributedLock distributedLock = new ZooKeeprDistributedLock.ZooKeeprDistributedLock(_zkOptions, _loggerFactory.CreateLogger<ZooKeeprDistributedLock.ZooKeeprDistributedLock>());
            distributedLock.LockName = "Product-xxxxx";

            if (await distributedLock.TryLockAsync(TimeSpan.FromSeconds(10)).ConfigureAwait(false))
            {
                DataResult dataResult = await _zooKeeper.getDataAsync(DATA_PATH).ConfigureAwait(false);
                int productStock = BitConverter.ToInt32(dataResult.Data, 0);
                //_logger.LogInformation($"\n{index} - {Thread.CurrentThread.ManagedThreadId}, 当前库存: {productStock}\n");
                Console.WriteLine($"\n{index} - {Thread.CurrentThread.ManagedThreadId}, 当前库存: {productStock}");

                if (productStock > 0)
                {
                    productStock--;
                    await _zooKeeper.setDataAsync(DATA_PATH, BitConverter.GetBytes(productStock)).ConfigureAwait(false);

                    //_logger.LogInformation($"\n{index} - {Thread.CurrentThread.ManagedThreadId} 秒杀成功, {DateTime.Now:yyyy-MM-dd HH:mm:ss.fffff}\n");
                    Console.WriteLine($"{index} - {Thread.CurrentThread.ManagedThreadId} 秒杀成功, {DateTime.Now:yyyy-MM-dd HH:mm:ss.fffff}\n");
                }
                else
                {
                    //_logger.LogWarning($"\n{index} - 已售罄, {Thread.CurrentThread.ManagedThreadId} - {DateTime.Now:yyyy-MM-dd HH:mm:ss.fffff}\n");
                    Console.WriteLine($"{index} - {Thread.CurrentThread.ManagedThreadId} 已售罄, {DateTime.Now:yyyy-MM-dd HH:mm:ss.fffff}\n");
                }
            }
            else
            {
                //_logger.LogWarning($"\n{index} - 秒杀失败, {Thread.CurrentThread.ManagedThreadId} - {DateTime.Now:yyyy-MM-dd HH:mm:ss.fffff}\n");
                Console.WriteLine($"{index} - {Thread.CurrentThread.ManagedThreadId} 秒杀失败, {DateTime.Now:yyyy-MM-dd HH:mm:ss.fffff}\n");
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            await _zooKeeper.closeAsync().ConfigureAwait(false);
            await base.StopAsync(cancellationToken);
        }
    }
}

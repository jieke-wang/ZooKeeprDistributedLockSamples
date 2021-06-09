using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ZooKeeprDistributedLockSample
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly ZooKeeprDistributedLock.ZooKeeprDistributedLock _distributedLock;

        public Worker(ILogger<Worker> logger, ZooKeeprDistributedLock.ZooKeeprDistributedLock distributedLock)
        {
            _logger = logger;
            _distributedLock = distributedLock;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _distributedLock.LockName = "LockName";
            while (!stoppingToken.IsCancellationRequested)
            {
                Console.WriteLine("\n\n*************************************************\n\n");
                try
                {
                    if (await _distributedLock.TryLockAsync(TimeSpan.FromSeconds(5)))
                    {
                        _logger.LogInformation($"\n获取锁成功, {Thread.CurrentThread.ManagedThreadId} - {DateTime.Now:yyyy-MM-dd HH:mm:ss.fffff}\n");
                        await Task.Delay(1000);
                    }
                    else
                    {
                        _logger.LogWarning($"\n获取锁失败, {Thread.CurrentThread.ManagedThreadId} - {DateTime.Now:yyyy-MM-dd HH:mm:ss.fffff}\n");
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError(e, DateTime.Now.ToString());
                    throw;
                }
                finally
                {
                    await Task.Delay(100, stoppingToken);
                    await _distributedLock.UnlockAsync();
                }
            }
        }
    }
}

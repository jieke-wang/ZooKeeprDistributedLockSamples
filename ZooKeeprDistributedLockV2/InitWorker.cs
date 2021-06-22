using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

using org.apache.zookeeper;

namespace ZooKeeprDistributedLockV2
{
    internal class InitWorker : BackgroundService
    {
        private readonly ZooKeeper _zk;

        public InitWorker(ZooKeeper zk)
        {
            _zk = zk;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var stat = await _zk.existsAsync(ZooKeeprDistributedLock.Root, false).ConfigureAwait(false);
            if (stat == null)
            {
                // 创建根节点                    
                await _zk.createAsync(ZooKeeprDistributedLock.Root, new byte[0], ZooDefs.Ids.OPEN_ACL_UNSAFE, CreateMode.PERSISTENT).ConfigureAwait(false);
            }
        }
    }
}

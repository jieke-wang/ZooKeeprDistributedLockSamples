using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

using org.apache.zookeeper;

namespace ZooKeeprDistributedLockV2
{
    public class DefaultWatcher : Watcher
    {
        public override Task process(WatchedEvent @event)
        {
            return Task.CompletedTask;
        }
    }
}

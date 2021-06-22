using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using org.apache.zookeeper;

namespace ZooKeeprDistributedLockV2
{
    public class ZooKeeprDistributedLockV2 : Watcher, IDisposable, IAsyncDisposable
    {
        //static Random random = new Random(Environment.TickCount);

        private readonly ILogger<ZooKeeprDistributedLockV2> _logger;
        private readonly ZooKeeper _zooKeeper;
        private AutoResetEvent _autoevent;
        internal const string Root = "/locks";   
        private string _lockName;
        private string _waitNode;
        private string _myZnode;

        public string LockName { set { _lockName = value; } }

        public ZooKeeprDistributedLockV2(ILogger<ZooKeeprDistributedLockV2> logger, ZooKeeper zooKeeper)
        {
            _logger = logger;
            _zooKeeper = zooKeeper;
        }

        public override async Task process(WatchedEvent @event)
        {
            await Task.Run(() =>
            {
                if(@event.getPath() == _waitNode && @event.get_Type() == Event.EventType.NodeDeleted)
                {
                    lock(this)
                    {
                        if (this._autoevent != null)
                        {
                            this._autoevent.Set();
                        }
                    }
                }
            }).ConfigureAwait(false);
        }

        protected async virtual Task<bool> WaitForLockAsync(string waitNode, TimeSpan timeout)
        {
            if (string.IsNullOrWhiteSpace(waitNode)) return true;

            var stat = await _zooKeeper.existsAsync($"{waitNode}", this).ConfigureAwait(false); // !!!
            // 判断比自己小一个数的节点是否存在,如果不存在则无需等待锁,同时注册监听
            if (stat != null)
            {
                _autoevent = new AutoResetEvent(false);
                // 阻止当前线程，直到当前实例收到信号，使用 TimeSpan 度量时间间隔并指定是否在等待之前退出同步域
                bool r = _autoevent.WaitOne(timeout);
                lock (this)
                {
                    _autoevent.Dispose();
                    _autoevent = null;
                }

                if (r)
                {
                    ChildrenResult childrenResult = await _zooKeeper.getChildrenAsync(Root, false).ConfigureAwait(false);
                    IList<string> subNodes = childrenResult.Children;
                    if (subNodes == null || subNodes.Count == 0) return false;
                    string minNode = string.Empty;
                    foreach (var subNode in subNodes.Where(x => x.StartsWith(_lockName)))
                    {
                        if (string.IsNullOrWhiteSpace(minNode))
                        {
                            minNode = subNode;
                            continue;
                        }
                        if (minNode.CompareTo(subNode) >= 0)
                            continue;

                        minNode = subNode;
                    }

                    string subMyZnode = _myZnode.Substring(_myZnode.LastIndexOf("/", StringComparison.Ordinal) + 1);
                    r = (minNode == subMyZnode);
                }

                return r;
            }

            return true;
        }

        protected async virtual Task<bool> WaitForLockAsync(string waitNode)
        {
            if (string.IsNullOrWhiteSpace(waitNode)) return true;

            var stat = await _zooKeeper.existsAsync($"{waitNode}", this).ConfigureAwait(false); // !!!
            // 判断比自己小一个数的节点是否存在,如果不存在则无需等待锁,同时注册监听
            if (stat != null)
            {
                _autoevent = new AutoResetEvent(false);
                // 阻止当前线程，直到当前实例收到信号，使用 TimeSpan 度量时间间隔并指定是否在等待之前退出同步域
                bool r = _autoevent.WaitOne();
                lock(this)
                {
                    _autoevent.Dispose();
                    _autoevent = null;
                }

                if(r)
                {
                    ChildrenResult childrenResult = await _zooKeeper.getChildrenAsync(Root, false).ConfigureAwait(false);
                    IList<string> subNodes = childrenResult.Children;
                    if (subNodes == null || subNodes.Count == 0) return false;
                    string minNode = string.Empty;
                    foreach (var subNode in subNodes.Where(x => x.StartsWith(_lockName)))
                    {
                        if(string.IsNullOrWhiteSpace(minNode))
                        {
                            minNode = subNode;
                            continue;
                        }
                        if (minNode.CompareTo(subNode) >= 0)
                            continue;

                        minNode = subNode;
                    }

                    string subMyZnode = _myZnode.Substring(_myZnode.LastIndexOf("/", StringComparison.Ordinal) + 1);
                    r = (minNode == subMyZnode);
                }

                return r;
            }

            return true;
        }

        protected async virtual Task CreateLockAsync()
        {
            try
            {
                string splitStr = "_lock_";

                _myZnode = await _zooKeeper.createAsync($"{Root}/{_lockName}{splitStr}", new byte[0], ZooDefs.Ids.OPEN_ACL_UNSAFE, CreateMode.EPHEMERAL_SEQUENTIAL).ConfigureAwait(false);
                
                int position = _myZnode.LastIndexOf(splitStr);
                if(position > -1)
                {
                    string strSN = _myZnode.Substring(position + 6);
                    long sn = long.Parse(strSN) - 1;
                    _waitNode = $"{Root}/{_lockName}{splitStr}{sn:0000000000}";
                }
            }
            catch (KeeperException e)
            {
                _logger.LogError(e, "TryLockAsync()");
                throw;
            }
        }

        public async virtual Task<bool> TryLockAsync(TimeSpan timeout)
        {
            try
            {
                //await Task.Delay(random.Next(10, 200));
                await CreateLockAsync().ConfigureAwait(false);
                return await WaitForLockAsync(_waitNode, timeout).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "TryLockAsync(TimeSpan timeout)");
                throw;
            }
        }

        public async virtual Task<bool> TryLockAsync()
        {
            try
            {
                await CreateLockAsync().ConfigureAwait(false);
                return await WaitForLockAsync(_waitNode).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "TryLockAsync(TimeSpan timeout)");
                throw;
            }
        }

        public virtual async Task UnlockAsync()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_myZnode) == false)
                {
                    await _zooKeeper.deleteAsync(_myZnode, -1).ConfigureAwait(false);
                    _myZnode = null;
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Unlock()");
                throw;
            }
        }

        public void Dispose()
        {
            UnlockAsync().ConfigureAwait(false).GetAwaiter().GetResult();
        }

        public ValueTask DisposeAsync()
        {
            return new ValueTask(UnlockAsync());
        }
    }
}

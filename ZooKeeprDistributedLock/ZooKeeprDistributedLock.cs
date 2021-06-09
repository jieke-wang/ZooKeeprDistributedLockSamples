//# define OPEN_LOG

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using org.apache.zookeeper;

namespace ZooKeeprDistributedLock
{
    public class ZooKeeprDistributedLock : Watcher, IDisposable, IAsyncDisposable
    {
        private ILogger<ZooKeeprDistributedLock> _logger;
        //private IOptions<ZooKeeprOptions> _zkOptions;
        private ZooKeeper _zk;
        private string _root = "/locks"; //根        
        private string _lockName; //竞争资源的标志        
        private string _waitNode; //等待前一个锁        
        private string _myZnode; //当前锁                 
        private AutoResetEvent _autoevent;
        private int _sessionTimeout = 50000;
        //private IList<Exception> _exception = new List<Exception>();
        private bool _disposed = false;

        /// <summary>
        /// 设置锁标识
        /// </summary>
        public string LockName { set { _lockName = value; } }

        // <summary>
        /// 创建分布式锁
        /// </summary>
        /// <param name="lockName">竞争资源标志,lockName中不能包含单词lock</param>
        public ZooKeeprDistributedLock(IOptions<ZooKeeprOptions> zkOptions, ILogger<ZooKeeprDistributedLock> logger)
        {
            this._logger = logger;
            // 创建一个与服务器的连接            
            try
            {
                _zk = new ZooKeeper(zkOptions.Value.ConnectionString, _sessionTimeout, this);
                Stopwatch sw = new Stopwatch();
                sw.Start();
                while (true)
                {
                    ZooKeeper.States state = _zk.getState();
                    if (state == ZooKeeper.States.CONNECTING) { break; }
                    if (state == ZooKeeper.States.CONNECTED) { break; }
                }
                sw.Stop();
                TimeSpan ts2 = sw.Elapsed;
#if OPEN_LOG
                _logger.LogInformation($"zoo连接总共花费{ts2.TotalMilliseconds}ms."); 
#endif

                var stat = _zk.existsAsync(_root, false).ConfigureAwait(false).GetAwaiter().GetResult();
                if (stat == null)
                {
                    // 创建根节点                    
                    _zk.createAsync(_root, new byte[0], ZooDefs.Ids.OPEN_ACL_UNSAFE, CreateMode.PERSISTENT).ConfigureAwait(false).GetAwaiter().GetResult();
                }
            }
            catch (KeeperException e)
            {
                throw e;
            }
        }

        /// <summary>        
        /// zookeeper节点的监视器        
        /// </summary>     
        public override async Task process(WatchedEvent @event)
        {
            await Task.Run(() =>
            {
#if OPEN_LOG
                _logger.LogInformation($"\nWatched Event: {@event.getPath()}; {@event.getState()}; {@event.get_Type()}\n"); 
#endif
                if (this._autoevent != null)
                {
                    // 将事件状态设置为终止状态，允许一个或多个等待线程继续；如果该操作成功，则返回true；否则，返回false
                    this._autoevent.Set();
                }
            });
        }

        public async virtual Task<bool> TryLockAsync()
        {
            try
            {
                string splitStr = "_lock_";
                //if (_lockName.Contains(splitStr))
                //{
                //    //throw new LockException("lockName can not contains \\u000B");                
                //}

                // 创建临时子节点
                _myZnode = await _zk.createAsync($"{_root}/{_lockName}{splitStr}", new byte[0], ZooDefs.Ids.OPEN_ACL_UNSAFE, CreateMode.EPHEMERAL_SEQUENTIAL);
#if OPEN_LOG
                _logger.LogInformation($"\n{_myZnode} 创建完成！\n"); 
#endif

                // 取出所有子节点         
                ChildrenResult childrenResult = await _zk.getChildrenAsync(_root, false);
                IList<string> subNodes = childrenResult.Children;
                if (subNodes == null || subNodes.Count == 0) return false;

                // 取出所有lockName的锁                
                List<string> lockObjNodes = new List<string>();
                foreach (string node in subNodes)
                {
                    if (node.StartsWith(_lockName))
                    {
                        lockObjNodes.Add(node);
                    }
                }
                if (lockObjNodes.Count == 0) return false;

                //Array alockObjNodes = lockObjNodes.ToArray();
                //Array.Sort(alockObjNodes);
                lockObjNodes.Sort();
#if OPEN_LOG
                _logger.LogInformation($"\n{_myZnode}=={lockObjNodes[0]}\n"); 
#endif

                if (_myZnode.Equals($"{_root}/{lockObjNodes[0]}"))
                {
                    // 如果是最小的节点,则表示取得锁   
#if OPEN_LOG
                    _logger.LogInformation($"\n{_myZnode} 获取锁成功！\n"); 
#endif
                    return true;
                }

                // 如果不是最小的节点，找到比自己小1的节点               
                string subMyZnode = _myZnode.Substring(_myZnode.LastIndexOf("/", StringComparison.Ordinal) + 1);
                //int position = Array.BinarySearch(alockObjNodes, subMyZnode) - 1;
                int position = lockObjNodes.BinarySearch(subMyZnode) - 1;

                if (position > -1 && position < lockObjNodes.Count)
                    _waitNode = lockObjNodes[position];
            }
            catch (KeeperException e)
            {
                _logger.LogError(e, "TryLockAsync()");
                throw;
            }

            return false;
        }

        public async virtual Task<bool> TryLockAsync(TimeSpan timeout)
        {
            try
            {
                if (await TryLockAsync()) return true;
                return await WaitForLockAsync(_waitNode, timeout);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "TryLockAsync(TimeSpan timeout)");
                throw;
            }
        }

        /// <summary>
        /// 等待锁
        /// </summary>
        /// <param name="waitNode">等待的节点</param>
        /// <param name="timeout">超时时间</param>
        /// <returns>等待结果</returns>
        protected async virtual Task<bool> WaitForLockAsync(string waitNode, TimeSpan timeout)
        {
            var stat = await _zk.existsAsync($"{_root}/{waitNode}", true);
            // 判断比自己小一个数的节点是否存在,如果不存在则无需等待锁,同时注册监听
            if (stat != null)
            {
#if OPEN_LOG
                _logger.LogInformation($"\nThread {Thread.CurrentThread.Name} waiting for {_root}/{waitNode}\n"); 
#endif
                _autoevent = new AutoResetEvent(false);
                // 阻止当前线程，直到当前实例收到信号，使用 TimeSpan 度量时间间隔并指定是否在等待之前退出同步域
                bool r = _autoevent.WaitOne(timeout);
                _autoevent.Dispose();
                _autoevent = null;

                return r;
            }

            return true;
        }

        public virtual async Task UnlockAsync(bool closeZK = false)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_myZnode) == false)
                {
#if OPEN_LOG
                    _logger.LogInformation($"\nunlock + {_myZnode}\n"); 
#endif
                    await _zk.deleteAsync(_myZnode, -1);
                    _myZnode = null;
                }

                if (closeZK)
                    await _zk.closeAsync();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Unlock()");
                throw;
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            try
            {
                UnlockAsync(true).ConfigureAwait(false).GetAwaiter().GetResult();
                _disposed = true;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "\nDisposeAsync()\n");
                throw;
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (_disposed) return;
            try
            {
                await UnlockAsync(true);
                _disposed = true;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "DisposeAsync()");
                throw;
            }
        }
    }
}

// https://github.com/shayhatsor/zookeeper/blob/trunk/src/csharp/test/ZooKeeperNetEx.Tests/test/WatcherTest.cs
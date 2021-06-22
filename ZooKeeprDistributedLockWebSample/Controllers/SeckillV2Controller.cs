using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using org.apache.zookeeper;

namespace ZooKeeprDistributedLockWebSample.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class SeckillV2Controller : ControllerBase
    {
        const string DATA_PATH = "/product-stock";
        const int PRODUCT_STOCK = 10;
        const string LockName = "Product-xxxxx";

        // 初始化库存
        [HttpGet]
        public async Task<IActionResult> InitAsync(int stock, [FromServices] ZooKeeper zooKeeper)
        {
            stock = stock <= 0 ? PRODUCT_STOCK : stock;
            if ((await zooKeeper.existsAsync(DATA_PATH)) == null)
            {
                await zooKeeper.createAsync(DATA_PATH, BitConverter.GetBytes(stock), ZooDefs.Ids.OPEN_ACL_UNSAFE, CreateMode.PERSISTENT).ConfigureAwait(false);
            }
            else
            {
                await zooKeeper.setDataAsync(DATA_PATH, BitConverter.GetBytes(stock)).ConfigureAwait(false);
            }

            return Ok("初始化成功");
        }

        // 获取当前库存
        [HttpGet]
        public async Task<IActionResult> GetStockAsync([FromServices] ZooKeeper zooKeeper)
        {
            DataResult dataResult = await zooKeeper.getDataAsync(DATA_PATH).ConfigureAwait(false);
            int productStock = BitConverter.ToInt32(dataResult.Data, 0);
            return Ok(new { stock = productStock });
        }

        [HttpGet]
        public async Task<IActionResult> SeckillAsync(string clientIdentity, [FromServices] ZooKeeper zooKeeper, [FromServices] ZooKeeprDistributedLockV2.ZooKeeprDistributedLockV2 zooKeeprDistributedLock)
        {
            //Console.WriteLine($"{clientIdentity} - 1");
            zooKeeprDistributedLock.LockName = LockName;
            int? currentStock = null;
            //Console.WriteLine($"{clientIdentity} - 2");

            string msg;
            if (await zooKeeprDistributedLock.TryLockAsync(TimeSpan.FromSeconds(10)).ConfigureAwait(false))
            //if (await zooKeeprDistributedLock.TryLockAsync(TimeSpan.FromSeconds(10)).ConfigureAwait(false))
            {
                DataResult dataResult = await zooKeeper.getDataAsync(DATA_PATH);
                int productStock = BitConverter.ToInt32(dataResult.Data, 0);
                currentStock = productStock;
                // Console.WriteLine($"\n{clientIdentity} - {Thread.CurrentThread.ManagedThreadId}, 当前库存: {productStock}");
                if (productStock > 0)
                {
                    productStock--;
                    await zooKeeper.setDataAsync(DATA_PATH, BitConverter.GetBytes(productStock)).ConfigureAwait(false);

                    msg = "秒杀成功";
                }
                else
                {
                    msg = "已售罄";
                }
            }
            else
            {
                msg = "秒杀失败";
            }
            DateTime executeTime = DateTime.Now;
            Console.WriteLine($"{clientIdentity} - {msg}, 当前库存: {currentStock}, {executeTime:yyyy-MM-dd HH:mm:ss.fffff}\n");
            //await zooKeeprDistributedLock.DisposeAsync().ConfigureAwait(false);

            return Ok(new
            {
                clientIdentity,
                msg,
                currentStock,
                executeTime
            });
        }
    }
}

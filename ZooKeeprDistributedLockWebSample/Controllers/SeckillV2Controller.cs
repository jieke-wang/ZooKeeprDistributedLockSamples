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
        static volatile int ProductStock = 0;

        // 初始化库存
        [HttpGet]
        public async Task<IActionResult> InitAsync(int stock, [FromServices] ZooKeeper zooKeeper)
        {
            stock = stock <= 0 ? PRODUCT_STOCK : stock;
            //if ((await zooKeeper.existsAsync(DATA_PATH)) == null)
            //{
            //    await zooKeeper.createAsync(DATA_PATH, BitConverter.GetBytes(stock), ZooDefs.Ids.OPEN_ACL_UNSAFE, CreateMode.PERSISTENT).ConfigureAwait(false);
            //}
            //else
            //{
            //    await zooKeeper.setDataAsync(DATA_PATH, BitConverter.GetBytes(stock)).ConfigureAwait(false);
            //}

            ProductStock = stock;

            return Ok("初始化成功");
        }

        // 获取当前库存
        [HttpGet]
        public async Task<IActionResult> GetStockAsync([FromServices] ZooKeeper zooKeeper)
        {
            //DataResult dataResult = await zooKeeper.getDataAsync(DATA_PATH).ConfigureAwait(false);
            //int productStock = BitConverter.ToInt32(dataResult.Data, 0);
            //return Ok(new { stock = productStock });
            return Ok(new { stock = ProductStock });
        }

        [HttpGet]
        public async Task<IActionResult> SeckillAsync(string clientIdentity, [FromServices] ZooKeeper zooKeeper, [FromServices] ZooKeeprDistributedLockV2.ZooKeeprDistributedLockV2 zooKeeprDistributedLock)
        {
            //Console.WriteLine($"{clientIdentity} - 1");
            zooKeeprDistributedLock.LockName = LockName;
            int? currentStock = null;
            //Console.WriteLine($"{clientIdentity} - 2");
            Random random = new Random(Environment.TickCount);

            int tryTimes = 0;
            string msg;
        Retry:
            if (await zooKeeprDistributedLock.TryLockAsync(TimeSpan.FromSeconds(10)).ConfigureAwait(false))
            //if (await zooKeeprDistributedLock.TryLockAsync(TimeSpan.FromSeconds(10)).ConfigureAwait(false))
            {
                //DataResult dataResult = await zooKeeper.getDataAsync(DATA_PATH);
                //int productStock = BitConverter.ToInt32(dataResult.Data, 0);
                //currentStock = productStock;
                // Console.WriteLine($"\n{clientIdentity} - {Thread.CurrentThread.ManagedThreadId}, 当前库存: {productStock}");

                int productStock = ProductStock;
                currentStock = productStock;
                if (productStock > 0)
                {
                    productStock--;
                    //await zooKeeper.setDataAsync(DATA_PATH, BitConverter.GetBytes(productStock)).ConfigureAwait(false);
                    ProductStock = productStock;

                    msg = "秒杀成功";
                }
                else
                {
                    msg = "已售罄";
                }
            }
            else
            {
                tryTimes++;
                if (tryTimes < 10)
                {
                    await zooKeeprDistributedLock.UnlockAsync().ConfigureAwait(false);
                    await Task.Delay(random.Next(10, 200) * tryTimes);
                    //Console.WriteLine($"{clientIdentity} 重试");
                    goto Retry;
                }
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

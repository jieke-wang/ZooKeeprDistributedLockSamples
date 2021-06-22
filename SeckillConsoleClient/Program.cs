using System;
using System.Threading;
using System.Threading.Tasks;

using RestSharp;

namespace SeckillConsoleClient
{
    class Program
    {
        const int Number_of_customers = 30;

        static async Task Main(string[] args)
        {
            try
            {
                var client = new RestClient("http://localhost:5000");
                //var client = new RestClient("http://192.168.199.133:5000");
                await InitAsync(client);
                await GetStockAsync(client);
                await StartSeckillAsync(client);
                await GetStockAsync(client);
                await Task.Delay(Timeout.Infinite);
            }
            catch (System.Exception ex)
            {
                Console.WriteLine(ex);
                Console.ReadKey();
            }
        }

        static async Task InitAsync(RestClient client)
        {
            var request = new RestRequest("/api/SeckillV2/Init", Method.GET);
            //var request = new RestRequest("/api/Seckill/Init", Method.GET);
            request.AddQueryParameter("stock", Number_of_customers.ToString());
            string res = await client.GetAsync<string>(request);
            Console.WriteLine(res);
        }

        static async Task GetStockAsync(RestClient client)
        {
            var request = new RestRequest("/api/SeckillV2/GetStock", Method.GET);
            //var request = new RestRequest("/api/Seckill/GetStock", Method.GET);
            string res = await client.GetAsync<string>(request);
            Console.WriteLine(res);
        }

        static async Task StartSeckillAsync(RestClient client)
        {
            DateTime now = DateTime.Now;
            DateTime startTime = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, 0).AddMinutes(1);
            TimeSpan waitTime = startTime - now;
            int waitSecond = (int)Math.Ceiling(waitTime.TotalSeconds);
            Console.WriteLine($"秒杀开始时间: {startTime}, 等待时间: {waitSecond}s");
            await Task.Delay(waitSecond * 1000);
            Console.WriteLine("开始秒杀");

            int maxCustomer = Number_of_customers;
            Task[] tasks = new Task[maxCustomer];
            for (int i = 0; i < maxCustomer; i++)
            {
                tasks[i] = SeckillAsync(client);
                //await Task.Delay(10);
            }
            await Task.WhenAll(tasks);
            Console.WriteLine("结束秒杀");
        }

        static async Task SeckillAsync(RestClient client)
        {
            try
            {
                var request = new RestRequest("/api/SeckillV2/Seckill", Method.GET);
                //var request = new RestRequest("/api/Seckill/Seckill", Method.GET);
                request.AddQueryParameter("clientIdentity", Guid.NewGuid().ToString());
                string res = await client.GetAsync<string>(request);
                Console.WriteLine(res);
            }
            catch (Exception)
            {
                // Console.WriteLine(ex);
                Console.WriteLine("服务器端异常");
            }
        }
    }
}

// RestSharp_包子大叔的笔记 https://blog.csdn.net/zheyiw/article/details/90083543

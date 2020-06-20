using System;
using System.Threading;
using System.Threading.Tasks;
using Timer = System.Timers.Timer;

namespace ColinChang.RedisHelper.ConsoleTest
{
    class Program
    {
        static void Main(string[] args)
        {
            LockExecuteTestAsync().Wait();
            Console.ReadKey();
        }

        static async Task LockExecuteTestAsync()
        {
            var func = new Func<int, int, Task<int>>((a, b) =>
            {
                // Console.Write($"thread-{Thread.CurrentThread.ManagedThreadId.ToString()} get the lock.");
                Thread.Sleep(4000);
                return Task.FromResult(a + b);
            });
            var redis =
                new RedisHelper(
                    "10.211.55.2:6379,password=123123,connectTimeout=1000,connectRetry=1,syncTimeout=10000");
            var rdm = new Random();
            for (var i = 0; i < 3; i++)
            {
                new Thread(async () =>
                        {
                            var success = redis.LockExecute("lockTest", Guid.NewGuid().ToString(), func, out var result,
                                TimeSpan.MaxValue,
                                3000, rdm.Next(0, 10), 0);

                            if (success)
                            {
                                var res = await (result as Task<int>);
                                Console.WriteLine($"result is {res}.\t{DateTime.Now.ToLongTimeString()}");
                            }
                            else
                                Console.WriteLine($"failed to get lock.\t{DateTime.Now.ToLongTimeString()}");
                        })
                        {IsBackground = true}
                    .Start();
                await Task.Delay(2000);
            }
        }
    }
}
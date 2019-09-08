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
            //LockExecuteTest();

            LockExecuteTestAsync();
            Console.ReadKey();
        }

        static void LockExecuteTest()
        {
            var key = "lockTest";
            var func = new Func<int, int, int>((a, b) =>
            {
                Console.Write(
                    $"thread-{Thread.CurrentThread.ManagedThreadId.ToString()} get the lock.");
                Thread.Sleep(1000);
                return a + b;
            });
            var redis =
                new RedisHelper("127.0.0.1:6379,connectTimeout=1000,connectRetry=1,syncTimeout=10000");
            for (var i = 0; i < 10; i++)
                new Thread(() =>
                        {
                            redis.LockExecute(key, Guid.NewGuid().ToString(), func, out var result, TimeSpan.MaxValue,
                                0, 1, 2);
                            Console.WriteLine($"result is {result}");
                        })
                        {IsBackground = true}
                    .Start();
        }

        static async Task LockExecuteTestAsync()
        {
            var func = new Func<int, int, Task<int>>((a, b) =>
            {
                Console.Write(
                    $"thread-{Thread.CurrentThread.ManagedThreadId.ToString()} get the lock.");
                Thread.Sleep(1000);
                return Task.FromResult(a + b);
            });
            var redis =
                new RedisHelper(
                    "192.168.0.200:6379,password=x~y!s@m#a$r%t^c&l*a(s)s_r+d,connectTimeout=1000,connectRetry=1,syncTimeout=10000");
            for (var i = 0; i < 10; i++)
                new Thread(async () =>
                        {
                            redis.LockExecute("lockTest", Guid.NewGuid().ToString(), func, out var result,
                                TimeSpan.MaxValue,
                                0, 1, 2);
                            var res = await (result as Task<int>);
                            Console.WriteLine($"result is {res}.\t{DateTime.Now.ToLongTimeString()}");
                        })
                        {IsBackground = true}
                    .Start();
        }
    }
}
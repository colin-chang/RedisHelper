using System;
using System.Threading;

namespace ColinChang.RedisHelper.ConsoleTest
{
    class Program
    {
        static void Main(string[] args)
        {
            LockExecuteTest();

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
                            redis.LockExecute(key, Guid.NewGuid().ToString(), func, out var result, 0, 3000, 1, 2);
                            Console.WriteLine($"result is {result}");
                        })
                        {IsBackground = true}
                    .Start();
        }
    }
}
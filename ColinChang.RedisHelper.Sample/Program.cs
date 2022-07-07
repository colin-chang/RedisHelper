using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ColinChang.RedisHelper.Sample
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.ConfigureServices((context, services) => services
                        // .AddRedisHelper(options =>
                        // {
                        //     options.ConnectionString =
                        //         "192.168.0.203:6379,password=123123,connectTimeout=1000,connectRetry=1,syncTimeout=10000";
                        //     options.DbNumber = 0;
                        // })
                        .AddRedisHelper(context.Configuration.GetSection(nameof(RedisHelperOptions))));

                    webBuilder.Configure(app => app.Run(async context =>
                    {
                        var redis = app.ApplicationServices.GetRequiredService<IRedisHelper>();
                        
                        const string key = "msg";
                        await redis.StringSetAsync(key, "hello world");
                        var content = await redis.StringGetAsync<string>(key);
                        await redis.KeyDeleteAsync(new[] {key});
                        await context.Response.WriteAsync(content);
                    }));
                })
                .Build()
                .Run();
        }
    }
}
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

namespace ColinChang.RedisHelper
{
    public static class RedisHelperExtensions
    {
        public static IServiceCollection AddRedisHelper(this IServiceCollection services, IConfiguration config)
        {
            services.AddOptions<RedisHelperOptions>()
                .Configure(config.Bind)
                .ValidateDataAnnotations();

            services.AddSingleton<IRedisHelper, RedisHelper>();
            return services;
        }
    }
}
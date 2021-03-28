using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

namespace ColinChang.RedisHelper
{
    public static class RedisHelperExtensions
    {
        public static IServiceCollection AddRedisHelper(this IServiceCollection services, IConfiguration configuration)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            services.AddOptions<RedisHelperOptions>()
                .Configure(configuration.Bind)
                .ValidateDataAnnotations();
            services.AddSingleton<IRedisHelper, RedisHelper>();
            return services;
        }

        public static IServiceCollection AddRedisHelper(this IServiceCollection services,
            Action<RedisHelperOptions> configureOptions)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            if (configureOptions == null)
                throw new ArgumentNullException(nameof(configureOptions));

            services.Configure(configureOptions);
            services.AddSingleton<IRedisHelper, RedisHelper>();
            return services;
        }
    }
}
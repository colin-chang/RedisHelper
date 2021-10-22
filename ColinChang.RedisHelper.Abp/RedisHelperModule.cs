using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace ColinChang.RedisHelper.Abp
{
    public class RedisHelperModule : AbpModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var services = context.Services;
            var configuration = context.Services.GetConfiguration();

            services.AddOptions<RedisHelperOptions>()
              .Configure(configuration.Bind)
              .ValidateDataAnnotations();
            services.AddSingleton<IRedisHelper, RedisHelper>();
        }
    }
}

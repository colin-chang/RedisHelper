using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace ColinChang.RedisHelper.WebSample.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class TestController : ControllerBase
    {
        [HttpGet]
        public async Task<string> GetAsync([FromServices] IRedisHelper redis)
        {
            const string key = "msg";
            await redis.StringSetAsync(key, "hello world");
            return await redis.StringGetAsync<string>(key);
        }
    }
}
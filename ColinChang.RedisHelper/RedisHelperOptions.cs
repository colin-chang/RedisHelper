using System.ComponentModel.DataAnnotations;

namespace ColinChang.RedisHelper
{
    public class RedisHelperOptions
    {
        public string ConnectionString { get; set; }

        [Range(0, 15, ErrorMessage = "DbNumber must be between 0 and 15")]
        public int DbNumber { get; set; }
    }
}
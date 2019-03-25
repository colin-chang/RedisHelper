using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StackExchange.Redis;
using Newtonsoft.Json;

namespace ColinChang.RedisHelper
{
    public class RedisHelper
    {
        private readonly ConnectionMultiplexer _conn;
        public int DbNumber { get; set; } = -1;

        public RedisHelper(string connectionString) => _conn = ConnectionMultiplexer.Connect(connectionString);

        private IDatabase Db => _conn.GetDatabase(DbNumber);

        #region String

        public async Task<bool> StringSetAsync<T>(string key, T value) =>
            await Db.StringSetAsync(key, value.ToRedisValue());

        public async Task<T> StringGetAsync<T>(string key) where T : class =>
            (await Db.StringGetAsync(key)).ToObject<T>();

        public async Task<double> StringIncrementAsync(string key, int value = 1) =>
            await Db.StringIncrementAsync(key, value);

        public async Task<double> StringDecrementAsync(string key, int value = 1) =>
            await Db.StringDecrementAsync(key, value);

        #endregion

        #region List

        public async Task<long> EnqueueAsync<T>(string key, T value) =>
            await Db.ListRightPushAsync(key, value.ToRedisValue());

        public async Task<T> DequeueAsync<T>(string key) where T : class =>
            (await Db.ListLeftPopAsync(key)).ToObject<T>();

        /// <summary>
        /// 从队列中读取数据而不出队
        /// </summary>
        /// <param name="key"></param>
        /// <param name="start">起始位置</param>
        /// <param name="stop">结束位置</param>
        /// <typeparam name="T">对象类型</typeparam>
        /// <returns>不指定 start、end 则获取所有数据</returns>
        public async Task<IEnumerable<T>> GetFromQueue<T>(string key, long start = 0, long stop = -1) where T : class =>
            (await Db.ListRangeAsync(key, start, stop)).ToObjects<T>();

        #endregion

        #region Set

        public async Task<bool> SetAddAsync<T>(string key, T value) =>
            await Db.SetAddAsync(key, value.ToRedisValue());

        public async Task<long> SetRemoveAsync<T>(string key, IEnumerable<T> values) =>
            await Db.SetRemoveAsync(key, values.ToRedisValues());

        public async Task<IEnumerable<T>> SetMembersAsync<T>(string key) where T : class =>
            (await Db.SetMembersAsync(key)).ToObjects<T>();
        
        public async Task<bool> SetContainsAsync<T>(string key, T value) =>
            await Db.SetContainsAsync(key, value.ToRedisValue());

        #endregion

        #region Sortedset

        public async Task<bool> SortedSetAddAsync(string key, string member, double score) =>
            await Db.SortedSetAddAsync(key, member, score);

        public async Task<long> SortedSetRemoveAsync(string key, IEnumerable<string> members) =>
            await Db.SortedSetRemoveAsync(key, members.ToRedisValues());

        public async Task<double> SortedSetIncrementAsync(string key, string member, double value) =>
            await Db.SortedSetIncrementAsync(key, member, value);

        public async Task<double> SortedSetDecrementAsync(string key, string member, double value) =>
            await Db.SortedSetDecrementAsync(key, member, value);

        /// <summary>
        /// 按序返回topN
        /// </summary>
        /// <param name="key"></param>
        /// <param name="start"></param>
        /// <param name="stop"></param>
        /// <param name="order"></param>
        /// <returns></returns>
        public async Task<Dictionary<string, double>> SortedSetRangeByRankWithScoresAsync(string key, long start = 0,
            long stop = -1,
            Order order = Order.Ascending) =>
            (await Db.SortedSetRangeByRankWithScoresAsync(key, start, stop, order)).ToDictionary();

        public async Task<Dictionary<string, double>> SortedSetRangeByScoreWithScoresAsync(string key,
            double start = double.NegativeInfinity, double stop = double.PositiveInfinity,
            Exclude exclude = Exclude.None, Order order = Order.Ascending, long skip = 0, long take = -1) =>
            (await Db.SortedSetRangeByScoreWithScoresAsync(key, start, stop, exclude, order, skip, take))
            .ToDictionary();

        #endregion

        #region Hash

        public async Task HashSetAsync(string key, Dictionary<string, string> entries) =>
            await Db.HashSetAsync(key, entries.ToHashEntries());

        public async Task<Dictionary<string, string>> HashGetAsync(string key, IEnumerable<string> fields) =>
            (await Db.HashGetAsync(key, fields.ToRedisValues())).ToDictionary(fields);


        public async Task<Dictionary<string, string>> HashGetAllAsync(string key) =>
            (await Db.HashGetAllAsync(key)).ToDictionary();

        #endregion

        #region Key

        /// <summary>
        /// 删除给定Key
        /// </summary>
        /// <param name="keys">待删除的key集合</param>
        /// <returns>删除key的数量</returns>
        public async Task<long> KeyDeleteAsync(IEnumerable<string> keys) =>
            await Db.KeyDeleteAsync(keys.Select(k => (RedisKey) k).ToArray());

        /// <summary>
        /// 设置指定key过期时间
        /// </summary>
        /// <param name="key"></param>
        /// <param name="expiry"></param>
        /// <returns></returns>
        public async Task<bool> KeyExpireAsync(string key, TimeSpan? expiry) => await Db.KeyExpireAsync(key, expiry);

        public async Task<bool> KeyExpireAsync(string key, DateTime? expiry) => await Db.KeyExpireAsync(key, expiry);

        #endregion

        #region Advanced

        public async Task<long> PublishAsync(string channel, string msg) =>
            await _conn.GetSubscriber().PublishAsync(channel, msg);

        public async Task SubscribeAsync(string channel, Action<string, string> handler)
        {
            await _conn.GetSubscriber().SubscribeAsync(channel, (chn, msg) => handler(chn, msg));
        }

        /// <summary>
        /// 批量执行Redis操作
        /// </summary>
        /// <param name="operations"></param>
        public Task BatchExecuteAsync(params Action[] operations) =>
            Task.Run(() =>
            {
                var batch = Db.CreateBatch();

                foreach (var operation in operations)
                    operation();

                batch.Execute();
            });

        /// <summary>
        /// 获取分布式锁并执行
        /// </summary>
        /// <param name="action">获取锁成功时执行的业务方法</param>
        /// <param name="key">要锁定的key</param>
        /// <param name="value">锁定的value，获取锁时赋值value，在解锁时必须是同一个value的客户端才能解锁</param>
        /// <param name="expiryMillisecond">超时时间</param>
        /// <returns></returns>
        public async Task<bool> LockExecuteAsync(Action action, string key, string value,
            int expiryMillisecond = 3000)
        {
            if (!await Db.LockTakeAsync(key, value, TimeSpan.FromMilliseconds(expiryMillisecond)))
                return false;

            try
            {
                action();
                return true;
            }
            finally
            {
                Db.LockRelease(key, value);
            }
        }

        #endregion
    }

    public static class StackExchangeRedisExtension
    {
        public static RedisValue ToRedisValue<T>(this T value) =>
            value is ValueType || value is string
                ? value.ToString()
                : JsonConvert.SerializeObject(value);

        public static RedisValue[] ToRedisValues<T>(this IEnumerable<T> values) =>
            values.Select(v => v.ToRedisValue()).ToArray();

        public static T ToObject<T>(this RedisValue value) where T : class => typeof(T) == typeof(string)
            ? value.ToString() as T
            : JsonConvert.DeserializeObject<T>(value.ToString());

        public static IEnumerable<T> ToObjects<T>(this IEnumerable<RedisValue> values) where T : class =>
            values.Select(v => v.ToObject<T>());

        public static HashEntry[] ToHashEntries(this Dictionary<string, string> entries)
        {
            var es = new HashEntry[entries.Count];
            for (var i = 0; i < entries.Count; i++)
            {
                var name = entries.Keys.ElementAt(i);
                var value = entries[name];
                es[i] = new HashEntry(name, value);
            }

            return es;
        }

        public static Dictionary<string, string> ToDictionary(this IEnumerable<HashEntry> entries)
        {
            var dict = new Dictionary<string, string>();
            foreach (var entry in entries)
                dict[entry.Name] = entry.Value;

            return dict;
        }

        public static Dictionary<string, string> ToDictionary(this RedisValue[] hashValues, IEnumerable<string> fields)
        {
            var dict = new Dictionary<string, string>();
            for (var i = 0; i < fields.Count(); i++)
                dict[fields.ElementAt(i)] = hashValues[i];

            return dict;
        }

        public static Dictionary<string, double> ToDictionary(this IEnumerable<SortedSetEntry> entries)
        {
            var dict = new Dictionary<string, double>();
            foreach (var entry in entries)
                dict[entry.Element] = entry.Score;

            return dict;
        }
    }
}

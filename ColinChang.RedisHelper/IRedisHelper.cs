using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace ColinChang.RedisHelper
{
    public interface IRedisHelper
    {
        #region String

        Task<bool> StringSetAsync<T>(string key, T value);

        Task<T> StringGetAsync<T>(string key); // where T : class;

        Task<double> StringIncrementAsync(string key, int value = 1);

        Task<double> StringDecrementAsync(string key, int value = 1);

        #endregion

        #region List

        Task<long> EnqueueAsync<T>(string key, T value);

        Task<T> DequeueAsync<T>(string key) where T : class;

        /// <summary>
        /// 从队列中读取数据而不出队
        /// </summary>
        /// <param name="key"></param>
        /// <param name="start">起始位置</param>
        /// <param name="stop">结束位置</param>
        /// <typeparam name="T">对象类型</typeparam>
        /// <returns>不指定 start、end 则获取所有数据</returns>
        Task<IEnumerable<T>> PeekRangeAsync<T>(string key, long start = 0, long stop = -1)
            where T : class;

        #endregion

        #region Set

        Task<bool> SetAddAsync<T>(string key, T value);

        Task<long> SetRemoveAsync<T>(string key, IEnumerable<T> values);

        Task<IEnumerable<T>> SetMembersAsync<T>(string key) where T : class;

        Task<bool> SetContainsAsync<T>(string key, T value);

        #endregion

        #region Sortedset

        Task<bool> SortedSetAddAsync(string key, string member, double score);

        Task<long> SortedSetRemoveAsync(string key, IEnumerable<string> members);

        Task<double> SortedSetIncrementAsync(string key, string member, double value);

        Task<double> SortedSetDecrementAsync(string key, string member, double value);

        /// <summary>
        /// 按序返回topN
        /// </summary>
        /// <param name="key"></param>
        /// <param name="start"></param>
        /// <param name="stop"></param>
        /// <param name="order"></param>
        /// <returns></returns>
        Task<ConcurrentDictionary<string, double>> SortedSetRangeByRankWithScoresAsync(string key,
            long start = 0,
            long stop = -1,
            Order order = Order.Ascending);

        Task<ConcurrentDictionary<string, double>> SortedSetRangeByScoreWithScoresAsync(string key,
            double start = double.NegativeInfinity, double stop = double.PositiveInfinity,
            Exclude exclude = Exclude.None, Order order = Order.Ascending, long skip = 0, long take = -1);

        #endregion

        #region Hash

        Task<ConcurrentDictionary<string, string>> HashGetAsync(string key);

        Task<ConcurrentDictionary<string, string>> HashGetFieldsAsync(string key,
            IEnumerable<string> fields);

        Task HashSetAsync(string key, ConcurrentDictionary<string, string> entries);

        Task HashSetFieldsAsync(string key, ConcurrentDictionary<string, string> fields);

        Task<bool> HashDeleteAsync(string key);

        Task<bool> HashDeleteFieldsAsync(string key, IEnumerable<string> fields);

        #endregion

        #region Key

        IEnumerable<string> GetAllKeys();

        IEnumerable<string> GetAllKeys(EndPoint endPoint);

        Task<bool> KeyExistsAsync(string key);

        /// <summary>
        /// 删除给定Key
        /// </summary>
        /// <param name="keys">待删除的key集合</param>
        /// <returns>删除key的数量</returns>
        Task<long> KeyDeleteAsync(IEnumerable<string> keys);

        /// <summary>
        /// 设置指定key过期时间
        /// </summary>
        /// <param name="key"></param>
        /// <param name="expiry"></param>
        /// <returns></returns>
        Task<bool> KeyExpireAsync(string key, TimeSpan? expiry);

        Task<bool> KeyExpireAsync(string key, DateTime? expiry);

        #endregion

        #region Advanced

        Task<long> PublishAsync(string channel, string msg);

        Task SubscribeAsync(string channel, Action<string, string> handler);

        /// <summary>
        /// 批量执行Redis操作
        /// </summary>
        /// <param name="operations"></param>
        Task ExecuteBatchAsync(params Action[] operations);

        /// <summary>
        /// 获取分布式锁并执行(非阻塞。加锁失败直接返回(false,null))
        /// </summary>
        /// <param name="key">要锁定的key</param>
        /// <param name="value">锁定的value，加锁时赋值value，在解锁时必须是同一个value的客户端才能解锁</param>
        /// <param name="del">加锁成功时执行的业务方法</param>
        /// <param name="expiry">持锁超时时间。超时后锁自动释放</param>
        /// <param name="args">业务方法参数</param>
        /// <returns>(success,return value of the del)</returns>
        Task<(bool, object)> LockExecuteAsync(string key, string value, Delegate del,
            TimeSpan expiry, params object[] args);

        /// <summary>
        /// 获取分布式锁并执行(阻塞。直到成功加锁或超时)
        /// </summary>
        /// <param name="key">要锁定的key</param>
        /// <param name="value">锁定的value，加锁时赋值value，在解锁时必须是同一个value的客户端才能解锁</param>
        /// <param name="del">加锁成功时执行的业务方法</param>
        /// <param name="result">del返回值</param>
        /// <param name="expiry">持锁超时时间。超时后锁自动释放</param>
        /// <param name="timeout">加锁超时时间(ms).0表示永不超时</param>
        /// <param name="args">业务方法参数</param>
        /// <returns>success</returns>
        bool LockExecute(string key, string value, Delegate del, out object result, TimeSpan expiry,
            int timeout = 0, params object[] args);

        bool LockExecute(string key, string value, Action action, TimeSpan expiry, int timeout = 0);

        bool LockExecute<T>(string key, string value, Action<T> action, T arg, TimeSpan expiry, int timeout = 0);

        bool LockExecute<T>(string key, string value, Func<T> func, out T result, TimeSpan expiry,
            int timeout = 0);

        bool LockExecute<T, TResult>(string key, string value, Func<T, TResult> func, T arg, out TResult result,
            TimeSpan expiry, int timeout = 0);

        #endregion
    }
}
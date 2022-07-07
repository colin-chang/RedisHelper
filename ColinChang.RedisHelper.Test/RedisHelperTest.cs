using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using StackExchange.Redis;
using Xunit;
using Xunit.Abstractions;

namespace ColinChang.RedisHelper.Test
{
    public class RedisHelperTest : IClassFixture<RedisHelperFixture>
    {
        private readonly IRedisHelper _redis;
        private readonly ITestOutputHelper _testOutputHelper;

        public RedisHelperTest(RedisHelperFixture redis, ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
            _redis = redis.Redis;
        }

        [Fact]
        public async Task StringTestAsync()
        {
            const string key = "name";
            const string value = "Colin";
            Assert.True(await _redis.StringSetAsync<string>(key, value));
            Assert.Equal(value, await _redis.StringGetAsync<string>(key));
            Assert.Equal(value, await _redis.StringGetAsync<string>(key));
            Assert.Null(await _redis.StringGetAsync<string>("not_exist"));

            const string objKey = "person";
            var people = new People("colin", 18);
            Assert.True(await _redis.StringSetAsync("person", people));
            Assert.Equal(people, await _redis.StringGetAsync<People>(objKey), new PeopleComparer());

            Assert.Equal(2, await _redis.KeyDeleteAsync(new[] { key, objKey }));
        }

        [Fact]
        public async Task ListTestAsync()
        {
            const string key = "redPocket";
            await _redis.EnqueueAsync(key, 4.8);
            await _redis.EnqueueAsync(key, 5.2);

            var pockets = await _redis.PeekRangeAsync<string>(key, 1, 1);
            Assert.Equal("5.2", pockets.FirstOrDefault());

            Assert.Equal("4.8", await _redis.DequeueAsync<string>(key));
            Assert.Equal("5.2", await _redis.DequeueAsync<string>(key));
        }

        [Fact]
        public async Task SetTestAsync()
        {
            const string key = "cameras";

            Assert.True(await _redis.SetAddAsync(key, 0));
            Assert.True(await _redis.SetAddAsync(key, 1));
            Assert.True(await _redis.SetContainsAsync(key, 1));
            Assert.False(await _redis.SetAddAsync(key, 1));

            var cameras = await _redis.SetMembersAsync<string>(key);
            foreach (var camera in cameras)
                _testOutputHelper.WriteLine(camera);

            Assert.Equal(2, await _redis.SetRemoveAsync(key, new[] { 0, 1 }));
        }

        [Fact]
        public async Task SortedSetTestAsync()
        {
            const string key = "top10";

            Assert.True(await _redis.SortedSetAddAsync(key, "colin", 8));
            var score0 = await _redis.SortedSetIncrementAsync(key, "colin", 1);
            var score1 = await _redis.SortedSetDecrementAsync(key, "colin", 1);
            Assert.Equal(1, score0 - score1);

            Assert.True(await _redis.SortedSetAddAsync(key, "robin", 6));
            Assert.True(await _redis.SortedSetAddAsync(key, "tom", 7));
            Assert.True(await _redis.SortedSetAddAsync(key, "bob", 5));
            Assert.True(await _redis.SortedSetAddAsync(key, "elle", 5));
            Assert.True(await _redis.SortedSetAddAsync(key, "helen", 5));

            //返回排名前五，无论分数多少
            var top5 = await _redis.SortedSetRangeByRankWithScoresAsync(key, 0, 4, Order.Descending);
            foreach (var (k, v) in top5)
                _testOutputHelper.WriteLine($"{k}\t{v}");

            _testOutputHelper.WriteLine("---------------");

            //返回6-10分之间前五
            var highScore =
                await _redis.SortedSetRangeByScoreWithScoresAsync(key, 6, 10, order: Order.Descending, take: 5);
            foreach (var (k, v) in highScore)
                _testOutputHelper.WriteLine($"{k}\t{v}");

            await _redis.KeyDeleteAsync(new[] { key });
        }

        [Fact]
        public async Task HashTestAsync()
        {
            const string key = "person";
            await _redis.HashSetAsync(key, new ConcurrentDictionary<string, string>
            {
                ["name"] = "colin",
                ["age"] = "18"
            });

            Assert.True(await _redis.HashDeleteFieldsAsync(key, new[] { "gender", "name" }));

            await _redis.HashSetFieldsAsync(key, new ConcurrentDictionary<string, string>
            {
                ["age"] = "20"
            });

            var dict = await _redis.HashGetFieldsAsync(key, new[] { "age" });
            Assert.Equal("20", dict["age"]);

            await _redis.HashDeleteAsync(key);
        }


        [Fact]
        public async Task BatchExecuteAsync()
        {
            await _redis.ExecuteBatchAsync(
                async () => await _redis.StringSetAsync("name", "colin"),
                async () => await _redis.SetAddAsync("guys", "robin")
            );

            Assert.Equal("colin", await _redis.StringGetAsync<string>("name"));
            Assert.Equal("robin", (await _redis.SetMembersAsync<string>("guys")).FirstOrDefault());

            await _redis.KeyDeleteAsync(new[] { "name", "guys" });
        }

        [Fact]
        public async Task KeyExpiryTestAsync()
        {
            const string key = "expirytest";
            const string value = "haha";

            await _redis.StringSetAsync(key, value);
            await _redis.KeyExpireAsync(key, TimeSpan.FromSeconds(3));

            Assert.Equal(value, await _redis.StringGetAsync<string>(key));
            await Task.Delay(3000);
            Assert.Null(await _redis.StringGetAsync<string>(key));
        }

        [Fact]
        public void GetAllKeys()
        {
            var keys = _redis.GetAllKeys();
            foreach (var key in keys)
                _testOutputHelper.WriteLine(key);
        }

        [Fact]
        public async Task PubSubTestAsync()
        {
            const string channel = "message";
            const string message = "hi there";

            await _redis.SubscribeAsync(channel, (chn, msg) =>
            {
                Assert.Equal(channel, chn);
                Assert.Equal(message, msg);
            });

            await _redis.PublishAsync(channel, message);
        }

        [Fact]
        public async Task LockExecuteTestAsync()
        {
            const string key = "lockTest";

            var func = new Func<int, int, Task<int>>(async (a, b) =>
            {
                // _testOutputHelper.WriteLine(
                //     $"thread-{Thread.CurrentThread.ManagedThreadId.ToString()} get the lock.");
                await Task.Delay(1000);
                return await Task.FromResult(a + b);
            });

            var rdm = new Random();
            for (var i = 0; i < 3; i++)
            {
                new Thread(async () =>
                        {
                            var success = _redis.LockExecute(key, Guid.NewGuid().ToString(), func,
                                out var result,
                                TimeSpan.MaxValue,
                                3000, rdm.Next(0, 10), 0);

                            if (success)
                            {
                                var res = await (result as Task<int>);
                                _testOutputHelper.WriteLine(
                                    $"result is {res}.\t{DateTime.Now.ToLongTimeString()}");
                            }
                            else
                                _testOutputHelper.WriteLine(
                                    $"failed to get lock.\t{DateTime.Now.ToLongTimeString()}");
                        })
                        { IsBackground = true }
                    .Start();
                await Task.Delay(500);
            }

            await Task.Delay(5000);
            _testOutputHelper.WriteLine("all done");
        }
    }

    public class People
    {
        public string Name { get; set; }

        public int Age { get; set; }

        public People(string name, int age)
        {
            Name = name;
            Age = age;
        }
    }

    public class PeopleComparer : IEqualityComparer<People>
    {
        public bool Equals(People x, People y) => x.Name == y.Name && x.Age == y.Age;

        public int GetHashCode(People obj) => obj.GetHashCode();
    }

    public class RedisHelperFixture
    {
        public IRedisHelper Redis { get; }

        public RedisHelperFixture() =>
            Redis = new RedisHelper(new RedisHelperOptions(
                "127.0.0.1:6379,password=123123,connectTimeout=1000,connectRetry=1,syncTimeout=10000", 0));
    }
}
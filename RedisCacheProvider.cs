using System;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace Microsoft.EntityFrameworkCore.DistributedCache
{
    public class RedisCacheProvider : ICacheProvider
    {
        static readonly SemaphoreSlim SemaphoreSlimFetchObject = new SemaphoreSlim(1, 1);
        static readonly SemaphoreSlim SemaphoreSlimFetchObjectAsync = new SemaphoreSlim(1, 1);
        static readonly SemaphoreSlim SemaphoreSlimFetchObjectWithLock = new SemaphoreSlim(1, 1);
        static readonly SemaphoreSlim SemaphoreSlimFetchObjectWithLockAsync = new SemaphoreSlim(1, 1);

        private readonly ConnectionMultiplexer _connectionMultiplexer;
        public IDatabase Db => _connectionMultiplexer.GetDatabase();
        private readonly JsonSerializerSettings _jsonSerializerSettings;
        private readonly string _token = Guid.NewGuid().ToString();//To be unique on each server, node for distributed lock

        public RedisCacheProvider(string connectionString) : this(connectionString, new JsonSerializerSettings())
        {
        }


        public RedisCacheProvider(string connectionString, JsonSerializerSettings jsonSerializerSettings)
        {
            _connectionMultiplexer = ConnectionMultiplexer.Connect(connectionString);
            _jsonSerializerSettings = jsonSerializerSettings;
        }


        public TEntity Get<TEntity>(string key)
        {
            var value = Db.StringGet(key, CommandFlags.PreferSlave);

            return value.HasValue ? JsonConvert.DeserializeObject<TEntity>(value) : default;
        }

        public async Task<TEntity> GetAsync<TEntity>(string key)
        {
            var value = await Db.StringGetAsync(key, CommandFlags.PreferSlave);

            return value.HasValue ? JsonConvert.DeserializeObject<TEntity>(value, _jsonSerializerSettings) : default;
        }

        public TEntity Set<TEntity>(string key, TEntity value, TimeSpan? expiry = null)
        {
            Db.StringSet(key, JsonConvert.SerializeObject(value, _jsonSerializerSettings), expiry);

            return value;
        }

        public async Task<TEntity> SetAsync<TEntity>(string key, TEntity value, TimeSpan? expiry = null)
        {
            await Db.StringSetAsync(key, JsonConvert.SerializeObject(value, _jsonSerializerSettings), expiry);

            return value;
        }

        public TEntity FetchObject<TEntity>(string key, Func<TEntity> func, TimeSpan? expiry = null)
        {
            SemaphoreSlimFetchObject.Wait();
            try
            {
                var value = Db.StringGet(key, CommandFlags.PreferSlave);

                if (value.HasValue)
                    return JsonConvert.DeserializeObject<TEntity>(value);

                var result = func.Invoke();

                return Set(key, result, expiry);
            }
            finally
            {
                SemaphoreSlimFetchObject.Release();
            }
        }

        public async Task<TEntity> FetchObjectAsync<TEntity>(string key, Func<Task<TEntity>> func, TimeSpan? expiry = null)
        {
            await SemaphoreSlimFetchObjectAsync.WaitAsync();
            try
            {
                var value = await Db.StringGetAsync(key, CommandFlags.PreferSlave);

                if (value.HasValue)
                    return JsonConvert.DeserializeObject<TEntity>(value);

                var result = await func.Invoke();

                return await SetAsync(key, result, expiry);
            }
            finally
            {
                SemaphoreSlimFetchObjectAsync.Release();
            }
        }

        public TEntity FetchObjectWithLock<TEntity>(string key, Func<TEntity> func, TimeSpan? expiry = null)
        {
            var token = _token + "FetchObjectWithLock";
            var lockKey = key + "_LockKey";

            SemaphoreSlimFetchObjectWithLock.Wait();
            try
            {
                var value = Db.StringGet(key, CommandFlags.PreferSlave);

                if (value.HasValue)
                    return JsonConvert.DeserializeObject<TEntity>(value);

                if (Db.LockTake(lockKey, token, TimeSpan.FromSeconds(60)))
                {
                    try
                    {
                        var result = func.Invoke();

                        return Set(key, result, expiry);
                    }
                    finally
                    {
                        Db.LockRelease(lockKey, token);
                    }
                }
                else
                {
                    while ((Db.LockQuery(lockKey)).HasValue)
                        Task.Delay(10).GetAwaiter().GetResult();

                    return Get<TEntity>(key);
                }
            }
            finally
            {
                SemaphoreSlimFetchObjectWithLock.Release();
            }
        }

        public async Task<TEntity> FetchObjectWithLockAsync<TEntity>(string key, Func<Task<TEntity>> func, TimeSpan? expiry = null)
        {
            var token = _token + "FetchObjectWithLockAsync";
            var lockKey = key + "_LockKey";

            await SemaphoreSlimFetchObjectWithLockAsync.WaitAsync();
            try
            {
                var value = await Db.StringGetAsync(key, CommandFlags.PreferSlave);

                if (value.HasValue)
                    return JsonConvert.DeserializeObject<TEntity>(value);


                if (await Db.LockTakeAsync(lockKey, token, TimeSpan.FromSeconds(60)))
                {
                    try
                    {
                        var result = await func.Invoke();

                        return await SetAsync(key, result, expiry);
                    }
                    finally
                    {
                        await Db.LockReleaseAsync(lockKey, token);
                    }
                }
                else
                {
                    while ((await Db.LockQueryAsync(lockKey)).HasValue)
                        await Task.Delay(10);

                    return await GetAsync<TEntity>(key);
                }
            }
            finally
            {
                SemaphoreSlimFetchObjectWithLockAsync.Release();
            }
        }

        public bool KeyExists(string key)
        {
            return Db.KeyExists(key);
        }

        public Task<bool> KeyExistsAsync(string key)
        {
            return Db.KeyExistsAsync(key);
        }
    }
}
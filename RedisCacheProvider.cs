using System;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace EFCore.AsCaching
{
    public class RedisCacheProvider : ICacheProvider
    {
        static readonly SemaphoreSlim SemaphoreSlim = new SemaphoreSlim(1, 1);

        private readonly ConnectionMultiplexer _connectionMultiplexer;
        private IDatabase Db => _connectionMultiplexer.GetDatabase();
        private readonly JsonSerializerSettings _jsonSerializerSettings;

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
            var value = Db.StringGet(key);

            if (value.HasValue)
                return JsonConvert.DeserializeObject<TEntity>(value);

            return default;
        }

        public async Task<TEntity> GetAsync<TEntity>(string key)
        {
            var value = await Db.StringGetAsync(key);

            if (value.HasValue)
                return JsonConvert.DeserializeObject<TEntity>(value, _jsonSerializerSettings);

            return default;
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
            var value = Get<TEntity>(key);
            if (value != null)
                return value;

            var result = func.Invoke();

            return Set(key, result, expiry);
        }

        public async Task<TEntity> FetchObjectAsync<TEntity>(string key, Func<Task<TEntity>> func, TimeSpan? expiry = null)
        {
            var value = await GetAsync<TEntity>(key);
            if (value != null)
                return value;

            var result = await func.Invoke();

            return await SetAsync(key, result, expiry);
        }

        public TEntity FetchObjectWithLock<TEntity>(string key, Func<TEntity> func, TimeSpan? expiry = null)
        {
            throw new NotImplementedException();
        }

        public Task<TEntity> FetchObjectWithLockAsync<TEntity>(string key, Func<Task<TEntity>> func, TimeSpan? expiry = null)
        {
            throw new NotImplementedException();
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
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace EFCore.AsCaching
{
    public class RoundRobinList<T>
    {
        private readonly IList<T> _list;
        private int _size;
        private int _position;

        public RoundRobinList()
        {
            _list = new List<T>();
            _size = _list.Count;
        }

        public T Next()
        {
            if (_size == 1)
                return _list[0];

            Interlocked.Increment(ref _position);
            var mod = _position % _size;
            return _list[mod];
        }

        public void Add(T value)
        {
            _list.Add(value);
            _size = _list.Count;
        }
    }

    public class RedisCacheProvider : ICacheProvider
    {
        static readonly SemaphoreSlim SemaphoreSlim = new SemaphoreSlim(1, 1);

        public IDatabase Db => _connectionMultiplexersPool.Next().GetDatabase();
        private readonly JsonSerializerSettings _jsonSerializerSettings;

        private readonly RoundRobinList<ConnectionMultiplexer> _connectionMultiplexersPool = new RoundRobinList<ConnectionMultiplexer>();

        public RedisCacheProvider(string connectionString) : this(connectionString, new JsonSerializerSettings())
        {
        }


        public RedisCacheProvider(string connectionString, JsonSerializerSettings jsonSerializerSettings)
        {
            var connectionMultiplexer = ConnectionMultiplexer.ConnectAsync(connectionString).GetAwaiter().GetResult();
            _connectionMultiplexersPool.Add(connectionMultiplexer);

//            Parallel.For(0, 100, num =>
//            {
//                var connectionMultiplexer = ConnectionMultiplexer.ConnectAsync(connectionString).GetAwaiter().GetResult();
//                _connectionMultiplexersPool.Add(connectionMultiplexer);
//            });


            _jsonSerializerSettings = jsonSerializerSettings;
        }


        public TEntity Get<TEntity>(string key)
        {
            var value = Db.StringGet(key);

            return value.HasValue ? JsonConvert.DeserializeObject<TEntity>(value) : default;
        }

        public async Task<TEntity> GetAsync<TEntity>(string key)
        {
            var value = await Db.StringGetAsync(key);

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
            var value = Db.StringGet(key);

            if (value.HasValue)
                return JsonConvert.DeserializeObject<TEntity>(value);

            var result = func.Invoke();

            return Set(key, result, expiry);
        }

        public async Task<TEntity> FetchObjectAsync<TEntity>(string key, Func<Task<TEntity>> func, TimeSpan? expiry = null)
        {
            var value = await Db.StringGetAsync(key);

            if (value.HasValue)
                return JsonConvert.DeserializeObject<TEntity>(value);

            var result = await func.Invoke();

            return await SetAsync(key, result, expiry);
        }

        public TEntity FetchObjectWithLock<TEntity>(string key, Func<TEntity> func, TimeSpan? expiry = null)
        {
            SemaphoreSlim.Wait();
            try
            {
                var value = Db.StringGet(key);

                if (value.HasValue)
                    return JsonConvert.DeserializeObject<TEntity>(value);

                var result = func.Invoke();

                return Set(key, result, expiry);
            }
            finally
            {
                SemaphoreSlim.Release();
            }
        }

        public async Task<TEntity> FetchObjectWithLockAsync<TEntity>(string key, Func<Task<TEntity>> func, TimeSpan? expiry = null)
        {
            await SemaphoreSlim.WaitAsync();
            try
            {
                var value = await Db.StringGetAsync(key);

                if (value.HasValue)
                    return JsonConvert.DeserializeObject<TEntity>(value);

                var result = await func.Invoke();

                return await SetAsync(key, result, expiry);
            }
            finally
            {
                SemaphoreSlim.Release();
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
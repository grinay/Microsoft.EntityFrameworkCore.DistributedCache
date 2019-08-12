using System;
using System.Threading.Tasks;

namespace EntityFramework.DistributedCache
{
    /// <summary>
    /// A cache provider to store and receive query results./>
    /// </summary>
    public interface ICacheProvider
    {
        TEntity Get<TEntity>(string key);
        Task<TEntity> GetAsync<TEntity>(string key);
        TEntity Set<TEntity>(string key, TEntity value, TimeSpan? expiry = null);
        Task<TEntity> SetAsync<TEntity>(string key, TEntity value, TimeSpan? expiry = null);
        TEntity FetchObject<TEntity>(string key, Func<TEntity> func, TimeSpan? expiry = null);
        Task<TEntity> FetchObjectAsync<TEntity>(string key, Func<Task<TEntity>> func, TimeSpan? expiry = null);
        TEntity FetchObjectWithLock<TEntity>(string key, Func<TEntity> func, TimeSpan? expiry = null);
        Task<TEntity> FetchObjectWithLockAsync<TEntity>(string key, Func<Task<TEntity>> func, TimeSpan? expiry = null);
        bool KeyExists(string key);
        Task<bool> KeyExistsAsync(string key);
    }
}
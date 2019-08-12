using System;

namespace EFCore.AsCaching
{
    /// <summary>
    /// Options how to handle result caching.
    /// </summary>
    public class CachingOptions
    {
        /// <summary>
        /// Limits the lifetime of cached query results. Default value is 5 minutes.
        /// </summary>
        public TimeSpan Expiry { get; set; } = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Distributed lock uses Semaphore on the local level of cache and applies Redis lock while writing new value into cache
        /// The algorithm is:
        /// Enter into cache function and trying to get cache
        /// if cache is not available it tries to get lock for exclusive access on local level of application with semaphore
        /// When local lock is acquired we trying to get value from cache (this is cover case when other node(server) setup value into cache already), if no cache is found,
        /// it tries to get lock on Redis level, to order have exclusive access between all nodes in case of many nodes using this cache
        /// When redis lock is acquired is writes new value into cache
        /// </summary>
        public bool DistributedLock { get; set; } = true;
    }
}
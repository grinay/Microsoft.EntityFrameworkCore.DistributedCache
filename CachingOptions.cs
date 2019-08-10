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

    }
}

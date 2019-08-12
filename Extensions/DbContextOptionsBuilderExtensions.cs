using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Extensions.Internal;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Query.Internal;

namespace EFCore.AsCaching.Extensions
{
    public static class DbContextOptionsBuilderExtensions
    {
        private static ICacheProvider _cacheProvider;

        private static object _lock = new object();

        /// <summary>
        /// Configures the context to support second level query caching.
        /// </summary>
        /// <param name="optionsBuilder">The builder being used to configure the context.</param>
        /// <param name="redisConnectionString"></param>
        /// <returns>The options builder so that further configuration can be chained.</returns>
//        public static DbContextOptionsBuilder UseDistributedSecondLevelCache(this DbContextOptionsBuilder optionsBuilder, string redisConnectionString)
//        {
//            lock (_lock)
//            {
//                if (_cacheProvider == null)
//                    _cacheProvider = new RedisCacheProvider(redisConnectionString);
//            }
//
//            return optionsBuilder.UseDistributedSecondLevelCache(_cacheProvider);
//        }

        /// <summary>
        /// Configures the context to support second level query caching.
        /// </summary>
        /// <param name="optionsBuilder">The builder being used to configure the context.</param>
        /// <param name="cacheProvider">The cache provider to storage query results.</param>
        /// <returns>The options builder so that further configuration can be chained.</returns>
        public static DbContextOptionsBuilder UseDistributedSecondLevelCache(this DbContextOptionsBuilder optionsBuilder, ICacheProvider cacheProvider)
        {
            optionsBuilder.ReplaceService<IQueryCompiler, CustomQueryCompiler>();

            ((IDbContextOptionsBuilderInfrastructure) optionsBuilder).AddOrUpdateExtension(new AsCachingOptionsExtension(cacheProvider));

            return optionsBuilder;
        }
    }
}
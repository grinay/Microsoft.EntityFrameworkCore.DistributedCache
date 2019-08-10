using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Query.Internal;

namespace EFCore.AsCaching.Extensions
{
    public static class DbContextOptionsBuilderExtensions
    {
        /// <summary>
        /// Configures the context to support second level query caching.
        /// </summary>
        /// <param name="optionsBuilder">The builder being used to configure the context.</param>
        /// <param name="redisConnectionString"></param>
        /// <returns>The options builder so that further configuration can be chained.</returns>
        public static DbContextOptionsBuilder UseDistributedSecondLevelCache(this DbContextOptionsBuilder optionsBuilder,string redisConnectionString)
        {
            return optionsBuilder.UseDistributedSecondLevelCache(new RedisCacheProvider(redisConnectionString));
        }

        /// <summary>
        /// Configures the context to support second level query caching.
        /// </summary>
        /// <param name="optionsBuilder">The builder being used to configure the context.</param>
        /// <param name="cacheProvider">The cache provider to storage query results.</param>
        /// <returns>The options builder so that further configuration can be chained.</returns>
        public static DbContextOptionsBuilder UseDistributedSecondLevelCache(this DbContextOptionsBuilder optionsBuilder, ICacheProvider cacheProvider)
        {
            optionsBuilder.ReplaceService<IQueryCompiler, CustomQueryCompiler>();

            ((IDbContextOptionsBuilderInfrastructure)optionsBuilder).AddOrUpdateExtension(new AsCachingOptionsExtension(cacheProvider));

            return optionsBuilder;
        }
    }
}

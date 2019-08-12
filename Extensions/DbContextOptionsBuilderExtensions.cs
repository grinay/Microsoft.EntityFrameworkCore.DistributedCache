using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Query.Internal;

namespace EntityFramework.DistributedCache.Extensions
{
    public static class DbContextOptionsBuilderExtensions
    {
        public static DbContextOptionsBuilder UseDistributedSecondLevelCache(this DbContextOptionsBuilder optionsBuilder, ICacheProvider cacheProvider)
        {
            optionsBuilder.ReplaceService<IQueryCompiler, CustomQueryCompiler>();

            ((IDbContextOptionsBuilderInfrastructure) optionsBuilder).AddOrUpdateExtension(new AsCachingOptionsExtension(cacheProvider));

            return optionsBuilder;
        }
    }
}
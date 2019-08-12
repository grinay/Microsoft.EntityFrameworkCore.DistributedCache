using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Query.Internal;

namespace Microsoft.EntityFrameworkCore.DistributedCache.Extensions
{
    public static class DbContextOptionsBuilderExtensions
    {
        public static DbContextOptionsBuilder UseDistributedCache(this DbContextOptionsBuilder optionsBuilder, ICacheProvider cacheProvider)
        {
            optionsBuilder.ReplaceService<IQueryCompiler, CustomQueryCompiler>();

            ((IDbContextOptionsBuilderInfrastructure) optionsBuilder).AddOrUpdateExtension(new AsCachingOptionsExtension(cacheProvider));

            return optionsBuilder;
        }
    }
}
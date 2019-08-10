using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace EFCore.AsCaching
{
    public class AsCachingOptionsExtension : IDbContextOptionsExtension
    {
        readonly ICacheProvider _cacheProvider;

        internal AsCachingOptionsExtension(ICacheProvider cacheProvider)
        {
            _cacheProvider = cacheProvider;
        }

        public string LogFragment => $"Using {_cacheProvider.GetType().Name}";

        public bool ApplyServices(IServiceCollection services)
        {
            services.AddSingleton<ICacheProvider>(_cacheProvider);

            return false;
        }

        public long GetServiceProviderHashCode() => 0L;

        public void Validate(IDbContextOptions options)
        {            
        }

        /// <summary>
        ///     The option set from the <see cref="DbContextOptionsBuilder.UseSecondLevelMemoryCache" /> method.
        /// </summary>
        public virtual ICacheProvider CacheProvider => _cacheProvider;
    }
}

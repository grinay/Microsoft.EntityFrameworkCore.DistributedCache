using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace EntityFramework.DistributedCache
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
        
        public virtual ICacheProvider CacheProvider => _cacheProvider;
    }
}

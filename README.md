[![Build Status](https://grinay.visualstudio.com/Microsoft.EntityFrameworkCore.DistributedCache/_apis/build/status/Microsoft.EntityFrameworkCore.DistributedCache-CI?branchName=master)](https://grinay.visualstudio.com/Microsoft.EntityFrameworkCore.DistributedCache/_build/latest?definitionId=6&branchName=master) [![NuGet Status](http://img.shields.io/nuget/v/Extention.Microsoft.EntityFrameworkCore.DistributedCache.svg?style=flat)](https://www.nuget.org/packages/Extention.Microsoft.EntityFrameworkCore.DistributedCache/) [![NuGet Status](http://img.shields.io/nuget/dt/Extention.Microsoft.EntityFrameworkCore.DistributedCache.svg?style=flat)](https://www.nuget.org/packages/Extention.Microsoft.EntityFrameworkCore.DistributedCache/)   

# Microsoft.EntityFrameworkCore.DistributedCache
Entity Framework Core Distributed Cache with Redis with fully support for asynchronous environments. That increase performance drastically.

# Usage example
```csharp
public void ConfigureServices(IServiceCollection services) {
    //Create instance of RedisCache
    ICacheProvider cacheProvider = new RedisCacheProvider(Configuration.GetConnectionString("Redis"));

    services.AddDbContext < AppDbContext > ((serviceProvider, options) => {
        options.UseSqlServer(Configuration.GetConnectionString("Default"));
        //Do not use like that 
        //options.UseDistributedSecondLevelCache(new RedisCacheProvider);
        //This method called with every request, and it will spawn many redis cache instances
        options.UseDistributedCache(cacheProvider);
    });

    services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2).AddControllersAsServices();
}
```

# Caching queries

By default all caching queries are use distributed lock between each nodes, to order only those instance(node,server) and thread who first acquire lock, will setup cache for this request. 

```csharp
return await _appDbContext.Products.AsCaching(TimeSpan.FromSeconds(30)).Where(x => x.Id != Guid.Empty).ToListAsync();
```
if you want to use it only on one server, it makes no sense to use distributed lock.
You can disable it like:
```csharp
var cacheOptions = new CachingOptions() {
    Expiry = TimeSpan.FromSeconds(30),
    DistributedLock = false
};

return await _appDbContext.Products.AsCaching(cacheOptions).Where(x => x.Id != Guid.Empty).ToListAsync();
```

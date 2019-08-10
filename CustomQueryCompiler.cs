using System.Collections.Generic;
using System.Data.HashFunction.xxHash;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EFCore.AsCaching.ExpressionVisitors;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.Internal;
using Microsoft.EntityFrameworkCore.Storage;
using Remotion.Linq.Parsing.ExpressionVisitors.TreeEvaluation;

namespace EFCore.AsCaching
{
    /// <summary>
    /// Extended <see cref="QueryCompiler"/> to handle query caching.
    /// </summary>
    public class CustomQueryCompiler : QueryCompiler
    {
        private readonly IQueryModelGenerator _queryModelGenerator;
        private readonly ICacheProvider _cacheProvider;
        private readonly IxxHash _xxHash;

        public CustomQueryCompiler(
            IQueryContextFactory queryContextFactory,
            ICompiledQueryCache compiledQueryCache,
            ICompiledQueryCacheKeyGenerator compiledQueryCacheKeyGenerator,
            IDatabase database,
            IDiagnosticsLogger<DbLoggerCategory.Query> logger,
            ICurrentDbContext currentContext,
            IQueryModelGenerator queryModelGenerator,
            IEvaluatableExpressionFilter evaluableExpressionFilter)
            : base(queryContextFactory, compiledQueryCache, compiledQueryCacheKeyGenerator, database, logger, currentContext, queryModelGenerator)
        {
            Check.NotNull(queryContextFactory, nameof(queryContextFactory));
            Check.NotNull(compiledQueryCache, nameof(compiledQueryCache));
            Check.NotNull(compiledQueryCacheKeyGenerator, nameof(compiledQueryCacheKeyGenerator));
            Check.NotNull(database, nameof(database));
            Check.NotNull(logger, nameof(logger));
            Check.NotNull(currentContext, nameof(currentContext));
            Check.NotNull(evaluableExpressionFilter, nameof(evaluableExpressionFilter));

            _queryModelGenerator = queryModelGenerator;

            _cacheProvider = currentContext.Context.GetService<ICacheProvider>();

            _xxHash = xxHashFactory.Instance.Create(new xxHashConfig()
            {
                HashSizeInBits = 64
            });
        }

        public override IAsyncEnumerable<TResult> ExecuteAsync<TResult>(Expression query)
        {
            var asCachingExpressionVisitor = new AsCachingExpressionVisitor();
            query = asCachingExpressionVisitor.GetExtractCachableParameter(query, out bool asCaching, out CachingOptions options);

            if (!asCaching)
                return base.ExecuteAsync<TResult>(query);

            var cacheKey = GetCacheKey(query);


            if (_cacheProvider.KeyExists(cacheKey))
                return _cacheProvider.Get<IEnumerable<TResult>>(cacheKey).ToAsyncEnumerable();

            var result = base.ExecuteAsync<TResult>(query).ToEnumerable().ToList();

            //Locking procedure goes here
            var resultOfCaching = _cacheProvider.Set(cacheKey, result, options.Expiry);

            return resultOfCaching.ToAsyncEnumerable();
        }

        public override TResult Execute<TResult>(Expression query)
        {
            var asCachingExpressionVisitor = new AsCachingExpressionVisitor();
            query = asCachingExpressionVisitor.GetExtractCachableParameter(query, out bool asCaching, out CachingOptions options);

            if (!asCaching)
                return base.Execute<TResult>(query);


            var cacheKey = GetCacheKey(query);

            if (_cacheProvider.KeyExists(cacheKey))
                return _cacheProvider.Get<TResult>(cacheKey);

            var result = base.Execute<TResult>(query);

            return _cacheProvider.Set(cacheKey, result, options.Expiry);
        }


        public override async Task<TResult> ExecuteAsync<TResult>(Expression query, CancellationToken cancellationToken)
        {
            var asCachingExpressionVisitor = new AsCachingExpressionVisitor();
            query = asCachingExpressionVisitor.GetExtractCachableParameter(query, out bool asCaching, out CachingOptions options);

            if (!asCaching)
                return await base.ExecuteAsync<TResult>(query, cancellationToken);

            var cacheKey = GetCacheKey(query);

            if (await _cacheProvider.KeyExistsAsync(cacheKey))
                return await _cacheProvider.GetAsync<TResult>(cacheKey);

            var result = await base.ExecuteAsync<TResult>(query, cancellationToken);

            return _cacheProvider.Set(cacheKey, result, options.Expiry);
        }

        private string GetCacheKey(Expression query)
        {
            var resultQuery = _queryModelGenerator.ParseQuery(query);
            var hashOfQuery = _xxHash.ComputeHash(Encoding.UTF8.GetBytes(resultQuery.ToString()));
            return hashOfQuery.AsBase64String();
        }
    }
}
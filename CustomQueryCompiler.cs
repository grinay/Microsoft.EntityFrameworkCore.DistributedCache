using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.HashFunction.xxHash;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.DistributedCache.ExpressionVisitors;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.Internal;
using Microsoft.EntityFrameworkCore.Storage;
using Remotion.Linq.Parsing.ExpressionVisitors.TreeEvaluation;

namespace Microsoft.EntityFrameworkCore.DistributedCache
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
            query = asCachingExpressionVisitor.GetExtractAsCachingParameter(query, out bool asCaching, out CachingOptions options);

            if (!asCaching)
                return base.ExecuteAsync<TResult>(query);

            var cacheKey = GetCacheKey(query);

            Task<List<TResult>> valuesTask;

            if (options.DistributedLock)
                valuesTask = _cacheProvider.FetchObjectWithLockAsync(cacheKey, () => base.ExecuteAsync<TResult>(query).ToList(), options.Expiry);
            else
                valuesTask = _cacheProvider.FetchObjectAsync(cacheKey, () => base.ExecuteAsync<TResult>(query).ToList(), options.Expiry);

            //Tricky implementation with custom implementation of AsyncEnumerable with by pass async function inside async iterator, and make code path async all the way down 
            //If execute in current block, we can't await here, and any wait method will blocks thread. 
            return new MyAsyncEnumerable<TResult>(valuesTask);
        }

        public override TResult Execute<TResult>(Expression query)
        {
            var asCachingExpressionVisitor = new AsCachingExpressionVisitor();
            query = asCachingExpressionVisitor.GetExtractAsCachingParameter(query, out bool asCaching, out CachingOptions options);

            if (!asCaching)
                return base.Execute<TResult>(query);

            var cacheKey = GetCacheKey(query);

            if (options.DistributedLock)
                return _cacheProvider.FetchObjectWithLock(cacheKey, () => base.Execute<TResult>(query), options.Expiry);

            return _cacheProvider.FetchObject(cacheKey, () => base.Execute<TResult>(query), options.Expiry);
        }


        public override async Task<TResult> ExecuteAsync<TResult>(Expression query, CancellationToken cancellationToken)
        {
            var asCachingExpressionVisitor = new AsCachingExpressionVisitor();
            query = asCachingExpressionVisitor.GetExtractAsCachingParameter(query, out bool asCaching, out CachingOptions options);

            if (!asCaching)
                return await base.ExecuteAsync<TResult>(query, cancellationToken);

            var cacheKey = GetCacheKey(query);
            if (options.DistributedLock)
                return await _cacheProvider.FetchObjectWithLockAsync(cacheKey, () => base.ExecuteAsync<TResult>(query, cancellationToken), options.Expiry);

            return await _cacheProvider.FetchObjectAsync(cacheKey, () => base.ExecuteAsync<TResult>(query, cancellationToken), options.Expiry);
        }

        private string GetCacheKey(Expression query)
        {
            var resultQuery = _queryModelGenerator.ParseQuery(query);
            var hashOfQuery = _xxHash.ComputeHash(Encoding.UTF8.GetBytes(resultQuery.ToString()));
            return hashOfQuery.AsBase64String();
        }
    }

    public class MyAsyncEnumerable<T> : IAsyncEnumerable<T>
    {
        private readonly Task<List<T>> _task;

        public MyAsyncEnumerable(Task<List<T>> task)
        {
            _task = task;
        }

        public IAsyncEnumerator<T> GetEnumerator()
        {
            return new MyAsyncEnumerator<T>(_task);
        }
    }

    public class MyAsyncEnumerator<T> : IAsyncEnumerator<T>
    {
        private readonly Task<List<T>> _task;
        private ConcurrentStack<T> _valuesCollection;

        public MyAsyncEnumerator(Task<List<T>> task)
        {
            _task = task;
        }

        public void Dispose()
        {
        }

        public async Task<bool> MoveNext(CancellationToken cancellationToken)
        {
            if (_valuesCollection == null)
                _valuesCollection = new ConcurrentStack<T>(await _task);

            if (_valuesCollection == null)
                throw new InvalidOperationException();

            if (!_valuesCollection.TryPop(out var value))
                return false;

            Current = value;
            return true;
        }


        public T Current { get; private set; }
    }
}
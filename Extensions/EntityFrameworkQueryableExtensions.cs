using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.Internal;

namespace EFCore.AsCaching.Extensions
{
    public static class EntityFrameworkQueryableExtensions
    {
        internal static readonly MethodInfo AsCachingMethodInfo
            = typeof(EntityFrameworkQueryableExtensions)
                .GetTypeInfo()
                .GetMethods()
                .Where(m => m.Name == nameof(AsCaching))
                .Single(m => m.GetParameters().Any(p => p.ParameterType == typeof(CachingOptions)));

        /// <summary>
        /// Returns a new query where the result will be cached base on the <see cref="TimeSpan"/> parameter.
        /// </summary>
        /// <typeparam name="T">The type of entity being queried.</typeparam>
        /// <param name="source">The source query.</param>
        /// <param name="timeToLive">Limits the lifetime of cached query results.</param>
        /// <returns>A new query where the result set will be cached.</returns>
        public static IQueryable<T> AsCaching<T>(this IQueryable<T> source, [NotParameterized] TimeSpan timeToLive)
        {
            Check.NotNull(source, nameof(source));
            Check.NotNull(timeToLive, nameof(timeToLive));

            return source.AsCaching<T>(new CachingOptions
            {
                Expiry = timeToLive
            });
        }

        /// <summary>
        /// Returns a new query where the result will be cached base on the <see cref="TimeSpan"/> parameter.
        /// </summary>
        /// <typeparam name="T">The type of entity being queried.</typeparam>
        /// <param name="source">The source query.</param>
        /// <param name="options">Options how to handle cached query results.</param>
        /// <returns>A new query where the result set will be cached.</returns>
        public static IQueryable<T> AsCaching<T>(this IQueryable<T> source, [NotParameterized] CachingOptions options)
        {
            Check.NotNull(source, nameof(source));
            Check.NotNull(options, nameof(options));

            return
                source.Provider is EntityQueryProvider
                    ? source.Provider.CreateQuery<T>(
                        Expression.Call(
                            instance: null,
                            method: AsCachingMethodInfo.MakeGenericMethod(typeof(T)),
                            arg0: source.Expression,
                            arg1: Expression.Constant(options)))
                    : source;
        }
    }
}
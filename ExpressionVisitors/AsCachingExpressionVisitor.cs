﻿using System;
using System.Linq;
using System.Linq.Expressions;

namespace Microsoft.EntityFrameworkCore.DistributedCache.ExpressionVisitors
{
    public class AsCachingExpressionVisitor : ExpressionVisitor
    {
        private Boolean _asCaching = false;
        private CachingOptions _options = null;

        public AsCachingExpressionVisitor()
        {
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (node.Method.IsGenericMethod)
            {
                var genericMethodDefinition = node.Method.GetGenericMethodDefinition();

                if (genericMethodDefinition == Extensions.EntityFrameworkQueryableExtensions.AsCachingMethodInfo)
                {
                    // get parameter with "last one win"
                    _options = node.Arguments
                        .OfType<ConstantExpression>()
                        .Where(a => a.Value is CachingOptions)
                        .Select(a => (CachingOptions) a.Value)
                        .Last();

                    _asCaching = true;

                    // cut out extension expression
                    return Visit(node.Arguments[0]) ?? throw new InvalidOperationException();
                }
            }

            return base.VisitMethodCall(node);
        }

        /// <summary>
        /// Visit the query expression tree and find extract cachable parameter
        /// </summary>
        /// <param name="expression">Query expression</param>
        /// <param name="asCaching">Is expression marked as cacheable</param>
        /// <param name="timeToLive">Timespan befor expiration of cached query result</param>
        /// <returns></returns>
        public virtual Expression GetExtractAsCachingParameter(Expression expression, out Boolean asCaching, out CachingOptions options)
        {
            var visitedExpression = Visit(expression);

            asCaching = _asCaching;
            options = _options;

            return visitedExpression;
        }
    }
}
using System;
using System.Linq;
using System.Linq.Expressions;
using EntityFrameworkQueryableExtensions = EFCore.AsCaching.Extensions.EntityFrameworkQueryableExtensions;

namespace EFCore.AsCaching.ExpressionVisitors
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

                // find cachable query extention calls
                if (genericMethodDefinition == EntityFrameworkQueryableExtensions.AsCachingMethodInfo)
                {
                    // get parameter with "last one win"
                    _options = node.Arguments
                        .OfType<ConstantExpression>()
                        .Where(a => a.Value is CachingOptions)
                        .Select(a => (CachingOptions)a.Value)
                        .Last();

                    _asCaching = true;

                    // cut out extension expression
                    return Visit(node.Arguments[0]);
                }
            }

            return base.VisitMethodCall(node);
        }

        /// <summary>
        /// Visit the query expression tree and find extract cachable parameter
        /// </summary>
        /// <param name="expression">Query expression</param>
        /// <param name="isCacheable">Is expression marked as cacheable</param>
        /// <param name="timeToLive">Timespan befor expiration of cached query result</param>
        /// <returns></returns>
        public virtual Expression GetExtractCachableParameter(Expression expression, out Boolean isCacheable, out CachingOptions options)
        {
            var visitedExpression = Visit(expression);

            isCacheable = _asCaching;
            options = _options;

            return visitedExpression;
        }
    }
}

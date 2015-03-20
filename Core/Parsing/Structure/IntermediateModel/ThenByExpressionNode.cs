// Copyright (c) rubicon IT GmbH, www.rubicon.eu
//
// See the NOTICE file distributed with this work for additional information
// regarding copyright ownership.  rubicon licenses this file to you under 
// the Apache License, Version 2.0 (the "License"); you may not use this 
// file except in compliance with the License.  You may obtain a copy of the 
// License at
//
//   http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software 
// distributed under the License is distributed on an "AS IS" BASIS, WITHOUT 
// WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.  See the 
// License for the specific language governing permissions and limitations
// under the License.
// 
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Remotion.Linq.Clauses;
using Remotion.Utilities;

namespace Remotion.Linq.Parsing.Structure.IntermediateModel
{
  /// <summary>
  /// Represents a <see cref="MethodCallExpression"/> for 
  /// <see cref="Queryable.ThenBy{TSource,TKey}(System.Linq.IOrderedQueryable{TSource},System.Linq.Expressions.Expression{System.Func{TSource,TKey}})"/>.
  /// It is generated by <see cref="ExpressionTreeParser"/> when an <see cref="Expression"/> tree is parsed.
  /// When this node is used, it follows an <see cref="OrderByExpressionNode"/>, an <see cref="OrderByDescendingExpressionNode"/>, 
  /// a <see cref="ThenByExpressionNode"/>, or a <see cref="ThenByDescendingExpressionNode"/>.
  /// </summary>
  public class ThenByExpressionNode : MethodCallExpressionNodeBase
  {
    public static readonly MethodInfo[] SupportedMethods = new[]
                                                           {
                                                               GetSupportedMethod (() => Queryable.ThenBy<object, object> (null, null)),
                                                               GetSupportedMethod (() => Enumerable.ThenBy<object, object> (null, null)),
                                                           };

    private readonly ResolvedExpressionCache<Expression> _cachedSelector;
    private readonly LambdaExpression _keySelector;

    public ThenByExpressionNode (MethodCallExpressionParseInfo parseInfo, LambdaExpression keySelector)
        : base (parseInfo)
    {
      ArgumentUtility.CheckNotNull ("keySelector", keySelector);

      if (keySelector.Parameters.Count != 1)
        throw new ArgumentException ("KeySelector must have exactly one parameter.", "keySelector");

      _keySelector = keySelector;
      _cachedSelector = new ResolvedExpressionCache<Expression> (this);
    }

    public LambdaExpression KeySelector
    {
      get { return _keySelector; }
    }

    public Expression GetResolvedKeySelector (ClauseGenerationContext clauseGenerationContext)
    {
      return _cachedSelector.GetOrCreate (r => r.GetResolvedExpression (_keySelector.Body, _keySelector.Parameters[0], clauseGenerationContext));
    }

    public override Expression Resolve (
        ParameterExpression inputParameter, Expression expressionToBeResolved, ClauseGenerationContext clauseGenerationContext)
    {
      ArgumentUtility.CheckNotNull ("inputParameter", inputParameter);
      ArgumentUtility.CheckNotNull ("expressionToBeResolved", expressionToBeResolved);

      // this simply streams its input data to the output without modifying its structure, so we resolve by passing on the data to the previous node
      return Source.Resolve (inputParameter, expressionToBeResolved, clauseGenerationContext);
    }

    protected override QueryModel ApplyNodeSpecificSemantics (QueryModel queryModel, ClauseGenerationContext clauseGenerationContext)
    {
      ArgumentUtility.CheckNotNull ("queryModel", queryModel);
      var orderByClause = GetOrderByClause (queryModel);

      if (orderByClause == null)
        throw new NotSupportedException ("ThenByDescending expressions must follow OrderBy, OrderByDescending, ThenBy, or ThenByDescending expressions.");

      orderByClause.Orderings.Add (new Ordering (GetResolvedKeySelector (clauseGenerationContext), OrderingDirection.Asc));
      return queryModel;
    }

    private OrderByClause GetOrderByClause (QueryModel queryModel)
    {
      if (queryModel.BodyClauses.Count == 0)
        return null;
      else
        return queryModel.BodyClauses[queryModel.BodyClauses.Count - 1] as OrderByClause;
    }
  }
}

// This file is part of the re-motion Core Framework (www.re-motion.org)
// Copyright (C) 2005-2009 rubicon informationstechnologie gmbh, www.rubicon.eu
// 
// The re-motion Core Framework is free software; you can redistribute it 
// and/or modify it under the terms of the GNU Lesser General Public License 
// version 3.0 as published by the Free Software Foundation.
// 
// re-motion is distributed in the hope that it will be useful, 
// but WITHOUT ANY WARRANTY; without even the implied warranty of 
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the 
// GNU Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public License
// along with re-motion; if not, see http://www.gnu.org/licenses.
// 
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Remotion.Data.Linq.Clauses;
using Remotion.Data.Linq.Clauses.Expressions;
using Remotion.Data.Linq.Parsing.ExpressionTreeVisitors;
using Remotion.Utilities;

namespace Remotion.Data.Linq.Parsing.Structure.IntermediateModel
{
  /// <summary>
  /// Represents the first expression in a LINQ query, which acts as the main query source.
  /// It is generated by <see cref="ExpressionTreeParser"/> when an <see cref="ParsedExpression"/> tree is parsed.
  /// This node usually marks the end (i.e. the first node) of an <see cref="IExpressionNode"/> chain that represents a query.
  /// </summary>
  public class MainSourceExpressionNode : IQuerySourceExpressionNode
  {
    public MainSourceExpressionNode (string associatedIdentifier, Expression expression)
    {
      ArgumentUtility.CheckNotNullOrEmpty ("associatedIdentifier", associatedIdentifier);
      ArgumentUtility.CheckNotNull ("expression", expression);

      QuerySourceType = expression.Type;

      try
      {
        QuerySourceElementType = ParserUtility.GetItemTypeOfIEnumerable (expression.Type);
      }
      catch (ArgumentTypeException)
      {
        throw new ArgumentTypeException ("expression", typeof (IEnumerable<>), expression.Type);
      }

      AssociatedIdentifier = associatedIdentifier;
      ParsedExpression = expression;
    }

    public Type QuerySourceElementType { get; private set; }
    public Type QuerySourceType { get; set; }
    public Expression ParsedExpression { get; private set; }
    public string AssociatedIdentifier { get; set; }

    public IExpressionNode Source
    {
      get { return null; }
    }

    public Expression Resolve (ParameterExpression inputParameter, Expression expressionToBeResolved, ClauseGenerationContext clauseGenerationContext)
    {
      ArgumentUtility.CheckNotNull ("inputParameter", inputParameter);
      ArgumentUtility.CheckNotNull ("expressionToBeResolved", expressionToBeResolved);

      // query sources resolve into references that point back to the respective clauses
      return QuerySourceExpressionNodeUtility.ReplaceParameterWithReference (
          this, 
          inputParameter, 
          expressionToBeResolved, 
          clauseGenerationContext.ClauseMapping);
    }

    public QueryModel Apply (QueryModel queryModel, ClauseGenerationContext clauseGenerationContext)
    {
      throw new NotSupportedException (
          "ConstantExpression nodes cannot be applied to a query model because they constitute the main source of the "
          + "query. Use CreateMainFromClause to create a MainFromClause from this node.");
    }

    public MainFromClause CreateMainFromClause (ClauseGenerationContext clauseGenerationContext)
    {
      var fromClause = new MainFromClause (
          AssociatedIdentifier,
          QuerySourceElementType,
          ParsedExpression);

      clauseGenerationContext.ClauseMapping.AddMapping (this, fromClause);
      return fromClause;
    }
  }
}
using System;
using System.Linq;
using System.Linq.Expressions;
using NUnit.Framework;
using Rubicon.Data.Linq.Clauses;
using Rubicon.Data.Linq.UnitTests.ParsingTest;

namespace Rubicon.Data.Linq.UnitTests.ClausesTest
{
  [TestFixture]
  public class QueryExpressionResolveVisitorTest
  {
    public class AnonymousType
    {
      public Student s1;
      public Student fzlbf;
      public AnonymousType transparent2;
    }

    [Test]
    public void FirstFromIdentifierSpecified()
    {
      QueryExpression queryExpression = CreateQueryExpression();

      Expression sourceExpression = Expression.Parameter (typeof (Student), "s1");
      QueryExpressionResolveVisitor.Result result = new QueryExpressionResolveVisitor (queryExpression).ParseAndReduce(sourceExpression);
      Assert.AreSame (sourceExpression, result.ReducedExpression);
      Assert.AreSame (queryExpression.FromClause, result.FromClause);
    }

    [Test]
    public void SecondFromIdentifierSpecified ()
    {
      QueryExpression queryExpression = CreateQueryExpression ();

      Expression sourceExpression = Expression.Parameter (typeof (Student), "s2");
      QueryExpressionResolveVisitor.Result result = new QueryExpressionResolveVisitor (queryExpression).ParseAndReduce (sourceExpression);
      Assert.AreSame (sourceExpression, result.ReducedExpression);
      Assert.AreSame (queryExpression.QueryBody.BodyClauses.First(), result.FromClause);
    }

    [Test]
    public void MemberAccess_WithFromIdentifier ()
    {
      QueryExpression queryExpression = CreateQueryExpression ();

      Expression sourceExpression = Expression.MakeMemberAccess (
          Expression.Parameter (typeof (Student), "s1"),
          typeof (Student).GetProperty ("First"));
      QueryExpressionResolveVisitor.Result result = new QueryExpressionResolveVisitor (queryExpression).ParseAndReduce (sourceExpression);
      Assert.AreSame (sourceExpression, result.ReducedExpression);
      Assert.AreSame (queryExpression.FromClause, result.FromClause);
    }

    [Test]
    public void TransparentIdentifier_ThenFromIdentifier ()
    {
      QueryExpression queryExpression = CreateQueryExpression ();

      Expression sourceExpression = Expression.MakeMemberAccess (
          Expression.Parameter (typeof (AnonymousType), "transparent1"),
          typeof (AnonymousType).GetField ("s1"));

      QueryExpressionResolveVisitor.Result result = new QueryExpressionResolveVisitor (queryExpression).ParseAndReduce (sourceExpression);
      ParameterExpression expectedReducedExpression = Expression.Parameter (typeof (Student), "s1");
      ExpressionTreeComparer.CheckAreEqualTrees (expectedReducedExpression, result.ReducedExpression);
      Assert.AreSame (queryExpression.FromClause, result.FromClause);
    }

    [Test]
    public void TransparentIdentifiers_ThenFromIdentifier ()
    {
      QueryExpression queryExpression = CreateQueryExpression ();

      Expression sourceExpression = Expression.MakeMemberAccess (
          Expression.MakeMemberAccess (
              Expression.Parameter (typeof (AnonymousType), "transparent1"),
              typeof (AnonymousType).GetField ("transparent2")),
          typeof (AnonymousType).GetField ("s1"));
      
      QueryExpressionResolveVisitor.Result result = new QueryExpressionResolveVisitor (queryExpression).ParseAndReduce (sourceExpression);
      ParameterExpression expectedReducedExpression = Expression.Parameter (typeof (Student), "s1");
      ExpressionTreeComparer.CheckAreEqualTrees (expectedReducedExpression, result.ReducedExpression);
      Assert.AreSame (queryExpression.FromClause, result.FromClause);
    }

    [Test]
    [ExpectedException (typeof (FieldAccessResolveException), ExpectedMessage = "The field access expression 'transparent1.transparent2.fzlbf' does "
        + "not contain a from clause identifier.")]
    public void NoFromIdentifierFound ()
    {
      QueryExpression queryExpression = CreateQueryExpression ();

      Expression sourceExpression = Expression.MakeMemberAccess (
          Expression.MakeMemberAccess (
              Expression.Parameter (typeof (AnonymousType), "transparent1"),
              typeof (AnonymousType).GetField ("transparent2")),
          typeof (AnonymousType).GetField ("fzlbf"));

      new QueryExpressionResolveVisitor (queryExpression).ParseAndReduce (sourceExpression);
    }

    private QueryExpression CreateQueryExpression ()
    {
      ParameterExpression s1 = Expression.Parameter (typeof (Student), "s1");
      ParameterExpression s2 = Expression.Parameter (typeof (Student), "s2");
      MainFromClause mainFromClause = new MainFromClause (s1, ExpressionHelper.CreateQuerySource ());
      AdditionalFromClause additionalFromClause =
          new AdditionalFromClause (mainFromClause, s2, ExpressionHelper.CreateLambdaExpression (), ExpressionHelper.CreateLambdaExpression ());

      QueryBody queryBody = new QueryBody (ExpressionHelper.CreateSelectClause ());
      queryBody.Add (additionalFromClause);

      return new QueryExpression (mainFromClause, queryBody);
    }
  }
}
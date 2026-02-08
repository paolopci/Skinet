using Core.Interfaces;
using System.Linq.Expressions;

namespace Core.Specification;

public class BaseSpecification<T>(Expression<Func<T, bool>>? criteria) : ISpecification<T>
{
    protected BaseSpecification() : this(null)
    {
    }
    // Criteri base della specifica.
    public Expression<Func<T, bool>>? Criteria => criteria;
    // Indica se la query deve essere distinta.
    public bool IsDistinct { get; private set; }
    // Ordinamento ascendente opzionale.
    public Expression<Func<T, object>>? OrderBy { get; private set; }
    // Ordinamento discendente opzionale.
    public Expression<Func<T, object>>? OrderByDescending { get; private set; }
    public List<Expression<Func<T, object>>> Includes { get; } = [];
    public List<string> IncludeStrings { get; } = [];

    public int Take { get; private set; }

    public int Skip { get; private set; }

    public bool IsPagingEnabled { get; private set; }

    // Imposta l'ordinamento ascendente.

    protected void AddOrderBy(Expression<Func<T, object>> orderBy)
    {
        OrderBy = orderBy;
    }

    // Imposta l'ordinamento discendente.
    protected void AddOrderByDescending(Expression<Func<T, object>> orderByDescending)
    {
        OrderByDescending = orderByDescending;
    }

    protected void AddInclude(Expression<Func<T, object>> includeExpression)
    {
        Includes.Add(includeExpression);
    }

    protected void AddInclude(string includeString)
    {
        IncludeStrings.Add(includeString);
    }

    // Applica paginazione alla query.
    protected void ApplyPaging(int skip, int take)
    {
        Skip = skip;
        Take = take;
        IsPagingEnabled = true;
    }

    // Imposta la query come distinct.
    protected void ApplyDistinct()
    {
        IsDistinct = true;
    }
}

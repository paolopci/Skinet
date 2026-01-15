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
    // Ordinamento ascendente opzionale.
    public Expression<Func<T, object>>? OrderBy { get; private set; }
    // Ordinamento discendente opzionale.
    public Expression<Func<T, object>>? OrderByDescending { get; private set; }

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
}

using System;
using System.Linq.Expressions;
using Core.Interfaces;

namespace Core.Specification;

public class BaseSpecification<T> : ISpecification<T>
{
    private readonly Expression<Func<T, bool>> _criteria;

    public BaseSpecification(Expression<Func<T, bool>> criteria)
    {
        this._criteria = criteria;
    }

    public Expression<Func<T, bool>> Criteria => _criteria;
}

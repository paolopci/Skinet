using System.Linq.Expressions;

namespace Core.Interfaces;

public interface ISpecification<T>
{
    // Filtro principale della specifica (criteri di selezione).
    Expression<Func<T, bool>>? Criteria { get; }
    // Indica se la query deve essere distinta.
    bool IsDistinct { get; }
    // Ordinamento ascendente opzionale.
    Expression<Func<T, object>>? OrderBy { get; }
    // Ordinamento discendente opzionale.
    Expression<Func<T, object>>? OrderByDescending { get; }

    int Take { get; }
    int Skip { get; }
    bool IsPagingEnabled { get; }
}


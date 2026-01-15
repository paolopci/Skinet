using System;
using Core.Entities;
using Core.Interfaces;

namespace Infrastructure.Data;

public class SpecificationEvaluator<T> where T : BaseEntity
{
    public static IQueryable<T> GetQuery(IQueryable<T> query, ISpecification<T> spec)
    {
        // Applica i criteri di filtro, se presenti.
        if (spec.Criteria != null)
        {
            query = query.Where(spec.Criteria); // x=>X.Brand==brand
        }

        // Applica l'ordinamento, se definito nella specifica.
        if (spec.OrderBy != null)
        {
            query = query.OrderBy(spec.OrderBy);
        }
        else if (spec.OrderByDescending != null)
        {
            query = query.OrderByDescending(spec.OrderByDescending);
        }

        return query;
    }
}

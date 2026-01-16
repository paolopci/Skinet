using Core.Entities;

namespace Core.Specification;

public class ProductCountSpecification : BaseSpecification<Product>
{
    public ProductCountSpecification(ProductSpecParams specParams) : base(x =>
        (!specParams.Brands.Any() || specParams.Brands.Contains(x.Brand)) &&
        (!specParams.Types.Any() || specParams.Types.Contains(x.Type)))
    {
        // Spec per conteggio: niente paging, solo filtri.
    }
}

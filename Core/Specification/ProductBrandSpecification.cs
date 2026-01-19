using Core.Entities;

namespace Core.Specification;

public class ProductBrandSpecification : BaseSpecification<Product>
{
    public ProductBrandSpecification() : base(x => !string.IsNullOrEmpty(x.Brand))
    {
        ApplyDistinct();
        AddOrderBy(x => x.Brand!);
    }
}

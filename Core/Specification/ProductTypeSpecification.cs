using Core.Entities;

namespace Core.Specification;

public class ProductTypeSpecification : BaseSpecification<Product>
{
    public ProductTypeSpecification() : base(x => !string.IsNullOrEmpty(x.Type))
    {
        ApplyDistinct();
        AddOrderBy(x => x.Type!);
    }
}

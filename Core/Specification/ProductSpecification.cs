using Core.Entities;

namespace Core.Specification;

public class ProductSpecification : BaseSpecification<Product>
{
    public ProductSpecification(ProductSpecParams specParams) : base(x =>
        (!specParams.Brands.Any() || specParams.Brands.Contains(x.Brand)) &&
        (!specParams.Types.Any() || specParams.Types.Contains(x.Type)))
    {
        ApplyPaging((specParams.PageIndex - 1) * specParams.PageSize, specParams.PageSize);

        switch (specParams.Sort)
        {
            case "priceAsc":
                AddOrderBy(x => x.Price);
                break;
            case "priceDesc":
                AddOrderByDescending(x => x.Price);
                break;
            default:
                AddOrderBy(x => x.Name);
                break;
        }
    }
}

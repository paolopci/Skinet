using Core.Entities;
using Core.Interfaces;
using Microsoft.EntityFrameworkCore;
namespace Infrastructure.Data
{
    public class ProductRepository(StoreContext context) : IProductRepository
    {

        public async Task<IReadOnlyList<Product>> GetProductsAsync(
            string? brand,
            string? type,
            ProductSort? sort,
            SortDirection? direction)
        {
            IQueryable<Product> query = context.Products;

            if (!string.IsNullOrWhiteSpace(brand))
            {
                query = query.Where(x => x.Brand == brand);
            }

            if (!string.IsNullOrWhiteSpace(type))
            {
                query = query.Where(x => x.Type == type);
            }

            if (sort == ProductSort.Name)
            {
                query = direction == SortDirection.Desc
                    ? query.OrderByDescending(x => x.Name)
                    : query.OrderBy(x => x.Name);
            }
            else if (sort == ProductSort.Price)
            {
                query = direction == SortDirection.Desc
                    ? query.OrderByDescending(x => x.Price)
                    : query.OrderBy(x => x.Price);
            }
            else
            {
                query = query.OrderBy(x => x.Id);
            }

            return await query.ToListAsync();
        }

        public async Task<Product?> GetProductByIdAsync(int id)
        {
            return await context.Products.FindAsync(id);
        }

        public async Task<IReadOnlyList<string>> GetBrandsAsync()
        {
            return await context.Products
                .Where(x => !string.IsNullOrWhiteSpace(x.Brand))
                .Select(x => x.Brand)
                .Distinct()
                .OrderBy(x => x)
                .ToListAsync();
        }

        public async Task<IReadOnlyList<string>> GetTypesAsync()
        {
            return await context.Products
                .Where(x => !string.IsNullOrWhiteSpace(x.Type))
                .Select(x => x.Type)
                .Distinct()
                .OrderBy(x => x)
                .ToListAsync();
        }

        public void AddProduct(Product product)
        {
            if (product == null)
            {
                throw new ArgumentNullException(nameof(product));
            }

            context.Products.Add(product);
        }

        public void UpdateProduct(Product product)
        {
            context.Entry(product).State = EntityState.Modified;
        }

        public void DeleteProduct(Product product)
        {
            context.Products.Remove(product);
        }

        public bool ProductExists(int id)
        {
            return context.Products.Any(x => x.Id == id);
        }

        public async Task<bool> SaveChangesAsync()
        {
            return await context.SaveChangesAsync() > 0;
        }
    }
}

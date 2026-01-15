using Core.Entities;
using Core.Interfaces;
using Core.Specification;
using Microsoft.AspNetCore.Mvc;
using System.Linq.Expressions;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController(IGenericRepository<Product> repository) : ControllerBase
    {
        [HttpGet] // api/products?brand={brand}&type={type}&sort={Name|Price}&direction={Asc|Desc}
        public async Task<ActionResult<IReadOnlyList<Product>>> GetProducts(
            [FromQuery] string? brand,
            [FromQuery] string? type,
            [FromQuery] ProductSort? sort,
            [FromQuery] SortDirection? direction)
        {
            var spec = new ProductSpecification(brand, type);
            Expression<Func<Product, object>> orderBy = sort switch
            {
                ProductSort.Name => x => x.Name,
                ProductSort.Price => x => x.Price,
                _ => x => x.Id
            };

            var products = await repository.ListAsync(spec, orderBy, direction);

            return Ok(products);

        }

        [HttpGet("brands")] // api/products/brands
        public async Task<ActionResult<IReadOnlyList<string>>> GetBrands()
        {
            // todo: implement method
            return Ok();
        }

        [HttpGet("types")] // api/products/types
        public async Task<ActionResult<IReadOnlyList<string>>> GetTypes()
        {
            // todo: implement method
            return Ok();
        }

        [HttpGet("{id:int}")] // api/products/5
        public async Task<ActionResult<Product>> GetProduct(int id)
        {
            var product = await repository.GetByIdAsync(id);
            if (product == null)
            {
                return NotFound();
            }
            return Ok(product);
        }

        [HttpPost] // api/products
        public async Task<ActionResult<Product>> CreateProduct([FromBody] Product model)
        {
            if (model == null)
            {
                return BadRequest();
            }

            var newProduct = new Product
            {
                Name = model.Name,
                Price = model.Price,
                Description = model.Description,
                PictureUrl = model.PictureUrl,
                Type = model.Type,
                Brand = model.Brand,
                QuantityInStock = model.QuantityInStock
            };
            repository.Add(newProduct);
            await repository.SaveAllAsync();
            return CreatedAtAction(nameof(GetProduct), new { id = newProduct.Id }, newProduct);
        }

        [HttpPut("{id:int}")] // api/products/5
        public async Task<ActionResult<Product>> UpdateProduct(int id, [FromBody] Product model)
        {
            if (model == null)
            {
                return BadRequest();
            }

            if (!ProductExists(id))
            {
                return NotFound();
            }

            var product = await repository.GetByIdAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            product.Name = model.Name;
            product.Price = model.Price;
            product.Description = model.Description;
            product.PictureUrl = model.PictureUrl;
            product.Type = model.Type;
            product.Brand = model.Brand;
            product.QuantityInStock = model.QuantityInStock;

            await repository.SaveAllAsync();
            return Ok(product);
        }

        [HttpDelete("{id:int}")] // api/products/5
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var product = await repository.GetByIdAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            repository.Remove(product);
            await repository.SaveAllAsync();
            return NoContent();
        }

        private bool ProductExists(int id)
        {
            return repository.Exists(id);
        }
    }
}

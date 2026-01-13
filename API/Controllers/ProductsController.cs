using Core.Entities;
using Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController(IProductRepository repository) : ControllerBase
    {
        [HttpGet] // api/products
        public async Task<ActionResult<IReadOnlyList<Product>>> GetProducts()
        {
            return Ok(await repository.GetProductsAsync());
        }

        [HttpGet("{id:int}")] // api/product/5
        public async Task<ActionResult<Product>> GetProduct(int id)
        {
            var product = await repository.GetProductByIdAsync(id);
            if (product == null)
            {
                return NotFound();
            }
            return Ok(product);
        }

        [HttpPost] // api/product
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
            repository.AddProduct(newProduct);
            await repository.SaveChangesAsync();
            return CreatedAtAction(nameof(GetProduct), new { id = newProduct.Id }, newProduct);
        }

        [HttpPut("{id:int}")] // api/product/5
        public async Task<ActionResult<Product>> UpdateProduct(int id, [FromBody] Product model)
        {
            if (model == null)
            {
                return BadRequest();
            }

            var product = await repository.GetProductByIdAsync(id);
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

            await repository.SaveChangesAsync();
            return Ok(product);
        }

        [HttpDelete("{id:int}")] // api/product/5
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var product = await repository.GetProductByIdAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            repository.DeleteProduct(product);
            await repository.SaveChangesAsync();
            return NoContent();
        }
    }
}

using Core.Entities;
using Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly StoreContext context;
        public ProductsController(StoreContext context)
        {
            this.context = context;
        }

        [HttpGet] // api/products
        public async Task<ActionResult<IEnumerable<Product>>> GetProducts()
        {
            return await context.Products.ToListAsync();
        }

        [HttpGet("{id:int}")] // api/product/5
        public async Task<ActionResult<Product>> GetProduct(int id)
        {
            var product = await context.Products.FindAsync(id);
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
            context.Products.Add(newProduct);
            await context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetProduct), new { id = newProduct.Id }, newProduct);
        }

        [HttpPut("{id:int}")] // api/product/5
        public async Task<ActionResult<Product>> UpdateProduct(int id, [FromBody] Product model)
        {
            if (model == null || id != model.Id)
            {
                return BadRequest();
            }

            var product = await context.Products.FindAsync(id);
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

            await context.SaveChangesAsync();
            return Ok(product);
        }

        [HttpDelete("{id:int}")] // api/product/5
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var product = await context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            context.Products.Remove(product);
            await context.SaveChangesAsync();
            return NoContent();
        }
    }
}

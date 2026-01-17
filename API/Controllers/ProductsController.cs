using Core.Entities;
using Core.Interfaces;
using Core.Specification;
using API.RequestHelpers;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{

    public class ProductsController(IGenericRepository<Product> repository) : BaseApiController
    {
        [HttpGet] // api/products?brands=brand1,brand2&types=type1,type2&sort={priceAsc|priceDesc}&pageIndex=1&pageSize=10
        public async Task<ActionResult<Pagination<Product>>> GetProducts(
            [FromQuery] ProductSpecParams specParams)
        {
            var spec = new ProductSpecification(specParams);
            // Spec per conteggio totale senza paging.
            var countSpec = new ProductCountSpecification(specParams);

            return await CreatePageResult(repository, spec, specParams.PageIndex, specParams.PageSize);
        }

        [HttpGet("brands")] // api/products/brands
        public async Task<ActionResult<IReadOnlyList<string>>> GetBrands()
        {
            var spec = new ProductBrandSpecification();
            var products = await repository.ListAsync(spec);
            var brands = products
                .Select(x => x.Brand!)
                .Distinct()
                .ToList();

            return Ok(brands);
        }

        [HttpGet("types")] // api/products/types
        public async Task<ActionResult<IReadOnlyList<string>>> GetTypes()
        {
            var spec = new ProductTypeSpecification();
            var products = await repository.ListAsync(spec);
            var types = products
                .Select(x => x.Type!)
                .Distinct()
                .ToList();

            return Ok(types);
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




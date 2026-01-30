using Core.Entities;
using Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{

    public class CartController(ICartService cartService) : BaseApiController
    {
        [HttpGet]
        public async Task<ActionResult<ShoppingCart>> GetCartById(string id)
        {
            var cart = await cartService.GetCartAsync(id);

            return Ok(cart ?? new ShoppingCart { Id = id });
        }

        [HttpPost]
        public async Task<ActionResult<ShoppingCart>> UpdateCart(ShoppingCart cart)
        {
            var updateCart = await cartService.SetCartAsync(cart);
            if (updateCart == null)
            {
                return BadRequest("Problem with cart.");
            }
            return Ok(updateCart);

        }

        [HttpDelete]
        public async Task<ActionResult> DeleteCart(string id)
        {
            var deleted = await cartService.DeleteCartAsync(id);
            if (!deleted)
            {
                return BadRequest("Problem with cart.");
            }
            return NoContent();
        }
    }
}

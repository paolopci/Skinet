using API.Errors;
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
                return BadRequest(new ApiErrorResponse(
                    StatusCodes.Status400BadRequest,
                    "Problema con il carrello",
                    null));
            }
            return Ok(updateCart);

        }

        [HttpDelete]
        public async Task<ActionResult> DeleteCart(string id)
        {
            var deleted = await cartService.DeleteCartAsync(id);
            if (!deleted)
            {
                return BadRequest(new ApiErrorResponse(
                    StatusCodes.Status400BadRequest,
                    "Problema con il carrello",
                    null));
            }
            return NoContent();
        }
    }
}

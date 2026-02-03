using API.DTOs;
using API.Errors;
using Core.Entities;
using Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{

    public class CartController(
        ICartService cartService,
        UserManager<AppUser> userManager) : BaseApiController
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

        [Authorize]
        [HttpPost("merge")]
        public async Task<ActionResult<ShoppingCart>> MergeCart([FromBody] MergeCartDto mergeDto)
        {
            if (string.IsNullOrWhiteSpace(mergeDto.GuestCartId))
            {
                return BadRequest(new ApiErrorResponse(
                    StatusCodes.Status400BadRequest,
                    "Guest cart id mancante",
                    null));
            }

            var user = await userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized();
            }

            var userCartId = $"user:{user.Id}";
            var guestCart = await cartService.GetCartAsync(mergeDto.GuestCartId);
            var userCart = await cartService.GetCartAsync(userCartId) ?? new ShoppingCart { Id = userCartId };

            if (guestCart == null)
            {
                return Ok(userCart);
            }

            var mergedItems = userCart.Items
                .GroupBy(item => item.ProductId)
                .ToDictionary(group => group.Key, group => group.First());

            foreach (var guestItem in guestCart.Items)
            {
                if (mergedItems.TryGetValue(guestItem.ProductId, out var existing))
                {
                    existing.Quantity += guestItem.Quantity;
                }
                else
                {
                    mergedItems[guestItem.ProductId] = guestItem;
                }
            }

            userCart.Items = mergedItems.Values.ToList();

            var savedCart = await cartService.SetCartAsync(userCart);
            if (savedCart == null)
            {
                return BadRequest(new ApiErrorResponse(
                    StatusCodes.Status400BadRequest,
                    "Problema nel merge del carrello",
                    null));
            }

            await cartService.DeleteCartAsync(mergeDto.GuestCartId);

            return Ok(savedCart);
        }
    }
}

using Core.Entities;
using Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{

    public class PaymentsController(IPaymentService paymentService,
                                    IGenericRepository<DeliveryMethod> dmRepo) : BaseApiController
    {
        [Authorize]
        [HttpPost("{cartId}")]
        public async Task<ActionResult<ShoppingCart>> CreateOrUpdatePaymentIntent(string cartId)
        {
            var cart = await paymentService.CreateUpdatePaymentIntent(cartId);

            if (cart == null)
            {
                return BadRequest("problem with your cart");
            }
            return Ok(cart);
        }

        [HttpGet("delivery-methods")]
        public async Task<ActionResult<IReadOnlyList<DeliveryMethod>>> GetDeliveryMethod()
        {
            return Ok(await dmRepo.ListAllAsync());
        }
    }
}

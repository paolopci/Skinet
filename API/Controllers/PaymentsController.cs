using Core.Entities;
using Core.Interfaces;
using Core.Payments;
using API.Errors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers
{

    public class PaymentsController(IPaymentService paymentService,
                                    IGenericRepository<DeliveryMethod> dmRepo) : BaseApiController
    {
        [Authorize]
        [HttpPost("{cartId}")]
        public async Task<ActionResult<ShoppingCart>> CreateOrUpdatePaymentIntent(string cartId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var result = await paymentService.CreateUpdatePaymentIntent(cartId, userId);

            if (!result.IsSuccess)
            {
                return result.Error switch
                {
                    PaymentIntentOperationError.CartNotFound => NotFound(
                        new ApiErrorResponse(StatusCodes.Status404NotFound, "Carrello non trovato", result.Message)),
                    PaymentIntentOperationError.InvalidCartId => BadRequest(
                        new ApiErrorResponse(StatusCodes.Status400BadRequest, "Richiesta non valida", result.Message)),
                    PaymentIntentOperationError.CartEmpty => BadRequest(
                        new ApiErrorResponse(StatusCodes.Status400BadRequest, "Carrello vuoto", result.Message)),
                    PaymentIntentOperationError.DeliveryMethodNotFound => BadRequest(
                        new ApiErrorResponse(StatusCodes.Status400BadRequest, "Metodo di spedizione non valido", result.Message)),
                    PaymentIntentOperationError.ProductNotFound => BadRequest(
                        new ApiErrorResponse(StatusCodes.Status400BadRequest, "Prodotto non disponibile", result.Message)),
                    PaymentIntentOperationError.PaymentProviderError => StatusCode(
                        StatusCodes.Status502BadGateway,
                        new ApiErrorResponse(StatusCodes.Status502BadGateway, "Errore provider pagamento", result.Message)),
                    _ => StatusCode(
                        StatusCodes.Status500InternalServerError,
                        new ApiErrorResponse(StatusCodes.Status500InternalServerError, "Errore interno", result.Message))
                };
            }

            return Ok(result.Cart);
        }

        [HttpGet("delivery-methods")]
        public async Task<ActionResult<IReadOnlyList<DeliveryMethod>>> GetDeliveryMethod()
        {
            return Ok(await dmRepo.ListAllAsync());
        }
    }
}

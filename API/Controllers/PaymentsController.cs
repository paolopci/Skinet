using API.DTOs;
using API.Errors;
using Core.Entities;
using Core.Interfaces;
using Core.Payments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using System.Text;

namespace API.Controllers
{

    public class PaymentsController(IPaymentService paymentService,
                                    IGenericRepository<DeliveryMethod> dmRepo,
                                    ILogger<PaymentsController> logger) : BaseApiController
    {
        [Authorize]
        [HttpPost("{cartId}")]
        public async Task<ActionResult<ShoppingCart>> CreateOrUpdatePaymentIntent(string cartId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            using var scope = logger.BeginScope(new Dictionary<string, object?>
            {
                ["TraceId"] = HttpContext.TraceIdentifier,
                ["CartId"] = cartId,
                ["UserId"] = userId
            });

            logger.LogInformation("Richiesta create/update PaymentIntent ricevuta");
            var result = await paymentService.CreateUpdatePaymentIntent(cartId, userId);

            if (!result.IsSuccess)
            {
                logger.LogWarning(
                    "Create/update PaymentIntent fallita con errore {ErrorCode}: {ErrorMessage}",
                    result.Error,
                    result.Message);
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

            logger.LogInformation("Create/update PaymentIntent completata con successo");
            return Ok(result.Cart);
        }

        [Authorize]
        [HttpPost("{cartId}/finalize")]
        public async Task<ActionResult<FinalizePaymentResponse>> FinalizePayment(
            string cartId,
            [FromBody] FinalizePaymentRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            using var scope = logger.BeginScope(new Dictionary<string, object?>
            {
                ["TraceId"] = HttpContext.TraceIdentifier,
                ["CartId"] = cartId,
                ["UserId"] = userId,
                ["PaymentIntentId"] = request.PaymentIntentId
            });

            logger.LogInformation("Richiesta finalize payment ricevuta");
            var result = await paymentService.FinalizePaymentAsync(cartId, userId, request.PaymentIntentId);

            if (!result.IsSuccess)
            {
                logger.LogWarning(
                    "Finalize payment fallita con errore {ErrorCode}: {ErrorMessage}",
                    result.Error,
                    result.Message);
                return result.Error switch
                {
                    FinalizePaymentError.InvalidCartId => BadRequest(
                        new ApiErrorResponse(StatusCodes.Status400BadRequest, "Richiesta non valida", result.Message)),
                    FinalizePaymentError.CartNotFound => NotFound(
                        new ApiErrorResponse(StatusCodes.Status404NotFound, "Carrello non trovato", result.Message)),
                    FinalizePaymentError.PaymentIntentMissing => BadRequest(
                        new ApiErrorResponse(StatusCodes.Status400BadRequest, "PaymentIntent mancante", result.Message)),
                    FinalizePaymentError.PaymentIntentMismatch => Conflict(
                        new ApiErrorResponse(StatusCodes.Status409Conflict, "PaymentIntent non coerente", result.Message)),
                    FinalizePaymentError.Forbidden => StatusCode(
                        StatusCodes.Status403Forbidden,
                        new ApiErrorResponse(StatusCodes.Status403Forbidden, "Operazione non autorizzata", result.Message)),
                    FinalizePaymentError.PaymentNotCompleted => StatusCode(
                        StatusCodes.Status409Conflict,
                        new ApiErrorResponse(StatusCodes.Status409Conflict, "Pagamento non completato", result.Message)),
                    FinalizePaymentError.PaymentFailed => BadRequest(
                        new ApiErrorResponse(StatusCodes.Status400BadRequest, "Pagamento fallito", result.Message)),
                    FinalizePaymentError.PaymentProviderError => StatusCode(
                        StatusCodes.Status502BadGateway,
                        new ApiErrorResponse(StatusCodes.Status502BadGateway, "Errore provider pagamento", result.Message)),
                    _ => StatusCode(
                        StatusCodes.Status500InternalServerError,
                        new ApiErrorResponse(StatusCodes.Status500InternalServerError, "Errore interno", result.Message))
                };
            }

            logger.LogInformation("Finalize payment completata con successo: orderId={OrderId}", result.OrderId);
            return Ok(new FinalizePaymentResponse
            {
                IsSuccess = true,
                Status = result.Status,
                OrderId = result.OrderId,
                PaymentIntentId = result.PaymentIntentId,
                Message = result.Message
            });
        }

        [AllowAnonymous]
        [HttpPost("webhook")]
        public async Task<IActionResult> HandleStripeWebhook()
        {
            Request.EnableBuffering();

            string payload;
            using (var reader = new StreamReader(Request.Body, Encoding.UTF8, leaveOpen: true))
            {
                payload = await reader.ReadToEndAsync();
                Request.Body.Position = 0;
            }

            var signatureHeader = Request.Headers["Stripe-Signature"].ToString();
            using var scope = logger.BeginScope(new Dictionary<string, object?>
            {
                ["TraceId"] = HttpContext.TraceIdentifier
            });
            logger.LogInformation("Webhook Stripe ricevuto");
            var result = await paymentService.ProcessWebhookAsync(payload, signatureHeader);

            if (!result.IsSuccess)
            {
                logger.LogWarning("Webhook Stripe non processato: {ErrorCode} - {Message}", result.Error, result.Message);
                return result.Error switch
                {
                    WebhookProcessError.MissingWebhookSecret => StatusCode(
                        StatusCodes.Status500InternalServerError,
                        new ApiErrorResponse(StatusCodes.Status500InternalServerError, "Configurazione webhook non valida", result.Message)),
                    WebhookProcessError.MissingSignature => BadRequest(
                        new ApiErrorResponse(StatusCodes.Status400BadRequest, "Firma webhook mancante", result.Message)),
                    WebhookProcessError.InvalidSignature => BadRequest(
                        new ApiErrorResponse(StatusCodes.Status400BadRequest, "Firma webhook non valida", result.Message)),
                    WebhookProcessError.InvalidPayload => BadRequest(
                        new ApiErrorResponse(StatusCodes.Status400BadRequest, "Payload webhook non valido", result.Message)),
                    _ => BadRequest(
                        new ApiErrorResponse(StatusCodes.Status400BadRequest, "Webhook non processato", result.Message))
                };
            }

            logger.LogInformation("Webhook Stripe processato con successo: {Message}", result.Message);
            return Ok(new { received = true, message = result.Message });
        }

        [HttpGet("delivery-methods")]
        public async Task<ActionResult<IReadOnlyList<DeliveryMethod>>> GetDeliveryMethod()
        {
            return Ok(await dmRepo.ListAllAsync());
        }
    }
}

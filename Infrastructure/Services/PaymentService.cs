using Core.Entities;
using Core.Interfaces;
using Core.Payments;
using Infrastructure.Data;
using Infrastructure.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;
using Stripe;

namespace Infrastructure.Services;

/// <summary>
/// Gestisce la creazione e l'aggiornamento dei Payment Intent Stripe
/// in base al contenuto corrente del carrello.
/// </summary>
public class PaymentService(IOptions<StripeSettings> stripeSettingsOptions,
                            ICartService cartService,
                            IGenericRepository<Core.Entities.Product> productRepo,
                            IGenericRepository<DeliveryMethod> dmRepo,
                            StoreContext dbContext) : IPaymentService
{
    /// <summary>
    /// Crea un nuovo Payment Intent o aggiorna quello esistente associato al carrello.
    /// Prima del calcolo dell'importo, sincronizza i prezzi degli articoli con il catalogo
    /// e applica l'eventuale costo di spedizione selezionato.
    /// </summary>
    /// <param name="cartId">Identificativo del carrello.</param>
    /// <returns>
    /// Esito dell'operazione con carrello aggiornato oppure errore semantico.
    /// </returns>
    public async Task<PaymentIntentOperationResult> CreateUpdatePaymentIntent(string cartId, string? userId)
    {
        if (string.IsNullOrWhiteSpace(cartId))
        {
            return PaymentIntentOperationResult.Failure(
                PaymentIntentOperationError.InvalidCartId,
                "Identificativo carrello non valido.");
        }

        var stripeSettings = stripeSettingsOptions.Value;

        // Inizializza la chiave segreta Stripe per la chiamata API corrente.
        StripeConfiguration.ApiKey = stripeSettings.SecretKey;

        var cart = await cartService.GetCartAsync(cartId);

        if (cart == null)
        {
            return PaymentIntentOperationResult.Failure(
                PaymentIntentOperationError.CartNotFound,
                $"Carrello '{cartId}' non trovato.");
        }

        if (cart.Items.Count == 0)
        {
            return PaymentIntentOperationResult.Failure(
                PaymentIntentOperationError.CartEmpty,
                "Il carrello non contiene articoli.");
        }

        var shippingPrice = 0m;

        if (cart.DeliverMethodId.HasValue)
        {
            var deliveryMethod = await dmRepo.GetByIdAsync((int)cart.DeliverMethodId);
            if (deliveryMethod == null)
            {
                return PaymentIntentOperationResult.Failure(
                    PaymentIntentOperationError.DeliveryMethodNotFound,
                    $"Metodo di spedizione '{cart.DeliverMethodId}' non trovato.");
            }

            shippingPrice = deliveryMethod.Price;
        }

        foreach (var item in cart.Items)
        {
            var productItem = await productRepo.GetByIdAsync(item.ProductId);

            if (productItem == null)
            {
                return PaymentIntentOperationResult.Failure(
                    PaymentIntentOperationError.ProductNotFound,
                    $"Prodotto '{item.ProductId}' non trovato.");
            }

            // Forza l'allineamento al prezzo di catalogo per evitare importi obsoleti o alterati lato client.
            if (item.Price != productItem.Price)
            {
                item.Price = productItem.Price;

            }
        }

        var cartHash = ComputeCartHash(cart, shippingPrice, stripeSettings.Currency);
        var metadata = BuildMetadata(cart, userId, cartHash);
        var totalAmountInMinorUnits = CalculateCartAmountInMinorUnits(cart, shippingPrice);

        var service = new PaymentIntentService();
        PaymentIntent? intent = null;

        try
        {
            if (string.IsNullOrEmpty(cart.PaymentIntentId))
            {
                var options = new PaymentIntentCreateOptions
                {
                    Amount = totalAmountInMinorUnits,
                    Currency = stripeSettings.Currency,
                    PaymentMethodTypes = ["card"],
                    Metadata = metadata
                };

                intent = await service.CreateAsync(options, new RequestOptions
                {
                    IdempotencyKey = $"pi:create:{SanitizeForIdempotency(cart.Id)}:{cartHash}"
                });
                cart.PaymentIntentId = intent.Id;
                cart.ClientSecret = intent.ClientSecret;
            }
            else
            {
                var options = new PaymentIntentUpdateOptions
                {
                    Amount = totalAmountInMinorUnits,
                    Metadata = metadata
                };
                intent = await service.UpdateAsync(cart.PaymentIntentId, options, new RequestOptions
                {
                    IdempotencyKey = $"pi:update:{SanitizeForIdempotency(cart.PaymentIntentId)}:{cartHash}"
                });

                if (!string.IsNullOrWhiteSpace(intent.ClientSecret))
                {
                    cart.ClientSecret = intent.ClientSecret;
                }
            }
        }
        catch (StripeException ex)
        {
            var message = ex.StripeError?.Message ?? ex.Message;
            return PaymentIntentOperationResult.Failure(
                PaymentIntentOperationError.PaymentProviderError,
                $"Errore Stripe durante aggiornamento PaymentIntent: {message}");
        }

        await cartService.SetCartAsync(cart);
        return PaymentIntentOperationResult.Success(cart);
    }

    public async Task<FinalizePaymentResult> FinalizePaymentAsync(string cartId, string? userId, string? paymentIntentId)
    {
        if (string.IsNullOrWhiteSpace(cartId))
        {
            return FinalizePaymentResult.Failure(
                FinalizePaymentError.InvalidCartId,
                "invalid_cart_id",
                "Identificativo carrello non valido.");
        }

        var cart = await cartService.GetCartAsync(cartId);
        if (cart == null)
        {
            return FinalizePaymentResult.Failure(
                FinalizePaymentError.CartNotFound,
                "cart_not_found",
                $"Carrello '{cartId}' non trovato.");
        }

        if (string.IsNullOrWhiteSpace(cart.PaymentIntentId))
        {
            return FinalizePaymentResult.Failure(
                FinalizePaymentError.PaymentIntentMissing,
                "missing_payment_intent",
                "Il carrello non ha un PaymentIntent associato.");
        }

        if (!string.IsNullOrWhiteSpace(paymentIntentId) &&
            !string.Equals(paymentIntentId, cart.PaymentIntentId, StringComparison.Ordinal))
        {
            return FinalizePaymentResult.Failure(
                FinalizePaymentError.PaymentIntentMismatch,
                "payment_intent_mismatch",
                "Il PaymentIntent specificato non corrisponde al carrello.");
        }

        var stripeSettings = stripeSettingsOptions.Value;
        StripeConfiguration.ApiKey = stripeSettings.SecretKey;

        PaymentIntent intent;
        try
        {
            var paymentIntentService = new PaymentIntentService();
            intent = await paymentIntentService.GetAsync(cart.PaymentIntentId);
        }
        catch (StripeException ex)
        {
            var message = ex.StripeError?.Message ?? ex.Message;
            return FinalizePaymentResult.Failure(
                FinalizePaymentError.PaymentProviderError,
                "stripe_error",
                $"Errore Stripe durante verifica pagamento: {message}");
        }

        if (!IsPaymentIntentOwnedByUser(intent, userId))
        {
            return FinalizePaymentResult.Failure(
                FinalizePaymentError.Forbidden,
                "forbidden",
                "Il PaymentIntent non appartiene all'utente autenticato.");
        }

        var paymentStatus = intent.Status?.ToLowerInvariant() ?? "unknown";
        if (paymentStatus != "succeeded")
        {
            if (paymentStatus == "requires_payment_method")
            {
                await UpsertOrderAsync(intent, PaymentOrderStatus.Failed, intent.LastPaymentError?.Message);
                return FinalizePaymentResult.Failure(
                    FinalizePaymentError.PaymentFailed,
                    paymentStatus,
                    "Pagamento non riuscito. Verifica i dati carta e riprova.");
            }

            return FinalizePaymentResult.Failure(
                FinalizePaymentError.PaymentNotCompleted,
                paymentStatus,
                $"Pagamento non ancora completato. Stato Stripe corrente: {paymentStatus}.");
        }

        var order = await UpsertOrderAsync(intent, PaymentOrderStatus.Paid, null);

        var metadataCartId = GetMetadataValue(intent.Metadata, "cartId");
        if (!string.IsNullOrWhiteSpace(metadataCartId))
        {
            await cartService.DeleteCartAsync(metadataCartId);
        }

        return FinalizePaymentResult.Success(order.Id, intent.Id);
    }

    public async Task<WebhookProcessResult> ProcessWebhookAsync(string payload, string? stripeSignatureHeader)
    {
        if (string.IsNullOrWhiteSpace(stripeSignatureHeader))
        {
            return WebhookProcessResult.Failure(
                WebhookProcessError.MissingSignature,
                "Header Stripe-Signature mancante.");
        }

        var stripeSettings = stripeSettingsOptions.Value;
        if (string.IsNullOrWhiteSpace(stripeSettings.WebhookSecret))
        {
            return WebhookProcessResult.Failure(
                WebhookProcessError.MissingWebhookSecret,
                "WebhookSecret Stripe non configurato.");
        }

        Event stripeEvent;
        try
        {
            stripeEvent = EventUtility.ConstructEvent(payload, stripeSignatureHeader, stripeSettings.WebhookSecret);
        }
        catch (StripeException ex)
        {
            return WebhookProcessResult.Failure(
                WebhookProcessError.InvalidSignature,
                $"Firma webhook non valida: {ex.Message}");
        }
        catch (Exception ex)
        {
            return WebhookProcessResult.Failure(
                WebhookProcessError.InvalidPayload,
                $"Payload webhook non valido: {ex.Message}");
        }

        if (stripeEvent.Data.Object is not PaymentIntent intent)
        {
            return WebhookProcessResult.Success("Evento ignorato: payload non contiene PaymentIntent.");
        }

        var eventType = stripeEvent.Type?.ToLowerInvariant() ?? string.Empty;
        switch (eventType)
        {
            case "payment_intent.succeeded":
                await UpsertOrderAsync(intent, PaymentOrderStatus.Paid, null);
                await DeleteCartFromMetadataAsync(intent);
                return WebhookProcessResult.Success("PaymentIntent succeeded processato.");

            case "payment_intent.payment_failed":
                await UpsertOrderAsync(intent, PaymentOrderStatus.Failed, intent.LastPaymentError?.Message);
                return WebhookProcessResult.Success("PaymentIntent failed processato.");

            default:
                return WebhookProcessResult.Success($"Evento ignorato: {stripeEvent.Type}");
        }
    }

    private static long CalculateCartAmountInMinorUnits(ShoppingCart cart, decimal shippingPrice)
    {
        checked
        {
            var itemsTotal = cart.Items.Sum(item => checked(ToMinorUnits(item.Price) * item.Quantity));
            var shippingTotal = ToMinorUnits(shippingPrice);
            return checked(itemsTotal + shippingTotal);
        }
    }

    private static long ToMinorUnits(decimal amount)
    {
        if (amount < 0)
        {
            throw new InvalidOperationException("L'importo non può essere negativo.");
        }

        var scaledAmount = decimal.Round(amount * 100m, 0, MidpointRounding.AwayFromZero);
        return decimal.ToInt64(scaledAmount);
    }

    private static Dictionary<string, string> BuildMetadata(ShoppingCart cart, string? userId, string cartHash)
    {
        return new Dictionary<string, string>
        {
            ["cartId"] = cart.Id,
            ["userId"] = string.IsNullOrWhiteSpace(userId) ? "anonymous" : userId,
            ["cartHash"] = cartHash
        };
    }

    private static string ComputeCartHash(ShoppingCart cart, decimal shippingPrice, string currency)
    {
        var normalizedItems = cart.Items
            .OrderBy(item => item.ProductId)
            .ThenBy(item => item.ProductName)
            .Select(item => $"{item.ProductId}:{item.Quantity}:{item.Price:0.00}");

        var payload = string.Join('|', normalizedItems);
        var completePayload = $"{cart.Id}|{payload}|shipping:{shippingPrice:0.00}|currency:{currency.ToLowerInvariant()}";

        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(completePayload));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string SanitizeForIdempotency(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "none";
        }

        return value.Trim().Replace(" ", "-", StringComparison.Ordinal);
    }

    private static bool IsPaymentIntentOwnedByUser(PaymentIntent intent, string? userId)
    {
        var metadataUserId = GetMetadataValue(intent.Metadata, "userId");
        if (string.IsNullOrWhiteSpace(metadataUserId))
        {
            return true;
        }

        if (string.Equals(metadataUserId, "anonymous", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return !string.IsNullOrWhiteSpace(userId) &&
               string.Equals(metadataUserId, userId, StringComparison.Ordinal);
    }

    private async Task DeleteCartFromMetadataAsync(PaymentIntent intent)
    {
        var cartId = GetMetadataValue(intent.Metadata, "cartId");
        if (string.IsNullOrWhiteSpace(cartId))
        {
            return;
        }

        await cartService.DeleteCartAsync(cartId);
    }

    private async Task<PaymentOrder> UpsertOrderAsync(
        PaymentIntent intent,
        PaymentOrderStatus incomingStatus,
        string? failureMessage)
    {
        var paymentIntentId = intent.Id ?? string.Empty;
        var cartId = GetMetadataValue(intent.Metadata, "cartId") ?? string.Empty;
        var userId = GetMetadataValue(intent.Metadata, "userId");
        var now = DateTime.UtcNow;

        var order = await dbContext.PaymentOrders
            .FirstOrDefaultAsync(order => order.PaymentIntentId == paymentIntentId);

        var finalStatus = ResolveStatus(order?.Status, incomingStatus);
        if (order == null)
        {
            order = new PaymentOrder
            {
                CartId = cartId,
                PaymentIntentId = paymentIntentId,
                UserId = userId,
                Amount = intent.Amount,
                Currency = intent.Currency ?? "usd",
                Status = finalStatus,
                FailureMessage = finalStatus == PaymentOrderStatus.Failed ? failureMessage : null,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            };

            dbContext.PaymentOrders.Add(order);
        }
        else
        {
            order.CartId = string.IsNullOrWhiteSpace(order.CartId) ? cartId : order.CartId;
            order.UserId = string.IsNullOrWhiteSpace(order.UserId) ? userId : order.UserId;
            order.Amount = intent.Amount;
            order.Currency = intent.Currency ?? order.Currency;
            order.Status = finalStatus;
            order.FailureMessage = finalStatus == PaymentOrderStatus.Failed ? failureMessage : null;
            order.UpdatedAtUtc = now;
        }

        await dbContext.SaveChangesAsync();
        return order;
    }

    private static PaymentOrderStatus ResolveStatus(PaymentOrderStatus? existingStatus, PaymentOrderStatus incomingStatus)
    {
        if (existingStatus == PaymentOrderStatus.Paid)
        {
            return PaymentOrderStatus.Paid;
        }

        return incomingStatus;
    }

    private static string? GetMetadataValue(IDictionary<string, string>? metadata, string key)
    {
        if (metadata == null)
        {
            return null;
        }

        return metadata.TryGetValue(key, out var value) ? value : null;
    }
}

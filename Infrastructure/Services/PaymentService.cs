using Core.Entities;
using Core.Interfaces;
using Core.Payments;
using Infrastructure.Settings;
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
                            IGenericRepository<DeliveryMethod> dmRepo) : IPaymentService
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
}

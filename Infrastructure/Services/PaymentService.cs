using Core.Entities;
using Core.Interfaces;
using Core.Payments;
using Infrastructure.Data;
using Infrastructure.Settings;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
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
                            StoreContext dbContext,
                            UserManager<AppUser> userManager,
                            ILogger<PaymentService> logger) : IPaymentService
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
    public async Task<PaymentIntentOperationResult> CreateUpdatePaymentIntent(
        string cartId,
        string? userId,
        bool savePaymentMethod,
        string? paymentMethodId)
    {
        using var scope = BeginCorrelationScope(cartId, null, userId);
        logger.LogInformation("Avvio create/update PaymentIntent");

        if (string.IsNullOrWhiteSpace(cartId))
        {
            logger.LogWarning("Create/update PaymentIntent fallita: cartId non valido");
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
            logger.LogWarning("Create/update PaymentIntent fallita: carrello non trovato");
            return PaymentIntentOperationResult.Failure(
                PaymentIntentOperationError.CartNotFound,
                $"Carrello '{cartId}' non trovato.");
        }

        if (cart.Items.Count == 0)
        {
            logger.LogWarning("Create/update PaymentIntent fallita: carrello vuoto");
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
                logger.LogWarning(
                    "Create/update PaymentIntent fallita: delivery method {DeliveryMethodId} non trovato",
                    cart.DeliverMethodId);
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
                logger.LogWarning(
                    "Create/update PaymentIntent fallita: product {ProductId} non trovato",
                    item.ProductId);
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
        var normalizedPaymentMethodId = string.IsNullOrWhiteSpace(paymentMethodId) ? null : paymentMethodId.Trim();

        var service = new PaymentIntentService();
        string? stripeCustomerId = null;
        var requiresCustomerContext = savePaymentMethod || normalizedPaymentMethodId is not null;
        if (requiresCustomerContext && !string.IsNullOrWhiteSpace(userId))
        {
            var customerResult = await GetStripeCustomerForExistingUserAsync(userId, createIfMissing: true);
            if (!customerResult.IsSuccess)
            {
                var customerError = customerResult.Error == SavedPaymentMethodOperationError.PaymentProviderError
                    ? PaymentIntentOperationError.PaymentProviderError
                    : PaymentIntentOperationError.Forbidden;
                return PaymentIntentOperationResult.Failure(customerError, customerResult.Message!);
            }

            stripeCustomerId = customerResult.CustomerId;
        }

        if (normalizedPaymentMethodId is not null && stripeCustomerId is null)
        {
            return PaymentIntentOperationResult.Failure(
                PaymentIntentOperationError.Forbidden,
                "Metodo di pagamento salvato non consentito per utente non autenticato.");
        }

        if (normalizedPaymentMethodId is not null)
        {
            var isOwnedByUser = await IsPaymentMethodOwnedByCustomerAsync(normalizedPaymentMethodId, stripeCustomerId!);
            if (!isOwnedByUser.IsSuccess)
            {
                return PaymentIntentOperationResult.Failure(isOwnedByUser.Error, isOwnedByUser.Message!);
            }
        }

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
                if (!string.IsNullOrWhiteSpace(stripeCustomerId))
                {
                    options.Customer = stripeCustomerId;
                }

                if (savePaymentMethod)
                {
                    options.SetupFutureUsage = "off_session";
                }

                if (normalizedPaymentMethodId is not null)
                {
                    options.PaymentMethod = normalizedPaymentMethodId;
                }

                intent = await service.CreateAsync(options, new RequestOptions
                {
                    IdempotencyKey = $"pi:create:{SanitizeForIdempotency(cart.Id)}:{cartHash}"
                });
                logger.LogInformation("PaymentIntent creato: {PaymentIntentId}", intent.Id);
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
                if (!string.IsNullOrWhiteSpace(stripeCustomerId))
                {
                    options.Customer = stripeCustomerId;
                }

                if (savePaymentMethod)
                {
                    options.SetupFutureUsage = "off_session";
                }

                if (normalizedPaymentMethodId is not null)
                {
                    options.PaymentMethod = normalizedPaymentMethodId;
                }

                try
                {
                    intent = await service.UpdateAsync(cart.PaymentIntentId, options, new RequestOptions
                    {
                        IdempotencyKey = $"pi:update:{SanitizeForIdempotency(cart.PaymentIntentId)}:{cartHash}"
                    });
                }
                catch (StripeException ex) when (ShouldRecreatePaymentIntentOnUpdateFailure(ex))
                {
                    logger.LogWarning(
                        ex,
                        "PaymentIntent {PaymentIntentId} non più aggiornabile. Rigenero un nuovo PaymentIntent.",
                        cart.PaymentIntentId);

                    cart.PaymentIntentId = null;
                    cart.ClientSecret = null;

                    var recreateOptions = new PaymentIntentCreateOptions
                    {
                        Amount = totalAmountInMinorUnits,
                        Currency = stripeSettings.Currency,
                        PaymentMethodTypes = ["card"],
                        Metadata = metadata
                    };
                    if (!string.IsNullOrWhiteSpace(stripeCustomerId))
                    {
                        recreateOptions.Customer = stripeCustomerId;
                    }

                    if (savePaymentMethod)
                    {
                        recreateOptions.SetupFutureUsage = "off_session";
                    }

                    if (normalizedPaymentMethodId is not null)
                    {
                        recreateOptions.PaymentMethod = normalizedPaymentMethodId;
                    }

                    intent = await service.CreateAsync(recreateOptions, new RequestOptions
                    {
                        IdempotencyKey = $"pi:recreate:{SanitizeForIdempotency(cart.Id)}:{cartHash}"
                    });
                    logger.LogInformation("PaymentIntent rigenerato: {PaymentIntentId}", intent.Id);
                    cart.PaymentIntentId = intent.Id;
                    cart.ClientSecret = intent.ClientSecret;
                }

                logger.LogInformation("PaymentIntent aggiornato: {PaymentIntentId}", cart.PaymentIntentId);

                if (!string.IsNullOrWhiteSpace(intent.ClientSecret))
                {
                    cart.ClientSecret = intent.ClientSecret;
                }
            }
        }
        catch (StripeException ex)
        {
            var message = ex.StripeError?.Message ?? ex.Message;
            logger.LogError(ex, "Errore Stripe durante create/update PaymentIntent: {StripeMessage}", message);
            return PaymentIntentOperationResult.Failure(
                PaymentIntentOperationError.PaymentProviderError,
                $"Errore Stripe durante aggiornamento PaymentIntent: {message}");
        }

        await cartService.SetCartAsync(cart);
        logger.LogInformation("Create/update PaymentIntent completata con successo");
        return PaymentIntentOperationResult.Success(cart);
    }

    public async Task<FinalizePaymentResult> FinalizePaymentAsync(string cartId, string? userId, string? paymentIntentId)
    {
        using var scope = BeginCorrelationScope(cartId, paymentIntentId, userId);
        logger.LogInformation("Avvio finalize payment");

        if (string.IsNullOrWhiteSpace(cartId))
        {
            logger.LogWarning("Finalize payment fallita: cartId non valido");
            return FinalizePaymentResult.Failure(
                FinalizePaymentError.InvalidCartId,
                "invalid_cart_id",
                "Identificativo carrello non valido.");
        }

        var cart = await cartService.GetCartAsync(cartId);
        if (cart == null)
        {
            logger.LogWarning("Finalize payment fallita: carrello non trovato");
            return FinalizePaymentResult.Failure(
                FinalizePaymentError.CartNotFound,
                "cart_not_found",
                $"Carrello '{cartId}' non trovato.");
        }

        if (string.IsNullOrWhiteSpace(cart.PaymentIntentId))
        {
            logger.LogWarning("Finalize payment fallita: paymentIntent mancante sul carrello");
            return FinalizePaymentResult.Failure(
                FinalizePaymentError.PaymentIntentMissing,
                "missing_payment_intent",
                "Il carrello non ha un PaymentIntent associato.");
        }

        if (!string.IsNullOrWhiteSpace(paymentIntentId) &&
            !string.Equals(paymentIntentId, cart.PaymentIntentId, StringComparison.Ordinal))
        {
            logger.LogWarning(
                "Finalize payment fallita: paymentIntent mismatch (cart:{CartPaymentIntentId}, request:{RequestPaymentIntentId})",
                cart.PaymentIntentId,
                paymentIntentId);
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
            logger.LogError(ex, "Errore Stripe durante finalize payment: {StripeMessage}", message);
            return FinalizePaymentResult.Failure(
                FinalizePaymentError.PaymentProviderError,
                "stripe_error",
                $"Errore Stripe durante verifica pagamento: {message}");
        }

        if (!IsPaymentIntentOwnedByUser(intent, userId))
        {
            logger.LogWarning("Finalize payment fallita: ownership paymentIntent non valida");
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
                await UpsertPaymentOrderAsync(intent, PaymentOrderStatus.Failed, intent.LastPaymentError?.Message);
                logger.LogWarning("Finalize payment fallita: stato Stripe requires_payment_method");
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

        var paymentOrder = await UpsertPaymentOrderAsync(intent, PaymentOrderStatus.Paid, null);
        var domainOrder = await EnsureDomainOrderAsync(intent, paymentOrder, userId, cart);
        if (domainOrder == null)
        {
            return FinalizePaymentResult.Failure(
                FinalizePaymentError.PaymentProviderError,
                "order_creation_failed",
                "Pagamento confermato ma creazione ordine non riuscita.");
        }

        var metadataCartId = GetMetadataValue(intent.Metadata, "cartId");
        if (!string.IsNullOrWhiteSpace(metadataCartId))
        {
            await cartService.DeleteCartAsync(metadataCartId);
            logger.LogInformation("Carrello eliminato dopo finalize: {CartId}", metadataCartId);
        }

        logger.LogInformation(
            "Finalize payment completata con successo: paymentIntentId={PaymentIntentId}, orderId={OrderId}",
            intent.Id,
            domainOrder.Id);
        return FinalizePaymentResult.Success(domainOrder.Id, intent.Id);
    }

    public async Task<WebhookProcessResult> ProcessWebhookAsync(string payload, string? stripeSignatureHeader)
    {
        using var scope = BeginCorrelationScope(null, null, null);
        logger.LogInformation("Avvio elaborazione webhook Stripe");

        if (string.IsNullOrWhiteSpace(stripeSignatureHeader))
        {
            logger.LogWarning("Webhook Stripe rifiutato: header firma mancante");
            return WebhookProcessResult.Failure(
                WebhookProcessError.MissingSignature,
                "Header Stripe-Signature mancante.");
        }

        var stripeSettings = stripeSettingsOptions.Value;
        if (string.IsNullOrWhiteSpace(stripeSettings.WebhookSecret))
        {
            logger.LogError("Webhook Stripe rifiutato: webhook secret non configurato");
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
            logger.LogWarning(ex, "Webhook Stripe rifiutato: firma non valida");
            return WebhookProcessResult.Failure(
                WebhookProcessError.InvalidSignature,
                $"Firma webhook non valida: {ex.Message}");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Webhook Stripe rifiutato: payload non valido");
            return WebhookProcessResult.Failure(
                WebhookProcessError.InvalidPayload,
                $"Payload webhook non valido: {ex.Message}");
        }

        if (stripeEvent.Data.Object is not PaymentIntent intent)
        {
            logger.LogInformation("Webhook Stripe ignorato: payload senza PaymentIntent");
            return WebhookProcessResult.Success("Evento ignorato: payload non contiene PaymentIntent.");
        }

        using var paymentIntentScope = BeginCorrelationScope(
            GetMetadataValue(intent.Metadata, "cartId"),
            intent.Id,
            GetMetadataValue(intent.Metadata, "userId"));

        var eventType = stripeEvent.Type?.ToLowerInvariant() ?? string.Empty;
        logger.LogInformation("Evento webhook Stripe ricevuto: {StripeEventType}", eventType);
        switch (eventType)
        {
            case "payment_intent.succeeded":
                var paymentOrder = await UpsertPaymentOrderAsync(intent, PaymentOrderStatus.Paid, null);
                await EnsureDomainOrderAsync(intent, paymentOrder, GetMetadataValue(intent.Metadata, "userId"), null);
                await DeleteCartFromMetadataAsync(intent);
                logger.LogInformation("Webhook processed: payment_intent.succeeded");
                return WebhookProcessResult.Success("PaymentIntent succeeded processato.");

            case "payment_intent.payment_failed":
                await UpsertPaymentOrderAsync(intent, PaymentOrderStatus.Failed, intent.LastPaymentError?.Message);
                logger.LogWarning(
                    "Webhook processed: payment_intent.payment_failed ({FailureMessage})",
                    intent.LastPaymentError?.Message);
                return WebhookProcessResult.Success("PaymentIntent failed processato.");

            default:
                logger.LogInformation("Webhook Stripe ignorato: evento {StripeEventType} non gestito", stripeEvent.Type);
                return WebhookProcessResult.Success($"Evento ignorato: {stripeEvent.Type}");
        }
    }

    public async Task<SavedPaymentMethodsResult> GetSavedPaymentMethodsAsync(string? userId)
    {
        var customerResult = await GetStripeCustomerForExistingUserAsync(userId, createIfMissing: false);
        if (!customerResult.IsSuccess)
        {
            return SavedPaymentMethodsResult.Failure(customerResult.Error, customerResult.Message!);
        }

        if (string.IsNullOrWhiteSpace(customerResult.CustomerId))
        {
            return SavedPaymentMethodsResult.Success([]);
        }

        StripeConfiguration.ApiKey = stripeSettingsOptions.Value.SecretKey;
        try
        {
            var customerService = new CustomerService();
            var customer = await customerService.GetAsync(customerResult.CustomerId);

            var paymentMethodService = new PaymentMethodService();
            var paymentMethods = await paymentMethodService.ListAsync(new PaymentMethodListOptions
            {
                Customer = customerResult.CustomerId,
                Type = "card"
            });

            var defaultPaymentMethodId = customer.InvoiceSettings?.DefaultPaymentMethodId;
            var mapped = paymentMethods.Data
                .Select(method => new SavedPaymentMethodDto(
                    method.Id,
                    method.Card?.Brand ?? "unknown",
                    method.Card?.Last4 ?? string.Empty,
                    method.Card?.ExpMonth ?? 0,
                    method.Card?.ExpYear ?? 0,
                    string.Equals(defaultPaymentMethodId, method.Id, StringComparison.Ordinal)))
                .ToList();

            return SavedPaymentMethodsResult.Success(mapped);
        }
        catch (StripeException ex)
        {
            var message = ex.StripeError?.Message ?? ex.Message;
            logger.LogError(ex, "Errore Stripe durante lettura payment methods: {StripeMessage}", message);
            return SavedPaymentMethodsResult.Failure(
                SavedPaymentMethodOperationError.PaymentProviderError,
                $"Errore Stripe durante lettura payment methods: {message}");
        }
    }

    public async Task<SavedPaymentMethodOperationResult> DeleteSavedPaymentMethodAsync(
        string? userId,
        string paymentMethodId)
    {
        var normalizedPaymentMethodId = paymentMethodId?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedPaymentMethodId))
        {
            return SavedPaymentMethodOperationResult.Failure(
                SavedPaymentMethodOperationError.InvalidPaymentMethodId,
                "PaymentMethodId non valido.");
        }

        var customerResult = await GetStripeCustomerForExistingUserAsync(userId, createIfMissing: false);
        if (!customerResult.IsSuccess)
        {
            return SavedPaymentMethodOperationResult.Failure(customerResult.Error, customerResult.Message!);
        }

        if (string.IsNullOrWhiteSpace(customerResult.CustomerId))
        {
            return SavedPaymentMethodOperationResult.Failure(
                SavedPaymentMethodOperationError.PaymentMethodNotFound,
                "Nessun metodo di pagamento salvato trovato per l'utente.");
        }

        StripeConfiguration.ApiKey = stripeSettingsOptions.Value.SecretKey;
        try
        {
            var paymentMethodOwnership = await IsSavedPaymentMethodOwnedByCustomerAsync(
                normalizedPaymentMethodId,
                customerResult.CustomerId);
            if (!paymentMethodOwnership.IsSuccess)
            {
                return paymentMethodOwnership;
            }

            var customerService = new CustomerService();
            var customer = await customerService.GetAsync(customerResult.CustomerId);
            if (string.Equals(
                    customer.InvoiceSettings?.DefaultPaymentMethodId,
                    normalizedPaymentMethodId,
                    StringComparison.Ordinal))
            {
                await customerService.UpdateAsync(customerResult.CustomerId, new CustomerUpdateOptions
                {
                    InvoiceSettings = new CustomerInvoiceSettingsOptions
                    {
                        DefaultPaymentMethod = string.Empty
                    }
                });
            }

            var paymentMethodService = new PaymentMethodService();
            await paymentMethodService.DetachAsync(normalizedPaymentMethodId);
            return SavedPaymentMethodOperationResult.Success();
        }
        catch (StripeException ex)
        {
            var message = ex.StripeError?.Message ?? ex.Message;
            logger.LogError(ex, "Errore Stripe durante delete payment method: {StripeMessage}", message);
            return SavedPaymentMethodOperationResult.Failure(
                SavedPaymentMethodOperationError.PaymentProviderError,
                $"Errore Stripe durante eliminazione payment method: {message}");
        }
    }

    public async Task<SavedPaymentMethodOperationResult> SetDefaultSavedPaymentMethodAsync(
        string? userId,
        string paymentMethodId)
    {
        var normalizedPaymentMethodId = paymentMethodId?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedPaymentMethodId))
        {
            return SavedPaymentMethodOperationResult.Failure(
                SavedPaymentMethodOperationError.InvalidPaymentMethodId,
                "PaymentMethodId non valido.");
        }

        var customerResult = await GetStripeCustomerForExistingUserAsync(userId, createIfMissing: false);
        if (!customerResult.IsSuccess)
        {
            return SavedPaymentMethodOperationResult.Failure(customerResult.Error, customerResult.Message!);
        }

        if (string.IsNullOrWhiteSpace(customerResult.CustomerId))
        {
            return SavedPaymentMethodOperationResult.Failure(
                SavedPaymentMethodOperationError.PaymentMethodNotFound,
                "Nessun metodo di pagamento salvato trovato per l'utente.");
        }

        StripeConfiguration.ApiKey = stripeSettingsOptions.Value.SecretKey;
        try
        {
            var paymentMethodOwnership = await IsSavedPaymentMethodOwnedByCustomerAsync(
                normalizedPaymentMethodId,
                customerResult.CustomerId);
            if (!paymentMethodOwnership.IsSuccess)
            {
                return paymentMethodOwnership;
            }

            var customerService = new CustomerService();
            await customerService.UpdateAsync(customerResult.CustomerId, new CustomerUpdateOptions
            {
                InvoiceSettings = new CustomerInvoiceSettingsOptions
                {
                    DefaultPaymentMethod = normalizedPaymentMethodId
                }
            });

            return SavedPaymentMethodOperationResult.Success();
        }
        catch (StripeException ex)
        {
            var message = ex.StripeError?.Message ?? ex.Message;
            logger.LogError(ex, "Errore Stripe durante set default payment method: {StripeMessage}", message);
            return SavedPaymentMethodOperationResult.Failure(
                SavedPaymentMethodOperationError.PaymentProviderError,
                $"Errore Stripe durante impostazione default payment method: {message}");
        }
    }

    private async Task<(bool IsSuccess, PaymentIntentOperationError Error, string? Message)>
        IsPaymentMethodOwnedByCustomerAsync(string paymentMethodId, string customerId)
    {
        StripeConfiguration.ApiKey = stripeSettingsOptions.Value.SecretKey;
        try
        {
            var paymentMethodService = new PaymentMethodService();
            var paymentMethod = await paymentMethodService.GetAsync(paymentMethodId);
            if (paymentMethod == null)
            {
                return (false, PaymentIntentOperationError.PaymentMethodNotFound, "Metodo di pagamento non trovato.");
            }

            if (!string.Equals(paymentMethod.CustomerId, customerId, StringComparison.Ordinal))
            {
                return (false, PaymentIntentOperationError.Forbidden, "Il metodo di pagamento non appartiene all'utente autenticato.");
            }

            return (true, PaymentIntentOperationError.None, null);
        }
        catch (StripeException ex) when (ex.HttpStatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return (false, PaymentIntentOperationError.PaymentMethodNotFound, "Metodo di pagamento non trovato.");
        }
        catch (StripeException ex)
        {
            var message = ex.StripeError?.Message ?? ex.Message;
            logger.LogError(ex, "Errore Stripe durante validazione ownership payment method: {StripeMessage}", message);
            return (false, PaymentIntentOperationError.PaymentProviderError, $"Errore Stripe durante verifica payment method: {message}");
        }
    }

    private async Task<SavedPaymentMethodOperationResult> IsSavedPaymentMethodOwnedByCustomerAsync(
        string paymentMethodId,
        string customerId)
    {
        var paymentMethodService = new PaymentMethodService();
        try
        {
            var paymentMethod = await paymentMethodService.GetAsync(paymentMethodId);
            if (paymentMethod == null)
            {
                return SavedPaymentMethodOperationResult.Failure(
                    SavedPaymentMethodOperationError.PaymentMethodNotFound,
                    "Metodo di pagamento non trovato.");
            }

            if (!string.Equals(paymentMethod.CustomerId, customerId, StringComparison.Ordinal))
            {
                return SavedPaymentMethodOperationResult.Failure(
                    SavedPaymentMethodOperationError.Forbidden,
                    "Il metodo di pagamento non appartiene all'utente autenticato.");
            }

            return SavedPaymentMethodOperationResult.Success();
        }
        catch (StripeException ex) when (ex.HttpStatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return SavedPaymentMethodOperationResult.Failure(
                SavedPaymentMethodOperationError.PaymentMethodNotFound,
                "Metodo di pagamento non trovato.");
        }
        catch (StripeException ex)
        {
            var message = ex.StripeError?.Message ?? ex.Message;
            logger.LogError(ex, "Errore Stripe durante validazione payment method salvato: {StripeMessage}", message);
            return SavedPaymentMethodOperationResult.Failure(
                SavedPaymentMethodOperationError.PaymentProviderError,
                $"Errore Stripe durante verifica payment method: {message}");
        }
    }

    private async Task<StripeCustomerResult> GetStripeCustomerForExistingUserAsync(string? userId, bool createIfMissing)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return StripeCustomerResult.Failure(
                SavedPaymentMethodOperationError.Forbidden,
                "Utente non autenticato.");
        }

        var user = await userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return StripeCustomerResult.Failure(
                SavedPaymentMethodOperationError.UserNotFound,
                "Utente non trovato.");
        }

        var existingCustomerId = user.StripeCustomerId?.Trim();
        if (!string.IsNullOrWhiteSpace(existingCustomerId))
        {
            StripeConfiguration.ApiKey = stripeSettingsOptions.Value.SecretKey;
            var customerService = new CustomerService();
            try
            {
                var existingCustomer = await customerService.GetAsync(existingCustomerId);
                if (!string.IsNullOrWhiteSpace(existingCustomer.Id))
                {
                    return StripeCustomerResult.Success(existingCustomer.Id);
                }
            }
            catch (StripeException ex) when (ex.HttpStatusCode == System.Net.HttpStatusCode.NotFound)
            {
                logger.LogWarning(
                    "Stripe customer non trovato per utente {UserId}. StripeCustomerId={StripeCustomerId}. Verrà rigenerato se richiesto.",
                    user.Id,
                    existingCustomerId);
                user.StripeCustomerId = null;
                await userManager.UpdateAsync(user);
            }
            catch (StripeException ex)
            {
                var message = ex.StripeError?.Message ?? ex.Message;
                logger.LogError(ex, "Errore Stripe durante verifica customer esistente: {StripeMessage}", message);
                return StripeCustomerResult.Failure(
                    SavedPaymentMethodOperationError.PaymentProviderError,
                    $"Errore Stripe durante verifica customer: {message}");
            }
        }

        if (!createIfMissing)
        {
            return StripeCustomerResult.Success(null);
        }

        StripeConfiguration.ApiKey = stripeSettingsOptions.Value.SecretKey;
        try
        {
            var fullName = $"{user.FirstName} {user.LastName}".Trim();
            var customerService = new CustomerService();
            var createOptions = new CustomerCreateOptions
            {
                Metadata = new Dictionary<string, string>
                {
                    ["userId"] = user.Id
                }
            };

            if (!string.IsNullOrWhiteSpace(user.Email))
            {
                createOptions.Email = user.Email;
            }

            if (!string.IsNullOrWhiteSpace(fullName))
            {
                createOptions.Name = fullName;
            }

            var customer = await customerService.CreateAsync(createOptions);
            user.StripeCustomerId = customer.Id;
            var identityResult = await userManager.UpdateAsync(user);
            if (!identityResult.Succeeded)
            {
                return StripeCustomerResult.Failure(
                    SavedPaymentMethodOperationError.UserNotFound,
                    "Impossibile salvare il customer Stripe per l'utente.");
            }

            return StripeCustomerResult.Success(customer.Id);
        }
        catch (StripeException ex)
        {
            var message = ex.StripeError?.Message ?? ex.Message;
            logger.LogError(ex, "Errore Stripe durante creazione customer: {StripeMessage}", message);
            return StripeCustomerResult.Failure(
                SavedPaymentMethodOperationError.PaymentProviderError,
                $"Errore Stripe durante creazione customer: {message}");
        }
    }

    private sealed record StripeCustomerResult(
        bool IsSuccess,
        SavedPaymentMethodOperationError Error,
        string? CustomerId,
        string? Message)
    {
        public static StripeCustomerResult Success(string? customerId) =>
            new(true, SavedPaymentMethodOperationError.None, customerId, null);

        public static StripeCustomerResult Failure(
            SavedPaymentMethodOperationError error,
            string message) =>
            new(false, error, null, message);
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

    private static bool ShouldRecreatePaymentIntentOnUpdateFailure(StripeException ex)
    {
        var code = ex.StripeError?.Code?.Trim();
        if (string.Equals(code, "resource_missing", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (string.Equals(code, "payment_intent_unexpected_state", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (ex.HttpStatusCode == System.Net.HttpStatusCode.BadRequest &&
            string.Equals(ex.StripeError?.Type, "invalid_request_error", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return ex.HttpStatusCode == System.Net.HttpStatusCode.NotFound;
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

    private async Task<PaymentOrder> UpsertPaymentOrderAsync(
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

    private async Task<Order?> EnsureDomainOrderAsync(
        PaymentIntent intent,
        PaymentOrder paymentOrder,
        string? fallbackUserId,
        ShoppingCart? knownCart)
    {
        if (paymentOrder.OrderId.HasValue)
        {
            return await dbContext.Orders
                .Include(order => order.Details)
                .FirstOrDefaultAsync(order => order.Id == paymentOrder.OrderId.Value);
        }

        var finalUserId = NormalizeUserId(GetMetadataValue(intent.Metadata, "userId")) ??
                          NormalizeUserId(fallbackUserId) ??
                          NormalizeUserId(paymentOrder.UserId);
        if (string.IsNullOrWhiteSpace(finalUserId))
        {
            logger.LogWarning(
                "Creazione ordine dominio saltata: userId non disponibile per paymentIntent {PaymentIntentId}",
                intent.Id);
            return null;
        }

        var cartId = GetMetadataValue(intent.Metadata, "cartId") ?? paymentOrder.CartId;
        var cart = knownCart;
        if (cart == null && !string.IsNullOrWhiteSpace(cartId))
        {
            cart = await cartService.GetCartAsync(cartId);
        }

        if (cart == null || cart.Items.Count == 0)
        {
            logger.LogWarning(
                "Creazione ordine dominio saltata: carrello non disponibile o vuoto per paymentIntent {PaymentIntentId}",
                intent.Id);
            return null;
        }

        var existingByIntent = await dbContext.PaymentOrders
            .Where(order => order.PaymentIntentId == paymentOrder.PaymentIntentId)
            .Where(order => order.OrderId.HasValue)
            .Select(order => order.OrderId)
            .FirstOrDefaultAsync();

        if (existingByIntent.HasValue)
        {
            var existingOrder = await dbContext.Orders
                .Include(order => order.Details)
                .FirstOrDefaultAsync(order => order.Id == existingByIntent.Value);
            if (existingOrder != null)
            {
                paymentOrder.OrderId = existingOrder.Id;
                paymentOrder.UserId = finalUserId;
                paymentOrder.UpdatedAtUtc = DateTime.UtcNow;
                await dbContext.SaveChangesAsync();
                return existingOrder;
            }
        }

        var order = new Order
        {
            UserId = finalUserId,
            OrderDate = DateTime.UtcNow,
            PaymentType = GetPaymentType(intent),
            CardNumberMasked = await ExtractCardMaskAsync(intent),
            OrderTotal = ToMajorUnits(intent.Amount, intent.Currency),
            OrderStatus = "completato",
            Details = cart.Items.Select(item => new OrderDetail
            {
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                UnitPrice = item.Price
            }).ToList()
        };

        dbContext.Orders.Add(order);
        await dbContext.SaveChangesAsync();

        paymentOrder.OrderId = order.Id;
        paymentOrder.UserId = finalUserId;
        paymentOrder.UpdatedAtUtc = DateTime.UtcNow;
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

    private static string GetPaymentType(PaymentIntent intent)
    {
        var paymentType = intent.PaymentMethodTypes?.FirstOrDefault();
        return string.IsNullOrWhiteSpace(paymentType) ? "card" : paymentType.Trim().ToLowerInvariant();
    }

    private static decimal ToMajorUnits(long amountMinorUnits, string? currency)
    {
        var normalizedCurrency = currency?.Trim().ToLowerInvariant() ?? "usd";
        var divisor = normalizedCurrency switch
        {
            "jpy" or "krw" or "vnd" => 1m,
            _ => 100m
        };

        return decimal.Round(amountMinorUnits / divisor, 2, MidpointRounding.AwayFromZero);
    }

    private static string? NormalizeUserId(string? userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return null;
        }

        return string.Equals(userId.Trim(), "anonymous", StringComparison.OrdinalIgnoreCase)
            ? null
            : userId.Trim();
    }

    private async Task<string?> ExtractCardMaskAsync(PaymentIntent intent)
    {
        var last4 = intent.PaymentMethod?.Card?.Last4;
        if (!string.IsNullOrWhiteSpace(last4))
        {
            return $"**** {last4}";
        }

        if (string.IsNullOrWhiteSpace(intent.LatestChargeId))
        {
            return null;
        }

        try
        {
            StripeConfiguration.ApiKey = stripeSettingsOptions.Value.SecretKey;
            var chargeService = new ChargeService();
            var charge = await chargeService.GetAsync(intent.LatestChargeId);
            var chargeLast4 = charge.PaymentMethodDetails?.Card?.Last4;
            return string.IsNullOrWhiteSpace(chargeLast4) ? null : $"**** {chargeLast4}";
        }
        catch (StripeException ex)
        {
            logger.LogWarning(
                ex,
                "Impossibile leggere le ultime 4 cifre carta per paymentIntent {PaymentIntentId}",
                intent.Id);
            return null;
        }
    }

    private static string? GetMetadataValue(IDictionary<string, string>? metadata, string key)
    {
        if (metadata == null)
        {
            return null;
        }

        return metadata.TryGetValue(key, out var value) ? value : null;
    }

    private IDisposable BeginCorrelationScope(string? cartId, string? paymentIntentId, string? userId)
    {
        var traceId = Guid.NewGuid().ToString("N");
        var scope = new Dictionary<string, object?>
        {
            ["TraceId"] = traceId,
            ["CartId"] = cartId,
            ["PaymentIntentId"] = paymentIntentId,
            ["UserId"] = userId
        };

        return logger.BeginScope(scope) ?? NoopDisposable.Instance;
    }

    private sealed class NoopDisposable : IDisposable
    {
        public static readonly NoopDisposable Instance = new();

        public void Dispose()
        {
        }
    }
}

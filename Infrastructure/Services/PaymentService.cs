using Core.Entities;
using Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Stripe;
using Stripe.V2;

namespace Infrastructure.Services;

/// <summary>
/// Gestisce la creazione e l'aggiornamento dei Payment Intent Stripe
/// in base al contenuto corrente del carrello.
/// </summary>
public class PaymentService(IConfiguration config,
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
    /// Il carrello aggiornato con <c>PaymentIntentId</c> e <c>ClientSecret</c>, oppure <c>null</c>
    /// se il carrello o una dipendenza dati (prodotto/metodo di consegna) non è disponibile.
    /// </returns>
    public async Task<ShoppingCart?> CreateUpdatePaymentIntent(string cartId)
    {
        // Inizializza la chiave segreta Stripe per la chiamata API corrente.
        StripeConfiguration.ApiKey = config["StripeSettings:SecretKey"];

        var cart = await cartService.GetCartAsync(cartId);

        if (cart == null) return null;

        var shippingPrice = 0m;

        if (cart.DeliverMethodId.HasValue)
        {
            var deliveryMethod = await dmRepo.GetByIdAsync((int)cart.DeliverMethodId);
            if (deliveryMethod == null) return null;

            shippingPrice = deliveryMethod.Price;
        }

        foreach (var item in cart.Items)
        {
            var productItem = await productRepo.GetByIdAsync(item.ProductId);

            if (productItem == null) return null;

            // Forza l'allineamento al prezzo di catalogo per evitare importi obsoleti o alterati lato client.
            if (item.Price != productItem.Price)
            {
                item.Price = productItem.Price;

            }
        }

        var service = new PaymentIntentService();
        PaymentIntent? intent = null;

        if (string.IsNullOrEmpty(cart.PaymentIntentId))
        {
            var options = new PaymentIntentCreateOptions
            {
                // Stripe richiede importi in centesimi; il totale include prodotti e spedizione.
                Amount = (long)cart.Items.Sum(x => x.Quantity * (x.Price * 100))
                + (long)shippingPrice * 100,
                Currency = "usd",
                PaymentMethodTypes = ["card"]
            };

            intent = await service.CreateAsync(options);
            cart.PaymentIntentId = intent.Id;
            cart.ClientSecret = intent.ClientSecret;
        }
        else
        {
            var options = new PaymentIntentUpdateOptions
            {
                // Aggiorna l'importo del Payment Intent esistente in base allo stato corrente del carrello.
                Amount = (long)cart.Items.Sum(x => x.Quantity * (x.Price * 100))
                + (long)shippingPrice * 100
            };
            intent = await service.UpdateAsync(cart.PaymentIntentId, options);

        }

        await cartService.SetCartAsync(cart);
        return cart;
    }
}

using System.Reflection;
using Core.Entities;
using Core.Interfaces;
using Infrastructure.Data;
using Infrastructure.Services;
using Infrastructure.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Stripe;

namespace Core.Tests;

public class PaymentServiceOrderPipelineTests
{
    [Fact]
    public async Task EnsureDomainOrderAsync_CreaOrdineEDettagli_ECollegaPaymentOrder()
    {
        var userId = "user_test_1";
        var cart = new ShoppingCart
        {
            Id = "cart_test_1",
            Items =
            [
                new CartItem
                {
                    ProductId = 10,
                    ProductName = "Prodotto A",
                    Price = 12.50m,
                    Quantity = 2,
                    PictureUrl = "pic-a",
                    Brand = "brand-a",
                    Type = "type-a"
                },
                new CartItem
                {
                    ProductId = 20,
                    ProductName = "Prodotto B",
                    Price = 4.75m,
                    Quantity = 1,
                    PictureUrl = "pic-b",
                    Brand = "brand-b",
                    Type = "type-b"
                }
            ]
        };

        await using var db = CreateDbContext();
        var paymentOrder = new PaymentOrder
        {
            CartId = cart.Id,
            PaymentIntentId = "pi_test_1",
            UserId = userId,
            Amount = 2975,
            Currency = "usd",
            Status = PaymentOrderStatus.Paid
        };
        db.PaymentOrders.Add(paymentOrder);
        await db.SaveChangesAsync();

        var service = CreateService(db, cart);
        var intent = CreateIntent("pi_test_1", cart.Id, userId, 2975);

        var order = await InvokeEnsureDomainOrderAsync(service, intent, paymentOrder, userId, cart);

        Assert.NotNull(order);
        Assert.True(order!.Id > 0);
        Assert.Equal(userId, order.UserId);
        Assert.Equal("card", order.PaymentType);
        Assert.Equal(29.75m, order.OrderTotal);
        Assert.Equal("completato", order.OrderStatus);
        Assert.Equal(2, order.Details.Count);

        var trackedPaymentOrder = await db.PaymentOrders.SingleAsync(x => x.PaymentIntentId == "pi_test_1");
        Assert.Equal(order.Id, trackedPaymentOrder.OrderId);
    }

    [Fact]
    public async Task EnsureDomainOrderAsync_SeOrderGiaCollegato_NonCreaDuplicati()
    {
        var userId = "user_test_2";
        await using var db = CreateDbContext();

        var existingOrder = new Order
        {
            UserId = userId,
            OrderDate = DateTime.UtcNow,
            PaymentType = "card",
            OrderTotal = 10m,
            OrderStatus = "completato",
            Details = []
        };
        db.Orders.Add(existingOrder);
        await db.SaveChangesAsync();

        var paymentOrder = new PaymentOrder
        {
            CartId = "cart_existing",
            PaymentIntentId = "pi_test_existing",
            UserId = userId,
            Amount = 1000,
            Currency = "usd",
            Status = PaymentOrderStatus.Paid,
            OrderId = existingOrder.Id
        };
        db.PaymentOrders.Add(paymentOrder);
        await db.SaveChangesAsync();

        var service = CreateService(db, null);
        var intent = CreateIntent("pi_test_existing", "cart_existing", userId, 1000);

        var order = await InvokeEnsureDomainOrderAsync(service, intent, paymentOrder, userId, null);

        Assert.NotNull(order);
        Assert.Equal(existingOrder.Id, order!.Id);
        Assert.Equal(1, await db.Orders.CountAsync());
    }

    private static async Task<Order?> InvokeEnsureDomainOrderAsync(
        PaymentService service,
        PaymentIntent intent,
        PaymentOrder paymentOrder,
        string? fallbackUserId,
        ShoppingCart? knownCart)
    {
        var method = typeof(PaymentService).GetMethod(
            "EnsureDomainOrderAsync",
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.NotNull(method);
        var task = (Task<Order?>?)method!.Invoke(service, [intent, paymentOrder, fallbackUserId, knownCart]);
        Assert.NotNull(task);
        return await task!;
    }

    private static PaymentService CreateService(StoreContext dbContext, ShoppingCart? cart)
    {
        var cartService = new FakeCartService(cart);
        var stripeOptions = Options.Create(new StripeSettings
        {
            SecretKey = "sk_test_placeholder",
            WebhookSecret = string.Empty,
            Currency = "usd"
        });

        return new PaymentService(
            stripeOptions,
            cartService,
            null!,
            null!,
            dbContext,
            null!,
            NullLogger<PaymentService>.Instance);
    }

    private static PaymentIntent CreateIntent(string paymentIntentId, string cartId, string userId, long amount)
    {
        return new PaymentIntent
        {
            Id = paymentIntentId,
            Amount = amount,
            Currency = "usd",
            PaymentMethodTypes = ["card"],
            Metadata = new Dictionary<string, string>
            {
                ["cartId"] = cartId,
                ["userId"] = userId
            }
        };
    }

    private static StoreContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<StoreContext>()
            .UseInMemoryDatabase($"skinet-tests-{Guid.NewGuid():N}")
            .Options;

        return new StoreContext(options);
    }

    private sealed class FakeCartService(ShoppingCart? cart) : ICartService
    {
        public Task<ShoppingCart?> GetCartAsync(string key) =>
            Task.FromResult(cart != null && string.Equals(cart.Id, key, StringComparison.Ordinal) ? cart : null);

        public Task<ShoppingCart?> SetCartAsync(ShoppingCart cartToSet) => Task.FromResult<ShoppingCart?>(cartToSet);

        public Task<bool> DeleteCartAsync(string key) => Task.FromResult(true);
    }
}

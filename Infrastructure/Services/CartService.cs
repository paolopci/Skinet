using Core.Entities;
using Core.Interfaces;
using StackExchange.Redis;
using System.Text.Json;

namespace Infrastructure.Services;

public class CartService(IConnectionMultiplexer redis) : ICartService
{
    // Redis DB instance used for cart operations.
    private readonly IDatabase _database = redis.GetDatabase();
    public async Task<bool> DeleteCartAsync(string key)
    {
        // Remove the cart key; returns true if the key was deleted.
        return await _database.KeyDeleteAsync(key);
    }

    public async Task<ShoppingCart?> GetCartAsync(string key)
    {
        var data = await _database.StringGetAsync(key);
        var json = data.ToString();
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        // Deserialize the stored JSON payload.
        return JsonSerializer.Deserialize<ShoppingCart>(json);
    }

    public async Task<ShoppingCart?> SetCartAsync(ShoppingCart cart)
    {
        // Persist the cart with a TTL to prevent stale data.
        var created = await _database.StringSetAsync(
            cart.Id,
            JsonSerializer.Serialize(cart),
            TimeSpan.FromDays(30));

        if (!created)
        {
            return null;
        }

        return await GetCartAsync(cart.Id);
    }
}

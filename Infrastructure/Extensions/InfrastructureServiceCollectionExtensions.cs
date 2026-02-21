using Core.Interfaces;
using Infrastructure.Data;
using Infrastructure.Services;
using Infrastructure.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace Infrastructure.Extensions;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<StripeSettings>()
            .Bind(configuration.GetSection(StripeSettings.SectionName))
            .Validate(
                settings => !string.IsNullOrWhiteSpace(settings.SecretKey),
                $"{StripeSettings.SectionName}:SecretKey non configurata")
            .Validate(
                settings => settings.SecretKey.StartsWith("sk_test_") || settings.SecretKey.StartsWith("sk_live_"),
                $"{StripeSettings.SectionName}:SecretKey deve iniziare con sk_test_ oppure sk_live_")
            .Validate(
                settings => string.IsNullOrWhiteSpace(settings.WebhookSecret)
                            || settings.WebhookSecret.StartsWith("whsec_"),
                $"{StripeSettings.SectionName}:WebhookSecret deve iniziare con whsec_")
            .Validate(
                settings => !string.IsNullOrWhiteSpace(settings.Currency),
                $"{StripeSettings.SectionName}:Currency non configurata")
            .ValidateOnStart();

        services.AddDbContext<StoreContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));

        var redisConnectionString = configuration.GetConnectionString("Redis") ??
                                    throw new Exception("Cannot get Redis connection string");

        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisConnectionString;
            options.InstanceName = "Skinet";
        });

        services.AddSingleton<IConnectionMultiplexer>(_ =>
        {
            var options = ConfigurationOptions.Parse(redisConnectionString, true);

            return ConnectionMultiplexer.Connect(options);
        });

        services.AddSingleton<ICartService, CartService>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddSingleton<IEmailSender, ConsoleEmailSender>();

        return services;
    }
}

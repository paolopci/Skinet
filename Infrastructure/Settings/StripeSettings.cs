namespace Infrastructure.Settings;

public class StripeSettings
{
    public const string SectionName = "StripeSettings";

    public string SecretKey { get; init; } = string.Empty;
    public string WebhookSecret { get; init; } = string.Empty;
    public string Currency { get; init; } = "usd";
}

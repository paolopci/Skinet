namespace Core.Payments;

public enum WebhookProcessError
{
    None = 0,
    MissingSignature = 1,
    InvalidSignature = 2,
    InvalidPayload = 3,
    MissingWebhookSecret = 4
}

public sealed record WebhookProcessResult(
    bool IsSuccess,
    WebhookProcessError Error,
    string Message)
{
    public static WebhookProcessResult Success(string message = "Webhook processato") =>
        new(true, WebhookProcessError.None, message);

    public static WebhookProcessResult Failure(WebhookProcessError error, string message) =>
        new(false, error, message);
}

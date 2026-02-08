namespace Core.Payments;

public enum FinalizePaymentError
{
    None = 0,
    InvalidCartId = 1,
    CartNotFound = 2,
    PaymentIntentMissing = 3,
    PaymentIntentMismatch = 4,
    Forbidden = 5,
    PaymentNotCompleted = 6,
    PaymentFailed = 7,
    PaymentProviderError = 8
}

public sealed record FinalizePaymentResult(
    bool IsSuccess,
    FinalizePaymentError Error,
    string Status,
    int? OrderId = null,
    string? PaymentIntentId = null,
    string? Message = null)
{
    public static FinalizePaymentResult Success(int orderId, string paymentIntentId) =>
        new(true, FinalizePaymentError.None, "paid", orderId, paymentIntentId);

    public static FinalizePaymentResult Failure(FinalizePaymentError error, string status, string message) =>
        new(false, error, status, null, null, message);
}

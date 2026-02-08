namespace Core.Payments;

public enum SavedPaymentMethodOperationError
{
    None = 0,
    Forbidden = 1,
    InvalidPaymentMethodId = 2,
    PaymentMethodNotFound = 3,
    PaymentProviderError = 4,
    UserNotFound = 5
}

public sealed record SavedPaymentMethodDto(
    string Id,
    string Brand,
    string Last4,
    long ExpMonth,
    long ExpYear,
    bool IsDefault);

public sealed record SavedPaymentMethodsResult(
    bool IsSuccess,
    SavedPaymentMethodOperationError Error,
    IReadOnlyList<SavedPaymentMethodDto>? PaymentMethods = null,
    string? Message = null)
{
    public static SavedPaymentMethodsResult Success(IReadOnlyList<SavedPaymentMethodDto> paymentMethods) =>
        new(true, SavedPaymentMethodOperationError.None, paymentMethods);

    public static SavedPaymentMethodsResult Failure(
        SavedPaymentMethodOperationError error,
        string message) =>
        new(false, error, null, message);
}

public sealed record SavedPaymentMethodOperationResult(
    bool IsSuccess,
    SavedPaymentMethodOperationError Error,
    string? Message = null)
{
    public static SavedPaymentMethodOperationResult Success() =>
        new(true, SavedPaymentMethodOperationError.None);

    public static SavedPaymentMethodOperationResult Failure(
        SavedPaymentMethodOperationError error,
        string message) =>
        new(false, error, message);
}

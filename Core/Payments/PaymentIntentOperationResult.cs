using Core.Entities;

namespace Core.Payments;

public enum PaymentIntentOperationError
{
    None = 0,
    InvalidCartId = 1,
    CartNotFound = 2,
    CartEmpty = 3,
    DeliveryMethodNotFound = 4,
    ProductNotFound = 5,
    PaymentProviderError = 6
}

public sealed record PaymentIntentOperationResult(
    ShoppingCart? Cart,
    PaymentIntentOperationError Error,
    string? Message = null)
{
    public bool IsSuccess => Error == PaymentIntentOperationError.None && Cart is not null;

    public static PaymentIntentOperationResult Success(ShoppingCart cart) =>
        new(cart, PaymentIntentOperationError.None);

    public static PaymentIntentOperationResult Failure(
        PaymentIntentOperationError error,
        string message) =>
        new(null, error, message);
}

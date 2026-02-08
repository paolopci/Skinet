using System;
using Core.Payments;

namespace Core.Interfaces;

public interface IPaymentService
{
    Task<PaymentIntentOperationResult> CreateUpdatePaymentIntent(
        string cartId,
        string? userId,
        bool savePaymentMethod,
        string? paymentMethodId);
    Task<FinalizePaymentResult> FinalizePaymentAsync(string cartId, string? userId, string? paymentIntentId);
    Task<WebhookProcessResult> ProcessWebhookAsync(string payload, string? stripeSignatureHeader);
    Task<SavedPaymentMethodsResult> GetSavedPaymentMethodsAsync(string? userId);
    Task<SavedPaymentMethodOperationResult> DeleteSavedPaymentMethodAsync(string? userId, string paymentMethodId);
    Task<SavedPaymentMethodOperationResult> SetDefaultSavedPaymentMethodAsync(string? userId, string paymentMethodId);
}

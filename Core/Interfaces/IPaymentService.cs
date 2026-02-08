using System;
using Core.Payments;

namespace Core.Interfaces;

public interface IPaymentService
{
    Task<PaymentIntentOperationResult> CreateUpdatePaymentIntent(string cartId, string? userId);
    Task<FinalizePaymentResult> FinalizePaymentAsync(string cartId, string? userId, string? paymentIntentId);
    Task<WebhookProcessResult> ProcessWebhookAsync(string payload, string? stripeSignatureHeader);
}

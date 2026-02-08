using System;
using Core.Payments;

namespace Core.Interfaces;

public interface IPaymentService
{
    Task<PaymentIntentOperationResult> CreateUpdatePaymentIntent(string cartId, string? userId);
}

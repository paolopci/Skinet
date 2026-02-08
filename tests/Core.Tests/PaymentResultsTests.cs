using Core.Entities;
using Core.Payments;

namespace Core.Tests;

public class PaymentResultsTests
{
    [Fact]
    public void PaymentIntentOperationResult_Success_ImpostaEsitoCorretto()
    {
        var cart = new ShoppingCart { Id = "cart_test_1" };

        var result = PaymentIntentOperationResult.Success(cart);

        Assert.True(result.IsSuccess);
        Assert.Equal(PaymentIntentOperationError.None, result.Error);
        Assert.Same(cart, result.Cart);
    }

    [Fact]
    public void PaymentIntentOperationResult_Failure_ImpostaErroreCorretto()
    {
        var result = PaymentIntentOperationResult.Failure(
            PaymentIntentOperationError.CartNotFound,
            "Carrello non trovato.");

        Assert.False(result.IsSuccess);
        Assert.Equal(PaymentIntentOperationError.CartNotFound, result.Error);
        Assert.Null(result.Cart);
        Assert.Equal("Carrello non trovato.", result.Message);
    }

    [Fact]
    public void FinalizePaymentResult_Success_ImpostaDatiConferma()
    {
        var result = FinalizePaymentResult.Success(10, "pi_123");

        Assert.True(result.IsSuccess);
        Assert.Equal(FinalizePaymentError.None, result.Error);
        Assert.Equal("paid", result.Status);
        Assert.Equal(10, result.OrderId);
        Assert.Equal("pi_123", result.PaymentIntentId);
    }

    [Fact]
    public void WebhookProcessResult_Failure_ImpostaMessaggioErrore()
    {
        var result = WebhookProcessResult.Failure(
            WebhookProcessError.InvalidSignature,
            "Firma non valida.");

        Assert.False(result.IsSuccess);
        Assert.Equal(WebhookProcessError.InvalidSignature, result.Error);
        Assert.Equal("Firma non valida.", result.Message);
    }
}

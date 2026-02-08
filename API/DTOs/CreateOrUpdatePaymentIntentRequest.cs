namespace API.DTOs;

public class CreateOrUpdatePaymentIntentRequest
{
    public bool SavePaymentMethod { get; set; }
    public string? PaymentMethodId { get; set; }
}

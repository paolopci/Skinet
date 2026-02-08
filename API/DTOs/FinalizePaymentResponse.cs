namespace API.DTOs;

public class FinalizePaymentResponse
{
    public bool IsSuccess { get; set; }
    public string Status { get; set; } = "unknown";
    public int? OrderId { get; set; }
    public string? PaymentIntentId { get; set; }
    public string? Message { get; set; }
}

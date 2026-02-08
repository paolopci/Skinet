namespace Core.Entities;

public class PaymentOrder : BaseEntity
{
    public string CartId { get; set; } = string.Empty;
    public string PaymentIntentId { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public long Amount { get; set; }
    public string Currency { get; set; } = "usd";
    public PaymentOrderStatus Status { get; set; } = PaymentOrderStatus.Pending;
    public string? FailureMessage { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

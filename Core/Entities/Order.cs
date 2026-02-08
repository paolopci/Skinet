namespace Core.Entities;

public class Order : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; } = DateTime.UtcNow;
    public string PaymentType { get; set; } = string.Empty;
    public string? CardNumberMasked { get; set; }
    public decimal OrderTotal { get; set; }
    public string OrderStatus { get; set; } = "completato";
    public List<OrderDetail> Details { get; set; } = [];
}

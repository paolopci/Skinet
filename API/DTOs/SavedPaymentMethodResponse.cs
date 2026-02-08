namespace API.DTOs;

public class SavedPaymentMethodResponse
{
    public string Id { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string Last4 { get; set; } = string.Empty;
    public long ExpMonth { get; set; }
    public long ExpYear { get; set; }
    public bool IsDefault { get; set; }
}

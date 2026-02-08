namespace API.DTOs;

public class OrderListItemDto
{
    public int OrderId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public DateTime DataOrdine { get; set; }
    public string TipoPagamento { get; set; } = string.Empty;
    public string? NumeroCarta { get; set; }
    public decimal TotaleOrdine { get; set; }
    public string StatoOrdine { get; set; } = string.Empty;
}

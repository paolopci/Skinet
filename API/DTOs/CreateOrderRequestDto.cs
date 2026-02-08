namespace API.DTOs;

public class CreateOrderRequestDto
{
    public string? UserId { get; set; }
    public DateTime? DataOrdine { get; set; }
    public string TipoPagamento { get; set; } = string.Empty;
    public string? NumeroCarta { get; set; }
    public decimal TotaleOrdine { get; set; }
    public string StatoOrdine { get; set; } = "completato";
    public List<OrderDetailDto> Dettagli { get; set; } = [];
}

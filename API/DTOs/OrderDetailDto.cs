namespace API.DTOs;

public class OrderDetailDto
{
    public int? DettaglioId { get; set; }
    public int ProdottoId { get; set; }
    public string NomeProdotto { get; set; } = string.Empty;
    public string? ImmagineUrl { get; set; }
    public int Quantita { get; set; }
    public decimal PrezzoUnitario { get; set; }
}

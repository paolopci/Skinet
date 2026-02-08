namespace API.DTOs;

public class OrderDetailDto
{
    public int? DettaglioId { get; set; }
    public int ProdottoId { get; set; }
    public int Quantita { get; set; }
    public decimal PrezzoUnitario { get; set; }
}

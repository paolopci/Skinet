namespace API.RequestHelpers;

public class Pagination<T>(int pageIndex, int pageSize, int totalCount, IReadOnlyList<T>? data)
{
    // Pagina richiesta dal client.
    public int PageIndex { get; set; } = pageIndex;
    // Dimensione pagina richiesta dal client.
    public int PageSize { get; set; } = pageSize;
    // Totale elementi disponibili (es. prodotti) per la query.
    public int TotalCount { get; set; } = totalCount;
    // Totale pagine calcolate in base a TotalCount e PageSize.
    public int TotalPages => PageSize > 0
        ? (int)Math.Ceiling(TotalCount / (double)PageSize)
        : 0;
    // Risultati della pagina selezionata.
    public IReadOnlyList<T>? Data { get; set; } = data;
}

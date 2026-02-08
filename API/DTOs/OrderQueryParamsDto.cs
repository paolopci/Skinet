namespace API.DTOs;

public class OrderQueryParamsDto
{
    public string? SortBy { get; set; }
    public string? Order { get; set; }
    public string? FilterByUserId { get; set; }
}

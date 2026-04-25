namespace APIlog.Server.DTOs.Books;

public record LowStockBranchDto(
    int BookStoreId,
    string BookStoreCode,
    string BookStoreName,
    string BookStoreAddress,
    IEnumerable<LowStockBookDto> CriticalBooks
);

public record LowStockBookDto(
    int BookId,
    int BookInStoreId,
    string BookTitle,
    IEnumerable<string> Authors,
    string? ISBN,
    decimal BookPrice,
    int Quantity
);

public class LowStockQueryParams
{
    public string? Search { get; set; }
    public string? SortBy { get; set; }
    public string SortOrder { get; set; } = "asc";
    public int CriticalThreshold { get; set; } = 5;
}

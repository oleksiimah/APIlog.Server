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
    string BookTitle,
    int Quantity
);

public class LowStockQueryParams
{
    public string? Search { get; set; }
    public int CriticalThreshold { get; set; } = 5;
}

/// <summary>Mapped from sp_CheckCriticalBookLevel result columns.</summary>
public class CriticalBookSpResult
{
    public string BookTitle { get; set; } = string.Empty;
    public short CurrentStock { get; set; }
}

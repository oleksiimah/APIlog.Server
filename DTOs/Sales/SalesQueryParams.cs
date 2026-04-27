namespace APIlog.Server.DTOs.Sales;

public class SalesQueryParams
{
    public string? Search { get; set; }
    public string? SortBy { get; set; }
    public string SortOrder { get; set; } = "desc";

    public decimal? AmountMin { get; set; }
    public decimal? AmountMax { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }

    public List<int>? BookIds { get; set; }
    public List<int>? EmployeeIds { get; set; }
    public List<int>? CustomerIds { get; set; }
    public string? PaymentStatus { get; set; }

    public int? ItemCountMin { get; set; }
    public int? ItemCountMax { get; set; }
    public int? TotalQtyMin { get; set; }
    public int? TotalQtyMax { get; set; }

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

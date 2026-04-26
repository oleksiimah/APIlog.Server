namespace APIlog.Server.DTOs.Purchases;

public class PurchasesQueryParams
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
    public List<int>? StatusIds { get; set; }
    public List<int>? SupplierIds { get; set; }

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

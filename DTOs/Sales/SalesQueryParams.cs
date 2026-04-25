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
}

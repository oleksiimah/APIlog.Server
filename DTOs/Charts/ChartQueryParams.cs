namespace APIlog.Server.DTOs.Charts;

public class ChartQueryParams
{
    public string Characteristic { get; set; } = "genre";
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public int TopN { get; set; } = 10;
}

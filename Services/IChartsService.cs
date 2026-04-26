using APIlog.Server.DTOs.Charts;

namespace APIlog.Server.Services;

public interface IChartsService
{
    Task<IEnumerable<SalesAttributeDto>> GetSalesByAttributeAsync(ChartQueryParams q);
    Task<IEnumerable<SalesTimelineSeriesDto>> GetSalesTimelineAsync(ChartQueryParams q);
}

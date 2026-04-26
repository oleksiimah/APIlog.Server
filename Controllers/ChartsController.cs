using APIlog.Server.DTOs.Charts;
using APIlog.Server.Models;
using APIlog.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace APIlog.Server.Controllers;

[ApiController]
[Route("api/charts")]
[Authorize(Roles = AppRoles.PurchaseManager)]
public class ChartsController : ControllerBase
{
    private readonly IChartsService _chartsService;

    public ChartsController(IChartsService chartsService) => _chartsService = chartsService;

    [HttpGet("sales-by-attribute")]
    public async Task<IActionResult> GetSalesByAttribute([FromQuery] ChartQueryParams q)
        => Ok(await _chartsService.GetSalesByAttributeAsync(q));

    [HttpGet("sales-timeline")]
    public async Task<IActionResult> GetSalesTimeline([FromQuery] ChartQueryParams q)
        => Ok(await _chartsService.GetSalesTimelineAsync(q));
}

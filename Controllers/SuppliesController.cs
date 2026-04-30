using System.Security.Claims;
using APIlog.Server.DTOs.Supplies;
using APIlog.Server.Models;
using APIlog.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace APIlog.Server.Controllers;

[ApiController]
[Route("api/supplies")]
[Authorize(Roles = $"{AppRoles.Storekeeper},{AppRoles.Admin}")]
public class SuppliesController : ControllerBase
{
    private readonly ISuppliesService _suppliesService;

    public SuppliesController(ISuppliesService suppliesService)
    {
        _suppliesService = suppliesService;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSupplyReceiptDto dto)
    {
        var employeeId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var receipt = await _suppliesService.CreateSupplyReceiptAsync(dto, employeeId);
        return Ok(receipt);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateSupplyReceiptDto dto)
    {
        var employeeId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var receipt = await _suppliesService.UpdateSupplyReceiptAsync(id, dto, employeeId);
        return Ok(receipt);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var employeeId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        await _suppliesService.DeleteSupplyReceiptAsync(id, employeeId);
        return NoContent();
    }
}

using System.Security.Claims;
using APIlog.Server.DTOs.Purchases;
using APIlog.Server.Models;
using APIlog.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace APIlog.Server.Controllers;

[ApiController]
[Route("api/purchases")]
public class PurchasesController : ControllerBase
{
    private readonly IPurchasesService _purchasesService;

    public PurchasesController(IPurchasesService purchasesService)
    {
        _purchasesService = purchasesService;
    }

    [HttpGet]
    [Authorize(Roles = $"{AppRoles.PurchaseManager},{AppRoles.Admin},{AppRoles.Storekeeper}")]
    public async Task<IActionResult> GetAll([FromQuery] PurchasesQueryParams queryParams)
    {
        var receipts = await _purchasesService.GetPurchaseReceiptsAsync(queryParams);
        return Ok(receipts);
    }

    [HttpGet("{id:int}")]
    [Authorize(Roles = $"{AppRoles.PurchaseManager},{AppRoles.Admin},{AppRoles.Storekeeper}")]
    public async Task<IActionResult> GetById(int id)
    {
        var receipt = await _purchasesService.GetPurchaseReceiptByIdAsync(id);
        return receipt is null ? NotFound() : Ok(receipt);
    }

    [HttpPost]
    [Authorize(Roles = $"{AppRoles.PurchaseManager},{AppRoles.Admin}")]
    public async Task<IActionResult> Create([FromBody] CreatePurchaseReceiptDto dto)
    {
        var employeeId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var receipt = await _purchasesService.CreatePurchaseReceiptAsync(dto, employeeId);
        return CreatedAtAction(nameof(GetById), new { id = receipt.PurchaseReceiptId }, receipt);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = $"{AppRoles.PurchaseManager},{AppRoles.Admin}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdatePurchaseReceiptDto dto)
    {
        var receipt = await _purchasesService.UpdatePurchaseReceiptAsync(id, dto);
        return Ok(receipt);
    }

    [HttpPatch("{id:int}/cancel")]
    [Authorize(Roles = $"{AppRoles.PurchaseManager},{AppRoles.Admin}")]
    public async Task<IActionResult> Cancel(int id)
    {
        await _purchasesService.CancelPurchaseReceiptAsync(id);
        return NoContent();
    }

    [HttpPatch("{id:int}/restore")]
    [Authorize(Roles = $"{AppRoles.PurchaseManager},{AppRoles.Admin}")]
    public async Task<IActionResult> Restore(int id)
    {
        await _purchasesService.RestorePurchaseReceiptAsync(id);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = $"{AppRoles.PurchaseManager},{AppRoles.Admin}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _purchasesService.DeletePurchaseReceiptAsync(id);
        return NoContent();
    }
}

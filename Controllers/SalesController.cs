using System.Security.Claims;
using APIlog.Server.DTOs.Payments;
using APIlog.Server.DTOs.Sales;
using APIlog.Server.Models;
using APIlog.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace APIlog.Server.Controllers;

[ApiController]
[Route("api/sales")]
[Authorize(Roles = $"{AppRoles.Cashier},{AppRoles.Admin}")]
public class SalesController : ControllerBase
{
    private readonly ISalesService _salesService;
    private readonly IPaymentsService _paymentsService;

    public SalesController(ISalesService salesService, IPaymentsService paymentsService)
    {
        _salesService = salesService;
        _paymentsService = paymentsService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] SalesQueryParams queryParams)
    {
        int? bookStoreId = null;
        if (User.IsInRole(AppRoles.Cashier))
            bookStoreId = GetBookStoreId();

        var receipts = await _salesService.GetSaleReceiptsAsync(queryParams, bookStoreId);
        return Ok(receipts);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var receipt = await _salesService.GetSaleReceiptByIdAsync(id);
        return receipt is null ? NotFound() : Ok(receipt);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSaleReceiptDto dto)
    {
        var employeeId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var receipt = await _salesService.CreateSaleReceiptAsync(dto, employeeId);
        return CreatedAtAction(nameof(GetById), new { id = receipt.SalesReceiptId }, receipt);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateSaleReceiptDto dto)
    {
        var receipt = await _salesService.UpdateSaleReceiptAsync(id, dto);
        return Ok(receipt);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _salesService.DeleteSaleReceiptAsync(id);
        return NoContent();
    }

    [HttpPost("{id:int}/payments")]
    public async Task<IActionResult> AddPayment(int id, [FromBody] AddPaymentDto dto)
    {
        var payment = await _paymentsService.AddPaymentAsync(id, dto);
        return Ok(payment);
    }

    private int? GetBookStoreId()
    {
        var val = User.FindFirstValue("bookstore_id");
        return int.TryParse(val, out var id) && id > 0 ? id : null;
    }
}

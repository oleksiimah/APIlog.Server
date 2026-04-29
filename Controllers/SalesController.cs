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
        try
        {
            var receipt = await _salesService.CreateSaleReceiptAsync(dto, employeeId);
            return CreatedAtAction(nameof(GetById), new { id = receipt.SalesReceiptId }, receipt);
        }
        catch (InvalidOperationException ex)
        {
            return UnprocessableEntity(new { message = ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateSaleReceiptDto dto)
    {
        var receipt = await _salesService.GetSaleReceiptByIdAsync(id);
        if (receipt == null) return NotFound();
        
        var error = CheckEditDeletePermission(receipt);
        if (error != null) return UnprocessableEntity(new { message = error });
        
        try
        {
            var updated = await _salesService.UpdateSaleReceiptAsync(id, dto);
            return Ok(updated);
        }
        catch (InvalidOperationException ex)
        {
            return UnprocessableEntity(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return UnprocessableEntity(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var receipt = await _salesService.GetSaleReceiptByIdAsync(id);
        if (receipt == null) return NotFound();
        
        var error = CheckEditDeletePermission(receipt);
        if (error != null) return UnprocessableEntity(new { message = error });
        
        await _salesService.DeleteSaleReceiptAsync(id);
        return NoContent();
    }

    [HttpGet("{id:int}/available-stores")]
    public async Task<IActionResult> GetAvailableStores(int id)
    {
        var stores = await _salesService.GetAvailableStoresForReceiptAsync(id);
        return Ok(stores);
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

    /// <summary>
    /// Check if a receipt can be edited or deleted.
    /// Rules:
    /// - Within 30 minutes of last payment (or creation if no payments): always allowed
    /// - After 30 minutes: only allowed if not fully paid
    /// </summary>
    private string? CheckEditDeletePermission(SaleReceiptDetailDto receipt)
    {
        DateTime? referenceTime = receipt.SalesReceiptDateTime;

        // Use last payment datetime if exists
        if (receipt.Payments.Any())
        {
            var lastPayment = receipt.Payments
                .Where(p => p.PaymentDateTime.HasValue)
                .OrderByDescending(p => p.PaymentDateTime)
                .FirstOrDefault();
            if (lastPayment != null)
                referenceTime = lastPayment.PaymentDateTime;
        }

        if (!referenceTime.HasValue)
            return "Невідомий час створення чека.";

        var age = DateTime.UtcNow - referenceTime.Value;
        if (age.TotalMinutes <= 30)
            return null; // Within 30 minutes: always allowed

        // After 30 minutes: check payment status
        if (receipt.PaymentStatus.Equals("paid", StringComparison.OrdinalIgnoreCase))
            return "Оплачені чеки неможливо редагувати або видаляти.";

        return null; // Unpaid: allowed
    }
}

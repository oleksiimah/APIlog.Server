using APIlog.Server.Infrastructure.Data;
using APIlog.Server.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace APIlog.Server.Controllers;

[ApiController]
[Route("api/suppliers")]
public class SuppliersController : ControllerBase
{
    private readonly BookstoreDbContext _db;

    public SuppliersController(BookstoreDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    [Authorize(Roles = $"{AppRoles.PurchaseManager},{AppRoles.Admin}")]
    public async Task<IActionResult> GetAll([FromQuery] string? search = null)
    {
        var query = _db.Suppliers.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(s =>
                s.SupplierName.Contains(search) ||
                (s.SupplierAddress != null && s.SupplierAddress.Contains(search)));

        var result = await query
            .OrderBy(s => s.SupplierName)
            .Select(s => new
            {
                supplierId = s.SupplierId,
                supplierName = s.SupplierName,
                supplierAddress = s.SupplierAddress ?? string.Empty
            })
            .ToListAsync();

        return Ok(result);
    }
}

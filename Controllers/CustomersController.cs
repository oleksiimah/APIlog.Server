using APIlog.Server.Models;
using APIlog.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace APIlog.Server.Controllers;

[ApiController]
[Route("api/customers")]
[Authorize(Roles = $"{AppRoles.Cashier},{AppRoles.Admin}")]
public class CustomersController : ControllerBase
{
    private readonly ICustomersService _customersService;

    public CustomersController(ICustomersService customersService)
    {
        _customersService = customersService;
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string? phone, [FromQuery] string? email)
    {
        var customers = await _customersService.SearchCustomersAsync(phone, email);
        return Ok(customers);
    }
}

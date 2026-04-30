using System.Security.Claims;
using APIlog.Server.DTOs.Employees;
using APIlog.Server.Models;
using APIlog.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace APIlog.Server.Controllers;

[ApiController]
[Route("api/employees")]
[Authorize]
public class EmployeesController : ControllerBase
{
    private readonly IEmployeesService _employeesService;

    public EmployeesController(IEmployeesService employeesService)
    {
        _employeesService = employeesService;
    }

    [HttpGet]
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.PurchaseManager},{AppRoles.Cashier}")]
    public async Task<IActionResult> GetAll([FromQuery] int? bookStoreId)
    {
        // Cashiers may only see employees from their own bookstore
        if (User.IsInRole(AppRoles.Cashier))
        {
            var claim = User.FindFirstValue("bookstore_id");
            bookStoreId = int.TryParse(claim, out var id) ? id : null;
        }

        var employees = await _employeesService.GetEmployeesAsync(bookStoreId);
        return Ok(employees);
    }

    [HttpGet("{id:int}")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> GetById(int id)
    {
        var employee = await _employeesService.GetEmployeeByIdAsync(id);
        return employee is null ? NotFound() : Ok(employee);
    }

    [HttpPost]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> Create([FromBody] CreateEmployeeDto dto)
    {
        var employee = await _employeesService.CreateEmployeeAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = employee.EmployeeId }, employee);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateEmployeeDto dto)
    {
        var employee = await _employeesService.UpdateEmployeeAsync(id, dto);
        return Ok(employee);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            await _employeesService.DeleteEmployeeAsync(id);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }
}

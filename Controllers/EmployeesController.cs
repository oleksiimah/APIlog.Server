using APIlog.Server.DTOs.Employees;
using APIlog.Server.Models;
using APIlog.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace APIlog.Server.Controllers;

[ApiController]
[Route("api/employees")]
[Authorize(Roles = AppRoles.Admin)]
public class EmployeesController : ControllerBase
{
    private readonly IEmployeesService _employeesService;

    public EmployeesController(IEmployeesService employeesService)
    {
        _employeesService = employeesService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int? bookStoreId)
    {
        var employees = await _employeesService.GetEmployeesAsync(bookStoreId);
        return Ok(employees);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var employee = await _employeesService.GetEmployeeByIdAsync(id);
        return employee is null ? NotFound() : Ok(employee);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateEmployeeDto dto)
    {
        var employee = await _employeesService.CreateEmployeeAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = employee.EmployeeId }, employee);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateEmployeeDto dto)
    {
        var employee = await _employeesService.UpdateEmployeeAsync(id, dto);
        return Ok(employee);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _employeesService.DeleteEmployeeAsync(id);
        return NoContent();
    }
}

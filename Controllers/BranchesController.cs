using APIlog.Server.DTOs.Branches;
using APIlog.Server.Models;
using APIlog.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace APIlog.Server.Controllers;

[ApiController]
[Route("api/branches")]
[Authorize]
public class BranchesController : ControllerBase
{
    private readonly IBranchesService _branchesService;

    public BranchesController(IBranchesService branchesService)
    {
        _branchesService = branchesService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var branches = await _branchesService.GetBranchesAsync();
        return Ok(branches);
    }

    [HttpPost]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> Create([FromBody] CreateBranchDto dto)
    {
        var branch = await _branchesService.CreateBranchAsync(dto);
        return Ok(branch);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateBranchDto dto)
    {
        var branch = await _branchesService.UpdateBranchAsync(id, dto);
        return Ok(branch);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> Delete(int id)
    {
        await _branchesService.DeleteBranchAsync(id);
        return NoContent();
    }
}

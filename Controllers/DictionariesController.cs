using APIlog.Server.DTOs.Dictionaries;
using APIlog.Server.Models;
using APIlog.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace APIlog.Server.Controllers;

[ApiController]
[Route("api/dictionaries")]
[Authorize]
public class DictionariesController : ControllerBase
{
    private readonly IDictionariesService _dictionariesService;

    public DictionariesController(IDictionariesService dictionariesService)
    {
        _dictionariesService = dictionariesService;
    }

    [HttpGet]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> GetCategories()
    {
        var categories = await _dictionariesService.GetCategoriesAsync();
        return Ok(categories);
    }

    [HttpGet("{entity}")]
    public async Task<IActionResult> GetItems(string entity)
    {
        var items = await _dictionariesService.GetItemsAsync(entity);
        return Ok(items);
    }

    [HttpPost("{entity}")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> Create(string entity, [FromBody] CreateDictionaryItemDto dto)
    {
        var item = await _dictionariesService.CreateItemAsync(entity, dto);
        return Ok(item);
    }

    [HttpPut("{entity}/{id:int}")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> Update(string entity, int id, [FromBody] UpdateDictionaryItemDto dto)
    {
        var item = await _dictionariesService.UpdateItemAsync(entity, id, dto);
        return Ok(item);
    }

    [HttpDelete("{entity}/{id:int}")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> Delete(string entity, int id)
    {
        await _dictionariesService.DeleteItemAsync(entity, id);
        return NoContent();
    }
}

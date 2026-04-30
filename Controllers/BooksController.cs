using System.Security.Claims;
using APIlog.Server.DTOs.Books;
using APIlog.Server.Models;
using APIlog.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace APIlog.Server.Controllers;

[ApiController]
[Route("api/books")]
[Authorize]
public class BooksController : ControllerBase
{
    private readonly IBooksService _booksService;

    public BooksController(IBooksService booksService)
    {
        _booksService = booksService;
    }

    [HttpGet]
    public async Task<IActionResult> GetBooks([FromQuery] BooksQueryParams queryParams)
    {
        int? bookStoreId = null;
        if (User.IsInRole(AppRoles.Cashier))
            bookStoreId = GetBookStoreId();

        var books = await _booksService.GetBooksAsync(queryParams, bookStoreId);
        return Ok(books);
    }

    [HttpGet("low-stock")]
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.PurchaseManager}")]
    public async Task<IActionResult> GetLowStock([FromQuery] LowStockQueryParams queryParams)
    {
        var result = await _booksService.GetLowStockAsync(queryParams);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        int? bookStoreId = null;
        if (User.IsInRole(AppRoles.Cashier))
            bookStoreId = GetBookStoreId();

        var book = await _booksService.GetBookByIdAsync(id, bookStoreId);
        return book is null ? NotFound() : Ok(book);
    }

    [HttpPost]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> Create([FromBody] CreateBookDto dto)
    {
        var book = await _booksService.CreateBookAsync(dto);
        return Ok(book);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateBookDto dto)
    {
        var book = await _booksService.UpdateBookAsync(id, dto);
        return book is null ? NotFound() : Ok(book);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            await _booksService.DeleteBookAsync(id);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    private int? GetBookStoreId()
    {
        var val = User.FindFirstValue("bookstore_id");
        return int.TryParse(val, out var id) && id > 0 ? id : null;
    }
}

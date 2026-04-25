using APIlog.Server.DTOs.Books;
using APIlog.Server.Infrastructure.Data;
using APIlog.Server.Models;
using Microsoft.EntityFrameworkCore;

namespace APIlog.Server.Services;

public class BookStoresService : IBookStoresService
{
    private readonly BookstoreDbContext _db;
    private readonly IBooksService _booksService;

    public BookStoresService(BookstoreDbContext db, IBooksService booksService)
    {
        _db = db;
        _booksService = booksService;
    }

    public Task<IEnumerable<BookListItemDto>> GetBooksInStoreAsync(int bookStoreId, BooksQueryParams queryParams)
        => _booksService.GetBooksAsync(queryParams, bookStoreId);

    public async Task<BookInStore> GetOrCreateBookInStoreAsync(int bookId, int bookStoreId)
    {
        var existing = await _db.BookInStores
            .FirstOrDefaultAsync(bis => bis.BookId == bookId && bis.BookStoreId == bookStoreId);

        if (existing is not null) return existing;

        var newRecord = new BookInStore
        {
            BookId = bookId,
            BookStoreId = bookStoreId,
            BookInStoreQuantity = 0
        };

        _db.BookInStores.Add(newRecord);
        await _db.SaveChangesAsync();

        return newRecord;
    }
}

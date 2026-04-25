using APIlog.Server.DTOs.Books;
using APIlog.Server.Models;

namespace APIlog.Server.Services;

public interface IBookStoresService
{
    Task<IEnumerable<BookListItemDto>> GetBooksInStoreAsync(int bookStoreId, BooksQueryParams queryParams);
    Task<BookInStore> GetOrCreateBookInStoreAsync(int bookId, int bookStoreId);
}

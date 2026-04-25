using APIlog.Server.DTOs.Books;

namespace APIlog.Server.Services;

public interface IBooksService
{
    Task<IEnumerable<BookListItemDto>> GetBooksAsync(BooksQueryParams queryParams, int? bookStoreId = null);
    Task<BookDetailDto?> GetBookByIdAsync(int bookId, int? bookStoreId = null);
    Task<BookDetailDto> CreateBookAsync(CreateBookDto dto);
    Task<BookDetailDto?> UpdateBookAsync(int bookId, UpdateBookDto dto);
    Task<IEnumerable<LowStockBranchDto>> GetLowStockAsync(LowStockQueryParams queryParams);
}

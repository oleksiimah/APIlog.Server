using APIlog.Server.DTOs.Books;
using APIlog.Server.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace APIlog.Server.Services;

public class BooksService : IBooksService
{
    private readonly BookstoreDbContext _db;

    public BooksService(BookstoreDbContext db)
    {
        _db = db;
    }

    public async Task<IEnumerable<BookListItemDto>> GetBooksAsync(BooksQueryParams q, int? bookStoreId = null)
    {
        var query = _db.Books
            .Include(b => b.BookAuthors).ThenInclude(ba => ba.Author)
            .Include(b => b.BookInStores)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(q.Search))
            query = query.Where(b => b.BookTitle.Contains(q.Search) ||
                (b.ISBN != null && b.ISBN.Contains(q.Search)) ||
                b.BookAuthors.Any(ba => ba.Author.AuthorFullName.Contains(q.Search)));

        if (q.PriceMin.HasValue) query = query.Where(b => b.BookPrice >= q.PriceMin.Value);
        if (q.PriceMax.HasValue) query = query.Where(b => b.BookPrice <= q.PriceMax.Value);
        if (q.YearMin.HasValue) query = query.Where(b => b.BookPublishYear >= q.YearMin.Value);
        if (q.YearMax.HasValue) query = query.Where(b => b.BookPublishYear <= q.YearMax.Value);
        if (q.PagesMin.HasValue) query = query.Where(b => b.BookPageCount >= q.PagesMin.Value);
        if (q.PagesMax.HasValue) query = query.Where(b => b.BookPageCount <= q.PagesMax.Value);
        if (q.HeightMin.HasValue) query = query.Where(b => b.BookHeight >= q.HeightMin.Value);
        if (q.HeightMax.HasValue) query = query.Where(b => b.BookHeight <= q.HeightMax.Value);
        if (q.WidthMin.HasValue) query = query.Where(b => b.BookWidth >= q.WidthMin.Value);
        if (q.WidthMax.HasValue) query = query.Where(b => b.BookWidth <= q.WidthMax.Value);
        if (q.DepthMin.HasValue) query = query.Where(b => b.BookDepth >= q.DepthMin.Value);
        if (q.DepthMax.HasValue) query = query.Where(b => b.BookDepth <= q.DepthMax.Value);

        if (q.AuthorIds?.Count > 0)
            query = query.Where(b => b.BookAuthors.Any(ba => q.AuthorIds.Contains(ba.AuthorId)));
        if (q.PublisherIds?.Count > 0)
            query = query.Where(b => b.PublisherId.HasValue && q.PublisherIds.Contains(b.PublisherId.Value));
        if (q.LanguageIds?.Count > 0)
            query = query.Where(b => b.LanguageId.HasValue && q.LanguageIds.Contains(b.LanguageId.Value));
        if (q.CoverTypeIds?.Count > 0)
            query = query.Where(b => b.CoverTypeId.HasValue && q.CoverTypeIds.Contains(b.CoverTypeId.Value));
        if (q.SubjectIds?.Count > 0)
            query = query.Where(b => b.SubjectId.HasValue && q.SubjectIds.Contains(b.SubjectId.Value));
        if (q.BookTypeIds?.Count > 0)
            query = query.Where(b => b.BookTypeId.HasValue && q.BookTypeIds.Contains(b.BookTypeId.Value));
        if (q.GenreIds?.Count > 0)
            query = query.Where(b => b.BookGenreId.HasValue && q.GenreIds.Contains(b.BookGenreId.Value));
        if (q.BookStoreIds?.Count > 0)
            query = query.Where(b => b.BookInStores.Any(bis =>
                q.BookStoreIds.Contains(bis.BookStoreId ?? 0) && bis.BookInStoreQuantity > 0));

        if (bookStoreId.HasValue)
            query = query.Where(b => b.BookInStores.Any(bis =>
                bis.BookStoreId == bookStoreId.Value && bis.BookInStoreQuantity > 0));

        var books = await query.ToListAsync();

        return books.Select(b =>
        {
            var totalQty = b.BookInStores.Sum(bis => (int)bis.BookInStoreQuantity);
            var branchQty = bookStoreId.HasValue
                ? (int?)b.BookInStores
                    .FirstOrDefault(bis => bis.BookStoreId == bookStoreId.Value)
                    ?.BookInStoreQuantity
                : null;

            return new BookListItemDto(
                b.BookId,
                b.BookTitle,
                b.BookAuthors.Select(ba => ba.Author.AuthorFullName),
                b.BookPrice,
                b.ISBN,
                totalQty,
                branchQty
            );
        });
    }

    public async Task<BookDetailDto?> GetBookByIdAsync(int bookId, int? bookStoreId = null)
    {
        var book = await _db.Books
            .Include(b => b.BookAuthors).ThenInclude(ba => ba.Author)
            .Include(b => b.Publisher)
            .Include(b => b.Language)
            .Include(b => b.CoverType)
            .Include(b => b.Subject)
            .Include(b => b.BookType)
            .Include(b => b.Genre)
            .Include(b => b.BookInStores).ThenInclude(bis => bis.BookStore)
            .FirstOrDefaultAsync(b => b.BookId == bookId);

        if (book is null) return null;

        return new BookDetailDto(
            book.BookId,
            book.BookTitle,
            book.BookPrice,
            book.ISBN,
            book.Publisher?.PublisherName,
            book.Language?.LanguageName,
            book.CoverType?.CoverTypeName,
            book.BookPublishYear,
            book.Subject?.SubjectName,
            book.BookPageCount,
            book.BookHeight,
            book.BookWidth,
            book.BookDepth,
            book.BookType?.BookTypeName,
            book.Genre?.GenreName,
            book.BookHasIllustrations,
            book.BookAuthors.Select(ba => ba.Author.AuthorFullName),
            book.BookInStores.Sum(bis => (int)bis.BookInStoreQuantity),
            book.BookInStores
                .Where(bis => bis.BookStore != null)
                .Select(bis => new BookStockByStoreDto(
                    bis.BookStoreId ?? 0,
                    bis.BookStore!.BookStoreName,
                    bis.BookInStoreQuantity))
        );
    }

    public async Task<IEnumerable<LowStockBranchDto>> GetLowStockAsync(LowStockQueryParams q)
    {
        var stores = await _db.BookStores
            .Include(bs => bs.BookInStores)
                .ThenInclude(bis => bis.Book)
                    .ThenInclude(b => b!.BookAuthors)
                        .ThenInclude(ba => ba.Author)
            .ToListAsync();

        var result = stores
            .Select(store =>
            {
                var criticalBooks = store.BookInStores
                    .Where(bis => bis.BookInStoreQuantity <= q.CriticalThreshold && bis.Book != null)
                    .Select(bis =>
                    {
                        var book = bis.Book!;
                        if (!string.IsNullOrWhiteSpace(q.Search) &&
                            !book.BookTitle.Contains(q.Search, StringComparison.OrdinalIgnoreCase))
                            return null;

                        return new LowStockBookDto(
                            book.BookId,
                            bis.BookInStoreId,
                            book.BookTitle,
                            book.BookAuthors.Select(ba => ba.Author.AuthorFullName),
                            book.ISBN,
                            book.BookPrice,
                            bis.BookInStoreQuantity
                        );
                    })
                    .Where(x => x != null)
                    .Cast<LowStockBookDto>()
                    .ToList();

                if (criticalBooks.Count == 0) return null;

                return new LowStockBranchDto(
                    store.BookStoreId,
                    store.BookStoreCode,
                    store.BookStoreName,
                    store.BookStoreAddress,
                    criticalBooks
                );
            })
            .Where(x => x != null)
            .Cast<LowStockBranchDto>();

        return result;
    }
}

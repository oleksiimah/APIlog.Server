using APIlog.Server.DTOs.Books;
using APIlog.Server.Infrastructure.Data;
using APIlog.Server.Models;
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

        query = q.SortBy?.ToLower() switch
        {
            "bookprice"       => q.SortOrder == "desc" ? query.OrderByDescending(b => b.BookPrice)       : query.OrderBy(b => b.BookPrice),
            "bookpublishyear" => q.SortOrder == "desc" ? query.OrderByDescending(b => b.BookPublishYear) : query.OrderBy(b => b.BookPublishYear),
            "bookpagecount"   => q.SortOrder == "desc" ? query.OrderByDescending(b => b.BookPageCount)   : query.OrderBy(b => b.BookPageCount),
            "bookheight"      => q.SortOrder == "desc" ? query.OrderByDescending(b => b.BookHeight)      : query.OrderBy(b => b.BookHeight),
            "bookwidth"       => q.SortOrder == "desc" ? query.OrderByDescending(b => b.BookWidth)       : query.OrderBy(b => b.BookWidth),
            "bookdepth"       => q.SortOrder == "desc" ? query.OrderByDescending(b => b.BookDepth)       : query.OrderBy(b => b.BookDepth),
            "bookstores"      => q.SortOrder == "desc"
                ? query.OrderByDescending(b => b.BookInStores.Sum(bis => (int)bis.BookInStoreQuantity))
                : query.OrderBy(b => b.BookInStores.Sum(bis => (int)bis.BookInStoreQuantity)),
            "publisher"       => q.SortOrder == "desc" ? query.OrderByDescending(b => b.Publisher!.PublisherName) : query.OrderBy(b => b.Publisher!.PublisherName),
            "language"        => q.SortOrder == "desc" ? query.OrderByDescending(b => b.Language!.LanguageName)   : query.OrderBy(b => b.Language!.LanguageName),
            "covertype"       => q.SortOrder == "desc" ? query.OrderByDescending(b => b.CoverType!.CoverTypeName) : query.OrderBy(b => b.CoverType!.CoverTypeName),
            "subject"         => q.SortOrder == "desc" ? query.OrderByDescending(b => b.Subject!.SubjectName)     : query.OrderBy(b => b.Subject!.SubjectName),
            "booktype"        => q.SortOrder == "desc" ? query.OrderByDescending(b => b.BookType!.BookTypeName)   : query.OrderBy(b => b.BookType!.BookTypeName),
            "genre"           => q.SortOrder == "desc" ? query.OrderByDescending(b => b.Genre!.GenreName)         : query.OrderBy(b => b.Genre!.GenreName),
            _                 => q.SortOrder == "desc" ? query.OrderByDescending(b => b.BookTitle)                : query.OrderBy(b => b.BookTitle),
        };

        var books = await query.ToListAsync();

        if (q.SortBy?.ToLower() == "authors")
        {
            books = q.SortOrder == "desc"
                ? books.OrderByDescending(b => string.Join(", ",
                    b.BookAuthors.OrderBy(ba => ba.Author.AuthorFullName)
                                 .Select(ba => ba.Author.AuthorFullName))).ToList()
                : books.OrderBy(b => string.Join(", ",
                    b.BookAuthors.OrderBy(ba => ba.Author.AuthorFullName)
                                 .Select(ba => ba.Author.AuthorFullName))).ToList();
        }

        return books.Select(b =>
        {
            var totalQty = b.BookInStores.Sum(bis => (int)bis.BookInStoreQuantity);
            var branchRecord = bookStoreId.HasValue
                ? b.BookInStores.FirstOrDefault(bis => bis.BookStoreId == bookStoreId.Value)
                : null;
            var branchQty = branchRecord is not null ? (int?)branchRecord.BookInStoreQuantity : null;
            var bookInStoreId = branchRecord?.BookInStoreId;

            return new BookListItemDto(
                b.BookId,
                b.BookTitle,
                b.BookAuthors.OrderBy(ba => ba.Author.AuthorFullName).Select(ba => ba.Author.AuthorFullName),
                b.BookPrice,
                b.ISBN,
                totalQty,
                branchQty,
                bookInStoreId
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

        return BuildDetailDto(book);
    }

    public async Task<BookDetailDto> CreateBookAsync(CreateBookDto dto)
    {
        var book = new Book
        {
            BookTitle = dto.BookTitle,
            BookPrice = dto.BookPrice,
            ISBN = dto.ISBN,
            PublisherId = dto.PublisherId,
            LanguageId = dto.LanguageId,
            CoverTypeId = dto.CoverTypeId,
            BookPublishYear = dto.BookPublishYear,
            SubjectId = dto.SubjectId,
            BookPageCount = dto.BookPageCount,
            BookHeight = dto.BookHeight,
            BookWidth = dto.BookWidth,
            BookDepth = dto.BookDepth,
            BookTypeId = dto.BookTypeId,
            BookGenreId = dto.BookGenreId,
            BookHasIllustrations = dto.BookHasIllustrations,
        };

        _db.Books.Add(book);
        await _db.SaveChangesAsync();

        if (dto.AuthorIds?.Count > 0)
        {
            foreach (var authorId in dto.AuthorIds)
                _db.BooksAuthors.Add(new BookAuthor { BookId = book.BookId, AuthorId = authorId });
            await _db.SaveChangesAsync();
        }

        return (await GetBookByIdAsync(book.BookId))!;
    }

    public async Task<BookDetailDto?> UpdateBookAsync(int bookId, UpdateBookDto dto)
    {
        var book = await _db.Books
            .Include(b => b.BookAuthors)
            .FirstOrDefaultAsync(b => b.BookId == bookId);

        if (book is null) return null;

        book.BookTitle = dto.BookTitle;
        book.BookPrice = dto.BookPrice;
        book.ISBN = dto.ISBN;
        book.PublisherId = dto.PublisherId;
        book.LanguageId = dto.LanguageId;
        book.CoverTypeId = dto.CoverTypeId;
        book.BookPublishYear = dto.BookPublishYear;
        book.SubjectId = dto.SubjectId;
        book.BookPageCount = dto.BookPageCount;
        book.BookHeight = dto.BookHeight;
        book.BookWidth = dto.BookWidth;
        book.BookDepth = dto.BookDepth;
        book.BookTypeId = dto.BookTypeId;
        book.BookGenreId = dto.BookGenreId;
        book.BookHasIllustrations = dto.BookHasIllustrations;

        _db.BooksAuthors.RemoveRange(book.BookAuthors);
        if (dto.AuthorIds?.Count > 0)
            foreach (var authorId in dto.AuthorIds)
                _db.BooksAuthors.Add(new BookAuthor { BookId = book.BookId, AuthorId = authorId });

        await _db.SaveChangesAsync();
        return await GetBookByIdAsync(book.BookId);
    }

    private BookDetailDto BuildDetailDto(Book book) => new BookDetailDto(
        book.BookId,
        book.BookTitle,
        book.BookPrice,
        book.ISBN,
        book.PublisherId,
        book.Publisher?.PublisherName,
        book.LanguageId,
        book.Language?.LanguageName,
        book.CoverTypeId,
        book.CoverType?.CoverTypeName,
        book.BookPublishYear,
        book.SubjectId,
        book.Subject?.SubjectName,
        book.BookPageCount,
        book.BookHeight,
        book.BookWidth,
        book.BookDepth,
        book.BookTypeId,
        book.BookType?.BookTypeName,
        book.BookGenreId,
        book.Genre?.GenreName,
        book.BookHasIllustrations,
        book.BookAuthors.OrderBy(ba => ba.Author.AuthorFullName).Select(ba => ba.AuthorId),
        book.BookAuthors.OrderBy(ba => ba.Author.AuthorFullName).Select(ba => ba.Author.AuthorFullName),
        book.BookInStores.Sum(bis => (int)bis.BookInStoreQuantity),
        book.BookInStores
            .Where(bis => bis.BookStore != null)
            .Select(bis => new BookStockByStoreDto(
                bis.BookStoreId ?? 0,
                bis.BookStore!.BookStoreName,
                bis.BookInStoreQuantity))
    );

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

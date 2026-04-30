namespace APIlog.Server.DTOs.Books;

public record BookListItemDto(
    int BookId,
    string BookTitle,
    IEnumerable<string> Authors,
    decimal BookPrice,
    string? ISBN,
    int TotalNetworkQuantity,
    int? BranchQuantity,
    int? BookInStoreId
);

public record BookDetailDto(
    int BookId,
    string BookTitle,
    decimal BookPrice,
    string? ISBN,
    int? PublisherId,
    string? PublisherName,
    int? LanguageId,
    string? LanguageName,
    int? CoverTypeId,
    string? CoverTypeName,
    short? BookPublishYear,
    int? SubjectId,
    string? SubjectName,
    short? BookPageCount,
    decimal? BookHeight,
    decimal? BookWidth,
    decimal? BookDepth,
    int? BookTypeId,
    string? BookTypeName,
    int? BookGenreId,
    string? GenreName,
    bool? BookHasIllustrations,
    IEnumerable<int> AuthorIds,
    IEnumerable<string> Authors,
    int TotalNetworkQuantity,
    IEnumerable<BookStockByStoreDto> StockByStore,
    bool CanDelete = false
);

public record BookStockByStoreDto(
    int BookStoreId,
    string BookStoreName,
    int Quantity
);

public record CreateBookDto(
    string BookTitle,
    decimal BookPrice,
    string? ISBN,
    int? PublisherId,
    int? LanguageId,
    int? CoverTypeId,
    short? BookPublishYear,
    int? SubjectId,
    short? BookPageCount,
    decimal? BookHeight,
    decimal? BookWidth,
    decimal? BookDepth,
    int? BookTypeId,
    int? BookGenreId,
    bool BookHasIllustrations,
    List<int> AuthorIds
);

public record UpdateBookDto(
    string BookTitle,
    decimal BookPrice,
    string? ISBN,
    int? PublisherId,
    int? LanguageId,
    int? CoverTypeId,
    short? BookPublishYear,
    int? SubjectId,
    short? BookPageCount,
    decimal? BookHeight,
    decimal? BookWidth,
    decimal? BookDepth,
    int? BookTypeId,
    int? BookGenreId,
    bool BookHasIllustrations,
    List<int> AuthorIds
);

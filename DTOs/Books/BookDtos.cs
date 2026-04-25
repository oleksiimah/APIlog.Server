namespace APIlog.Server.DTOs.Books;

public record BookListItemDto(
    int BookId,
    string BookTitle,
    IEnumerable<string> Authors,
    decimal BookPrice,
    string? ISBN,
    int TotalNetworkQuantity,
    int? BranchQuantity
);

public record BookDetailDto(
    int BookId,
    string BookTitle,
    decimal BookPrice,
    string? ISBN,
    string? Publisher,
    string? Language,
    string? CoverType,
    short? BookPublishYear,
    string? Subject,
    short? BookPageCount,
    decimal? BookHeight,
    decimal? BookWidth,
    decimal? BookDepth,
    string? BookType,
    string? Genre,
    bool? BookHasIllustrations,
    IEnumerable<string> Authors,
    int TotalNetworkQuantity,
    IEnumerable<BookStockByStoreDto> StockByStore
);

public record BookStockByStoreDto(
    int BookStoreId,
    string BookStoreName,
    int Quantity
);

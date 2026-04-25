namespace APIlog.Server.DTOs.Sales;

public record SaleReceiptListItemDto(
    int SalesReceiptId,
    string SalesReceiptNumber,
    DateTime? SalesReceiptDateTime,
    string? CustomerFullName,
    string EmployeeFullName,
    decimal? SalesReceiptTotalAmount,
    string PaymentStatus,
    int ItemCount,
    int TotalQuantity,
    IEnumerable<SaleItemSummaryDto> Items
);

public record SaleItemSummaryDto(
    int BookId,
    string BookTitle,
    IEnumerable<string> Authors,
    string? ISBN,
    decimal Price,
    int Quantity,
    decimal LineTotal
);

public record SaleReceiptDetailDto(
    int SalesReceiptId,
    string SalesReceiptNumber,
    DateTime? SalesReceiptDateTime,
    int? CustomerId,
    string? CustomerFullName,
    string? CustomerPhoneNumber,
    string? CustomerEmail,
    int EmployeeId,
    string EmployeeFullName,
    int BookStoreId,
    string BookStoreName,
    decimal? SalesReceiptTotalAmount,
    string PaymentStatus,
    IEnumerable<SaleItemDetailDto> Items,
    IEnumerable<PaymentSummaryDto> Payments
);

public record SaleItemDetailDto(
    int SaleReceiptItemId,
    int BookInStoreId,
    int BookId,
    string BookTitle,
    IEnumerable<string> Authors,
    string? ISBN,
    decimal Price,
    int Quantity,
    decimal LineTotal
);

public record PaymentSummaryDto(
    int PaymentId,
    string? PaymentNumber,
    DateTime? PaymentDateTime,
    decimal PaymentAmount,
    string? PaymentMethodName
);

public record CreateSaleReceiptDto(
    int? CustomerId,
    string? CustomerFullName,
    string? CustomerPhoneNumber,
    string? CustomerEmail,
    IEnumerable<CreateSaleItemDto> Items
);

public record CreateSaleItemDto(
    int BookInStoreId,
    int Quantity
);

public record UpdateSaleReceiptDto(
    int? CustomerId,
    string? CustomerFullName,
    string? CustomerPhoneNumber,
    string? CustomerEmail,
    IEnumerable<UpdateSaleItemDto> Items
);

public record UpdateSaleItemDto(
    int SaleReceiptItemId,
    int Quantity
);

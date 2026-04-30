namespace APIlog.Server.DTOs.Supplies;

public record SupplyReceiptDetailDto(
    int SupplyReceiptId,
    string SupplyReceiptNumber,
    DateTime? SupplyReceiptDateTime,
    int EmployeeId,
    string EmployeeFullName,
    decimal? SupplyReceiptTotalAmount,
    IEnumerable<SupplyItemDetailDto> Items
);

public record SupplyItemDetailDto(
    int SupplyReceiptItemId,
    int PurchaseReceiptItemId,
    int BookId,
    string BookTitle,
    IEnumerable<string> Authors,
    string? ISBN,
    decimal PricePerUnit,
    int Quantity,
    decimal LineTotal,
    int BookStoreId,
    string BookStoreName
);

public record CreateSupplyReceiptDto(
    int PurchaseReceiptId,
    IEnumerable<CreateSupplyItemDto> Items
);

public record CreateSupplyItemDto(
    int PurchaseReceiptItemId,
    int BookStoreId,
    int Quantity
);

public record UpdateSupplyReceiptDto(
    IEnumerable<UpdateSupplyItemDto> Items
);

public record UpdateSupplyItemDto(
    int PurchaseReceiptItemId,
    int BookStoreId,
    int Quantity
);

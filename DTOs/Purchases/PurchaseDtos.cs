namespace APIlog.Server.DTOs.Purchases;

public record PurchaseReceiptListItemDto(
    int PurchaseReceiptId,
    string PurchaseReceiptNumber,
    DateTime? PurchaseReceiptDateTime,
    DateTime? PurchaseReceiptSupplyDateTime,
    int? EmployeeId,
    string EmployeeFullName,
    string? EmployeePersonnelNumber,
    int? SupplierId,
    string? SupplierName,
    string? SupplierAddress,
    int StatusId,
    string StatusName,
    decimal? PurchaseReceiptTotalAmount,
    int ItemCount,
    int TotalQuantity,
    IEnumerable<PurchaseItemSummaryDto> Items
);

public record PurchaseItemSummaryDto(
    int PurchaseReceiptItemId,
    int BookId,
    string BookTitle,
    IEnumerable<string> Authors,
    string? ISBN,
    decimal BookPricePerUnit,
    int OrderedQuantity,
    int SuppliedQuantity
);

public record PurchaseReceiptDetailDto(
    int PurchaseReceiptId,
    string PurchaseReceiptNumber,
    DateTime? PurchaseReceiptDateTime,
    DateTime? PurchaseReceiptSupplyDateTime,
    int EmployeeId,
    string EmployeeFullName,
    string? EmployeePersonnelNumber,
    int? SupplierId,
    string? SupplierName,
    string? SupplierAddress,
    string StatusName,
    decimal? PurchaseReceiptTotalAmount,
    IEnumerable<PurchaseItemDetailDto> Items,
    IEnumerable<SupplySummaryDto> Supplies
);

public record PurchaseItemDetailDto(
    int PurchaseReceiptItemId,
    int BookId,
    string BookTitle,
    IEnumerable<string> Authors,
    string? ISBN,
    decimal BookPricePerUnit,
    int OrderedQuantity,
    int SuppliedQuantity,
    decimal LineTotal
);

public record SupplySummaryDto(
    int SupplyReceiptId,
    string SupplyReceiptNumber,
    DateTime? SupplyReceiptDateTime,
    int EmployeeId,
    string EmployeeFullName,
    string? EmployeePersonnelNumber,
    decimal? SupplyReceiptTotalAmount,
    IEnumerable<SupplyItemDto> Items
);

public record SupplyItemDto(
    int SupplyReceiptItemId,
    int PurchaseOrderItemId,
    int? BookStoreId,
    string? BookStoreName,
    int SupplyReceiptItemQuantity
);

public record CreatePurchaseReceiptDto(
    DateTime PurchaseReceiptSupplyDateTime,
    int? SupplierId,
    string? SupplierName,
    string? SupplierAddress,
    IEnumerable<CreatePurchaseItemDto> Items
);

public record CreatePurchaseItemDto(
    int BookId,
    int Quantity,
    decimal PricePerUnit
);

public record UpdatePurchaseReceiptDto(
    DateTime PurchaseReceiptSupplyDateTime,
    int? SupplierId,
    string? SupplierName,
    string? SupplierAddress,
    IEnumerable<UpdatePurchaseItemDto> Items
);

public record UpdatePurchaseItemDto(
    int PurchaseReceiptItemId,
    int Quantity,
    decimal PricePerUnit
);

namespace APIlog.Server.DTOs.Branches;

public record BranchListItemDto(
    int BookStoreId,
    string BookStoreCode,
    string BookStoreName,
    string BookStoreAddress,
    int EmployeeCount,
    int TotalBookTitles,
    int TotalBooksQuantity
);

public record CreateBranchDto(
    string BookStoreCode,
    string BookStoreName,
    string BookStoreAddress
);

public record UpdateBranchDto(
    string BookStoreCode,
    string BookStoreName,
    string BookStoreAddress
);

namespace APIlog.Server.DTOs.Employees;

public record EmployeeDto(
    int EmployeeId,
    string EmployeeFullName,
    string? EmployeePersonnelNumber,
    int? PostId,
    string? PostName,
    int? BookStoreId,
    string? BookStoreName,
    string? Email
);

public record CreateEmployeeDto(
    string FullName,
    string PersonnelNumber,
    int PostId,
    int BookStoreId,
    string Email,
    string Password
);

public record UpdateEmployeeDto(
    string FullName,
    string PersonnelNumber,
    int PostId,
    int BookStoreId,
    string? Email,
    string? Password
);

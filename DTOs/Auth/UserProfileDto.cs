namespace APIlog.Server.DTOs.Auth;

public record UserProfileDto(
    int EmployeeId,
    string FullName,
    string PersonnelNumber,
    string PostName,
    string? BookStoreName,
    int? BookStoreId,
    string Email,
    string Role
);

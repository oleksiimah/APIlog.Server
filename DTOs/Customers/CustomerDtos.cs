namespace APIlog.Server.DTOs.Customers;

public record CustomerDto(
    int CustomerId,
    string CustomerFullName,
    string? CustomerPhoneNumber,
    string? CustomerEmail
);

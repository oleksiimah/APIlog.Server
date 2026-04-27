using APIlog.Server.DTOs.Customers;

namespace APIlog.Server.Services;

public interface ICustomersService
{
    Task<IEnumerable<CustomerDto>> SearchCustomersAsync(string? phone, string? email, string? name);
    Task<CustomerDto> GetOrCreateCustomerAsync(string fullName, string? phone, string? email);
}

using APIlog.Server.DTOs.Customers;
using APIlog.Server.Infrastructure.Data;
using APIlog.Server.Models;
using Microsoft.EntityFrameworkCore;

namespace APIlog.Server.Services;

public class CustomersService : ICustomersService
{
    private readonly BookstoreDbContext _db;

    public CustomersService(BookstoreDbContext db)
    {
        _db = db;
    }

    public async Task<IEnumerable<CustomerDto>> SearchCustomersAsync(string? phone, string? email)
    {
        if (string.IsNullOrWhiteSpace(phone) && string.IsNullOrWhiteSpace(email))
            return [];

        var query = _db.Customers.AsQueryable();

        if (!string.IsNullOrWhiteSpace(phone))
            query = query.Where(c => c.CustomerPhoneNumber != null &&
                c.CustomerPhoneNumber.Contains(phone));

        if (!string.IsNullOrWhiteSpace(email))
            query = query.Where(c => c.CustomerEmail != null &&
                c.CustomerEmail.Contains(email));

        var customers = await query.ToListAsync();

        return customers.Select(c => new CustomerDto(
            c.CustomerId,
            c.CustomerFullName,
            c.CustomerPhoneNumber,
            c.CustomerEmail));
    }

    public async Task<CustomerDto> GetOrCreateCustomerAsync(string fullName, string? phone, string? email)
    {
        Customer? existing = null;

        if (!string.IsNullOrWhiteSpace(phone))
            existing = await _db.Customers
                .FirstOrDefaultAsync(c => c.CustomerPhoneNumber == phone);

        if (existing is null && !string.IsNullOrWhiteSpace(email))
            existing = await _db.Customers
                .FirstOrDefaultAsync(c => c.CustomerEmail == email);

        if (existing is not null)
            return new CustomerDto(existing.CustomerId, existing.CustomerFullName,
                existing.CustomerPhoneNumber, existing.CustomerEmail);

        var newCustomer = new Customer
        {
            CustomerFullName = fullName,
            CustomerPhoneNumber = phone,
            CustomerEmail = email
        };

        _db.Customers.Add(newCustomer);
        await _db.SaveChangesAsync();

        return new CustomerDto(newCustomer.CustomerId, newCustomer.CustomerFullName,
            newCustomer.CustomerPhoneNumber, newCustomer.CustomerEmail);
    }
}

using APIlog.Server.DTOs.Auth;
using APIlog.Server.Infrastructure.Data;
using FirebaseAdmin.Auth;
using Microsoft.EntityFrameworkCore;

namespace APIlog.Server.Services;

public class AuthService : IAuthService
{
    private readonly BookstoreDbContext _db;

    public AuthService(BookstoreDbContext db)
    {
        _db = db;
    }

    public async Task<UserProfileDto?> GetProfileAsync(int employeeId, string role)
    {
        var employee = await _db.Employees
            .Include(e => e.Post)
            .Include(e => e.BookStore)
            .FirstOrDefaultAsync(e => e.EmployeeId == employeeId);

        if (employee is null) return null;

        string email = string.Empty;
        try
        {
            var firebaseUser = await FindFirebaseUserByPersonnelNumberAsync(
                employee.EmployeePersonnelNumber ?? string.Empty);
            email = firebaseUser?.Email ?? string.Empty;
        }
        catch { }

        return new UserProfileDto(
            employee.EmployeeId,
            employee.EmployeeFullName,
            employee.EmployeePersonnelNumber ?? string.Empty,
            employee.Post?.PostName ?? string.Empty,
            employee.BookStore?.BookStoreName,
            employee.BookStoreId,
            email,
            role
        );
    }

    private static async Task<UserRecord?> FindFirebaseUserByPersonnelNumberAsync(string personnelNumber)
    {
        var pagedEnumerable = FirebaseAuth.DefaultInstance.ListUsersAsync(null);
        await foreach (var user in pagedEnumerable)
        {
            if (user.CustomClaims != null &&
                user.CustomClaims.TryGetValue("employee_id", out var value) &&
                value?.ToString() == personnelNumber)
            {
                return user;
            }
        }
        return null;
    }
}

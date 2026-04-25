using APIlog.Server.DTOs.Employees;
using APIlog.Server.Infrastructure.Data;
using APIlog.Server.Models;
using FirebaseAdmin.Auth;
using Microsoft.EntityFrameworkCore;

namespace APIlog.Server.Services;

public class EmployeesService : IEmployeesService
{
    private readonly BookstoreDbContext _db;

    public EmployeesService(BookstoreDbContext db)
    {
        _db = db;
    }

    public async Task<IEnumerable<EmployeeDto>> GetEmployeesAsync(int? bookStoreId = null)
    {
        var query = _db.Employees
            .Include(e => e.Post)
            .Include(e => e.BookStore)
            .AsQueryable();

        if (bookStoreId.HasValue)
            query = query.Where(e => e.BookStoreId == bookStoreId.Value);

        var employees = await query.OrderBy(e => e.EmployeeFullName).ToListAsync();

        return employees.Select(e => new EmployeeDto(
            e.EmployeeId,
            e.EmployeeFullName,
            e.EmployeePersonnelNumber,
            e.EmployeePostId,
            e.Post?.PostName,
            e.BookStoreId,
            e.BookStore?.BookStoreName,
            null
        ));
    }

    public async Task<EmployeeDto?> GetEmployeeByIdAsync(int id)
    {
        var employee = await _db.Employees
            .Include(e => e.Post)
            .Include(e => e.BookStore)
            .FirstOrDefaultAsync(e => e.EmployeeId == id);

        if (employee is null) return null;

        string? email = null;
        if (employee.EmployeePersonnelNumber is not null)
        {
            var firebaseUser = await FindFirebaseUserByPersonnelNumberAsync(
                employee.EmployeePersonnelNumber);
            email = firebaseUser?.Email;
        }

        return new EmployeeDto(
            employee.EmployeeId,
            employee.EmployeeFullName,
            employee.EmployeePersonnelNumber,
            employee.EmployeePostId,
            employee.Post?.PostName,
            employee.BookStoreId,
            employee.BookStore?.BookStoreName,
            email
        );
    }

    public async Task<EmployeeDto> CreateEmployeeAsync(CreateEmployeeDto dto)
    {
        var employee = new Employee
        {
            EmployeeFullName = dto.FullName,
            EmployeePersonnelNumber = dto.PersonnelNumber,
            EmployeePostId = dto.PostId,
            BookStoreId = dto.BookStoreId
        };

        _db.Employees.Add(employee);
        await _db.SaveChangesAsync();

        var userArgs = new UserRecordArgs
        {
            Email = dto.Email,
            Password = dto.Password,
            DisplayName = dto.FullName,
            EmailVerified = true
        };

        var firebaseUser = await FirebaseAuth.DefaultInstance.CreateUserAsync(userArgs);

        await FirebaseAuth.DefaultInstance.SetCustomUserClaimsAsync(
            firebaseUser.Uid,
            new Dictionary<string, object> { ["employee_id"] = dto.PersonnelNumber });

        await _db.SaveChangesAsync();

        return new EmployeeDto(
            employee.EmployeeId,
            employee.EmployeeFullName,
            employee.EmployeePersonnelNumber,
            employee.EmployeePostId,
            null,
            employee.BookStoreId,
            null,
            dto.Email
        );
    }

    public async Task<EmployeeDto> UpdateEmployeeAsync(int id, UpdateEmployeeDto dto)
    {
        var employee = await _db.Employees
            .Include(e => e.Post)
            .Include(e => e.BookStore)
            .FirstOrDefaultAsync(e => e.EmployeeId == id)
            ?? throw new KeyNotFoundException($"Employee {id} not found.");

        var oldPersonnelNumber = employee.EmployeePersonnelNumber;

        employee.EmployeeFullName = dto.FullName;
        employee.EmployeePersonnelNumber = dto.PersonnelNumber;
        employee.EmployeePostId = dto.PostId;
        employee.BookStoreId = dto.BookStoreId;

        await _db.SaveChangesAsync();

        if (oldPersonnelNumber is not null)
        {
            var firebaseUser = await FindFirebaseUserByPersonnelNumberAsync(oldPersonnelNumber);
            if (firebaseUser is not null)
            {
                var updateArgs = new UserRecordArgs { Uid = firebaseUser.Uid };

                if (!string.IsNullOrWhiteSpace(dto.Email))
                    updateArgs.Email = dto.Email;
                if (!string.IsNullOrWhiteSpace(dto.Password))
                    updateArgs.Password = dto.Password;

                updateArgs.DisplayName = dto.FullName;

                await FirebaseAuth.DefaultInstance.UpdateUserAsync(updateArgs);

                if (oldPersonnelNumber != dto.PersonnelNumber)
                {
                    await FirebaseAuth.DefaultInstance.SetCustomUserClaimsAsync(
                        firebaseUser.Uid,
                        new Dictionary<string, object> { ["employee_id"] = dto.PersonnelNumber });
                }
            }
        }

        return new EmployeeDto(
            employee.EmployeeId,
            employee.EmployeeFullName,
            employee.EmployeePersonnelNumber,
            employee.EmployeePostId,
            employee.Post?.PostName,
            employee.BookStoreId,
            employee.BookStore?.BookStoreName,
            dto.Email
        );
    }

    public async Task DeleteEmployeeAsync(int id)
    {
        var employee = await _db.Employees.FindAsync(id)
            ?? throw new KeyNotFoundException($"Employee {id} not found.");

        if (employee.EmployeePersonnelNumber is not null)
        {
            var firebaseUser = await FindFirebaseUserByPersonnelNumberAsync(
                employee.EmployeePersonnelNumber);
            if (firebaseUser is not null)
                await FirebaseAuth.DefaultInstance.DeleteUserAsync(firebaseUser.Uid);
        }

        _db.Employees.Remove(employee);
        await _db.SaveChangesAsync();
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

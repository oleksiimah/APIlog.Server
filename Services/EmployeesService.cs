using APIlog.Server.DTOs.Employees;
using APIlog.Server.Infrastructure.Data;
using APIlog.Server.Models;
using FirebaseAdmin.Auth;
using Google.Cloud.Firestore;
using Microsoft.EntityFrameworkCore;

namespace APIlog.Server.Services;

public class EmployeesService : IEmployeesService
{
    private readonly BookstoreDbContext _db;
    private readonly FirestoreDb _firestore;
    private const string CredCollection = "employeeCredentials";

    public EmployeesService(BookstoreDbContext db, FirestoreDb firestore)
    {
        _db = db;
        _firestore = firestore;
    }

    // ── Firestore helpers ──────────────────────────────────────────────────────

    private DocumentReference CredDoc(string personnelNumber) =>
        _firestore.Collection(CredCollection).Document(personnelNumber);

    private async Task StoreCredentialsAsync(
        string personnelNumber, string uid, string email, string password)
    {
        try
        {
            await CredDoc(personnelNumber).SetAsync(new Dictionary<string, object>
            {
                ["uid"]      = uid,
                ["email"]    = email,
                ["password"] = password,
            });
        }
        catch { /* Firestore not yet enabled; credentials will be migrated on next save */ }
    }

    private async Task<(string? uid, string? email, string? password)>
        ReadCredentialsAsync(string personnelNumber)
    {
        try
        {
            var snap = await CredDoc(personnelNumber).GetSnapshotAsync();
            if (!snap.Exists) return (null, null, null);
            snap.TryGetValue<string>("uid",      out var uid);
            snap.TryGetValue<string>("email",    out var email);
            snap.TryGetValue<string>("password", out var password);
            return (uid, email, password);
        }
        catch
        {
            return (null, null, null);
        }
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

        var empInSales = await _db.SalesReceipts
            .Where(sr => sr.EmployeeId != null)
            .Select(sr => sr.EmployeeId!.Value)
            .Distinct().ToHashSetAsync();
        var empInPurchases = await _db.PurchaseReceipts
            .Where(pr => pr.EmployeeId != null)
            .Select(pr => pr.EmployeeId!.Value)
            .Distinct().ToHashSetAsync();
        var empInSupplies = await _db.SupplyReceipts
            .Where(sr => sr.EmployeeId != null)
            .Select(sr => sr.EmployeeId!.Value)
            .Distinct().ToHashSetAsync();

        return employees.Select(e => new EmployeeDto(
            e.EmployeeId,
            e.EmployeeFullName,
            e.EmployeePersonnelNumber,
            e.EmployeePostId,
            e.Post?.PostName,
            e.BookStoreId,
            e.BookStore?.BookStoreName,
            null,
            CanDelete: !empInSales.Contains(e.EmployeeId)
                    && !empInPurchases.Contains(e.EmployeeId)
                    && !empInSupplies.Contains(e.EmployeeId)
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
        string? password = null;
        if (employee.EmployeePersonnelNumber is not null)
        {
            var (uid, fsEmail, fsPassword) =
                await ReadCredentialsAsync(employee.EmployeePersonnelNumber);

            if (uid is not null)
            {
                // Fast path: UID known — single Firebase call
                email    = fsEmail;
                password = fsPassword;
            }
            else
            {
                // Fallback for employees created before Firestore migration
                var firebaseUser = await FindFirebaseUserByPersonnelNumberAsync(
                    employee.EmployeePersonnelNumber);
                email = firebaseUser?.Email;
                // Auto-seed Firestore so the next request is fast (password unknown here)
                if (firebaseUser is not null)
                    await StoreCredentialsAsync(
                        employee.EmployeePersonnelNumber,
                        firebaseUser.Uid,
                        firebaseUser.Email ?? string.Empty,
                        string.Empty);
            }
        }

        bool canDelete =
            !await _db.SalesReceipts.AnyAsync(sr => sr.EmployeeId == employee.EmployeeId) &&
            !await _db.PurchaseReceipts.AnyAsync(pr => pr.EmployeeId == employee.EmployeeId) &&
            !await _db.SupplyReceipts.AnyAsync(sr => sr.EmployeeId == employee.EmployeeId);

        return new EmployeeDto(
            employee.EmployeeId,
            employee.EmployeeFullName,
            employee.EmployeePersonnelNumber,
            employee.EmployeePostId,
            employee.Post?.PostName,
            employee.BookStoreId,
            employee.BookStore?.BookStoreName,
            email,
            password,
            CanDelete: canDelete
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

        await StoreCredentialsAsync(dto.PersonnelNumber, firebaseUser.Uid, dto.Email, dto.Password);

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
            var (fsUid, fsOldEmail, fsOldPassword) = await ReadCredentialsAsync(oldPersonnelNumber);

            // Fallback to slow enumeration if this employee pre-dates Firestore migration
            string? resolvedUid = fsUid;
            if (resolvedUid is null)
            {
                var fbUser = await FindFirebaseUserByPersonnelNumberAsync(oldPersonnelNumber);
                resolvedUid = fbUser?.Uid;
            }

            if (resolvedUid is not null)
            {
                var updateArgs = new UserRecordArgs { Uid = resolvedUid };
                if (!string.IsNullOrWhiteSpace(dto.Email))    updateArgs.Email    = dto.Email;
                if (!string.IsNullOrWhiteSpace(dto.Password)) updateArgs.Password = dto.Password;
                updateArgs.DisplayName = dto.FullName;
                await FirebaseAuth.DefaultInstance.UpdateUserAsync(updateArgs);

                if (oldPersonnelNumber != dto.PersonnelNumber)
                {
                    await FirebaseAuth.DefaultInstance.SetCustomUserClaimsAsync(
                        resolvedUid,
                        new Dictionary<string, object> { ["employee_id"] = dto.PersonnelNumber });
                    await CredDoc(oldPersonnelNumber).DeleteAsync();
                }

                var storedEmail    = !string.IsNullOrWhiteSpace(dto.Email)    ? dto.Email    : (fsOldEmail    ?? string.Empty);
                var storedPassword = !string.IsNullOrWhiteSpace(dto.Password) ? dto.Password : (fsOldPassword ?? string.Empty);
                await StoreCredentialsAsync(dto.PersonnelNumber, resolvedUid, storedEmail, storedPassword);
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

        if (await _db.SalesReceipts.AnyAsync(sr => sr.EmployeeId == id))
            throw new InvalidOperationException("Працівник присутній у чеках продажів і не може бути видалений.");

        if (await _db.PurchaseReceipts.AnyAsync(pr => pr.EmployeeId == id))
            throw new InvalidOperationException("Працівник присутній у закупівлях і не може бути видалений.");

        if (await _db.SupplyReceipts.AnyAsync(sr => sr.EmployeeId == id))
            throw new InvalidOperationException("Працівник присутній у прийомах товару і не може бути видалений.");

        if (employee.EmployeePersonnelNumber is not null)
        {
            var (uid, _, _) = await ReadCredentialsAsync(employee.EmployeePersonnelNumber);

            // Fallback to slow enumeration if pre-dates Firestore migration
            if (uid is null)
            {
                var fbUser = await FindFirebaseUserByPersonnelNumberAsync(employee.EmployeePersonnelNumber);
                uid = fbUser?.Uid;
            }

            if (uid is not null)
                await FirebaseAuth.DefaultInstance.DeleteUserAsync(uid);

            try { await CredDoc(employee.EmployeePersonnelNumber).DeleteAsync(); } catch { }
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

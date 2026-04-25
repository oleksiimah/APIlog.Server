using System.Security.Claims;
using APIlog.Server.Infrastructure.Data;
using FirebaseAdmin.Auth;
using Microsoft.EntityFrameworkCore;

namespace APIlog.Server.Middleware;

public class FirebaseAuthMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IConfiguration _configuration;

    public FirebaseAuthMiddleware(RequestDelegate next, IConfiguration configuration)
    {
        _next = next;
        _configuration = configuration;
    }

    public async Task InvokeAsync(HttpContext context, BookstoreDbContext db)
    {
        var authHeader = context.Request.Headers.Authorization.ToString();

        if (!authHeader.StartsWith("Bearer "))
        {
            await _next(context);
            return;
        }

        var idToken = authHeader["Bearer ".Length..];

        FirebaseToken decodedToken;
        try
        {
            decodedToken = await FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(idToken);
        }
        catch (FirebaseAuthException)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        if (!decodedToken.Claims.TryGetValue("employee_id", out var employeeIdObj))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        var personnelNumber = employeeIdObj.ToString();

        var employee = await db.Employees
            .Include(e => e.Post)
            .FirstOrDefaultAsync(e => e.EmployeePersonnelNumber == personnelNumber);

        if (employee is null)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        var role = ResolveRole(employee.Post?.PostName);

        if (role is null)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return;
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, employee.EmployeeId.ToString()),
            new(ClaimTypes.Name, employee.EmployeeFullName),
            new(ClaimTypes.Role, role),
            new("personnel_number", personnelNumber ?? string.Empty),
            new("bookstore_id", employee.BookStoreId?.ToString() ?? string.Empty),
            new("post_name", employee.Post?.PostName ?? string.Empty)
        };

        context.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Firebase"));

        await _next(context);
    }

    private string? ResolveRole(string? postName)
    {
        if (postName is null) return null;

        var mapping = _configuration.GetSection("RoleMapping");

        foreach (var role in new[] { "Admin", "Cashier", "PurchaseManager", "Storekeeper" })
        {
            if (mapping[role] == postName) return role;
        }

        return null;
    }
}

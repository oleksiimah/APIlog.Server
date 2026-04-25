using APIlog.Server.DTOs.Auth;

namespace APIlog.Server.Services;

public interface IAuthService
{
    Task<UserProfileDto?> GetProfileAsync(int employeeId, string role);
}

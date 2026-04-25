using APIlog.Server.DTOs.Employees;

namespace APIlog.Server.Services;

public interface IEmployeesService
{
    Task<IEnumerable<EmployeeDto>> GetEmployeesAsync(int? bookStoreId = null);
    Task<EmployeeDto?> GetEmployeeByIdAsync(int id);
    Task<EmployeeDto> CreateEmployeeAsync(CreateEmployeeDto dto);
    Task<EmployeeDto> UpdateEmployeeAsync(int id, UpdateEmployeeDto dto);
    Task DeleteEmployeeAsync(int id);
}

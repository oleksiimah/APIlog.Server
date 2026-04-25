using APIlog.Server.DTOs.Supplies;

namespace APIlog.Server.Services;

public interface ISuppliesService
{
    Task<SupplyReceiptDetailDto> CreateSupplyReceiptAsync(CreateSupplyReceiptDto dto, int employeeId);
    Task<SupplyReceiptDetailDto> UpdateSupplyReceiptAsync(int id, UpdateSupplyReceiptDto dto, int employeeId);
}

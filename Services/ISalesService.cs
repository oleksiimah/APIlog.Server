using APIlog.Server.DTOs.Sales;

namespace APIlog.Server.Services;

public interface ISalesService
{
    Task<IEnumerable<SaleReceiptListItemDto>> GetSaleReceiptsAsync(SalesQueryParams queryParams, int? bookStoreId = null);
    Task<SaleReceiptDetailDto?> GetSaleReceiptByIdAsync(int id);
    Task<SaleReceiptDetailDto> CreateSaleReceiptAsync(CreateSaleReceiptDto dto, int employeeId);
    Task<SaleReceiptDetailDto> UpdateSaleReceiptAsync(int id, UpdateSaleReceiptDto dto);
    Task DeleteSaleReceiptAsync(int id);
}

using APIlog.Server.DTOs.Purchases;

namespace APIlog.Server.Services;

public interface IPurchasesService
{
    Task<IEnumerable<PurchaseReceiptListItemDto>> GetPurchaseReceiptsAsync(PurchasesQueryParams queryParams);
    Task<PurchaseReceiptDetailDto?> GetPurchaseReceiptByIdAsync(int id);
    Task<PurchaseReceiptDetailDto> CreatePurchaseReceiptAsync(CreatePurchaseReceiptDto dto, int employeeId);
    Task<PurchaseReceiptDetailDto> UpdatePurchaseReceiptAsync(int id, UpdatePurchaseReceiptDto dto);
    Task CancelPurchaseReceiptAsync(int id);
    Task RestorePurchaseReceiptAsync(int id);
    Task DeletePurchaseReceiptAsync(int id);
    Task<string> GetNextReceiptNumberAsync();
}

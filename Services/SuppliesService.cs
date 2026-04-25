using APIlog.Server.DTOs.Supplies;
using APIlog.Server.Infrastructure.Data;
using APIlog.Server.Models;
using Microsoft.EntityFrameworkCore;

namespace APIlog.Server.Services;

public class SuppliesService : ISuppliesService
{
    private readonly BookstoreDbContext _db;
    private readonly IBookStoresService _bookStoresService;

    public SuppliesService(BookstoreDbContext db, IBookStoresService bookStoresService)
    {
        _db = db;
        _bookStoresService = bookStoresService;
    }

    public async Task<SupplyReceiptDetailDto> CreateSupplyReceiptAsync(
        CreateSupplyReceiptDto dto, int employeeId)
    {
        var purchaseReceipt = await _db.PurchaseReceipts
            .Include(pr => pr.PurchaseReceiptItems)
            .FirstOrDefaultAsync(pr => pr.PurchaseReceiptId == dto.PurchaseReceiptId)
            ?? throw new KeyNotFoundException($"PurchaseReceipt {dto.PurchaseReceiptId} not found.");

        var supply = new SupplyReceipt
        {
            SupplyReceiptNumber = GenerateReceiptNumber("D"),
            EmployeeId = employeeId,
            PurchaseReceiptId = dto.PurchaseReceiptId
        };

        _db.SupplyReceipts.Add(supply);
        await _db.SaveChangesAsync();

        decimal total = 0;
        foreach (var item in dto.Items)
        {
            var purchaseItem = purchaseReceipt.PurchaseReceiptItems
                .FirstOrDefault(pi => pi.PurchaseReceiptItemId == item.PurchaseReceiptItemId)
                ?? throw new InvalidOperationException(
                    $"PurchaseReceiptItem {item.PurchaseReceiptItemId} not found.");

            var bookInStore = await _bookStoresService.GetOrCreateBookInStoreAsync(
                purchaseItem.BookId ?? 0, item.BookStoreId);

            _db.SupplyReceiptItems.Add(new SupplyReceiptItem
            {
                SupplyReceiptId = supply.SupplyReceiptId,
                PurchaseReceiptItemId = item.PurchaseReceiptItemId,
                BookInStoreId = bookInStore.BookInStoreId,
                SupplyReceiptItemQuantity = (short)item.Quantity
            });

            total += purchaseItem.BookPricePerUnit * item.Quantity;
        }

        supply.SupplyReceiptTotalAmount = total;
        await _db.SaveChangesAsync();

        return MapToDetail(await LoadWithDetailsAsync(supply.SupplyReceiptId));
    }

    public async Task<SupplyReceiptDetailDto> UpdateSupplyReceiptAsync(
        int id, UpdateSupplyReceiptDto dto, int employeeId)
    {
        var supply = await _db.SupplyReceipts
            .Include(sr => sr.SupplyReceiptItems)
                .ThenInclude(si => si.PurchaseReceiptItem)
            .FirstOrDefaultAsync(sr => sr.SupplyReceiptId == id && sr.EmployeeId == employeeId)
            ?? throw new KeyNotFoundException($"SupplyReceipt {id} not found or access denied.");

        foreach (var item in dto.Items)
        {
            var existing = supply.SupplyReceiptItems
                .FirstOrDefault(si => si.SupplyReceiptItemId == item.SupplyReceiptItemId);
            if (existing is null) continue;

            var purchaseItem = existing.PurchaseReceiptItem;
            var bookInStore = await _bookStoresService.GetOrCreateBookInStoreAsync(
                purchaseItem?.BookId ?? 0, item.BookStoreId);

            existing.BookInStoreId = bookInStore.BookInStoreId;
            existing.SupplyReceiptItemQuantity = (short)item.Quantity;
        }

        supply.SupplyReceiptTotalAmount = supply.SupplyReceiptItems
            .Sum(si => (si.PurchaseReceiptItem?.BookPricePerUnit ?? 0) * si.SupplyReceiptItemQuantity);

        await _db.SaveChangesAsync();
        return MapToDetail(await LoadWithDetailsAsync(id));
    }

    private async Task<SupplyReceipt> LoadWithDetailsAsync(int id)
    {
        return await _db.SupplyReceipts
            .Include(sr => sr.Employee)
            .Include(sr => sr.SupplyReceiptItems)
                .ThenInclude(si => si.PurchaseReceiptItem)
                    .ThenInclude(pi => pi!.Book)
                        .ThenInclude(b => b!.BookAuthors)
                            .ThenInclude(ba => ba.Author)
            .Include(sr => sr.SupplyReceiptItems)
                .ThenInclude(si => si.BookInStore)
                    .ThenInclude(bis => bis!.BookStore)
            .FirstAsync(sr => sr.SupplyReceiptId == id);
    }

    private static SupplyReceiptDetailDto MapToDetail(SupplyReceipt sr)
    {
        var items = sr.SupplyReceiptItems
            .Where(si => si.PurchaseReceiptItem?.Book != null)
            .Select(si =>
            {
                var book = si.PurchaseReceiptItem!.Book!;
                var price = si.PurchaseReceiptItem.BookPricePerUnit;
                return new SupplyItemDetailDto(
                    si.SupplyReceiptItemId,
                    si.PurchaseReceiptItemId ?? 0,
                    book.BookId,
                    book.BookTitle,
                    book.BookAuthors.Select(ba => ba.Author.AuthorFullName),
                    book.ISBN,
                    price,
                    si.SupplyReceiptItemQuantity,
                    price * si.SupplyReceiptItemQuantity,
                    si.BookInStore?.BookStoreId ?? 0,
                    si.BookInStore?.BookStore?.BookStoreName ?? string.Empty
                );
            });

        return new SupplyReceiptDetailDto(
            sr.SupplyReceiptId,
            sr.SupplyReceiptNumber,
            sr.SupplyReceiptDateTime,
            sr.EmployeeId ?? 0,
            sr.Employee?.EmployeeFullName ?? string.Empty,
            sr.SupplyReceiptTotalAmount,
            items
        );
    }

    private static string GenerateReceiptNumber(string prefix)
        => prefix + Guid.NewGuid().ToString("N")[..12].ToUpper();
}

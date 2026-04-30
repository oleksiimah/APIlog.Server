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
            SupplyReceiptNumber = await GenerateReceiptNumberAsync(),
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

        // Remove items that are no longer in the incoming list
        var incomingIds = dto.Items.Select(i => i.PurchaseReceiptItemId).ToHashSet();
        var toRemove = supply.SupplyReceiptItems
            .Where(si => !incomingIds.Contains(si.PurchaseReceiptItemId ?? 0))
            .ToList();
        _db.SupplyReceiptItems.RemoveRange(toRemove);

        decimal total = 0m;
        foreach (var item in dto.Items)
        {
            var existing = supply.SupplyReceiptItems
                .FirstOrDefault(si => si.PurchaseReceiptItemId == item.PurchaseReceiptItemId);

            if (existing is not null)
            {
                var bookInStore = await _bookStoresService.GetOrCreateBookInStoreAsync(
                    existing.PurchaseReceiptItem?.BookId ?? 0, item.BookStoreId);
                existing.BookInStoreId = bookInStore.BookInStoreId;
                existing.SupplyReceiptItemQuantity = (short)item.Quantity;
                total += (existing.PurchaseReceiptItem?.BookPricePerUnit ?? 0) * item.Quantity;
            }
            else
            {
                // New item added to this supply — look up the purchase receipt item
                var purchaseItem = await _db.PurchaseReceiptItems
                    .FirstOrDefaultAsync(pi => pi.PurchaseReceiptItemId == item.PurchaseReceiptItemId);
                if (purchaseItem is null) continue;

                var bookInStore = await _bookStoresService.GetOrCreateBookInStoreAsync(
                    purchaseItem.BookId ?? 0, item.BookStoreId);
                _db.SupplyReceiptItems.Add(new SupplyReceiptItem
                {
                    SupplyReceiptId = supply.SupplyReceiptId,
                    PurchaseReceiptItemId = item.PurchaseReceiptItemId,
                    BookInStoreId = bookInStore.BookInStoreId,
                    SupplyReceiptItemQuantity = (short)item.Quantity,
                });
                total += purchaseItem.BookPricePerUnit * item.Quantity;
            }
        }

        supply.SupplyReceiptTotalAmount = total;

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

    public async Task DeleteSupplyReceiptAsync(int id, int employeeId)
    {
        var supply = await _db.SupplyReceipts
            .Include(sr => sr.SupplyReceiptItems)
            .FirstOrDefaultAsync(sr => sr.SupplyReceiptId == id && sr.EmployeeId == employeeId)
            ?? throw new KeyNotFoundException($"SupplyReceipt {id} not found or access denied.");

        var purchaseReceiptId = supply.PurchaseReceiptId;

        _db.SupplyReceiptItems.RemoveRange(supply.SupplyReceiptItems);
        _db.SupplyReceipts.Remove(supply);
        await _db.SaveChangesAsync();

        if (purchaseReceiptId.HasValue)
        {
            var purchase = await _db.PurchaseReceipts.FindAsync(purchaseReceiptId.Value);
            if (purchase?.PurchaseReceiptStatusId == 2)
            {
                purchase.PurchaseReceiptStatusId = 1;
                await _db.SaveChangesAsync();
            }
        }
    }

    private async Task<string> GenerateReceiptNumberAsync()
    {
        var prefix = "S" + DateTime.Today.ToString("yyyyMMdd");

        var last = await _db.SupplyReceipts
            .Where(sr => sr.SupplyReceiptNumber.StartsWith(prefix))
            .OrderByDescending(sr => sr.SupplyReceiptNumber)
            .Select(sr => sr.SupplyReceiptNumber)
            .FirstOrDefaultAsync();

        int next = 1;
        if (last is not null && int.TryParse(last[prefix.Length..], out var seq))
            next = seq + 1;

        return $"{prefix}{next:D4}";
    }
}

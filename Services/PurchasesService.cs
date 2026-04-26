using APIlog.Server.DTOs.Purchases;
using APIlog.Server.Infrastructure.Data;
using APIlog.Server.Models;
using Microsoft.EntityFrameworkCore;

namespace APIlog.Server.Services;

public class PurchasesService : IPurchasesService
{
    private readonly BookstoreDbContext _db;

    public PurchasesService(BookstoreDbContext db)
    {
        _db = db;
    }

    public async Task<IEnumerable<PurchaseReceiptListItemDto>> GetPurchaseReceiptsAsync(PurchasesQueryParams q)
    {
        var query = _db.PurchaseReceipts
            .Include(pr => pr.Employee)
            .Include(pr => pr.Supplier)
            .Include(pr => pr.PurchaseReceiptStatus)
            .Include(pr => pr.PurchaseReceiptItems)
                .ThenInclude(pi => pi.Book)
                    .ThenInclude(b => b!.BookAuthors)
                        .ThenInclude(ba => ba.Author)
            .Include(pr => pr.SupplyReceipts)
                .ThenInclude(sr => sr.SupplyReceiptItems)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(q.Search))
            query = query.Where(pr => pr.PurchaseReceiptNumber.Contains(q.Search));
        if (q.DateFrom.HasValue) query = query.Where(pr => pr.PurchaseReceiptDateTime >= q.DateFrom.Value);
        if (q.DateTo.HasValue) query = query.Where(pr => pr.PurchaseReceiptDateTime <= q.DateTo.Value);
        if (q.AmountMin.HasValue) query = query.Where(pr => pr.PurchaseReceiptTotalAmount >= q.AmountMin.Value);
        if (q.AmountMax.HasValue) query = query.Where(pr => pr.PurchaseReceiptTotalAmount <= q.AmountMax.Value);
        if (q.EmployeeIds?.Count > 0)
            query = query.Where(pr => pr.EmployeeId.HasValue && q.EmployeeIds.Contains(pr.EmployeeId.Value));
        if (q.StatusIds?.Count > 0)
            query = query.Where(pr => pr.PurchaseReceiptStatusId.HasValue &&
                q.StatusIds.Contains(pr.PurchaseReceiptStatusId.Value));
        if (q.BookIds?.Count > 0)
            query = query.Where(pr => pr.PurchaseReceiptItems.Any(pi =>
                pi.BookId.HasValue && q.BookIds.Contains(pi.BookId.Value)));
        if (q.SupplierIds?.Count > 0)
            query = query.Where(pr => pr.SupplierId.HasValue && q.SupplierIds.Contains(pr.SupplierId.Value));

        query = (q.SortBy, q.SortOrder) switch
        {
            ("amount", "asc")   => query.OrderBy(pr => pr.PurchaseReceiptTotalAmount),
            ("amount", _)       => query.OrderByDescending(pr => pr.PurchaseReceiptTotalAmount),
            ("supplier", "asc") => query.OrderBy(pr => pr.Supplier != null ? pr.Supplier.SupplierName : ""),
            ("supplier", _)     => query.OrderByDescending(pr => pr.Supplier != null ? pr.Supplier.SupplierName : ""),
            ("employee", "asc") => query.OrderBy(pr => pr.Employee != null ? pr.Employee.EmployeeFullName : ""),
            ("employee", _)     => query.OrderByDescending(pr => pr.Employee != null ? pr.Employee.EmployeeFullName : ""),
            ("status", "asc")   => query.OrderBy(pr => pr.PurchaseReceiptStatus != null ? pr.PurchaseReceiptStatus.PurchaseReceiptStatusName : ""),
            ("status", _)       => query.OrderByDescending(pr => pr.PurchaseReceiptStatus != null ? pr.PurchaseReceiptStatus.PurchaseReceiptStatusName : ""),
            ("number", "asc")   => query.OrderBy(pr => pr.PurchaseReceiptNumber),
            ("number", _)       => query.OrderByDescending(pr => pr.PurchaseReceiptNumber),
            ("itemcount", "asc")   => query.OrderBy(pr => pr.PurchaseReceiptItems.Count()),
            ("itemcount", _)       => query.OrderByDescending(pr => pr.PurchaseReceiptItems.Count()),
            ("totalqty", "asc")    => query.OrderBy(pr => pr.PurchaseReceiptItems.Sum(pi => (int)pi.PurchaseReceiptItemQuantity)),
            ("totalqty", _)        => query.OrderByDescending(pr => pr.PurchaseReceiptItems.Sum(pi => (int)pi.PurchaseReceiptItemQuantity)),
            (_, "asc")          => query.OrderBy(pr => pr.PurchaseReceiptDateTime),
            _                   => query.OrderByDescending(pr => pr.PurchaseReceiptDateTime)
        };

        var receipts = await query
            .Skip((q.Page - 1) * q.PageSize)
            .Take(q.PageSize)
            .ToListAsync();
        return receipts.Select(pr => MapToListItem(pr));
    }

    public async Task<PurchaseReceiptDetailDto?> GetPurchaseReceiptByIdAsync(int id)
    {
        var receipt = await LoadWithDetailsAsync(id);
        return receipt is null ? null : MapToDetail(receipt);
    }

    public async Task<PurchaseReceiptDetailDto> CreatePurchaseReceiptAsync(
        CreatePurchaseReceiptDto dto, int employeeId)
    {
        int? supplierId = dto.SupplierId;

        if (supplierId is null && !string.IsNullOrWhiteSpace(dto.SupplierName))
        {
            var supplier = new Supplier
            {
                SupplierName = dto.SupplierName,
                SupplierAddress = dto.SupplierAddress
            };
            _db.Suppliers.Add(supplier);
            await _db.SaveChangesAsync();
            supplierId = supplier.SupplierId;
        }

        var pendingStatus = await _db.PurchaseReceiptStatuses
            .FirstOrDefaultAsync(s => s.PurchaseReceiptStatusName == "в обробці")
            ?? await _db.PurchaseReceiptStatuses.FirstAsync();

        var receipt = new PurchaseReceipt
        {
            PurchaseReceiptNumber = GenerateReceiptNumber("P"),
            SupplierId = supplierId,
            EmployeeId = employeeId,
            PurchaseReceiptSupplyDateTime = dto.PurchaseReceiptSupplyDateTime,
            PurchaseReceiptStatusId = pendingStatus.PurchaseReceiptStatusId
        };

        _db.PurchaseReceipts.Add(receipt);
        await _db.SaveChangesAsync();

        decimal total = 0;
        foreach (var item in dto.Items)
        {
            _db.PurchaseReceiptItems.Add(new PurchaseReceiptItem
            {
                PurchaseReceiptId = receipt.PurchaseReceiptId,
                BookId = item.BookId,
                PurchaseReceiptItemQuantity = (short)item.Quantity,
                BookPricePerUnit = item.PricePerUnit
            });
            total += item.PricePerUnit * item.Quantity;
        }

        receipt.PurchaseReceiptTotalAmount = total;
        await _db.SaveChangesAsync();

        return MapToDetail(await LoadWithDetailsAsync(receipt.PurchaseReceiptId));
    }

    public async Task<PurchaseReceiptDetailDto> UpdatePurchaseReceiptAsync(int id, UpdatePurchaseReceiptDto dto)
    {
        var receipt = await _db.PurchaseReceipts
            .Include(pr => pr.PurchaseReceiptItems)
            .FirstOrDefaultAsync(pr => pr.PurchaseReceiptId == id)
            ?? throw new KeyNotFoundException($"PurchaseReceipt {id} not found.");

        receipt.SupplierId = dto.SupplierId;
        receipt.PurchaseReceiptSupplyDateTime = dto.PurchaseReceiptSupplyDateTime;

        foreach (var item in dto.Items)
        {
            var existing = receipt.PurchaseReceiptItems
                .FirstOrDefault(pi => pi.PurchaseReceiptItemId == item.PurchaseReceiptItemId);
            if (existing is null) continue;
            existing.PurchaseReceiptItemQuantity = (short)item.Quantity;
            existing.BookPricePerUnit = item.PricePerUnit;
        }

        receipt.PurchaseReceiptTotalAmount = receipt.PurchaseReceiptItems
            .Sum(pi => pi.BookPricePerUnit * pi.PurchaseReceiptItemQuantity);

        await _db.SaveChangesAsync();
        return MapToDetail(await LoadWithDetailsAsync(id));
    }

    public async Task CancelPurchaseReceiptAsync(int id)
    {
        var receipt = await _db.PurchaseReceipts
            .Include(pr => pr.PurchaseReceiptStatus)
            .FirstOrDefaultAsync(pr => pr.PurchaseReceiptId == id)
            ?? throw new KeyNotFoundException($"PurchaseReceipt {id} not found.");

        var cancelledStatus = await _db.PurchaseReceiptStatuses
            .FirstOrDefaultAsync(s => s.PurchaseReceiptStatusName == "скасовано")
            ?? throw new InvalidOperationException("Cancelled status not found in dictionary.");

        receipt.PurchaseReceiptStatusId = cancelledStatus.PurchaseReceiptStatusId;
        await _db.SaveChangesAsync();
    }

    private async Task<PurchaseReceipt> LoadWithDetailsAsync(int id)
    {
        return await _db.PurchaseReceipts
            .Include(pr => pr.Employee)
            .Include(pr => pr.Supplier)
            .Include(pr => pr.PurchaseReceiptStatus)
            .Include(pr => pr.PurchaseReceiptItems)
                .ThenInclude(pi => pi.Book)
                    .ThenInclude(b => b!.BookAuthors)
                        .ThenInclude(ba => ba.Author)
            .Include(pr => pr.SupplyReceipts).ThenInclude(sr => sr.Employee)
            .Include(pr => pr.SupplyReceipts).ThenInclude(sr => sr.SupplyReceiptItems)
                .ThenInclude(si => si.BookInStore).ThenInclude(bis => bis!.BookStore)
            .FirstAsync(pr => pr.PurchaseReceiptId == id);
    }

    private static PurchaseReceiptListItemDto MapToListItem(PurchaseReceipt pr)
    {
        var suppliedQtyByItemId = pr.SupplyReceipts
            .SelectMany(sr => sr.SupplyReceiptItems)
            .Where(si => si.PurchaseReceiptItemId.HasValue)
            .GroupBy(si => si.PurchaseReceiptItemId!.Value)
            .ToDictionary(g => g.Key, g => g.Sum(si => (int)si.SupplyReceiptItemQuantity));

        var items = pr.PurchaseReceiptItems
            .Where(pi => pi.Book != null)
            .Select(pi => new PurchaseItemSummaryDto(
                pi.PurchaseReceiptItemId,
                pi.Book!.BookId,
                pi.Book.BookTitle,
                pi.Book.BookAuthors.Select(ba => ba.Author.AuthorFullName),
                pi.Book.ISBN,
                pi.BookPricePerUnit,
                pi.PurchaseReceiptItemQuantity,
                suppliedQtyByItemId.GetValueOrDefault(pi.PurchaseReceiptItemId, 0)
            ));

        return new PurchaseReceiptListItemDto(
            pr.PurchaseReceiptId,
            pr.PurchaseReceiptNumber,
            pr.PurchaseReceiptDateTime,
            pr.PurchaseReceiptSupplyDateTime,
            pr.EmployeeId,
            pr.Employee?.EmployeeFullName ?? string.Empty,
            pr.Employee?.EmployeePersonnelNumber,
            pr.SupplierId,
            pr.Supplier?.SupplierName,
            pr.Supplier?.SupplierAddress,
            pr.PurchaseReceiptStatusId ?? 0,
            pr.PurchaseReceiptStatus?.PurchaseReceiptStatusName ?? string.Empty,
            pr.PurchaseReceiptTotalAmount,
            pr.PurchaseReceiptItems.Count,
            pr.PurchaseReceiptItems.Sum(pi => (int)pi.PurchaseReceiptItemQuantity),
            items
        );
    }

    private static PurchaseReceiptDetailDto MapToDetail(PurchaseReceipt pr)
    {
        var suppliedQtyByItemId = pr.SupplyReceipts
            .SelectMany(sr => sr.SupplyReceiptItems)
            .Where(si => si.PurchaseReceiptItemId.HasValue)
            .GroupBy(si => si.PurchaseReceiptItemId!.Value)
            .ToDictionary(g => g.Key, g => g.Sum(si => (int)si.SupplyReceiptItemQuantity));

        var items = pr.PurchaseReceiptItems
            .Where(pi => pi.Book != null)
            .Select(pi => new PurchaseItemDetailDto(
                pi.PurchaseReceiptItemId,
                pi.Book!.BookId,
                pi.Book.BookTitle,
                pi.Book.BookAuthors.Select(ba => ba.Author.AuthorFullName),
                pi.Book.ISBN,
                pi.BookPricePerUnit,
                pi.PurchaseReceiptItemQuantity,
                suppliedQtyByItemId.GetValueOrDefault(pi.PurchaseReceiptItemId, 0),
                pi.BookPricePerUnit * pi.PurchaseReceiptItemQuantity
            ));

        var supplies = pr.SupplyReceipts.Select(sr => new SupplySummaryDto(
            sr.SupplyReceiptId,
            sr.SupplyReceiptNumber,
            sr.SupplyReceiptDateTime,
            sr.EmployeeId ?? 0,
            sr.Employee?.EmployeeFullName ?? string.Empty,
            sr.SupplyReceiptTotalAmount,
            sr.SupplyReceiptItems.Select(si => new SupplyItemDto(
                si.SupplyReceiptItemId,
                si.PurchaseReceiptItemId ?? 0,
                si.BookInStore?.BookStoreId,
                si.BookInStore?.BookStore?.BookStoreName,
                si.SupplyReceiptItemQuantity
            ))
        ));

        return new PurchaseReceiptDetailDto(
            pr.PurchaseReceiptId,
            pr.PurchaseReceiptNumber,
            pr.PurchaseReceiptDateTime,
            pr.PurchaseReceiptSupplyDateTime,
            pr.EmployeeId ?? 0,
            pr.Employee?.EmployeeFullName ?? string.Empty,
            pr.Employee?.EmployeePersonnelNumber,
            pr.SupplierId,
            pr.Supplier?.SupplierName,
            pr.Supplier?.SupplierAddress,
            pr.PurchaseReceiptStatus?.PurchaseReceiptStatusName ?? string.Empty,
            pr.PurchaseReceiptTotalAmount,
            items,
            supplies
        );
    }

    private static string GenerateReceiptNumber(string prefix)
        => prefix + Guid.NewGuid().ToString("N")[..12].ToUpper();
}

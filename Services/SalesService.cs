using APIlog.Server.DTOs.Sales;
using APIlog.Server.Infrastructure.Data;
using APIlog.Server.Models;
using Microsoft.EntityFrameworkCore;

namespace APIlog.Server.Services;

public class SalesService : ISalesService
{
    private readonly BookstoreDbContext _db;
    private readonly ICustomersService _customersService;

    public SalesService(BookstoreDbContext db, ICustomersService customersService)
    {
        _db = db;
        _customersService = customersService;
    }

    public async Task<IEnumerable<SaleReceiptListItemDto>> GetSaleReceiptsAsync(
        SalesQueryParams q, int? bookStoreId = null)
    {
        var query = _db.SalesReceipts
            .Include(sr => sr.Customer)
            .Include(sr => sr.Employee)
            .Include(sr => sr.SaleReceiptItems)
                .ThenInclude(si => si.BookInStore)
                    .ThenInclude(bis => bis!.Book)
                        .ThenInclude(b => b!.BookAuthors)
                            .ThenInclude(ba => ba.Author)
            .Include(sr => sr.Payments)
            .AsQueryable();

        if (bookStoreId.HasValue)
            query = query.Where(sr => sr.Employee != null &&
                sr.Employee.BookStoreId == bookStoreId.Value);

        if (!string.IsNullOrWhiteSpace(q.Search))
            query = query.Where(sr => sr.SalesReceiptNumber.Contains(q.Search));

        if (q.DateFrom.HasValue)
            query = query.Where(sr => sr.SalesReceiptDateTime >= q.DateFrom.Value);
        if (q.DateTo.HasValue)
            query = query.Where(sr => sr.SalesReceiptDateTime <= q.DateTo.Value);
        if (q.AmountMin.HasValue)
            query = query.Where(sr => sr.SalesReceiptTotalAmount >= q.AmountMin.Value);
        if (q.AmountMax.HasValue)
            query = query.Where(sr => sr.SalesReceiptTotalAmount <= q.AmountMax.Value);

        if (q.EmployeeIds?.Count > 0)
            query = query.Where(sr => sr.EmployeeId.HasValue &&
                q.EmployeeIds.Contains(sr.EmployeeId.Value));
        if (q.CustomerIds?.Count > 0)
            query = query.Where(sr => sr.CustomerId.HasValue &&
                q.CustomerIds.Contains(sr.CustomerId.Value));
        if (q.BookIds?.Count > 0)
            query = query.Where(sr => sr.SaleReceiptItems.Any(si =>
                si.BookInStore != null && si.BookInStore.BookId.HasValue &&
                q.BookIds.Contains(si.BookInStore.BookId.Value)));

        var receipts = await query.ToListAsync();

        if (!string.IsNullOrWhiteSpace(q.PaymentStatus))
        {
            receipts = q.PaymentStatus switch
            {
                "paid" => receipts.Where(sr =>
                    sr.Payments.Sum(p => p.PaymentAmount) >= (sr.SalesReceiptTotalAmount ?? 0)).ToList(),
                "partial" => receipts.Where(sr =>
                {
                    var paid = sr.Payments.Sum(p => p.PaymentAmount);
                    return paid > 0 && paid < (sr.SalesReceiptTotalAmount ?? 0);
                }).ToList(),
                "unpaid" => receipts.Where(sr =>
                    sr.Payments.Sum(p => p.PaymentAmount) == 0).ToList(),
                _ => receipts
            };
        }

        query = (q.SortBy, q.SortOrder) switch
        {
            ("amount", "desc") => query.OrderByDescending(sr => sr.SalesReceiptTotalAmount),
            ("amount", _) => query.OrderBy(sr => sr.SalesReceiptTotalAmount),
            (_, "asc") => query.OrderBy(sr => sr.SalesReceiptDateTime),
            _ => query.OrderByDescending(sr => sr.SalesReceiptDateTime)
        };

        return receipts.Select(sr => MapToListItem(sr));
    }

    public async Task<SaleReceiptDetailDto?> GetSaleReceiptByIdAsync(int id)
    {
        var receipt = await LoadReceiptWithDetailsAsync(id);
        return receipt is null ? null : MapToDetail(receipt);
    }

    public async Task<SaleReceiptDetailDto> CreateSaleReceiptAsync(CreateSaleReceiptDto dto, int employeeId)
    {
        var employee = await _db.Employees
            .Include(e => e.BookStore)
            .FirstOrDefaultAsync(e => e.EmployeeId == employeeId)
            ?? throw new InvalidOperationException("Employee not found.");

        int? customerId = dto.CustomerId;
        if (customerId is null && !string.IsNullOrWhiteSpace(dto.CustomerFullName))
        {
            var customer = await _customersService.GetOrCreateCustomerAsync(
                dto.CustomerFullName, dto.CustomerPhoneNumber, dto.CustomerEmail);
            customerId = customer.CustomerId;
        }

        var receipt = new SalesReceipt
        {
            SalesReceiptNumber = GenerateReceiptNumber("S"),
            CustomerId = customerId,
            EmployeeId = employeeId
        };

        _db.SalesReceipts.Add(receipt);
        await _db.SaveChangesAsync();

        foreach (var item in dto.Items)
        {
            _db.SaleReceiptItems.Add(new SaleReceiptItem
            {
                SalesReceiptId = receipt.SalesReceiptId,
                SaleBookInStoreId = item.BookInStoreId,
                SaleReceiptItemQuantity = (byte)item.Quantity
            });
        }

        var bis = await _db.BookInStores
            .Where(b => dto.Items.Select(i => i.BookInStoreId).Contains(b.BookInStoreId))
            .Include(b => b.Book)
            .ToListAsync();

        receipt.SalesReceiptTotalAmount = dto.Items.Sum(item =>
        {
            var store = bis.FirstOrDefault(b => b.BookInStoreId == item.BookInStoreId);
            return (store?.Book?.BookPrice ?? 0) * item.Quantity;
        });

        await _db.SaveChangesAsync();

        return MapToDetail(await LoadReceiptWithDetailsAsync(receipt.SalesReceiptId));
    }

    public async Task<SaleReceiptDetailDto> UpdateSaleReceiptAsync(int id, UpdateSaleReceiptDto dto)
    {
        var receipt = await _db.SalesReceipts
            .Include(sr => sr.SaleReceiptItems)
            .FirstOrDefaultAsync(sr => sr.SalesReceiptId == id)
            ?? throw new KeyNotFoundException($"SalesReceipt {id} not found.");

        if (dto.CustomerId.HasValue)
            receipt.CustomerId = dto.CustomerId;

        foreach (var item in dto.Items)
        {
            var existing = receipt.SaleReceiptItems
                .FirstOrDefault(si => si.SaleReceiptItemId == item.SaleReceiptItemId);
            if (existing is not null)
                existing.SaleReceiptItemQuantity = (byte)item.Quantity;
        }

        await _db.SaveChangesAsync();

        return MapToDetail(await LoadReceiptWithDetailsAsync(id));
    }

    public async Task DeleteSaleReceiptAsync(int id)
    {
        var receipt = await _db.SalesReceipts
            .Include(sr => sr.SaleReceiptItems)
            .Include(sr => sr.Payments)
            .FirstOrDefaultAsync(sr => sr.SalesReceiptId == id)
            ?? throw new KeyNotFoundException($"SalesReceipt {id} not found.");

        _db.SaleReceiptItems.RemoveRange(receipt.SaleReceiptItems);
        _db.Payments.RemoveRange(receipt.Payments);
        _db.SalesReceipts.Remove(receipt);
        await _db.SaveChangesAsync();
    }

    private async Task<SalesReceipt> LoadReceiptWithDetailsAsync(int id)
    {
        return await _db.SalesReceipts
            .Include(sr => sr.Customer)
            .Include(sr => sr.Employee).ThenInclude(e => e!.BookStore)
            .Include(sr => sr.SaleReceiptItems)
                .ThenInclude(si => si.BookInStore)
                    .ThenInclude(bis => bis!.Book)
                        .ThenInclude(b => b!.BookAuthors)
                            .ThenInclude(ba => ba.Author)
            .Include(sr => sr.Payments).ThenInclude(p => p.PaymentMethod)
            .FirstAsync(sr => sr.SalesReceiptId == id);
    }

    private static SaleReceiptListItemDto MapToListItem(SalesReceipt sr)
    {
        var paid = sr.Payments.Sum(p => p.PaymentAmount);
        var total = sr.SalesReceiptTotalAmount ?? 0;
        var paymentStatus = paid >= total && total > 0 ? "paid"
            : paid > 0 ? "partial"
            : "unpaid";

        var items = sr.SaleReceiptItems
            .Where(si => si.BookInStore?.Book != null)
            .Select(si =>
            {
                var book = si.BookInStore!.Book!;
                var price = book.BookPrice;
                return new SaleItemSummaryDto(
                    book.BookId,
                    book.BookTitle,
                    book.BookAuthors.Select(ba => ba.Author.AuthorFullName),
                    book.ISBN,
                    price,
                    si.SaleReceiptItemQuantity,
                    price * si.SaleReceiptItemQuantity
                );
            });

        return new SaleReceiptListItemDto(
            sr.SalesReceiptId,
            sr.SalesReceiptNumber,
            sr.SalesReceiptDateTime,
            sr.Customer?.CustomerFullName,
            sr.Employee?.EmployeeFullName ?? string.Empty,
            sr.SalesReceiptTotalAmount,
            paymentStatus,
            sr.SaleReceiptItems.Count,
            sr.SaleReceiptItems.Sum(si => (int)si.SaleReceiptItemQuantity),
            items
        );
    }

    private static SaleReceiptDetailDto MapToDetail(SalesReceipt sr)
    {
        var paid = sr.Payments.Sum(p => p.PaymentAmount);
        var total = sr.SalesReceiptTotalAmount ?? 0;
        var paymentStatus = paid >= total && total > 0 ? "paid"
            : paid > 0 ? "partial"
            : "unpaid";

        var items = sr.SaleReceiptItems
            .Where(si => si.BookInStore?.Book != null)
            .Select(si =>
            {
                var book = si.BookInStore!.Book!;
                var price = book.BookPrice;
                return new SaleItemDetailDto(
                    si.SaleReceiptItemId,
                    si.SaleBookInStoreId ?? 0,
                    book.BookId,
                    book.BookTitle,
                    book.BookAuthors.Select(ba => ba.Author.AuthorFullName),
                    book.ISBN,
                    price,
                    si.SaleReceiptItemQuantity,
                    price * si.SaleReceiptItemQuantity
                );
            });

        var payments = sr.Payments.Select(p => new PaymentSummaryDto(
            p.PaymentId,
            p.PaymentNumber,
            p.PaymentDateTime,
            p.PaymentAmount,
            p.PaymentMethod?.PaymentMethodName
        ));

        return new SaleReceiptDetailDto(
            sr.SalesReceiptId,
            sr.SalesReceiptNumber,
            sr.SalesReceiptDateTime,
            sr.CustomerId,
            sr.Customer?.CustomerFullName,
            sr.Customer?.CustomerPhoneNumber,
            sr.Customer?.CustomerEmail,
            sr.EmployeeId ?? 0,
            sr.Employee?.EmployeeFullName ?? string.Empty,
            sr.Employee?.BookStoreId ?? 0,
            sr.Employee?.BookStore?.BookStoreName ?? string.Empty,
            sr.SalesReceiptTotalAmount,
            paymentStatus,
            items,
            payments
        );
    }

    private static string GenerateReceiptNumber(string prefix)
        => prefix + Guid.NewGuid().ToString("N")[..12].ToUpper();
}

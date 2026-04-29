using APIlog.Server.DTOs.Sales;
using APIlog.Server.Infrastructure.Data;
using APIlog.Server.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace APIlog.Server.Services;

public class SalesService : ISalesService
{
    private readonly BookstoreDbContext _db;

    public SalesService(BookstoreDbContext db)
    {
        _db = db;
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
            .Include(sr => sr.SaleReceiptItems)
                .ThenInclude(si => si.BookInStore)
                    .ThenInclude(bis => bis!.BookStore)
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

        if (q.ItemCountMin.HasValue)
            receipts = receipts.Where(sr => sr.SaleReceiptItems.Count >= q.ItemCountMin.Value).ToList();
        if (q.ItemCountMax.HasValue)
            receipts = receipts.Where(sr => sr.SaleReceiptItems.Count <= q.ItemCountMax.Value).ToList();
        if (q.TotalQtyMin.HasValue)
            receipts = receipts.Where(sr => sr.SaleReceiptItems.Sum(si => (int)si.SaleReceiptItemQuantity) >= q.TotalQtyMin.Value).ToList();
        if (q.TotalQtyMax.HasValue)
            receipts = receipts.Where(sr => sr.SaleReceiptItems.Sum(si => (int)si.SaleReceiptItemQuantity) <= q.TotalQtyMax.Value).ToList();

        receipts = (q.SortBy?.ToLower(), q.SortOrder?.ToLower()) switch
        {
            ("amount", "asc")              => receipts.OrderBy(sr => sr.SalesReceiptTotalAmount).ToList(),
            ("amount", _)                  => receipts.OrderByDescending(sr => sr.SalesReceiptTotalAmount).ToList(),
            ("salesreceiptnumber", "desc") => receipts.OrderByDescending(sr => sr.SalesReceiptNumber).ToList(),
            ("salesreceiptnumber", _)      => receipts.OrderBy(sr => sr.SalesReceiptNumber).ToList(),
            ("employee", "desc")           => receipts.OrderByDescending(sr => sr.Employee?.EmployeeFullName).ToList(),
            ("employee", _)                => receipts.OrderBy(sr => sr.Employee?.EmployeeFullName).ToList(),
            ("customer", "desc")           => receipts.OrderByDescending(sr => sr.Customer?.CustomerFullName).ToList(),
            ("customer", _)                => receipts.OrderBy(sr => sr.Customer?.CustomerFullName).ToList(),
            ("itemcount", "asc")           => receipts.OrderBy(sr => sr.SaleReceiptItems.Count).ToList(),
            ("itemcount", _)               => receipts.OrderByDescending(sr => sr.SaleReceiptItems.Count).ToList(),
            ("totalqty", "asc")            => receipts.OrderBy(sr => sr.SaleReceiptItems.Sum(si => (int)si.SaleReceiptItemQuantity)).ToList(),
            ("totalqty", _)                => receipts.OrderByDescending(sr => sr.SaleReceiptItems.Sum(si => (int)si.SaleReceiptItemQuantity)).ToList(),
            (_, "asc")                     => receipts.OrderBy(sr => sr.SalesReceiptDateTime).ToList(),
            _                              => receipts.OrderByDescending(sr => sr.SalesReceiptDateTime).ToList()
        };

        var page = Math.Max(1, q.Page);
        var pageSize = Math.Clamp(q.PageSize, 1, 100);
        receipts = receipts.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        return receipts.Select(sr => MapToListItem(sr));
    }

    public async Task<SaleReceiptDetailDto?> GetSaleReceiptByIdAsync(int id)
    {
        var receipt = await LoadReceiptWithDetailsAsync(id);
        return receipt is null ? null : MapToDetail(receipt);
    }

    public async Task<SaleReceiptDetailDto> CreateSaleReceiptAsync(CreateSaleReceiptDto dto, int employeeId)
    {
        var itemList = dto.Items.ToList();
        if (itemList.Count == 0)
            throw new InvalidOperationException("Кошик порожній. Додайте хоч а б одну книгу.");

        var employee = await _db.Employees
            .Include(e => e.BookStore)
            .FirstOrDefaultAsync(e => e.EmployeeId == employeeId)
            ?? throw new InvalidOperationException("Касира не знайдено.");

        var receiptNumber = await GenerateNextReceiptNumberAsync(
            employee.EmployeePersonnelNumber ?? "UNK01-0000");

        var strategy = _db.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                // ── 1. Stock validation ───────────────────────────────────────────
                var bisIds = itemList.Select(i => i.SaleBookInStoreId).Distinct().ToList();
                var stocks = await _db.BookInStores
                    .Where(bis => bisIds.Contains(bis.BookInStoreId))
                    .Include(bis => bis.Book)
                    .ToDictionaryAsync(bis => bis.BookInStoreId);

                var shortfall = itemList
                    .Where(i => !stocks.TryGetValue(i.SaleBookInStoreId, out var s)
                                || s.BookInStoreQuantity < i.SaleReceiptItemQuantity)
                    .Select(i => stocks.TryGetValue(i.SaleBookInStoreId, out var s)
                        ? (s.Book?.BookTitle ?? $"позиція {i.SaleBookInStoreId}")
                        : $"позиція {i.SaleBookInStoreId}")
                    .ToList();

                if (shortfall.Count > 0)
                    throw new InvalidOperationException(
                        "Недостатньо примірників на складі: " + string.Join(", ", shortfall) + ".");

                // ── 2. Customer resolution (exact 3-field match or create) ──────────
                var customerId = await ResolveCustomerAsync(
                    dto.CustomerId, dto.CustomerFullName,
                    dto.CustomerPhoneNumber, dto.CustomerEmail);

                // ── 3. Create receipt + items (single SaveChangesAsync → trigger fires) ─
                var receipt = new SalesReceipt
                {
                    SalesReceiptNumber = receiptNumber,
                    CustomerId         = customerId,
                    EmployeeId         = employeeId
                };
                _db.SalesReceipts.Add(receipt);
                await _db.SaveChangesAsync();

                foreach (var item in itemList)
                {
                    _db.SaleReceiptItems.Add(new SaleReceiptItem
                    {
                        SalesReceiptId          = receipt.SalesReceiptId,
                        SaleBookInStoreId       = item.SaleBookInStoreId,
                        SaleReceiptItemQuantity = (byte)item.SaleReceiptItemQuantity
                    });
                }
                await _db.SaveChangesAsync();

                await tx.CommitAsync();
                return MapToDetail(await LoadReceiptWithDetailsAsync(receipt.SalesReceiptId));
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        });
    }

    public async Task<SaleReceiptDetailDto> UpdateSaleReceiptAsync(int id, UpdateSaleReceiptDto dto)
    {
        var strategy = _db.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                var receipt = await _db.SalesReceipts
                    .Include(sr => sr.SaleReceiptItems)
                        .ThenInclude(sri => sri.BookInStore)
                            .ThenInclude(bis => bis!.Book)
                    .FirstOrDefaultAsync(sr => sr.SalesReceiptId == id)
                    ?? throw new KeyNotFoundException($"SalesReceipt {id} not found.");

                // ── 1. Branch transfer (if bookStoreId changed) ───────────────────
                var currentStoreId = receipt.SaleReceiptItems.FirstOrDefault()?.BookInStore?.BookStoreId;
                if (dto.BookStoreId.HasValue && currentStoreId != dto.BookStoreId)
                {
                    await _db.Database.ExecuteSqlRawAsync(
                        "EXEC dbo.sp_ProcessBookStoreTransfer @SalesReceiptId, @NewBookStoreId",
                        new SqlParameter("@SalesReceiptId", id),
                        new SqlParameter("@NewBookStoreId", dto.BookStoreId.Value));
                }

                // ── 2. Customer resolution ────────────────────────────────────
                receipt.CustomerId = await ResolveCustomerAsync(
                    dto.CustomerId, dto.CustomerFullName,
                    dto.CustomerPhoneNumber, dto.CustomerEmail);

                // ── 2. Stock validation for quantity increases ─────────────────
                var shortfall = new List<string>();
                var updatableItems = dto.Items.Where(i => i.SaleReceiptItemId > 0).ToList();

                foreach (var updateItem in updatableItems)
                {
                    var existing = receipt.SaleReceiptItems
                        .FirstOrDefault(si => si.SaleReceiptItemId == updateItem.SaleReceiptItemId);
                    if (existing is null) continue;

                    var newQty = updateItem.SaleReceiptItemQuantity;
                    var oldQty = (int)existing.SaleReceiptItemQuantity;
                    var additional = newQty - oldQty;

                    if (additional > 0 && existing.SaleBookInStoreId.HasValue
                        && existing.BookInStore?.BookInStoreQuantity < additional)
                    {
                        shortfall.Add(existing.BookInStore?.Book?.BookTitle
                                      ?? $"позиція {existing.SaleReceiptItemId}");
                    }

                    existing.SaleReceiptItemQuantity = (byte)newQty;
                }

                if (shortfall.Count > 0)
                    throw new InvalidOperationException(
                        "Недостатньо примірників на складі: " + string.Join(", ", shortfall) + ".");

                await _db.SaveChangesAsync();
                await tx.CommitAsync();
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        });

        // ── 3. Book-store transfer (its own transaction via stored procedure) ─
        if (dto.BookStoreId.HasValue)
        {
            var items = await _db.SaleReceiptItems
                .Where(sri => sri.SalesReceiptId == id)
                .Include(sri => sri.BookInStore)
                .ToListAsync();

            var currentStoreId = items
                .FirstOrDefault(sri => sri.BookInStore?.BookStoreId != null)
                ?.BookInStore?.BookStoreId;

            if (currentStoreId.HasValue && currentStoreId.Value != dto.BookStoreId.Value)
            {
                try
                {
                    await _db.Database.ExecuteSqlInterpolatedAsync(
                        $"EXEC dbo.sp_ProcessBookStoreTransfer {id}, {dto.BookStoreId.Value}");
                }
                catch (Exception ex) when
                    (ex.Message.Contains("Помилка переміщення") ||
                     ex.InnerException?.Message.Contains("Помилка переміщення") == true)
                {
                    throw new ArgumentException("Недостатньо товару на складі нової книгарні.");
                }
            }
        }

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
            .Include(sr => sr.Employee)
            .Include(sr => sr.SaleReceiptItems)
                .ThenInclude(si => si.BookInStore)
                    .ThenInclude(bis => bis!.Book)
                        .ThenInclude(b => b!.BookAuthors)
                            .ThenInclude(ba => ba.Author)
            .Include(sr => sr.SaleReceiptItems)
                .ThenInclude(si => si.BookInStore)
                    .ThenInclude(bis => bis!.BookStore)
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
                    si.SaleReceiptItemId,
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
            sr.CustomerId,
            sr.Customer?.CustomerFullName,
            sr.Customer?.CustomerPhoneNumber,
            sr.Customer?.CustomerEmail,
            sr.EmployeeId,
            sr.Employee?.EmployeeFullName ?? string.Empty,
            sr.Employee?.EmployeePersonnelNumber,
            sr.SaleReceiptItems.FirstOrDefault()?.BookInStore?.BookStoreId,
            sr.SaleReceiptItems.FirstOrDefault()?.BookInStore?.BookStore?.BookStoreName,
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
            sr.Employee?.EmployeePersonnelNumber,
            sr.SaleReceiptItems.FirstOrDefault()?.BookInStore?.BookStoreId ?? 0,
            sr.SaleReceiptItems.FirstOrDefault()?.BookInStore?.BookStore?.BookStoreName ?? string.Empty,
            sr.SalesReceiptTotalAmount,
            paymentStatus,
            items,
            payments
        );
    }

    public async Task<IEnumerable<AvailableBookStoreDto>> GetAvailableStoresForReceiptAsync(
        int receiptId)
    {
        var requirements = await _db.SaleReceiptItems
            .Where(sri => sri.SalesReceiptId == receiptId && sri.BookInStore != null)
            .Include(sri => sri.BookInStore)
            .Select(sri => new
            {
                BookId    = (int?)sri.BookInStore!.BookId,
                Qty       = (int)sri.SaleReceiptItemQuantity
            })
            .Where(r => r.BookId.HasValue)
            .ToListAsync();

        if (!requirements.Any())
            return Enumerable.Empty<AvailableBookStoreDto>();

        var bookReqs = requirements
            .GroupBy(r => r.BookId!.Value)
            .Select(g => new { BookId = g.Key, Qty = g.Sum(r => r.Qty) })
            .ToList();

        var requiredBookIds = bookReqs.Select(r => r.BookId).ToList();

        var stock = await _db.BookInStores
            .Where(bis => bis.BookId.HasValue && bis.BookStoreId.HasValue &&
                          requiredBookIds.Contains(bis.BookId.Value))
            .Select(bis => new
            {
                StoreId = bis.BookStoreId!.Value,
                BookId  = bis.BookId!.Value,
                Qty     = (int)bis.BookInStoreQuantity
            })
            .ToListAsync();

        var eligibleStoreIds = stock
            .GroupBy(s => s.StoreId)
            .Where(g =>
            {
                var storeStock = g.ToList();
                return bookReqs.All(req =>
                    storeStock.Any(s => s.BookId == req.BookId && s.Qty >= req.Qty));
            })
            .Select(g => g.Key)
            .ToHashSet();

        return await _db.BookStores
            .Where(bs => eligibleStoreIds.Contains(bs.BookStoreId))
            .OrderBy(bs => bs.BookStoreCode)
            .Select(bs => new AvailableBookStoreDto(
                bs.BookStoreId,
                bs.BookStoreName,
                bs.BookStoreAddress,
                bs.BookStoreCode))
            .ToListAsync();
    }

    /// <summary>
    /// Exact 3-field customer match: find an existing customer whose name, phone,
    /// and email all equal the provided values (null == empty string treated as null).
    /// If no match: create a new customer. If no name provided: returns null (anonymous).
    /// </summary>
    private async Task<int?> ResolveCustomerAsync(
        int? customerId,
        string? fullName,
        string? phone,
        string? email)
    {
        // Explicit ID shortcut (e.g. selected from dropdown)
        if (customerId.HasValue) return customerId;

        var name = fullName?.Trim();
        // Name is required for customer resolution
        if (string.IsNullOrWhiteSpace(name))
            return null;

        var ph = string.IsNullOrWhiteSpace(phone) ? null : phone!.Trim();
        var em = string.IsNullOrWhiteSpace(email) ? null : email!.Trim();

        // Exact 3-field match
        var existing = await _db.Customers.FirstOrDefaultAsync(c =>
            c.CustomerFullName    == name &&
            c.CustomerPhoneNumber == ph   &&
            c.CustomerEmail       == em);

        if (existing is not null)
            return existing.CustomerId;

        // Create new customer
        var newCustomer = new Customer
        {
            CustomerFullName    = name,
            CustomerPhoneNumber = ph,
            CustomerEmail       = em
        };
        _db.Customers.Add(newCustomer);
        await _db.SaveChangesAsync();
        return newCustomer.CustomerId;
    }

    private async Task<string> GenerateNextReceiptNumberAsync(string personnelNumber)
    {
        var prefix = personnelNumber.Split('-')[0];
        var pattern = prefix + "-";

        var lastNumber = await _db.SalesReceipts
            .Where(sr => sr.SalesReceiptNumber.StartsWith(pattern))
            .OrderByDescending(sr => sr.SalesReceiptNumber)
            .Select(sr => sr.SalesReceiptNumber)
            .FirstOrDefaultAsync();

        int next = 1;
        if (lastNumber is not null)
        {
            var seq = lastNumber[(pattern.Length)..];
            if (int.TryParse(seq, out var last))
                next = last + 1;
        }

        return $"{prefix}-{next:D7}";
    }
}

using APIlog.Server.DTOs.Charts;
using APIlog.Server.Infrastructure.Data;
using APIlog.Server.Models;
using Microsoft.EntityFrameworkCore;

namespace APIlog.Server.Services;

public class ChartsService : IChartsService
{
    private readonly BookstoreDbContext _db;

    public ChartsService(BookstoreDbContext db) => _db = db;

    public async Task<IEnumerable<SalesAttributeDto>> GetSalesByAttributeAsync(ChartQueryParams q)
    {
        var items = await LoadItemsAsync(q.DateFrom, q.DateTo);
        return GetLabeledItems(items, q.Characteristic)
            .GroupBy(x => x.Label)
            .Select(g => new SalesAttributeDto(g.Key, g.Sum(x => x.Qty)))
            .OrderByDescending(x => x.Count)
            .Take(q.TopN > 0 ? q.TopN : 10);
    }

    public async Task<IEnumerable<SalesTimelineSeriesDto>> GetSalesTimelineAsync(ChartQueryParams q)
    {
        var items = await LoadItemsAsync(q.DateFrom, q.DateTo);
        var labeled = GetLabeledItems(items, q.Characteristic).ToList();

        var topLabels = labeled
            .GroupBy(x => x.Label)
            .OrderByDescending(g => g.Sum(x => x.Qty))
            .Take(q.TopN > 0 ? q.TopN : 10)
            .Select(g => g.Key)
            .ToHashSet();

        var filtered = labeled.Where(x => topLabels.Contains(x.Label)).ToList();

        var from = q.DateFrom ?? (filtered.Count > 0
            ? filtered.Where(x => x.Date.HasValue).Min(x => x.Date!.Value)
            : DateTime.UtcNow.AddYears(-1));
        var to = q.DateTo ?? (filtered.Count > 0
            ? filtered.Where(x => x.Date.HasValue).Max(x => x.Date!.Value)
            : DateTime.UtcNow);

        var totalDays = (to - from).TotalDays;

        Func<DateTime?, string> bucket = totalDays <= 31
            ? dt => dt?.ToString("yyyy-MM-dd") ?? ""
            : totalDays <= 1825
                ? dt => $"{dt?.Year:D4}-{dt?.Month:D2}"
                : dt => $"{dt?.Year:D4}";

        return filtered
            .GroupBy(x => x.Label)
            .Select(lg => new SalesTimelineSeriesDto(
                lg.Key,
                lg.GroupBy(x => bucket(x.Date))
                  .Where(g => !string.IsNullOrEmpty(g.Key))
                  .Select(tg => new SalesTimelinePointDto(tg.Key, tg.Sum(x => x.Qty)))
                  .OrderBy(p => p.Date)
            ))
            .OrderByDescending(s => s.Points.Sum(p => p.Count));
    }

    private async Task<List<SaleReceiptItem>> LoadItemsAsync(DateTime? from, DateTime? to)
    {
        var query = _db.SaleReceiptItems
            .Include(si => si.SalesReceipt)
            .Include(si => si.BookInStore)
                .ThenInclude(bis => bis!.Book)
                    .ThenInclude(b => b!.Genre)
            .Include(si => si.BookInStore)
                .ThenInclude(bis => bis!.Book)
                    .ThenInclude(b => b!.Publisher)
            .Include(si => si.BookInStore)
                .ThenInclude(bis => bis!.Book)
                    .ThenInclude(b => b!.Language)
            .Include(si => si.BookInStore)
                .ThenInclude(bis => bis!.Book)
                    .ThenInclude(b => b!.CoverType)
            .Include(si => si.BookInStore)
                .ThenInclude(bis => bis!.Book)
                    .ThenInclude(b => b!.Subject)
            .Include(si => si.BookInStore)
                .ThenInclude(bis => bis!.Book)
                    .ThenInclude(b => b!.BookType)
            .Include(si => si.BookInStore)
                .ThenInclude(bis => bis!.Book)
                    .ThenInclude(b => b!.BookAuthors)
                        .ThenInclude(ba => ba.Author)
            .Where(si => si.SalesReceipt != null
                && si.BookInStore != null
                && si.BookInStore.Book != null);

        if (from.HasValue)
            query = query.Where(si => si.SalesReceipt!.SalesReceiptDateTime >= from);
        if (to.HasValue)
            query = query.Where(si => si.SalesReceipt!.SalesReceiptDateTime <= to);

        return await query.ToListAsync();
    }

    private record LabeledItem(string Label, DateTime? Date, int Qty);

    private static IEnumerable<LabeledItem> GetLabeledItems(List<SaleReceiptItem> items, string characteristic)
    {
        if (characteristic.Equals("author", StringComparison.OrdinalIgnoreCase))
        {
            return items
                .Where(si => si.BookInStore?.Book != null)
                .SelectMany(si =>
                {
                    var authors = si.BookInStore!.Book!.BookAuthors.ToList();
                    if (authors.Count == 0)
                        return (IEnumerable<LabeledItem>)[new LabeledItem("(без автора)",
                            si.SalesReceipt?.SalesReceiptDateTime, si.SaleReceiptItemQuantity)];
                    return authors.Select(ba => new LabeledItem(
                        ba.Author.AuthorFullName,
                        si.SalesReceipt?.SalesReceiptDateTime,
                        si.SaleReceiptItemQuantity));
                });
        }

        return items
            .Where(si => si.BookInStore?.Book != null)
            .Select(si => new LabeledItem(
                GetLabel(si.BookInStore!.Book!, characteristic),
                si.SalesReceipt?.SalesReceiptDateTime,
                si.SaleReceiptItemQuantity));
    }

    private static string GetLabel(Book book, string characteristic) =>
        characteristic.ToLower() switch
        {
            "genre"     => book.Genre?.GenreName               ?? "(без жанру)",
            "publisher" => book.Publisher?.PublisherName        ?? "(без видавця)",
            "language"  => book.Language?.LanguageName          ?? "(без мови)",
            "covertype" => book.CoverType?.CoverTypeName        ?? "(без типу обкладинки)",
            "subject"   => book.Subject?.SubjectName            ?? "(без тематики)",
            "booktype"  => book.BookType?.BookTypeName          ?? "(без типу книги)",
            "book"      => book.BookTitle,
            "year"      => book.BookPublishYear?.ToString()     ?? "(без року)",
            "price"     => FormatPriceBucket(book.BookPrice),
            _           => "(невідомо)"
        };

    private static string FormatPriceBucket(decimal price) => price switch
    {
        < 100m   => "до 100 грн",
        < 200m   => "100–200 грн",
        < 300m   => "200–300 грн",
        < 500m   => "300–500 грн",
        < 1000m  => "500–1000 грн",
        _        => "1000+ грн"
    };
}

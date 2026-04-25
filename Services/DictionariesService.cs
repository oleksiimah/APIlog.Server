using APIlog.Server.DTOs.Dictionaries;
using APIlog.Server.Infrastructure.Data;
using APIlog.Server.Models;
using Microsoft.EntityFrameworkCore;

namespace APIlog.Server.Services;

public class DictionariesService : IDictionariesService
{
    private readonly BookstoreDbContext _db;

    private static readonly Dictionary<string, string> EntityDisplayNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ["authors"] = "Автори",
        ["publishers"] = "Видавництва",
        ["genres"] = "Жанри",
        ["subjects"] = "Тематики",
        ["languages"] = "Мови",
        ["covertypes"] = "Типи обкладинки",
        ["booktypes"] = "Типи книги",
        ["posts"] = "Посади",
        ["paymentmethods"] = "Методи оплати",
        ["suppliers"] = "Постачальники",
        ["purchasestatuses"] = "Статуси закупівель"
    };

    public DictionariesService(BookstoreDbContext db)
    {
        _db = db;
    }

    public async Task<IEnumerable<DictionaryCategoryDto>> GetCategoriesAsync()
    {
        var counts = await Task.WhenAll(
            _db.Authors.CountAsync(),
            _db.Publishers.CountAsync(),
            _db.Genres.CountAsync(),
            _db.Subjects.CountAsync(),
            _db.Languages.CountAsync(),
            _db.CoverTypes.CountAsync(),
            _db.BookTypes.CountAsync(),
            _db.Posts.CountAsync(),
            _db.PaymentMethods.CountAsync(),
            _db.Suppliers.CountAsync(),
            _db.PurchaseReceiptStatuses.CountAsync()
        );

        var keys = EntityDisplayNames.Keys.ToArray();
        return keys.Zip(counts, (key, count) =>
            new DictionaryCategoryDto(key, EntityDisplayNames[key], count));
    }

    public async Task<IEnumerable<DictionaryItemDto>> GetItemsAsync(string entity)
    {
        return entity.ToLower() switch
        {
            "authors" => (await _db.Authors.OrderBy(x => x.AuthorFullName).ToListAsync())
                .Select(x => new DictionaryItemDto(x.AuthorId, x.AuthorFullName)),
            "publishers" => (await _db.Publishers.OrderBy(x => x.PublisherName).ToListAsync())
                .Select(x => new DictionaryItemDto(x.PublisherId, x.PublisherName)),
            "genres" => (await _db.Genres.OrderBy(x => x.GenreName).ToListAsync())
                .Select(x => new DictionaryItemDto(x.GenreId, x.GenreName)),
            "subjects" => (await _db.Subjects.OrderBy(x => x.SubjectName).ToListAsync())
                .Select(x => new DictionaryItemDto(x.SubjectId, x.SubjectName)),
            "languages" => (await _db.Languages.OrderBy(x => x.LanguageName).ToListAsync())
                .Select(x => new DictionaryItemDto(x.LanguageId, x.LanguageName)),
            "covertypes" => (await _db.CoverTypes.OrderBy(x => x.CoverTypeName).ToListAsync())
                .Select(x => new DictionaryItemDto(x.CoverTypeId, x.CoverTypeName)),
            "booktypes" => (await _db.BookTypes.OrderBy(x => x.BookTypeName).ToListAsync())
                .Select(x => new DictionaryItemDto(x.BookTypeId, x.BookTypeName)),
            "posts" => (await _db.Posts.OrderBy(x => x.PostName).ToListAsync())
                .Select(x => new DictionaryItemDto(x.PostId, x.PostName)),
            "paymentmethods" => (await _db.PaymentMethods.OrderBy(x => x.PaymentMethodName).ToListAsync())
                .Select(x => new DictionaryItemDto(x.PaymentMethodId, x.PaymentMethodName)),
            "suppliers" => (await _db.Suppliers.OrderBy(x => x.SupplierName).ToListAsync())
                .Select(x => new DictionaryItemDto(x.SupplierId, x.SupplierName)),
            "purchasestatuses" => (await _db.PurchaseReceiptStatuses.OrderBy(x => x.PurchaseReceiptStatusName).ToListAsync())
                .Select(x => new DictionaryItemDto(x.PurchaseReceiptStatusId, x.PurchaseReceiptStatusName)),
            _ => throw new ArgumentException($"Unknown entity: {entity}")
        };
    }

    public async Task<DictionaryItemDto> CreateItemAsync(string entity, CreateDictionaryItemDto dto)
    {
        return entity.ToLower() switch
        {
            "authors" => await CreateAndReturn<Author>(
                new Author { AuthorFullName = dto.Name },
                x => new DictionaryItemDto(x.AuthorId, x.AuthorFullName)),
            "publishers" => await CreateAndReturn<Publisher>(
                new Publisher { PublisherName = dto.Name },
                x => new DictionaryItemDto(x.PublisherId, x.PublisherName)),
            "genres" => await CreateAndReturn<Genre>(
                new Genre { GenreName = dto.Name },
                x => new DictionaryItemDto(x.GenreId, x.GenreName)),
            "subjects" => await CreateAndReturn<Subject>(
                new Subject { SubjectName = dto.Name },
                x => new DictionaryItemDto(x.SubjectId, x.SubjectName)),
            "languages" => await CreateAndReturn<Language>(
                new Language { LanguageName = dto.Name },
                x => new DictionaryItemDto(x.LanguageId, x.LanguageName)),
            "covertypes" => await CreateAndReturn<CoverType>(
                new CoverType { CoverTypeName = dto.Name },
                x => new DictionaryItemDto(x.CoverTypeId, x.CoverTypeName)),
            "booktypes" => await CreateAndReturn<BookType>(
                new BookType { BookTypeName = dto.Name },
                x => new DictionaryItemDto(x.BookTypeId, x.BookTypeName)),
            "posts" => await CreateAndReturn<Post>(
                new Post { PostName = dto.Name },
                x => new DictionaryItemDto(x.PostId, x.PostName)),
            "paymentmethods" => await CreateAndReturn<PaymentMethod>(
                new PaymentMethod { PaymentMethodName = dto.Name },
                x => new DictionaryItemDto(x.PaymentMethodId, x.PaymentMethodName)),
            "suppliers" => await CreateAndReturn<Supplier>(
                new Supplier { SupplierName = dto.Name },
                x => new DictionaryItemDto(x.SupplierId, x.SupplierName)),
            _ => throw new ArgumentException($"Unknown entity: {entity}")
        };
    }

    public async Task<DictionaryItemDto> UpdateItemAsync(string entity, int id, UpdateDictionaryItemDto dto)
    {
        switch (entity.ToLower())
        {
            case "authors":
                var author = await _db.Authors.FindAsync(id)
                    ?? throw new KeyNotFoundException();
                author.AuthorFullName = dto.Name;
                await _db.SaveChangesAsync();
                return new DictionaryItemDto(author.AuthorId, author.AuthorFullName);
            case "publishers":
                var publisher = await _db.Publishers.FindAsync(id)
                    ?? throw new KeyNotFoundException();
                publisher.PublisherName = dto.Name;
                await _db.SaveChangesAsync();
                return new DictionaryItemDto(publisher.PublisherId, publisher.PublisherName);
            case "genres":
                var genre = await _db.Genres.FindAsync(id) ?? throw new KeyNotFoundException();
                genre.GenreName = dto.Name;
                await _db.SaveChangesAsync();
                return new DictionaryItemDto(genre.GenreId, genre.GenreName);
            case "subjects":
                var subject = await _db.Subjects.FindAsync(id) ?? throw new KeyNotFoundException();
                subject.SubjectName = dto.Name;
                await _db.SaveChangesAsync();
                return new DictionaryItemDto(subject.SubjectId, subject.SubjectName);
            case "languages":
                var language = await _db.Languages.FindAsync(id) ?? throw new KeyNotFoundException();
                language.LanguageName = dto.Name;
                await _db.SaveChangesAsync();
                return new DictionaryItemDto(language.LanguageId, language.LanguageName);
            case "covertypes":
                var coverType = await _db.CoverTypes.FindAsync(id) ?? throw new KeyNotFoundException();
                coverType.CoverTypeName = dto.Name;
                await _db.SaveChangesAsync();
                return new DictionaryItemDto(coverType.CoverTypeId, coverType.CoverTypeName);
            case "booktypes":
                var bookType = await _db.BookTypes.FindAsync(id) ?? throw new KeyNotFoundException();
                bookType.BookTypeName = dto.Name;
                await _db.SaveChangesAsync();
                return new DictionaryItemDto(bookType.BookTypeId, bookType.BookTypeName);
            case "posts":
                var post = await _db.Posts.FindAsync(id) ?? throw new KeyNotFoundException();
                post.PostName = dto.Name;
                await _db.SaveChangesAsync();
                return new DictionaryItemDto(post.PostId, post.PostName);
            case "paymentmethods":
                var method = await _db.PaymentMethods.FindAsync(id) ?? throw new KeyNotFoundException();
                method.PaymentMethodName = dto.Name;
                await _db.SaveChangesAsync();
                return new DictionaryItemDto(method.PaymentMethodId, method.PaymentMethodName);
            case "suppliers":
                var supplier = await _db.Suppliers.FindAsync(id) ?? throw new KeyNotFoundException();
                supplier.SupplierName = dto.Name;
                await _db.SaveChangesAsync();
                return new DictionaryItemDto(supplier.SupplierId, supplier.SupplierName);
            default:
                throw new ArgumentException($"Unknown entity: {entity}");
        }
    }

    public async Task DeleteItemAsync(string entity, int id)
    {
        switch (entity.ToLower())
        {
            case "authors":
                var author = await _db.Authors.FindAsync(id) ?? throw new KeyNotFoundException();
                _db.Authors.Remove(author); break;
            case "publishers":
                var publisher = await _db.Publishers.FindAsync(id) ?? throw new KeyNotFoundException();
                _db.Publishers.Remove(publisher); break;
            case "genres":
                var genre = await _db.Genres.FindAsync(id) ?? throw new KeyNotFoundException();
                _db.Genres.Remove(genre); break;
            case "subjects":
                var subject = await _db.Subjects.FindAsync(id) ?? throw new KeyNotFoundException();
                _db.Subjects.Remove(subject); break;
            case "languages":
                var language = await _db.Languages.FindAsync(id) ?? throw new KeyNotFoundException();
                _db.Languages.Remove(language); break;
            case "covertypes":
                var coverType = await _db.CoverTypes.FindAsync(id) ?? throw new KeyNotFoundException();
                _db.CoverTypes.Remove(coverType); break;
            case "booktypes":
                var bookType = await _db.BookTypes.FindAsync(id) ?? throw new KeyNotFoundException();
                _db.BookTypes.Remove(bookType); break;
            case "posts":
                var post = await _db.Posts.FindAsync(id) ?? throw new KeyNotFoundException();
                _db.Posts.Remove(post); break;
            case "paymentmethods":
                var method = await _db.PaymentMethods.FindAsync(id) ?? throw new KeyNotFoundException();
                _db.PaymentMethods.Remove(method); break;
            case "suppliers":
                var supplier = await _db.Suppliers.FindAsync(id) ?? throw new KeyNotFoundException();
                _db.Suppliers.Remove(supplier); break;
            default:
                throw new ArgumentException($"Unknown entity: {entity}");
        }
        await _db.SaveChangesAsync();
    }

    private async Task<DictionaryItemDto> CreateAndReturn<T>(T entity, Func<T, DictionaryItemDto> map)
        where T : class
    {
        _db.Set<T>().Add(entity);
        await _db.SaveChangesAsync();
        return map(entity);
    }
}

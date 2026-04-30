using APIlog.Server.DTOs.Branches;
using APIlog.Server.Infrastructure.Data;
using APIlog.Server.Models;
using Microsoft.EntityFrameworkCore;

namespace APIlog.Server.Services;

public class BranchesService : IBranchesService
{
    private readonly BookstoreDbContext _db;

    public BranchesService(BookstoreDbContext db)
    {
        _db = db;
    }

    public async Task<IEnumerable<BranchListItemDto>> GetBranchesAsync()
    {
        var stores = await _db.BookStores
            .Include(bs => bs.Employees)
            .Include(bs => bs.BookInStores)
            .OrderBy(bs => bs.BookStoreCode)
            .ToListAsync();

        return stores.Select(bs => new BranchListItemDto(
            bs.BookStoreId,
            bs.BookStoreCode,
            bs.BookStoreName,
            bs.BookStoreAddress,
            bs.Employees.Count,
            bs.BookInStores.Count(bis => bis.BookInStoreQuantity > 0),
            bs.BookInStores.Sum(bis => (int)bis.BookInStoreQuantity),
            CanDelete: bs.Employees.Count == 0 && !bs.BookInStores.Any(bis => bis.BookInStoreQuantity > 0)
        ));
    }

    public async Task<BranchListItemDto> CreateBranchAsync(CreateBranchDto dto)
    {
        var store = new BookStore
        {
            BookStoreCode = dto.BookStoreCode,
            BookStoreName = dto.BookStoreName,
            BookStoreAddress = dto.BookStoreAddress
        };

        _db.BookStores.Add(store);
        await _db.SaveChangesAsync();

        return new BranchListItemDto(
            store.BookStoreId, store.BookStoreCode,
            store.BookStoreName, store.BookStoreAddress,
            0, 0, 0);
    }

    public async Task<BranchListItemDto> UpdateBranchAsync(int id, UpdateBranchDto dto)
    {
        var store = await _db.BookStores
            .Include(bs => bs.Employees)
            .Include(bs => bs.BookInStores)
            .FirstOrDefaultAsync(bs => bs.BookStoreId == id)
            ?? throw new KeyNotFoundException($"BookStore {id} not found.");

        store.BookStoreCode = dto.BookStoreCode;
        store.BookStoreName = dto.BookStoreName;
        store.BookStoreAddress = dto.BookStoreAddress;

        await _db.SaveChangesAsync();

        return new BranchListItemDto(
            store.BookStoreId, store.BookStoreCode,
            store.BookStoreName, store.BookStoreAddress,
            store.Employees.Count,
            store.BookInStores.Count(bis => bis.BookInStoreQuantity > 0),
            store.BookInStores.Sum(bis => (int)bis.BookInStoreQuantity)
        );
    }

    public async Task DeleteBranchAsync(int id)
    {
        var store = await _db.BookStores.FindAsync(id)
            ?? throw new KeyNotFoundException($"BookStore {id} not found.");

        if (await _db.Employees.AnyAsync(e => e.BookStoreId == id))
            throw new InvalidOperationException("Філія має співробітників і не може бути видалена.");

        if (await _db.BookInStores.AnyAsync(bis => bis.BookStoreId == id && bis.BookInStoreQuantity > 0))
            throw new InvalidOperationException("Філія має книги в наявності і не може бути видалена.");

        _db.BookStores.Remove(store);
        await _db.SaveChangesAsync();
    }
}

using APIlog.Server.Models;
using Microsoft.EntityFrameworkCore;

namespace APIlog.Server.Infrastructure.Data;

public class BookstoreDbContext : DbContext
{
    public BookstoreDbContext(DbContextOptions<BookstoreDbContext> options) : base(options) { }

    public DbSet<Author> Authors => Set<Author>();
    public DbSet<Book> Books => Set<Book>();
    public DbSet<BookAuthor> BooksAuthors => Set<BookAuthor>();
    public DbSet<BookInStore> BookInStores => Set<BookInStore>();
    public DbSet<BookStore> BookStores => Set<BookStore>();
    public DbSet<BookType> BookTypes => Set<BookType>();
    public DbSet<CoverType> CoverTypes => Set<CoverType>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<Genre> Genres => Set<Genre>();
    public DbSet<Language> Languages => Set<Language>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<PaymentMethod> PaymentMethods => Set<PaymentMethod>();
    public DbSet<Post> Posts => Set<Post>();
    public DbSet<Publisher> Publishers => Set<Publisher>();
    public DbSet<PurchaseReceipt> PurchaseReceipts => Set<PurchaseReceipt>();
    public DbSet<PurchaseReceiptItem> PurchaseReceiptItems => Set<PurchaseReceiptItem>();
    public DbSet<PurchaseReceiptStatus> PurchaseReceiptStatuses => Set<PurchaseReceiptStatus>();
    public DbSet<SalesReceipt> SalesReceipts => Set<SalesReceipt>();
    public DbSet<SaleReceiptItem> SaleReceiptItems => Set<SaleReceiptItem>();
    public DbSet<Subject> Subjects => Set<Subject>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<SupplyReceipt> SupplyReceipts => Set<SupplyReceipt>();
    public DbSet<SupplyReceiptItem> SupplyReceiptItems => Set<SupplyReceiptItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // BooksAuthors — composite primary key
        modelBuilder.Entity<BookAuthor>()
            .HasKey(ba => new { ba.BookId, ba.AuthorId });

        // Book → Genre: non-conventional FK name (BookGenreId vs expected GenreId)
        modelBuilder.Entity<Book>()
            .HasOne(b => b.Genre)
            .WithMany(g => g.Books)
            .HasForeignKey(b => b.BookGenreId);

        // Employee → Post: non-conventional FK name (EmployeePostId vs expected PostId)
        modelBuilder.Entity<Employee>()
            .HasOne(e => e.Post)
            .WithMany(p => p.Employees)
            .HasForeignKey(e => e.EmployeePostId);

        // Book — decimal column types
        modelBuilder.Entity<Book>(entity =>
        {
            entity.Property(b => b.BookPrice).HasColumnType("decimal(7,2)");
            entity.Property(b => b.BookHeight).HasColumnType("decimal(3,1)");
            entity.Property(b => b.BookWidth).HasColumnType("decimal(3,1)");
            entity.Property(b => b.BookDepth).HasColumnType("decimal(3,1)");
        });

        // PurchaseReceiptItem — decimal column type
        modelBuilder.Entity<PurchaseReceiptItem>()
            .Property(pi => pi.BookPricePerUnit)
            .HasColumnType("decimal(7,2)");

        // Payment — decimal + server-side default datetime + unique index
        modelBuilder.Entity<Payment>(entity =>
        {
            entity.Property(p => p.PaymentAmount).HasColumnType("decimal(9,2)");
            entity.Property(p => p.PaymentDateTime).HasDefaultValueSql("sysdatetime()");
            entity.HasIndex(p => p.PaymentNumber).IsUnique();
        });

        // PurchaseReceipt — decimal + server-side default datetime + unique index
        modelBuilder.Entity<PurchaseReceipt>(entity =>
        {
            entity.Property(pr => pr.PurchaseReceiptTotalAmount).HasColumnType("decimal(9,2)");
            entity.Property(pr => pr.PurchaseReceiptDateTime).HasDefaultValueSql("sysdatetime()");
            entity.HasIndex(pr => pr.PurchaseReceiptNumber).IsUnique();
        });

        // SalesReceipt — decimal + server-side default datetime + unique index
        modelBuilder.Entity<SalesReceipt>(entity =>
        {
            entity.Property(sr => sr.SalesReceiptTotalAmount).HasColumnType("decimal(9,2)");
            entity.Property(sr => sr.SalesReceiptDateTime).HasDefaultValueSql("sysdatetime()");
            entity.HasIndex(sr => sr.SalesReceiptNumber).IsUnique();
        });

        // SupplyReceipt — decimal + server-side default datetime + unique index
        modelBuilder.Entity<SupplyReceipt>(entity =>
        {
            entity.Property(sr => sr.SupplyReceiptTotalAmount).HasColumnType("decimal(9,2)");
            entity.Property(sr => sr.SupplyReceiptDateTime).HasDefaultValueSql("sysdatetime()");
            entity.HasIndex(sr => sr.SupplyReceiptNumber).IsUnique();
        });

        // BookStore — unique code
        modelBuilder.Entity<BookStore>()
            .HasIndex(bs => bs.BookStoreCode)
            .IsUnique();

        // BookInStore — default quantity
        modelBuilder.Entity<BookInStore>()
            .Property(bis => bis.BookInStoreQuantity)
            .HasDefaultValue((short)0);
    }
}

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace APIlog.Server.Models;

[Table("Books")]
public class Book
{
    [Key]
    public int BookId { get; set; }

    [Required]
    [MaxLength(200)]
    public string BookTitle { get; set; } = string.Empty;

    public decimal BookPrice { get; set; }

    public int? PublisherId { get; set; }
    public int? LanguageId { get; set; }
    public int? CoverTypeId { get; set; }
    public short? BookPublishYear { get; set; }
    public int? SubjectId { get; set; }
    public short? BookPageCount { get; set; }
    public decimal? BookHeight { get; set; }
    public decimal? BookWidth { get; set; }
    public decimal? BookDepth { get; set; }
    public int? BookTypeId { get; set; }
    public int? BookGenreId { get; set; }
    public bool? BookHasIllustrations { get; set; }

    [MaxLength(13)]
    public string? ISBN { get; set; }

    public Publisher? Publisher { get; set; }
    public Language? Language { get; set; }
    public CoverType? CoverType { get; set; }
    public Subject? Subject { get; set; }
    public BookType? BookType { get; set; }
    public Genre? Genre { get; set; }

    public ICollection<BookAuthor> BookAuthors { get; set; } = [];
    public ICollection<BookInStore> BookInStores { get; set; } = [];
    public ICollection<PurchaseReceiptItem> PurchaseReceiptItems { get; set; } = [];
}

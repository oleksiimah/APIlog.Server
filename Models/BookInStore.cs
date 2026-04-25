using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace APIlog.Server.Models;

[Table("BookInStores")]
public class BookInStore
{
    [Key]
    public int BookInStoreId { get; set; }

    public int? BookId { get; set; }
    public int? BookStoreId { get; set; }
    public short BookInStoreQuantity { get; set; }

    public Book? Book { get; set; }
    public BookStore? BookStore { get; set; }

    public ICollection<SaleReceiptItem> SaleReceiptItems { get; set; } = [];
    public ICollection<SupplyReceiptItem> SupplyReceiptItems { get; set; } = [];
}

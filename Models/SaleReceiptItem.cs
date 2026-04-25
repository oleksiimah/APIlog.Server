using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace APIlog.Server.Models;

[Table("SaleReceiptItems")]
public class SaleReceiptItem
{
    [Key]
    public int SaleReceiptItemId { get; set; }

    public int? SalesReceiptId { get; set; }
    public int? SaleBookInStoreId { get; set; }
    public byte SaleReceiptItemQuantity { get; set; }

    public SalesReceipt? SalesReceipt { get; set; }

    [ForeignKey(nameof(SaleBookInStoreId))]
    public BookInStore? BookInStore { get; set; }
}

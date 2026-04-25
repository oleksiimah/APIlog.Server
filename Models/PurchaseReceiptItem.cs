using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace APIlog.Server.Models;

[Table("PurchaseReceiptItems")]
public class PurchaseReceiptItem
{
    [Key]
    public int PurchaseReceiptItemId { get; set; }

    public int? PurchaseReceiptId { get; set; }
    public int? BookId { get; set; }
    public short PurchaseReceiptItemQuantity { get; set; }
    public decimal BookPricePerUnit { get; set; }

    public PurchaseReceipt? PurchaseReceipt { get; set; }
    public Book? Book { get; set; }

    public ICollection<SupplyReceiptItem> SupplyReceiptItems { get; set; } = [];
}

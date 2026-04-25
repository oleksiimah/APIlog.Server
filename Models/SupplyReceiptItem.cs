using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace APIlog.Server.Models;

[Table("SupplyReceiptItems")]
public class SupplyReceiptItem
{
    [Key]
    public int SupplyReceiptItemId { get; set; }

    public int? SupplyReceiptId { get; set; }
    public int? PurchaseReceiptItemId { get; set; }
    public int? BookInStoreId { get; set; }
    public short SupplyReceiptItemQuantity { get; set; }

    public SupplyReceipt? SupplyReceipt { get; set; }
    public PurchaseReceiptItem? PurchaseReceiptItem { get; set; }
    public BookInStore? BookInStore { get; set; }
}

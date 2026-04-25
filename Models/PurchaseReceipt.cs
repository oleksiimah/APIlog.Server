using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace APIlog.Server.Models;

[Table("PurchaseReceipts")]
public class PurchaseReceipt
{
    [Key]
    public int PurchaseReceiptId { get; set; }

    [Required]
    [MaxLength(13)]
    public string PurchaseReceiptNumber { get; set; } = string.Empty;

    public int? SupplierId { get; set; }
    public int? EmployeeId { get; set; }
    public DateTime? PurchaseReceiptDateTime { get; set; }
    public DateTime? PurchaseReceiptSupplyDateTime { get; set; }
    public decimal? PurchaseReceiptTotalAmount { get; set; }
    public int? PurchaseReceiptStatusId { get; set; }

    public Supplier? Supplier { get; set; }
    public Employee? Employee { get; set; }
    public PurchaseReceiptStatus? PurchaseReceiptStatus { get; set; }

    public ICollection<PurchaseReceiptItem> PurchaseReceiptItems { get; set; } = [];
    public ICollection<SupplyReceipt> SupplyReceipts { get; set; } = [];
}

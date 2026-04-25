using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace APIlog.Server.Models;

[Table("SupplyReceipts")]
public class SupplyReceipt
{
    [Key]
    public int SupplyReceiptId { get; set; }

    [Required]
    [MaxLength(13)]
    public string SupplyReceiptNumber { get; set; } = string.Empty;

    public int? EmployeeId { get; set; }
    public DateTime? SupplyReceiptDateTime { get; set; }
    public decimal? SupplyReceiptTotalAmount { get; set; }
    public int? PurchaseReceiptId { get; set; }

    public Employee? Employee { get; set; }
    public PurchaseReceipt? PurchaseReceipt { get; set; }

    public ICollection<SupplyReceiptItem> SupplyReceiptItems { get; set; } = [];
}

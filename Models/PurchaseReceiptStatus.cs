using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace APIlog.Server.Models;

[Table("PurchaseReceiptStatuses")]
public class PurchaseReceiptStatus
{
    [Key]
    public int PurchaseReceiptStatusId { get; set; }

    [Required]
    [MaxLength(14)]
    public string PurchaseReceiptStatusName { get; set; } = string.Empty;

    public ICollection<PurchaseReceipt> PurchaseReceipts { get; set; } = [];
}

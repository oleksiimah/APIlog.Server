using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace APIlog.Server.Models;

[Table("Suppliers")]
public class Supplier
{
    [Key]
    public int SupplierId { get; set; }

    [Required]
    [MaxLength(200)]
    public string SupplierName { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? SupplierAddress { get; set; }

    public ICollection<PurchaseReceipt> PurchaseReceipts { get; set; } = [];
}

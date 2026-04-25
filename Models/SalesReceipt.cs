using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace APIlog.Server.Models;

[Table("SalesReceipts")]
public class SalesReceipt
{
    [Key]
    public int SalesReceiptId { get; set; }

    [Required]
    [MaxLength(13)]
    public string SalesReceiptNumber { get; set; } = string.Empty;

    public int? CustomerId { get; set; }
    public int? EmployeeId { get; set; }
    public DateTime? SalesReceiptDateTime { get; set; }
    public decimal? SalesReceiptTotalAmount { get; set; }

    public Customer? Customer { get; set; }
    public Employee? Employee { get; set; }

    public ICollection<SaleReceiptItem> SaleReceiptItems { get; set; } = [];
    public ICollection<Payment> Payments { get; set; } = [];
}

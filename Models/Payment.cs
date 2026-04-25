using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace APIlog.Server.Models;

[Table("Payments")]
public class Payment
{
    [Key]
    public int PaymentId { get; set; }

    public int? SalesReceiptId { get; set; }

    [MaxLength(36)]
    public string? PaymentNumber { get; set; }

    public DateTime? PaymentDateTime { get; set; }

    public decimal PaymentAmount { get; set; }

    public int? PaymentMethodId { get; set; }

    public SalesReceipt? SalesReceipt { get; set; }
    public PaymentMethod? PaymentMethod { get; set; }
}

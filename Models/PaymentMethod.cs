using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace APIlog.Server.Models;

[Table("PaymentMethods")]
public class PaymentMethod
{
    [Key]
    public int PaymentMethodId { get; set; }

    [Required]
    [MaxLength(25)]
    public string PaymentMethodName { get; set; } = string.Empty;

    public ICollection<Payment> Payments { get; set; } = [];
}

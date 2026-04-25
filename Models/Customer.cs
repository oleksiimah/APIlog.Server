using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace APIlog.Server.Models;

[Table("Customers")]
public class Customer
{
    [Key]
    public int CustomerId { get; set; }

    [Required]
    [MaxLength(200)]
    public string CustomerFullName { get; set; } = string.Empty;

    [MaxLength(16)]
    public string? CustomerPhoneNumber { get; set; }

    [MaxLength(254)]
    public string? CustomerEmail { get; set; }

    public ICollection<SalesReceipt> SalesReceipts { get; set; } = [];
}

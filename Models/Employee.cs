using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace APIlog.Server.Models;

[Table("Employees")]
public class Employee
{
    [Key]
    public int EmployeeId { get; set; }

    [Required]
    [MaxLength(200)]
    public string EmployeeFullName { get; set; } = string.Empty;

    public int? EmployeePostId { get; set; }

    [MaxLength(10)]
    public string? EmployeePersonnelNumber { get; set; }

    public int? BookStoreId { get; set; }

    public Post? Post { get; set; }
    public BookStore? BookStore { get; set; }

    public ICollection<SalesReceipt> SalesReceipts { get; set; } = [];
    public ICollection<PurchaseReceipt> PurchaseReceipts { get; set; } = [];
    public ICollection<SupplyReceipt> SupplyReceipts { get; set; } = [];
}

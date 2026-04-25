using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace APIlog.Server.Models;

[Table("BookStores")]
public class BookStore
{
    [Key]
    public int BookStoreId { get; set; }

    [Required]
    [MaxLength(5)]
    public string BookStoreCode { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string BookStoreName { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string BookStoreAddress { get; set; } = string.Empty;

    public ICollection<Employee> Employees { get; set; } = [];
    public ICollection<BookInStore> BookInStores { get; set; } = [];
}

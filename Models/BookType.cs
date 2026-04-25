using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace APIlog.Server.Models;

[Table("BookTypes")]
public class BookType
{
    [Key]
    public int BookTypeId { get; set; }

    [Required]
    [MaxLength(10)]
    public string BookTypeName { get; set; } = string.Empty;

    public ICollection<Book> Books { get; set; } = [];
}

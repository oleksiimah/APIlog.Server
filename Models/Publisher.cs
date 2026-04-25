using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace APIlog.Server.Models;

[Table("Publishers")]
public class Publisher
{
    [Key]
    public int PublisherId { get; set; }

    [Required]
    [MaxLength(200)]
    public string PublisherName { get; set; } = string.Empty;

    public ICollection<Book> Books { get; set; } = [];
}

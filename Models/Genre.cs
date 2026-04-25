using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace APIlog.Server.Models;

[Table("Genres")]
public class Genre
{
    [Key]
    public int GenreId { get; set; }

    [Required]
    [MaxLength(20)]
    public string GenreName { get; set; } = string.Empty;

    public ICollection<Book> Books { get; set; } = [];
}

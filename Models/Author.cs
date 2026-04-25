using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace APIlog.Server.Models;

[Table("Authors")]
public class Author
{
    [Key]
    public int AuthorId { get; set; }

    [Required]
    [MaxLength(200)]
    public string AuthorFullName { get; set; } = string.Empty;

    public ICollection<BookAuthor> BookAuthors { get; set; } = [];
}

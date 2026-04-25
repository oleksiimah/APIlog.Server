using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace APIlog.Server.Models;

[Table("Subjects")]
public class Subject
{
    [Key]
    public int SubjectId { get; set; }

    [Required]
    [MaxLength(200)]
    public string SubjectName { get; set; } = string.Empty;

    public ICollection<Book> Books { get; set; } = [];
}

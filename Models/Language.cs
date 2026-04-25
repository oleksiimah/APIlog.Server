using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace APIlog.Server.Models;

[Table("Languages")]
public class Language
{
    [Key]
    public int LanguageId { get; set; }

    [Required]
    [MaxLength(50)]
    public string LanguageName { get; set; } = string.Empty;

    public ICollection<Book> Books { get; set; } = [];
}

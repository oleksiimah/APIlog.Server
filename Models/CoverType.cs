using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace APIlog.Server.Models;

[Table("CoverTypes")]
public class CoverType
{
    [Key]
    public int CoverTypeId { get; set; }

    [Required]
    [MaxLength(15)]
    public string CoverTypeName { get; set; } = string.Empty;

    public ICollection<Book> Books { get; set; } = [];
}

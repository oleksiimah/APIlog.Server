using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace APIlog.Server.Models;

[Table("Posts")]
public class Post
{
    [Key]
    public int PostId { get; set; }

    [Required]
    [MaxLength(50)]
    public string PostName { get; set; } = string.Empty;

    public ICollection<Employee> Employees { get; set; } = [];
}

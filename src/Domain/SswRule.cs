using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Numerics;

namespace Domain;

public class SswRule
{
    [Key]
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;

    [Column(TypeName = "vector(1536)")]
    public Vector<double>? Embedding { get; set; }
}

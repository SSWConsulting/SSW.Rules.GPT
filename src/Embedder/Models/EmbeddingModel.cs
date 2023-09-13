using Postgrest.Attributes;
using Postgrest.Models;

namespace RulesEmbeddingFunction.Models;

[Table("rules_test")]
public class EmbeddingModel : BaseModel
{
    [PrimaryKey("id", false)]
    public int ID { get; set; }
    
    [Column("name")]
    public string Name { get; set; } = null!;
    
    [Column("content")]
    public string Content { get; set; } = null!;
    
    [Column("embeddings")]
    public string Embeddings { get; set; } = null!;
}
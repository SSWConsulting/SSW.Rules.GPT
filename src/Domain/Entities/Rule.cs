using System.ComponentModel.DataAnnotations.Schema;
using Pgvector.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Pgvector.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;
using Pgvector;

//using Vector = Pgvector.Vector;

namespace Domain.Entities;

public class Rule
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? Content { get; set; }

    [Column(TypeName = "vector(1536)")]
    //TODO: Fix name
    public Vector? Embeddings { get; set; }
}

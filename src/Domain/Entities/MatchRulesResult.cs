using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pgvector;

namespace Domain.Entities;

public class MatchRulesResult
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? Content { get; set; }
    public Vector? Embeddings { get; set; }
    public double? Similarity { get; set; }
}

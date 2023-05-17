using Application.Contracts;
using Domain.DTOs;
using Microsoft.EntityFrameworkCore;
using Pgvector;

namespace Application.Services;

public class EmbeddingNeighboursService
{
    private readonly IRulesContext _rulesContext;

    public EmbeddingNeighboursService(IRulesContext rulesContext)
    {
        _rulesContext = rulesContext;
    }

    public async Task<List<RuleDto>> CalculateNearestNeighbours(List<Vector> vectorList)
    {
        if (vectorList is null)
        {
            return new List<RuleDto>();
        }

        var aggregateList = new List<RuleDto>();

        foreach (var vector in vectorList)
        {
            // Once we upgrade to EF Core 8 use Unmapped Types
            var nearestNeighbours = await _rulesContext.Rules
                //TODO: Fix names
                // .FromSql(@$"
                //     SELECT * FROM rules ORDER BY embeddings <=> {vector} LIMIT 5")
                .FromSql(
                    @$"
                    select
                    *
                    from rules
                    order by (1 - (rules.embeddings <=> {vector})) DESC
                    limit 5"
                )
                .Select(
                    s =>
                        new RuleDto
                        {
                            Id = s.Id,
                            Name = s.Name,
                            Content = s.Content,
                        }
                )
                .ToListAsync();
            //Console.WriteLine("Nearest Neighbours:");
            //nearestNeighbours.ForEach(s => Console.WriteLine(s.Name));
            // only add distinct rules based on id to aggregateList
            aggregateList.AddRange(
                nearestNeighbours.Where(s => !aggregateList.Any(a => a.Id == s.Id))
            );
        }

        // Uncomment to debug most similar rules
        //Console.WriteLine("Aggregate List:");
        //aggregateList.ForEach(s => Console.WriteLine(s.Name));
        return aggregateList;
    }
}

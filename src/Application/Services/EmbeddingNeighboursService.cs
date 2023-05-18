using Microsoft.EntityFrameworkCore;
using Domain;
using Domain.Entities;
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

        var aggregateList = new List<MatchRulesResult>();

        foreach (var vector in vectorList)
        {
            var similarRules = await _rulesContext.MatchRulesResults.FromSql($"SELECT * FROM match_rules({vector}, 10)").ToListAsync();
            // only add distinct rules based on id to aggregateList
            aggregateList.AddRange(
                similarRules.Where(s => !aggregateList.Any(a => a.Id == s.Id))
            );
        }

        var convertedList = aggregateList.Select(s =>
            new RuleDto
            {
                Name = s.Name,
                Content = s.Content,
                Similarity = s.Similarity
            })
            .ToList();
        // Uncomment to debug most similar rules
        //Console.WriteLine("Aggregate List:");
        //aggregateList.ForEach(s => Console.WriteLine(s.Name));

        return convertedList;
    }
}

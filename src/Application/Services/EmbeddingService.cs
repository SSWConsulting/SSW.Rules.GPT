using System.Numerics;
using Domain;
using Microsoft.EntityFrameworkCore;
using NpgsqlTypes;
using Pgvector;
using OpenAI.GPT3.Interfaces;
using OpenAI.GPT3.ObjectModels;
using OpenAI.GPT3.ObjectModels.RequestModels;
using OpenAI.GPT3.ObjectModels.ResponseModels;

namespace Application.Services;

public class EmbeddingService
{
    private readonly IOpenAIService _openAiService;
    private readonly IRulesContext _rulesContext;

    public EmbeddingService(IOpenAIService openAiService, IRulesContext rulesContext)
    {
        _openAiService = openAiService;
        _rulesContext = rulesContext;
    }

    public async Task<List<Pgvector.Vector>> GetEmbedding(List<string> stringList)
    {
        var result = await _openAiService.Embeddings.CreateEmbedding(
            new EmbeddingCreateRequest
            {
                InputAsList = stringList,
                Model = Models.TextEmbeddingAdaV2
            }
        );

        if (result.Successful)
        {
            var vectorList = new List<Pgvector.Vector>();
            foreach (var embedding in result.Data)
            {
                var doubleArray = embedding.Embedding.ToArray();
                var floatArray = doubleArray.Select(s => (float)s).ToArray();
                var vector = new Pgvector.Vector(floatArray);
                vectorList.Add(vector);
            }

            return vectorList;
        }
        else
        {
            if (result.Error == null)
            {
                throw new Exception("Unknown Error");
            }

            Console.WriteLine($"{result.Error.Code}: {result.Error.Message}");
            return null;
        }
    }

    public async Task<List<RuleDto>> CalculateNearestNeighbours(List<Pgvector.Vector> vectorList)
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
                .FromSql(@$"
                    select
                    *
                    from rules
                    order by (1 - (rules.embeddings <=> {vector})) DESC
                    limit 5")
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

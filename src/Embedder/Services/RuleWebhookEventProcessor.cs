using System.Text.RegularExpressions;
using Octokit.Webhooks;
using Octokit.Webhooks.Events;
using RulesEmbeddingFunction.Models;

namespace RulesEmbeddingFunction.Services;

public class RuleWebhookEventProcessor : WebhookEventProcessor
{
    private static readonly Regex RULE_NAME_REGEX = new (@"^(?:rules\/)(.*)(?:\/rule\.md)$");
    
    private readonly EmbeddingService _embeddingService;
    private readonly DatabaseService _database;

    public RuleWebhookEventProcessor(
        EmbeddingService embeddingService, 
        DatabaseService database
    )
    {
        _embeddingService = embeddingService;
        _database = database;
    }
    
    protected override async Task ProcessPushWebhookAsync(WebhookHeaders headers, PushEvent pushEvent)
    {
        //We only care about pushes to main
        if (pushEvent.Ref != "refs/heads/main")
            return;
        
        var embed = new List<string>();
        var remove = new List<string>();

        foreach (var commit in pushEvent.Commits)
        {
            embed.AddRange(
                commit.Added.Select(
                    value => RULE_NAME_REGEX.Match(value).Groups[1].Value));
            
            embed.AddRange(
                commit.Modified.Select(
                    value => RULE_NAME_REGEX.Match(value).Groups[1].Value));
            
            remove.AddRange(
                commit.Removed.Select(
                    value => RULE_NAME_REGEX.Match(value).Groups[1].Value));
        }

        var embeddings = new List<Embedding>();
        
        foreach (var value in embed)
        {
            var embedding = await _embeddingService.GetEmbedding(value);
            if (embedding != null)
                embeddings.AddRange(embedding);
        }
        
        await _database.SaveEmbeddings(embeddings);
        await _database.DeleteEmbeddings(remove);

        await base.ProcessPushWebhookAsync(headers, pushEvent);
    }
}
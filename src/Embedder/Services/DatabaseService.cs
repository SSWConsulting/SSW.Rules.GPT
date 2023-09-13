using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RulesEmbeddingFunction.Models;
using Supabase;

namespace RulesEmbeddingFunction.Services;

public class DatabaseService
{
    private readonly ILogger<DatabaseService> _logger;
    private readonly IConfiguration _configuration;

    public DatabaseService(ILoggerFactory loggerFactory, IConfiguration configuration)
    {
        _configuration = configuration;
        _logger = loggerFactory.CreateLogger<DatabaseService>();
    }
    
    public async Task SaveEmbeddings(ICollection<Embedding> embeddings)
    {
        var options = new SupabaseOptions { AutoConnectRealtime = true };
        var client = new Client(
            _configuration.GetValue<string>("DATABASE_URL"),
            _configuration.GetValue<string>("DATABASE_KEY"),
            options
        );
        
        await client.InitializeAsync();
        await client.Auth.SignIn(
            _configuration.GetValue<string>("DATABASE_EMAIL"),
            _configuration.GetValue<string>("DATABASE_PASSWORD")
        );
        
        //Chose to delete existing embeddings and not update as the number of chunks in the rule could change
        foreach (var embedding in embeddings)
        {
            await client
                .From<EmbeddingModel>()
                .Where(s => s.Name == embedding.Name)
                .Delete();
        }

        var models = embeddings
            .Select(embedding => new EmbeddingModel
            {
                Name = embedding.Name,
                Content = embedding.Content,
                Embeddings = embedding.Embeddings
            })
            .ToList();

        await client.From<EmbeddingModel>().Insert(models);
    }
}
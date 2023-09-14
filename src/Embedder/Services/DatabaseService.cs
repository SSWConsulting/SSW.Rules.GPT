using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RulesEmbeddingFunction.Models;
using Supabase;

namespace RulesEmbeddingFunction.Services;

public class DatabaseService
{
    private readonly ILogger<DatabaseService> _logger;
    private readonly IConfiguration _configuration;

    private Client _client = null!;

    public DatabaseService(ILoggerFactory loggerFactory, IConfiguration configuration)
    {
        _configuration = configuration;
        _logger = loggerFactory.CreateLogger<DatabaseService>();

        Task.Run(() => InitialiseClient().Wait());
    }

    private async Task InitialiseClient()
    {
        var options = new SupabaseOptions { AutoConnectRealtime = true };
        _client = new Client(
            _configuration.GetValue<string>("DATABASE_URL"),
            _configuration.GetValue<string>("DATABASE_KEY"),
            options
        );

        await _client.InitializeAsync();
        await _client.Auth.SignIn(
            _configuration.GetValue<string>("DATABASE_EMAIL"),
            _configuration.GetValue<string>("DATABASE_PASSWORD")
        );
    }

    public async Task SaveEmbeddings(ICollection<Embedding> embeddings)
    {
        //Chose to delete existing embeddings and not update as the number of chunks in the rule could change
        foreach (var embedding in embeddings)
        {
            await _client
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

        await _client.From<EmbeddingModel>().Insert(models);
    }

    public async Task DeleteEmbeddings(IEnumerable<string> rules)
    {
        foreach (var rule in rules)
        {
            await _client
                .From<EmbeddingModel>()
                .Where(s => s.Name == rule)
                .Delete();
        }
    }
}
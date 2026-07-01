using Application.Services;
using Infrastructure;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Pgvector.EntityFrameworkCore;

namespace Application.IntegrationTests.Services;

public class RulesSearchServiceTests
{
    [Theory]
    [InlineData("How do I send a v2 email?", "email-send-a-v2")]
    public async Task RulesSearchService_WhenQueryMatchesRule_ReturnsRuleWithSlugAndUrl(
        string query,
        string expectedSlug
    )
    {
        // Setup — real PostgreSQL + pgvector DB and real OpenAI embeddings (from user secrets).
        IConfiguration mockConfig = new ConfigurationBuilder()
            .AddUserSecrets<WebAPI.SignalR.RulesHub>()
            .Build();

        var connectionString = mockConfig.GetConnectionString("DefaultConnection");

        var dbContextOptions = new DbContextOptionsBuilder<RulesContext>()
            .UseNpgsql(connectionString, x => x.UseVector())
            .Options;
        var rulesContext = new RulesContext(dbContextOptions);
        var embeddingNeighboursService = new EmbeddingNeighboursService(rulesContext);

        var services = new ServiceCollection();
        services.AddHttpClient(OpenAiClientFactory.HttpClientName);
        var httpClientFactory = services.BuildServiceProvider().GetRequiredService<IHttpClientFactory>();

        var clientFactory = new OpenAiClientFactory(mockConfig, httpClientFactory);
        var openAiEmbeddingService = new OpenAiEmbeddingService(
            clientFactory,
            NullLogger<OpenAiEmbeddingService>.Instance
        );

        // SUT
        var rulesSearchService = new RulesSearchService(
            openAiEmbeddingService,
            embeddingNeighboursService
        );

        // Act
        var results = await rulesSearchService.SearchAsync(query, maxResults: 5, minSimilarity: 0.5);

        // Assert
        results.Should().Contain(r => r.Slug == expectedSlug);
        results
            .Should()
            .OnlyContain(r => r.Url == $"{RulesSearchService.RuleUrlBase}/{r.Slug}");
        results.Should().OnlyContain(r => r.Similarity >= 0.5);
    }
}

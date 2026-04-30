using Application.Services;
using Infrastructure;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Pgvector.EntityFrameworkCore;
using SharedClasses;

namespace Application.IntegrationTests.Services;

public class RelevantRulesServiceTests
{
    [Theory]
    [InlineData("Hello", "no-hello")]
    [InlineData("How do I send a v2 email?", "email-send-a-v2")]
    public async Task RelevantRulesService_WhenUserMessageIsX_ReturnsXRule(
        string userMessageString,
        string dbRuleName
    )
    {
        // Setup
        IConfiguration mockConfig = new ConfigurationBuilder()
            .AddUserSecrets<WebAPI.SignalR.RulesHub>()
            .Build();

        var connectionString = mockConfig.GetConnectionString("DefaultConnection");

        var dbContextOptions = new DbContextOptionsBuilder<RulesContext>()
            .UseNpgsql(connectionString, x => x.UseVector())
            .Options;
        var rulesContext = new RulesContext(dbContextOptions);
        var embeddingNeighboursService = new EmbeddingNeighboursService(rulesContext);

        var tokenService = new TokenService();
        var pruningService = new PruningService(tokenService);

        var services = new ServiceCollection();
        services.AddHttpClient(OpenAiClientFactory.HttpClientName);
        var httpClientFactory = services.BuildServiceProvider().GetRequiredService<IHttpClientFactory>();

        var clientFactory = new OpenAiClientFactory(mockConfig, httpClientFactory);
        var openAiEmbeddingService = new OpenAiEmbeddingService(clientFactory);

        // SUT
        var relevantRulesService = new RelevantRulesService(
            openAiEmbeddingService,
            embeddingNeighboursService,
            pruningService
        );

        // Arrange
        var messageList = new List<ChatMessage>
        {
            new ChatMessage(role: "user", content: userMessageString)
        };

        //Act
        var relevantRules = await relevantRulesService.GetRelevantRules(
            messageList,
            apiKey: null,
            gptModel: AvailableGptModels.Gpt54Nano
        );

        //Assert
        relevantRules.Should().Contain(s => s.Name == dbRuleName);
    }
}

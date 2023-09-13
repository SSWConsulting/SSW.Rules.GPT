using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Octokit;
using RulesEmbeddingFunction.Models;
using ProductHeaderValue = Octokit.ProductHeaderValue;

namespace RulesEmbeddingFunction.Services;

public class EmbeddingService
{
    private const int MAX_CHUNK_LENGTH = 1000;
    private const string OPENAI_API_URL = "https://api.openai.com/v1/embeddings";

    private readonly ILogger _logger;
    private readonly IConfiguration _configuration;
    private readonly TokenService _tokenService;

    private readonly GitHubClient _githubClient;
    private readonly HttpClient _openAiHttpClient;

    public EmbeddingService(
        ILoggerFactory loggerFactory,
        IConfiguration configuration,
        TokenService tokenService
    )
    {
        _configuration = configuration;
        _tokenService = tokenService;
        _logger = loggerFactory.CreateLogger<EmbeddingService>();

        var openaiKey = _configuration.GetValue<string>("OPENAI_KEY");
        var githubKey = _configuration.GetValue<string>("GITHUB_KEY");
        
        Console.WriteLine("API KEY: " + _configuration.GetValue<string>("OPENAI_KEY"));

        _githubClient = new GitHubClient(new ProductHeaderValue("rules-embedder"));
        _githubClient.Credentials = new Credentials(githubKey);
        
        _openAiHttpClient = new HttpClient();
        _openAiHttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", openaiKey);
    }

    public async Task<List<Embedding>?> GetEmbedding(string name)
    {
        var path = $"rules/{name}/rule.md";
        var request = await _githubClient.Repository.Content.GetAllContents("SSWConsulting", "SSW.Rules.Content", path);
        var content = request[0].Content;
        if (content.Length == 0)
        {
            _logger.LogError("Failed to read file at {path}.", path);
            return null;
        }

        _logger.LogTrace("Successfully read file at {path}.", path);

        var bodyContent = GetBodyContent(content, name);
        var embedded = new List<Embedding>();

        foreach (var chunk in bodyContent)
        {
            var result = await EmbedText(name, chunk);
            if (result != null)
                embedded.Add(result);
        }

        _logger.LogTrace("Successfully embedded {count} chunks for file at {path}.", embedded.Count, path);

        return embedded;
    }

    private async Task<Embedding?> EmbedText(string name, string value)
    {
        try
        {
            var requestData = JsonSerializer.Serialize(new
            {
                input = value,
                model = "text-embedding-ada-002"
            });

            var response = await _openAiHttpClient.PostAsync(
                OPENAI_API_URL,
                new StringContent(requestData, Encoding.UTF8, "application/json"));

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("HTTP request to OpenAI failed with code {responseCode}.\n {message}", response.StatusCode, response.ReasonPhrase);
                return null;
            }

            var embedding = await JsonSerializer.DeserializeAsync<EmbeddingResponse>(await response.Content.ReadAsStreamAsync());
            if (embedding != null)
                return Embedding.FromResponse(name, value, embedding);

            _logger.LogError("Failed to deserialize response from API.");
            return null;
        }

        catch (Exception e)
        {
            _logger.LogCritical("Error: {message}", e.Message);
            throw;
        }
    }

    private IEnumerable<string> GetBodyContent(string content, string title)
    {
        var cleaned = content.Split("---").Last();

        //Remove markdown syntax (e.g. <!--endintro-->, ::: graybox)
        var formattingRegex = new Regex(@"^:::(.*)", RegexOptions.Multiline);
        var endSectionRegex = new Regex(@"<!--\s*end\w+\s*-->", RegexOptions.Multiline);

        //Remove image tags and bold text tags (but not the bold text itself)
        var imageTagRegex = new Regex(@"^!\[.*?\]\(.*?\)", RegexOptions.Multiline);
        var boldTextRegex = new Regex(@"\*\*", RegexOptions.Multiline);

        //Replace unnecessary line endings and spaces
        var duplicateSpaceRegex = new Regex(@" {2,}", RegexOptions.Multiline);

        cleaned = formattingRegex.Replace(cleaned, "");
        cleaned = endSectionRegex.Replace(cleaned, "");

        cleaned = imageTagRegex.Replace(cleaned, "");
        cleaned = boldTextRegex.Replace(cleaned, "");

        cleaned = cleaned.ReplaceLineEndings(" ");
        cleaned = duplicateSpaceRegex.Replace(cleaned, " ");

        var chunks = _tokenService.GetTokenCount(cleaned) > MAX_CHUNK_LENGTH
            ? SplitIntoChunks(cleaned, title)
            : new List<string> { cleaned };

        return chunks;
    }

    private List<string> SplitIntoChunks(string content, string title)
    {
        var headerRegex = new Regex(@"#{2,}", RegexOptions.Multiline);
        var headerChunks = headerRegex.Split(content);

        var trimmedChunks = new List<string>();

        //We first split by header to try and keep chunks related to the same topic together
        //We then split by length to ensure we don't exceed the max length
        foreach (var chunk in headerChunks)
        {
            var chunkLength = _tokenService.GetTokenCount(chunk);
            if (chunkLength <= MAX_CHUNK_LENGTH)
                trimmedChunks.Add(chunk);

            else
                trimmedChunks.AddRange(SplitByLength(chunk));
        }

        //Prepend the title to each chunk
        for (var i = 0; i < trimmedChunks.Count; i++)
            trimmedChunks[i] = $"# {title}{trimmedChunks[i]}".Trim();

        return trimmedChunks;
    }

    private List<string> SplitByLength(string content)
    {
        var contentLength = _tokenService.GetTokenCount(content);
        var splits = (int)Math.Ceiling((double)contentLength / MAX_CHUNK_LENGTH);

        var words = content.Split(" ");
        var chunks = new List<string>();
        var chunkSize = words.Length / splits;

        for (var i = 0; i < splits; i++)
        {
            var chunkWords = words.Skip(i * chunkSize).Take(chunkSize);
            var chunk = string.Join(" ", chunkWords);

            chunks.Add(chunk);
        }

        return chunks;
    }
}
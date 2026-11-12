using System.Text.RegularExpressions;
using Application.Contracts;
using Domain.DTOs;

namespace Application.Services;

/// <summary>
/// Query-string entry point to the rules vector search, for callers (such as the MCP server)
/// that don't have chat messages. Embeds the query, finds nearest-neighbour rule chunks, then
/// collapses the chunks into one result per rule with a URL and a snippet.
/// </summary>
public class RulesSearchService
{
    public const string RuleUrlBase = "https://www.ssw.com.au/rules";

    private readonly IOpenAiEmbeddingService _embeddingService;
    private readonly EmbeddingNeighboursService _embeddingNeighboursService;

    public RulesSearchService(
        IOpenAiEmbeddingService embeddingService,
        EmbeddingNeighboursService embeddingNeighboursService
    )
    {
        _embeddingService = embeddingService;
        _embeddingNeighboursService = embeddingNeighboursService;
    }

    public async Task<List<RuleSearchResult>> SearchAsync(
        string query,
        int maxResults = 5,
        double minSimilarity = 0.5
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(query);

        // match_rules fetches at most 10 chunks, so we can never surface more than ~10 distinct
        // rules; clamp the inputs so behaviour matches the tool's advertised contract.
        maxResults = Math.Clamp(maxResults, 1, 10);
        minSimilarity = Math.Clamp(minSimilarity, 0d, 1d);

        // Pass apiKey: null so the embedding service uses the server-configured OpenAiApiKey.
        var embedding = await _embeddingService.GetEmbedding(query, apiKey: null);
        if (embedding is null)
        {
            throw new InvalidOperationException(
                "Could not generate an embedding for the query (the embedding service was rate limited or returned no data). Try again shortly."
            );
        }

        var neighbours = await _embeddingNeighboursService.CalculateNearestNeighbours([embedding]);
        return GroupBySlug(neighbours, minSimilarity, maxResults);
    }

    /// <summary>
    /// Collapses chunk-level matches (many rows can share one rule slug) into one result per rule,
    /// keeping the best similarity and best snippet. Pure function — unit-testable without a DB.
    /// </summary>
    public static List<RuleSearchResult> GroupBySlug(
        IEnumerable<RuleDto> rules,
        double minSimilarity,
        int maxResults
    )
    {
        if (maxResults < 1)
        {
            maxResults = 1;
        }

        return rules
            .Where(r => !string.IsNullOrWhiteSpace(r.Name) && (r.Similarity ?? 0) >= minSimilarity)
            .GroupBy(r => r.Name!)
            .Select(group =>
            {
                var best = group.OrderByDescending(r => r.Similarity ?? 0).First();
                return new RuleSearchResult
                {
                    Slug = group.Key,
                    Title = ExtractTitle(best.Content, group.Key),
                    Url = $"{RuleUrlBase}/{group.Key}",
                    Similarity = Math.Round(best.Similarity ?? 0, 4),
                    Snippet = BuildSnippet(best.Content),
                };
            })
            .OrderByDescending(r => r.Similarity)
            .Take(maxResults)
            .ToList();
    }

    /// <summary>
    /// Each stored chunk begins with a "# {title}" heading (added during ingestion); recover the
    /// title from there, falling back to a humanised slug.
    /// </summary>
    internal static string ExtractTitle(string? content, string slug)
    {
        if (!string.IsNullOrWhiteSpace(content))
        {
            var firstLine = content.TrimStart().Split('\n', 2)[0].Trim();
            if (firstLine.StartsWith("# ", StringComparison.Ordinal))
            {
                var title = firstLine[2..].Trim();
                if (title.Length > 0)
                {
                    return title;
                }
            }
        }

        return Humanise(slug);
    }

    internal static string BuildSnippet(string? content, int maxLength = 240)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return string.Empty;
        }

        var text = content.TrimStart();

        // Drop the leading "# {title}" heading line so the snippet is the actual guidance. If the
        // chunk is only a heading (no body), there is nothing meaningful to show.
        if (text.StartsWith('#'))
        {
            var newline = text.IndexOf('\n');
            text = newline >= 0 ? text[(newline + 1)..] : string.Empty;
        }

        text = Regex.Replace(text, @"\s+", " ").Trim();

        if (text.Length <= maxLength)
        {
            return text;
        }

        // Don't cut through the middle of a surrogate pair (e.g. an emoji) — a lone surrogate would
        // serialize to a U+FFFD replacement character in the JSON the agent sees.
        var end = maxLength;
        if (char.IsHighSurrogate(text[end - 1]))
        {
            end--;
        }

        return string.Concat(text.AsSpan(0, end).TrimEnd(), "…");
    }

    private static string Humanise(string slug)
    {
        var words = slug.Split('-', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0)
        {
            return slug;
        }

        words[0] = char.ToUpperInvariant(words[0][0]) + words[0][1..];
        return string.Join(' ', words);
    }
}

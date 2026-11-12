using System.Collections.Concurrent;
using System.Net;
using System.Text.RegularExpressions;
using Application.Contracts;
using Domain.DTOs;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

/// <summary>
/// Fetches the full, authoritative markdown for a rule from the SSW.Rules.Content GitHub repo
/// (raw.githubusercontent.com), caching results in-process. This is preferred over reassembling
/// the chunked, pre-processed content stored in the database, which is lossy.
/// </summary>
public class RuleContentService : IRuleContentService
{
    public const string HttpClientName = "SswRulesContent";

    private const string RawBaseUrl =
        "https://raw.githubusercontent.com/SSWConsulting/SSW.Rules.Content/main/rules";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<RuleContentService> _logger;

    // Caches both hits and misses (null) so repeated bad slugs don't re-hit GitHub.
    private readonly ConcurrentDictionary<string, RuleContent?> _cache = new();

    public RuleContentService(
        IHttpClientFactory httpClientFactory,
        ILogger<RuleContentService> logger
    )
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<RuleContent?> GetRuleContentAsync(
        string slug,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(slug);
        slug = slug.Trim().Trim('/');

        // Slugs are kebab-case identifiers. Reject anything else so an attacker can't path-traverse
        // the raw URL (e.g. "../../other-repo/...") into fetching arbitrary content.
        if (!IsValidSlug(slug))
        {
            return null;
        }

        if (_cache.TryGetValue(slug, out var cached))
        {
            return cached;
        }

        var client = _httpClientFactory.CreateClient(HttpClientName);
        var rawUrl = $"{RawBaseUrl}/{slug}/rule.md";

        using var response = await client.GetAsync(rawUrl, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            // Definitive miss — safe to cache so repeated bad slugs don't re-hit GitHub.
            _cache[slug] = null;
            return null;
        }

        if (!response.IsSuccessStatusCode)
        {
            // Transient (rate limit, 5xx, etc.) — log and return null WITHOUT caching, so a later
            // call can succeed.
            _logger.LogWarning(
                "Failed to fetch SSW rule markdown for slug {Slug} ({StatusCode})",
                slug,
                (int)response.StatusCode
            );
            return null;
        }

        var markdown = await response.Content.ReadAsStringAsync(cancellationToken);
        var (title, body) = ParseFrontMatter(markdown, slug);

        var result = new RuleContent
        {
            Slug = slug,
            Title = title,
            Url = $"{RulesSearchUrlBase}/{slug}",
            Content = body,
        };

        _cache[slug] = result;
        return result;
    }

    // Kept local (not referencing Application) to avoid a project dependency inversion.
    private const string RulesSearchUrlBase = "https://www.ssw.com.au/rules";

    /// <summary>
    /// Strips a leading YAML front-matter block and returns the rule title (from the front-matter
    /// "title:" key, falling back to a humanised slug) plus the markdown body.
    /// </summary>
    internal static (string Title, string Body) ParseFrontMatter(string markdown, string slug)
    {
        var title = Humanise(slug);
        var body = markdown;

        if (markdown.StartsWith("---", StringComparison.Ordinal))
        {
            var match = Regex.Match(
                markdown,
                @"^---\s*\r?\n(.*?)\r?\n---\s*\r?\n",
                RegexOptions.Singleline
            );

            if (match.Success)
            {
                var frontMatter = match.Groups[1].Value;
                body = markdown[match.Length..].TrimStart('\r', '\n');

                var titleMatch = Regex.Match(
                    frontMatter,
                    @"^title:\s*(.+?)\s*$",
                    RegexOptions.Multiline
                );
                if (titleMatch.Success)
                {
                    title = titleMatch.Groups[1].Value.Trim().Trim('\'', '"');
                }
            }
        }

        return (title, body);
    }

    private static bool IsValidSlug(string slug) =>
        slug.Length > 0 && slug.All(c => char.IsAsciiLetterOrDigit(c) || c == '-');

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

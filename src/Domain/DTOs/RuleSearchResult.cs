namespace Domain.DTOs;

/// <summary>
/// A single rule returned by a semantic search, collapsed from one-or-more matching chunks
/// of the same rule. Lightweight by design: a pointer plus a snippet, not the full rule body.
/// Fetch the full markdown via the rule content service / get_ssw_rule tool.
/// </summary>
public class RuleSearchResult
{
    /// <summary>The rule slug, e.g. "do-you-use-async-await". Identical to the DB rule name.</summary>
    public string Slug { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    /// <summary>Canonical public rule URL, e.g. https://www.ssw.com.au/rules/do-you-use-async-await.</summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>Best cosine similarity (0-1) across the rule's matching chunks.</summary>
    public double Similarity { get; set; }

    /// <summary>A short excerpt of the best-matching chunk, for the agent to triage relevance.</summary>
    public string Snippet { get; set; } = string.Empty;
}

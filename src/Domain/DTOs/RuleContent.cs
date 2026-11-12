namespace Domain.DTOs;

/// <summary>
/// The full content of a single SSW rule, fetched authoritatively from the rules content repo
/// (rather than reassembled from the chunked embeddings stored in the database).
/// </summary>
public class RuleContent
{
    public string Slug { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    /// <summary>Canonical public rule URL, e.g. https://www.ssw.com.au/rules/do-you-use-async-await.</summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>The rule's markdown body, with YAML front-matter stripped.</summary>
    public string Content { get; set; } = string.Empty;
}

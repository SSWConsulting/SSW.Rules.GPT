using Domain.DTOs;

namespace Application.Contracts;

/// <summary>
/// Fetches the full, authoritative markdown for a single SSW rule by slug.
/// </summary>
public interface IRuleContentService
{
    /// <summary>
    /// Returns the full rule content, or <c>null</c> if no rule exists for the given slug.
    /// </summary>
    Task<RuleContent?> GetRuleContentAsync(string slug, CancellationToken cancellationToken = default);
}

using System.ComponentModel;
using Application.Contracts;
using Application.Services;
using Domain.DTOs;
using ModelContextProtocol.Server;

namespace WebAPI.Mcp;

/// <summary>
/// MCP tools that expose the SSW Rules knowledge base to coding agents. A new instance is
/// constructed by the MCP runtime per tool invocation, with dependencies resolved from the
/// request scope — so the scoped <see cref="RulesSearchService"/> (and its DbContext) is safe.
/// </summary>
[McpServerToolType]
public class SswRulesTools
{
    private readonly RulesSearchService _rulesSearchService;
    private readonly IRuleContentService _ruleContentService;

    public SswRulesTools(
        RulesSearchService rulesSearchService,
        IRuleContentService ruleContentService
    )
    {
        _rulesSearchService = rulesSearchService;
        _ruleContentService = ruleContentService;
    }

    [McpServerTool(Name = "search_ssw_rules"),
     Description(
         "Search SSW's best-practice rules by topic using semantic (vector) search. Call this "
         + "before writing or reviewing code (Blazor, .NET, Azure, EF Core, Git, etc.) to pull "
         + "relevant SSW standards into context. Returns a ranked list of matching rules, each "
         + "with a title, URL, similarity score and a short snippet. Then fetch the full text of "
         + "the rules you care about with get_ssw_rule."
     )]
    public async Task<IReadOnlyList<RuleSearchResult>> SearchSswRules(
        [Description(
            "What to search for. Describe the tech stack and the task, e.g. 'Blazor MudBlazor "
            + "confirmation dialog for a destructive delete action'."
        )]
            string query,
        [Description("Maximum number of rules to return (1-10). Default 5.")] int maxResults = 5,
        [Description(
            "Minimum cosine similarity (0-1) for a rule to be included. Default 0.5."
        )]
            double minSimilarity = 0.5
    )
    {
        return await _rulesSearchService.SearchAsync(query, maxResults, minSimilarity);
    }

    [McpServerTool(Name = "get_ssw_rule"),
     Description(
         "Fetch the full markdown of a single SSW rule by its slug (the 'slug' field from a "
         + "search_ssw_rules result, e.g. 'do-you-use-async-await'). Use this once you've decided "
         + "a rule is relevant and want its complete guidance, including good/bad examples."
     )]
    public async Task<RuleContent> GetSswRule(
        [Description("The rule slug, e.g. 'do-you-use-async-await'.")] string slug,
        CancellationToken cancellationToken
    )
    {
        var rule = await _ruleContentService.GetRuleContentAsync(slug, cancellationToken);
        if (rule is null)
        {
            throw new InvalidOperationException(
                $"No SSW rule was found for slug '{slug}'. Use search_ssw_rules to discover valid slugs."
            );
        }

        return rule;
    }
}

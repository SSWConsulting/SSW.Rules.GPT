using Application.Services;
using Domain.DTOs;

namespace Application.UnitTests.Services;

public class RulesSearchServiceTests
{
    private static RuleDto Chunk(string? name, string? content, double? similarity) =>
        new()
        {
            Name = name,
            Content = content,
            Similarity = similarity,
        };

    [Fact]
    public void GroupBySlug_WhenRuleHasMultipleChunks_CollapsesToOneResultWithBestSimilarity()
    {
        var rules = new List<RuleDto>
        {
            Chunk("send-a-v2-email", "# Send a v2 email\n Use the v2 endpoint when...", 0.71),
            Chunk("send-a-v2-email", "# Send a v2 email\n Always include a plain-text body...", 0.92),
            Chunk("do-you-use-async-await", "# Do you use async await\n Avoid async void...", 0.64),
        };

        var results = RulesSearchService.GroupBySlug(rules, minSimilarity: 0.5, maxResults: 10);

        results.Should().HaveCount(2);

        var email = results.Single(r => r.Slug == "send-a-v2-email");
        email.Similarity.Should().Be(0.92);
        email.Title.Should().Be("Send a v2 email");
        email.Url.Should().Be("https://www.ssw.com.au/rules/send-a-v2-email");
        email.Snippet.Should().NotContain("#").And.Contain("plain-text body");
    }

    [Fact]
    public void GroupBySlug_WhenSimilarityBelowThreshold_ExcludesRule()
    {
        var rules = new List<RuleDto>
        {
            Chunk("strong-match", "# Strong\n content", 0.80),
            Chunk("weak-match", "# Weak\n content", 0.30),
        };

        var results = RulesSearchService.GroupBySlug(rules, minSimilarity: 0.5, maxResults: 10);

        results.Should().ContainSingle();
        results[0].Slug.Should().Be("strong-match");
    }

    [Fact]
    public void GroupBySlug_WhenMoreMatchesThanMax_ReturnsHighestSimilarityFirstAndCaps()
    {
        var rules = new List<RuleDto>
        {
            Chunk("rule-a", "# A\n a", 0.60),
            Chunk("rule-b", "# B\n b", 0.95),
            Chunk("rule-c", "# C\n c", 0.75),
        };

        var results = RulesSearchService.GroupBySlug(rules, minSimilarity: 0.5, maxResults: 2);

        results.Should().HaveCount(2);
        results[0].Slug.Should().Be("rule-b");
        results[1].Slug.Should().Be("rule-c");
    }

    [Fact]
    public void GroupBySlug_WhenNameIsNullOrEmpty_DropsThoseRows()
    {
        var rules = new List<RuleDto>
        {
            Chunk(null, "# Null\n content", 0.99),
            Chunk("", "# Empty\n content", 0.99),
            Chunk("valid-rule", "# Valid\n content", 0.55),
        };

        var results = RulesSearchService.GroupBySlug(rules, minSimilarity: 0.5, maxResults: 10);

        results.Should().ContainSingle();
        results[0].Slug.Should().Be("valid-rule");
    }

    [Fact]
    public void ExtractTitle_WhenContentHasNoHeading_FallsBackToHumanisedSlug()
    {
        var title = RulesSearchService.ExtractTitle("no heading here", "do-you-use-async-await");

        title.Should().Be("Do you use async await");
    }

    [Fact]
    public void BuildSnippet_WhenContentIsLong_StripsHeadingAndTruncates()
    {
        var content = "# Title\n " + new string('x', 500);

        var snippet = RulesSearchService.BuildSnippet(content, maxLength: 100);

        snippet.Should().NotStartWith("#");
        snippet.Length.Should().BeLessThanOrEqualTo(101); // 100 chars + ellipsis
        snippet.Should().EndWith("…");
    }

    [Fact]
    public void BuildSnippet_WhenContentIsHeadingOnly_ReturnsEmpty()
    {
        RulesSearchService.BuildSnippet("# Only a heading").Should().BeEmpty();
    }

    [Fact]
    public void BuildSnippet_WhenTruncationFallsOnEmoji_DoesNotSplitTheSurrogatePair()
    {
        // 99 'x' then an emoji (surrogate pair) puts the emoji's high surrogate exactly at the
        // truncation boundary (index 99 with maxLength 100). It must not be cut in half.
        var content = "#\n" + new string('x', 99) + "😀" + new string('y', 50);

        var snippet = RulesSearchService.BuildSnippet(content, maxLength: 100);

        // A lone surrogate becomes U+FFFD when round-tripped through UTF-8 (what serialization does).
        var roundTripped = System.Text.Encoding.UTF8.GetString(
            System.Text.Encoding.UTF8.GetBytes(snippet)
        );
        roundTripped.Should().Be(snippet);
        roundTripped.Should().NotContain("�");
    }
}

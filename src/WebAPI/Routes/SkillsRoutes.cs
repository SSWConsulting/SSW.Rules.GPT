using System.Text;

namespace WebAPI.Routes;

/// <summary>
/// Serves agent skill markdown over plain URLs so they can be installed with a one-line curl.
/// The files are the repo's own skills (e.g. <c>.claude/skills/ssw-rules-audit/SKILL.md</c>),
/// linked into the build output under a <c>Skills/</c> folder (see WebAPI.csproj). Anonymous and
/// read-only — map directly on the app, not the authorized <c>api</c> group.
/// </summary>
public static class SkillsRoutes
{
    /// <summary>Rate-limit policy shared by the anonymous public endpoints (/mcp and /skills).</summary>
    public const string PublicRateLimitPolicy = "public-anon";

    private static string SkillsDirectory => Path.Combine(AppContext.BaseDirectory, "Skills");

    public static void MapSkillsRoutes(this IEndpointRouteBuilder app)
    {
        // Index: lists installable skills with a copy-paste install command.
        app.MapGet(
            "/skills",
            (HttpContext context) =>
            {
                var baseUrl = $"{context.Request.Scheme}://{context.Request.Host}";
                var names = ListSkillNames();

                var sb = new StringBuilder();
                sb.AppendLine("# SSW Rules GPT — Agent Skills");
                sb.AppendLine();

                if (names.Count == 0)
                {
                    sb.AppendLine("_No skills are currently published._");
                    return Results.Text(sb.ToString(), "text/markdown", Encoding.UTF8);
                }

                sb.AppendLine(
                    "Install a skill into Claude Code by dropping it into your skills folder:"
                );
                sb.AppendLine();
                foreach (var name in names)
                {
                    sb.AppendLine($"## {name}");
                    sb.AppendLine("```bash");
                    sb.AppendLine($"mkdir -p ~/.claude/skills/{name} && \\");
                    sb.AppendLine($"  curl -fsSL {baseUrl}/skills/{name} -o ~/.claude/skills/{name}/SKILL.md");
                    sb.AppendLine("```");
                    sb.AppendLine();
                }

                return Results.Text(sb.ToString(), "text/markdown", Encoding.UTF8);
            }
        )
            .RequireRateLimiting(PublicRateLimitPolicy)
            .ExcludeFromDescription();

        // Raw skill markdown. Accepts "name" or "name.md".
        app.MapGet(
            "/skills/{name}",
            async (string name) =>
            {
                // Strip any extension/path and allow only skill-name characters — no traversal.
                var safeName = Path.GetFileNameWithoutExtension(name);
                if (
                    string.IsNullOrWhiteSpace(safeName)
                    || !safeName.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_')
                )
                {
                    return Results.NotFound();
                }

                var path = Path.Combine(SkillsDirectory, safeName + ".md");
                if (!File.Exists(path))
                {
                    return Results.NotFound();
                }

                var content = await File.ReadAllTextAsync(path);
                return Results.Text(content, "text/markdown", Encoding.UTF8);
            }
        )
            .RequireRateLimiting(PublicRateLimitPolicy)
            .ExcludeFromDescription();
    }

    private static List<string> ListSkillNames()
    {
        if (!Directory.Exists(SkillsDirectory))
        {
            return [];
        }

        return Directory
            .GetFiles(SkillsDirectory, "*.md")
            .Select(Path.GetFileNameWithoutExtension)
            .OfType<string>()
            .OrderBy(n => n)
            .ToList();
    }
}

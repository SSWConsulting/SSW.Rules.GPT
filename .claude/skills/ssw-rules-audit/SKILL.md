---
name: ssw-rules-audit
description: Audit a codebase (or a diff/PR) against the relevant SSW best-practice rules using the SSW Rules MCP server. Use when the user asks to "check against SSW rules", "do an SSW audit / SSW rules review", "does this follow SSW standards", or to verify code conformance before a PR.
---

# SSW Rules Audit

Check code against SSW's published best-practice rules (https://www.ssw.com.au/rules) and report
violations as actionable, citable findings. The rules are retrieved live via the **SSW Rules MCP
server** — you supply the code-reading and judgement; the server supplies the rules.

## Prerequisites — the MCP tools

This skill depends on two tools from the `ssw-rules` MCP server:

- `search_ssw_rules(query, maxResults?, minSimilarity?)` → ranked `{ slug, title, url, similarity, snippet }`.
- `get_ssw_rule(slug)` → `{ slug, title, url, content }` (full rule markdown).

Before anything else, confirm those tools are available. If they are **not** connected:

- Tell the user to add the server, then stop — never invent rule content. One-time setup:
  ```bash
  claude mcp add --transport http ssw-rules https://rules-gpt.ssw.com.au/mcp
  ```
  (Or, inside the SSW.Rules.GPT repo, the bundled `.mcp.json` points at the local API — run it with
  `dotnet run --project src/WebAPI/WebAPI.csproj --launch-profile https`.)

## Scope (ask only if ambiguous)

Default scope is **the current diff** — cheap and high-signal. Determine it in this order:

1. If the user named a target (a PR, a directory, specific files), use that.
2. Else if there are staged/unstaged changes or commits ahead of the base branch, audit that diff
   (`git diff --merge-base origin/main`, or `git diff` / `git diff --staged`).
3. Else fall back to a focused subset (the most relevant directory) and say you're not auditing the
   whole repo unless asked — whole-repo audits are noisy.

State the chosen scope in one line before proceeding.

## Procedure

1. **Detect the stack.** From the in-scope files and manifests (`*.csproj`, `package.json`, file
   extensions, frameworks) build a short list of technologies/concerns, e.g. `Blazor`, `MudBlazor`,
   `EF Core`, `Azure Functions`, `xUnit`, `git/PR hygiene`.

2. **Gather candidate rules.** For each tech/concern call `search_ssw_rules` with a *specific* query
   naming the tech and the pattern you see (not just "Blazor" — e.g. "Blazor component dispose
   IDisposable event handlers"). Run several targeted searches, not one broad one. Collect the unique
   rules across all of them.

3. **Pull the rules that matter.** For each strong candidate (high similarity, on-topic snippet) call
   `get_ssw_rule(slug)` for the full guidance, including good/bad examples. Skip rules the snippet
   shows are irrelevant — don't fetch everything.

4. **Check the code.** Compare in-scope code against each rule's actual criteria. Every finding must
   point to a concrete `path:line` and a concrete rule.

5. **Verify before reporting (kill false positives).** Rules are contextual. For each candidate
   finding, adversarially ask: *could this be intentional / acceptable here?* Re-read the rule and the
   surrounding code; drop anything you can't defend. Prefer fewer solid findings over a noisy list.

6. **Report.** Group by severity (Must-fix → Should-fix → Consider). Use the SSW good/bad framing:

   ```
   ### ❌ <short title>   ·   `path/to/File.cs:42`
   Rule: [<rule title>](<rule url>)

   What's wrong: <one or two sentences tied to the rule's criteria>

   ✅ Fix: <the concrete change, with a minimal code sketch if useful>
   ```

   End with a one-line summary: counts per severity, plus the list of rule URLs consulted.

## Options

- **`--fix`** (or "and fix them"): after reporting, apply the safe mechanical corrections to the
  working tree, leave judgement-heavy ones as report-only, and summarise what changed vs. was left.
- **`--comment`** / a PR reference: post each finding as an inline review comment on the relevant line
  via `gh`, instead of one report.

## Style

- Be specific and cite the rule URL every time — an uncited finding is just an opinion.
- Don't restate rules the code already follows unless asked.
- Match the target codebase's conventions when proposing fixes.

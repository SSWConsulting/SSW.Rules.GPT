# AGENTS.md
Canonical agent instructions for this repository. `CLAUDE.md` is symlinked to this file.

## Repo Summary
- Repo: `SSW.Rules.GPT`
- Stack: `.NET 10`, ASP.NET Core, Blazor WebAssembly, PostgreSQL, SignalR
- Solution: `SSW.Rules.GPT.slnx`
- Projects:
  - `src/Application` - orchestration and app services
  - `src/Domain` - entities and domain types
  - `src/Infrastructure` - EF Core, OpenAI, persistence
  - `src/WebAPI` - backend and OpenAPI generation
  - `src/WebUI` - Blazor WebAssembly frontend
  - `tests/Application.UnitTests` / `tests/Application.IntegrationTests`
  - `src/AzureFunctions` - Node/Azure Functions helper

## Repo Rules
- No repo-local Cursor rules were found in `.cursor/rules/` or `.cursorrules`.
- No repo-local Copilot rules were found in `.github/copilot-instructions.md`.
- If those files are added later, merge them into this document.

## Core Commands
- Always use `SSW.Rules.GPT.slnx` for solution-wide .NET commands.

Restore:
```bash
dotnet restore "SSW.Rules.GPT.slnx"
```

Build debug:
```bash
dotnet build "SSW.Rules.GPT.slnx"
```

Build release:
```bash
dotnet build "SSW.Rules.GPT.slnx" --configuration Release
```

Test all:
```bash
dotnet test "SSW.Rules.GPT.slnx"
```

Test unit project:
```bash
dotnet test "tests/Application.UnitTests/Application.UnitTests.csproj"
```

Test integration project:
```bash
dotnet test "tests/Application.IntegrationTests/Application.IntegrationTests.csproj"
```

Run one test by fully qualified name:
```bash
dotnet test "tests/Application.IntegrationTests/Application.IntegrationTests.csproj" --filter "FullyQualifiedName~Application.IntegrationTests.Services.RelevantRulesServiceTests.RelevantRulesService_WhenUserMessageIsX_ReturnsXRule"
```

Run one test by display name fragment:
```bash
dotnet test "tests/Application.IntegrationTests/Application.IntegrationTests.csproj" --filter "DisplayName~How do I send a v2 email"
```

Run tests without rebuilding:
```bash
dotnet test "SSW.Rules.GPT.slnx" --no-build
```

Collect coverage:
```bash
dotnet test "SSW.Rules.GPT.slnx" --collect:"XPlat Code Coverage"
```

Run WebAPI:
```bash
dotnet run --project "src/WebAPI/WebAPI.csproj" --launch-profile https
```

Run WebUI:
```bash
dotnet run --project "src/WebUI/WebUI.csproj"
```

Hot reload WebUI:
```bash
dotnet watch run --project "src/WebUI/WebUI.csproj"
```

Local URLs:
- WebAPI: `https://localhost:7104`
- WebUI: `https://localhost:5002`

Azure Functions helper:
```bash
npm install --prefix "src/AzureFunctions"
npm start --prefix "src/AzureFunctions"
```

## Build Notes
- `Directory.Build.props` enables `TreatWarningsAsErrors` in `Release`.
- Debug builds of `src/WebAPI` regenerate `src/WebUI/RulesGptApiClients.cs` via NSwag.
- Build outputs go to `artifacts/`.
- `src/AzureFunctions/package.json` has no real lint/test command; `npm test` is a placeholder.

## Secrets and Configuration
- WebAPI uses user secrets (`src/WebAPI/WebAPI.csproj` has the `UserSecretsId`).
- Common local secrets:
  - `OpenAiApiKey`
  - `ConnectionStrings:DefaultConnection`
- Integration tests load the same secrets using `AddUserSecrets<WebAPI.SignalR.RulesHub>()`.
- Check secrets before changing code if integration tests fail.

Useful commands:
```bash
dotnet user-secrets list --project "src/WebAPI/WebAPI.csproj"
dotnet user-secrets set "OpenAiApiKey" "..." --project "src/WebAPI/WebAPI.csproj"
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "..." --project "src/WebAPI/WebAPI.csproj"
```

## Generated Files
- `src/WebUI/RulesGptApiClients.cs` is generated; do not hand-edit unless unavoidable.
- Prefer changing `src/WebAPI/nswag.json` or the API surface instead.

## Code Style
- There is no repo `.editorconfig`; match existing patterns.
- Use file-scoped namespaces.
- Use 4 spaces for indentation in C#.
- Keep `using` directives at the top.
- Use simple `using` statements; alias only when needed.
- Nullable reference types are enabled; keep nullability honest.
- Prefer constructor injection for services.
- Use `_camelCase` for private readonly fields.
- Use PascalCase for public types, methods, properties, and constants.
- Use camelCase for locals and parameters.
- Prefix interfaces with `I`.
- Preserve local async naming conventions; do not mass-rename to add `Async`.
- Prefer early guard clauses.
- Use `ArgumentNullException.ThrowIfNull(...)` where it improves clarity.
- Keep methods small and extraction-oriented when practical.

## Architecture Conventions
- Put orchestration/business flow in `src/Application`.
- Keep EF Core, persistence, and OpenAI code in `src/Infrastructure`.
- Keep transport in `src/WebAPI` and UI concerns in `src/WebUI`.
- Avoid leaking Web-specific types into Domain.
- Register new services in the relevant `DependencyInjection.cs`.

## Data and Error Handling
- This repo currently uses `Pgvector.EntityFrameworkCore`; do not replace it casually.
- Preserve existing `RulesContext` mappings unless a schema change is intentional.
- If you change database access, rerun integration tests.
- Throw meaningful exceptions for missing required configuration.
- Do not swallow exceptions silently.
- Return `null` only where the surrounding API already uses null as a failure signal.
- Prefer existing ASP.NET Core logging patterns over ad hoc console output.

## Frontend and Tests
- Preserve the existing MudBlazor patterns and overall UI structure.
- Keep auth and API URLs in config, not hardcoded in components.
- Use DI for UI services in `Program.cs` and components.
- Be careful with auth and cross-origin behavior; the app depends on an external signing authority.
- Tests use xUnit and AwesomeAssertions.
- Follow `Thing_WhenCondition_ReturnsOrDoesX` naming.
- Integration tests here are real integration tests unless intentionally mocked.

## Validation Expectations
Preferred validation for substantial changes:
```bash
dotnet build "SSW.Rules.GPT.slnx"
dotnet test "SSW.Rules.GPT.slnx"
```

For targeted changes, run the narrowest relevant validation and report what you ran.

## Known Quirks
- `tests/Application.UnitTests` currently has no discoverable tests.
- The package graph can emit a pgvector/EF Core compatibility warning during restore/build.
- Generated warning suppressions in `src/WebUI/RulesGptApiClients.cs` should be left alone.
- `.github/workflows/weekly-db-keepalive.yml` pings the API health endpoint.

## Agent Notes
- Prefer minimal, surgical edits over broad refactors.
- Do not rename projects, solution files, or generated artifacts unless asked.
- Preserve launch URLs and auth-related config unless the task explicitly changes them.
- If a change touches OpenAPI or API contracts, expect NSwag-generated client churn.
- Keep this file current and keep `CLAUDE.md` pointing here.

# AppAutomation Adoption Journal

## Scope
- Consumer repo: `SkillChat`
- AUT: `SkillChat.Client` (`Avalonia`, `net10.0`)
- Adoption date: `2026-03-24`
- Goal: integrate `AppAutomation`, add deterministic smoke tests, and record friction during real consumer onboarding.

## Entry 1. Pre-flight research
- Step:
  - Reviewed `README.md`, `quickstart.md`, `adoption-checklist.md`, `project-topology.md`, `advanced-integration.md`.
- What worked:
  - The canonical consumer flow is easy to understand once all docs are read together.
  - The split between `Authoring`, `Headless`, `FlaUI`, and `TestHost` is clear.
- Friction:
  - Version guidance is inconsistent across official repo surfaces:
    - repository README Fast Path still references `1.1.0`;
    - GitHub release list shows latest release `1.2.0` dated `2026-03-18`;
    - `docs/appautomation/quickstart.md` on `master` references `2.1.0`.
  - The consumer has to infer which version is authoritative before even running the first command.
- Suggestions:
  - Keep one single version source in the repo and generate all install snippets from it.
  - Add a short "If docs and releases disagree, trust X" note near the Fast Path.
  - Publish a compact compatibility/version matrix directly in the README, not only in deeper docs.

## Entry 2. Template/tool install from quickstart
- Step:
  - Ran `dotnet new install AppAutomation.Templates::2.1.0`.
  - Ran `dotnet tool install --tool-path .\\.tools AppAutomation.Tooling --version 2.1.0`.
- What worked:
  - The commands are copy-pasteable from docs.
- Friction:
  - Both commands failed against the default consumer feed (`https://api.nuget.org/v3/index.json`).
  - `AppAutomation.Templates::2.1.0` was not found.
  - `AppAutomation.Tooling` version `2.1.0` was not found.
  - `dotnet new` additionally warns that `::` is deprecated in favor of `@`, while framework docs still use the deprecated syntax.
- Suggestions:
  - Publish install docs only for versions that are actually available to consumers.
  - Add a visible prerequisite section that states the required package feed if packages are not on `nuget.org`.
  - Update template install syntax from `Package::Version` to `Package@Version`.
  - Add a "verify published artifacts" check to the release process before documentation is updated.

## Entry 3. NuGet package discovery
- Step:
  - Queried official NuGet flat-container endpoints for:
    - `appautomation.tooling`
    - `appautomation.templates`
    - `appautomation.abstractions`
- What worked:
  - Official NuGet API made it easy to confirm consumer-visible package versions.
- Friction:
  - Published reality differs from the quickstart:
    - `AppAutomation.Tooling`: only `1.2.0`
    - `AppAutomation.Templates`: only `1.2.0`
    - `AppAutomation.Abstractions`: `1.0.0`, `1.2.0`
  - `publishing.md` claims a publish example for `2.1.0`, but the consumer feed still exposes `1.2.0`.
- Suggestions:
  - Add a consumer-facing "currently published versions" table to docs, generated from the actual feed.
  - Gate release docs updates on successful publish plus a post-publish validation against NuGet.

## Entry 4. Tool installation fallback
- Step:
  - Installed `AppAutomation.Tooling` as a repo-local .NET tool via manifest at version `1.2.0`.
- What worked:
  - Local manifest install succeeded and gives a reproducible consumer command path (`dotnet tool run appautomation` / `dotnet appautomation`).
- Friction:
  - The documented `--tool-path .\\.tools` route proved brittle in practice during consumer onboarding.
  - The repo-local manifest approach is operationally better for teams, but it is not the documented primary path.
- Suggestions:
  - Make local tool manifest the recommended consumer setup.
  - If `--tool-path` remains documented, explain when it is preferable over a manifest and how it should be committed/ignored.

## Entry 5. Template defaults inspection
- Step:
  - Checked `dotnet new appauto-avalonia --help`.
  - Confirmed template symbols in `.template.config/template.json`.
- What worked:
  - The template exposes `AppAutomationVersion`, `TestTargetFramework`, and `FlaUiTargetFramework`, so consumers can override defaults.
- Friction:
  - Template defaults are stale for a current consumer:
    - default `AppAutomationVersion` is `1.1.0`;
    - default test TFM is `net8.0`;
    - default FlaUI TFM is `net8.0-windows7.0`.
  - A consumer on `net10.0` must override multiple parameters immediately to avoid generating obsolete project files.
- Suggestions:
  - Update template defaults to the latest published package version.
  - Either move default TFMs forward or document very explicitly that the template intentionally targets the lowest supported baseline.

## Entry 6. Doctor and scaffold validation
- Step:
  - Generated canonical topology with:
    - `dotnet new appauto-avalonia --name SkillChat --AppAutomationVersion 1.2.0 --TestTargetFramework net10.0 --FlaUiTargetFramework net10.0-windows7.0`
  - Ran:
    - `dotnet tool run appautomation doctor --repo-root .`
  - Added generated projects to `SkillChat.sln`.
  - Built generated projects individually.
- What worked:
  - Template generated the expected canonical layout.
  - `doctor` succeeded immediately with `0 error(s), 0 warning(s)`.
  - Existing `SkillChat.Client/nuget.config` was sufficient for doctor; no extra root `NuGet.Config` was needed.
  - Generated projects build successfully after adding them to the solution.
- Friction:
  - Consumer docs/checklists strongly imply a root-level `NuGet.Config`, but the real doctor behavior is looser and more useful than the wording suggests.
  - The template intentionally leaves the consumer at a placeholder-heavy state, so the first successful build is not the same as "ready to author tests"; that boundary could be clearer in docs.
- Suggestions:
  - Clarify in docs that `doctor` accepts valid repo-level or nested NuGet configuration and does not strictly require the file to live at repo root.
  - Add a short note explaining that the generated topology is only a compile-time scaffold until `TestHost` placeholders and AUT selectors are replaced.

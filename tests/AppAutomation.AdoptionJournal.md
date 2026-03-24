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

## Entry 7. Deterministic bootstrap integration
- Step:
  - Extracted shared client bootstrap from `Program`/`App`.
  - Added `SKILLCHAT_SETTINGS_PATH`-based settings resolution.
  - Wired `SkillChat.AppAutomation.TestHost` to:
    - real solution/project paths;
    - isolated temp `Settings.json`;
    - headless window factory via the shared bootstrap.
  - Built `SkillChat.Client`, `SkillChat.AppAutomation.TestHost`, `SkillChat.UiTests.Headless`, `SkillChat.UiTests.FlaUI`.
- What worked:
  - The AUT can now be initialized through one shared bootstrap seam instead of duplicating setup code inside tests.
  - `TestHost` can launch the real `MainWindow` with test-specific settings.
  - Moving IP/runtime environment discovery from constructor-time into `ConnectCommand` removes an unnecessary startup side effect for smoke tests.
- Friction:
  - A normal `dotnet build` initially failed because `Avalonia.Designer.HostApp` from the IDE was locking `SkillChat.Client\bin\Debug\net10.0`.
  - From a consumer perspective this means the desktop launch/build path is sensitive to design-time tooling that keeps the AUT output directory open.
  - There is no obvious `AppAutomation` hook in `AvaloniaDesktopLaunchHost` to redirect build output into an isolated temp directory before launch.
- Suggestions:
  - Document the "close Avalonia designer / design host before desktop runtime validation" edge case for Avalonia consumers.
  - Consider allowing custom MSBuild properties or isolated output paths in desktop launch options, so desktop automation is less coupled to the AUT's default `bin` directory.

## Entry 8. Headless smoke authoring on a real Avalonia screen flow
- Step:
  - Added minimal `AutomationProperties.AutomationId` anchors for the real `login/register` flow.
  - Replaced tap-only text links with semantic `Button` controls while preserving the same visual role.
  - Implemented `HeadlessSessionHooks` with:
    - `HeadlessUnitTestSession.StartNew(SkillChatAppLaunchHost.AvaloniaAppType)`
    - `HeadlessRuntime.SetSession(...)`
  - Authored a deterministic smoke path that covers:
    - launch on login page;
    - navigation to register page;
    - consent toggle enabling the submit button;
    - filling register fields;
    - navigation back to login page.
  - Ran:
    - `dotnet test --project tests\\SkillChat.UiTests.Headless\\SkillChat.UiTests.Headless.csproj -c Debug`
- What worked:
  - Once the global headless session was registered in hooks, `DesktopAppSession.Launch(...)` and `HeadlessControlResolver` worked against the real `MainWindow`.
  - The canonical `Authoring -> Headless` split maps well to a consumer-owned smoke scenario.
  - UI automation surfaced a genuine AUT threading bug in `MainWindow` (`ScrollViewer.Offset` touched off the UI thread), so the framework is valuable as an early integration detector rather than only as a selector runner.
- Friction:
  - The generated `HeadlessSessionHooks.cs` is only a TODO placeholder. A consumer still has to discover the exact required API pair (`HeadlessUnitTestSession.StartNew` + `HeadlessRuntime.SetSession`) by trial, source reading, or runtime failure.
  - First failing runtime message was actionable (`Headless session is not initialized...`), but it arrived only after executing the test. This setup requirement could have been documented or scaffolded directly.
  - `Button.Content` / `TextBlock.Text` were not reliably consumable through `WaitUntilNameEquals` / `WaitUntilNameContains` in this Avalonia headless path until explicit `AutomationProperties.Name` values were assigned.
  - Dynamic label text (`RegisterErrorLabel`) was not a stable assertion surface for this smoke path via automation `Name`, so the consumer had to narrow the test to navigation/state changes instead of validating the inline error message.
  - The generated TUnit/Microsoft Testing Platform runner does not accept the classic `dotnet test --filter ...` workflow; it expects runner-specific filtering semantics (`--treenode-filter` / `--filter-uid`). This is surprising during debugging if the docs only show generic `dotnet test`.
  - During authoring, source-generated page members were effectively a black box from the consumer perspective unless they inspected build outputs manually; that slowed down iterative discovery of the supported page-object contract.
- Suggestions:
  - Replace the placeholder headless hooks with a compilable default for Avalonia templates, or at minimum include the exact namespaces/types in the scaffold comment.
  - Add a short troubleshooting block near headless quickstart:
    - "If you see `Headless session is not initialized`, wire `HeadlessUnitTestSession.StartNew(...)` into `HeadlessRuntime.SetSession(...)`."
  - Document explicitly that consumers should set `AutomationProperties.Name` for interactive elements if they plan to use `WaitUntilName*` helpers, instead of assuming `Content`/`Text` will be projected consistently.
  - Add guidance on which assertions are robust for Avalonia headless:
    - prefer `AutomationId`, `IsEnabled`, `IsChecked`, and direct control properties;
    - treat dynamic accessible names as optional unless explicitly assigned.
  - Document the TUnit/MTP command-line differences in consumer docs:
    - `dotnet test --project ...`
    - filtering/debugging via the runner-specific options rather than classic `--filter`.
  - Consider exposing an easier way to inspect generated page-object members, or emit the generated `.g.cs` into a predictable, documented location for consumers.

## Entry 9. FlaUI desktop smoke runtime
- Step:
  - Reused the same `Authoring` smoke path against the desktop runtime.
  - Ran:
    - `dotnet test --project tests\\SkillChat.UiTests.FlaUI\\SkillChat.UiTests.FlaUI.csproj -c Debug`
  - Added defensive exception wrapping in `MainWindowFlaUiTests` for launch/page creation, matching the headless runtime.
- What worked:
  - The same selector contract (`AutomationId` + explicit `AutomationProperties.Name`) carried over from headless to FlaUI without extra AUT changes.
  - Desktop launch through `SkillChatAppLaunchHost.CreateDesktopLaunchOptions()` worked against the isolated test settings created in `TestHost`.
  - The smoke path passed end-to-end on Windows desktop runtime.
- Friction:
  - No new framework-specific blockers appeared in this milestone beyond the selector/accessibility requirements already captured in Entry 8.
  - The main consumer challenge at this point is signal-to-noise: large build warning output from the AUT can bury the actual automation result during `dotnet test`, even when the UI runtime itself is healthy.
- Suggestions:
  - Keep FlaUI docs aligned with the headless guidance around `AutomationProperties.Name` / `AutomationId`, so consumers know the same selector contract should work across both runtimes.
  - Consider documenting a concise "green path" validation order for consumers:
    - headless first;
    - desktop runtime second;
    - shared page objects/scenarios for both.

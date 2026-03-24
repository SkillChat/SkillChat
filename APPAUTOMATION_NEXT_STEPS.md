# AppAutomation Runbook

## Topology
- `tests/SkillChat.AppAutomation.TestHost`
  Repo-specific launch/bootstrap and isolated `Settings.json`.
- `tests/SkillChat.UiTests.Authoring`
  Shared page objects and smoke scenarios.
- `tests/SkillChat.UiTests.Headless`
  Avalonia headless runtime wiring.
- `tests/SkillChat.UiTests.FlaUI`
  Windows desktop runtime wiring.
- `tests/AppAutomation.AdoptionJournal.md`
  Integration friction log and feedback for framework developers.

## Stable smoke path
- Screen flow: `Login -> Register -> Login`
- Shared page object: `tests/SkillChat.UiTests.Authoring/Pages/MainWindowPage.cs`
- Shared smoke scenario: `tests/SkillChat.UiTests.Authoring/Tests/MainWindowScenariosBase.cs`
- Critical selectors live in:
  - `SkillChat.Client/Views/MainWindow.xaml`
  - `SkillChat.Client/Views/LoginPage.xaml`
  - `SkillChat.Client/Views/RegisterPage.xaml`

## Commands
```powershell
dotnet tool run appautomation doctor --repo-root .
dotnet test --project tests\SkillChat.UiTests.Headless\SkillChat.UiTests.Headless.csproj -c Debug
dotnet test --project tests\SkillChat.UiTests.FlaUI\SkillChat.UiTests.FlaUI.csproj -c Debug
dotnet build SkillChat.sln
dotnet test --solution SkillChat.sln
```

## Working rules for this repo
1. Start with `Headless`. Only move to `FlaUI` after the same shared scenario passes there.
2. Add `AutomationProperties.AutomationId` for selector stability.
3. Add `AutomationProperties.Name` explicitly for interactive elements if the test relies on `WaitUntilName*`.
4. Keep shared test logic in `Authoring`; runtime projects should stay thin.
5. Record every real integration friction and suggested framework/doc improvement in `tests/AppAutomation.AdoptionJournal.md` before the next milestone commit.

## Troubleshooting
- If `Headless` fails with `Headless session is not initialized`, verify `tests/SkillChat.UiTests.Headless/Infrastructure/HeadlessSessionHooks.cs`.
- If desktop build/launch fails with file locks in `SkillChat.Client\bin\Debug\net10.0`, close the Avalonia designer host in the IDE and retry.
- On this TUnit/Microsoft Testing Platform stack, prefer `dotnet test --project ...`; classic `--filter` is not the primary debugging path here.

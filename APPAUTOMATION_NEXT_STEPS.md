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

## Stable smoke paths
- `Anonymous`:
  - flow: `Login -> Register -> Login`
  - shared scenario: `tests/SkillChat.UiTests.Authoring/Tests/MainWindowScenariosBase.cs`
- `SignedInSmoke`:
  - flow: shell/sidebar/profile/settings/header menu/selection/confirmation/attachments
  - shared scenario: `tests/SkillChat.UiTests.Authoring/Tests/MainWindowSignedInScenariosBase.cs`
- Shared page object:
  - `tests/SkillChat.UiTests.Authoring/Pages/MainWindowPage.cs`
- Critical selector files:
  - `SkillChat.Client/Views/MainWindow.xaml`
  - `SkillChat.Client/Views/SendMessageControl.xaml`
  - `SkillChat.Client/Views/Profile/Settings.xaml`
  - `SkillChat.Client/Views/Settings/Settings.xaml`
  - `SkillChat.Client/Views/Settings/More.xaml`
  - `SkillChat.Client/Views/Confirmation.xaml`
  - `SkillChat.Client/Views/AttachmentView.xaml`

## Commands
```powershell
dotnet tool run appautomation doctor --repo-root .
dotnet test --project tests\SkillChat.UiTests.Headless\SkillChat.UiTests.Headless.csproj -c Debug
dotnet test --project tests\SkillChat.UiTests.FlaUI\SkillChat.UiTests.FlaUI.csproj -c Debug
dotnet build SkillChat.sln
dotnet test --solution SkillChat.sln
```

## Working rules for this repo
1. Start with `Headless` signed-in smoke, then validate full parity in `FlaUI`.
2. Keep selectors on interactive/content controls (`Button`, `TextBox`, `ToggleSwitch`, root `UserControl`) rather than on pure layout nodes.
3. Keep shared test logic in `Authoring`; runtime projects should stay thin wrappers.
4. For desktop signed-in launch, ensure automation env/state is set via `SkillChatAppLaunchHost.CreateDesktopLaunchOptions(...)`.
5. Record every real integration friction and suggested framework/doc improvement in `tests/AppAutomation.AdoptionJournal.md`.

## Troubleshooting
- If `Headless` fails with `Headless session is not initialized`, verify `tests/SkillChat.UiTests.Headless/Infrastructure/HeadlessSessionHooks.cs`.
- If `Headless` mixed-scenario run hangs, keep scenarios isolated by runtime project boundaries (current setup: signed-in in `Headless`, anonymous + signed-in in `FlaUI`).
- If desktop build/launch fails with file locks in `SkillChat.Client\bin\Debug\net10.0`, close the Avalonia designer host in the IDE and retry.
- On this TUnit/Microsoft Testing Platform stack, prefer `dotnet test --project ...`; classic `--filter` is not the primary debugging path here.

# Внедрение AppAutomation для SkillChat.Client

## 0. Метаданные
- Тип (профиль): `delivery-task`
- Instruction stack: `quest-governance` + `collaboration-baseline` + `testing-baseline` + `testing-dotnet` + `dotnet-desktop-client` + `ui-automation-testing` + `commit-message-policy`
- Владелец: Codex
- Масштаб: large
- Целевой релиз / ветка: текущая рабочая ветка репозитория
- Ограничения:
  - следовать документации `AppAutomation` и использовать canonical topology `Authoring + Headless + FlaUI + TestHost`;
  - не переходить на source dependency `AppAutomation`;
  - разбить реализацию на мейлстоуны и фиксировать каждый мейлстоун отдельным коммитом в формате Conventional Commits;
  - на каждом шаге внедрения и написания кода дополнять отдельный журнал обратной связи по фреймворку;
  - первая итерация smoke-тестов должна быть deterministic и не требовать поднятого backend;
  - не выходить за пределы минимально-нужного UI-контракта для smoke path;
  - не менять публичный API приложения без отдельного согласования.
- Связанные ссылки:
  - `https://github.com/Kibnet/AppAutomation`
  - `https://raw.githubusercontent.com/Kibnet/AppAutomation/master/docs/appautomation/quickstart.md`
  - `https://raw.githubusercontent.com/Kibnet/AppAutomation/master/docs/appautomation/adoption-checklist.md`
  - `https://raw.githubusercontent.com/Kibnet/AppAutomation/master/docs/appautomation/project-topology.md`
  - `https://raw.githubusercontent.com/Kibnet/AppAutomation/master/docs/appautomation/advanced-integration.md`

## 1. Overview / Цель
Подключить `AppAutomation` к Avalonia-клиенту `SkillChat.Client`, создать canonical topology UI-тестов, настроить deterministic bootstrap для `Headless` и `FlaUI`, написать первый набор smoke-сценариев и вести отдельный журнал внедрения с проблемами процесса и предложениями по улучшению фреймворка.

## 2. Текущее состояние (AS-IS)
- В репозитории нет UI automation проектов и нет canonical topology `tests/*`.
- В `SkillChat.Client` отсутствуют `AutomationId`, на которые может опираться `AppAutomation`.
- `SkillChat.Client` запускается через `Program.Main()` + `App.OnFrameworkInitializationCompleted()`, где `MainWindow` получает `new MainWindowViewModel()`.
- `MainWindowViewModel` в конструкторе:
  - читает и нормализует runtime settings;
  - создаёт `JsonServiceClient`;
  - создаёт `ProfileViewModel` и `SettingsViewModel`;
  - пытается получить внешний IP адрес;
  - автологинится при наличии токена, вызывая `ConnectCommand.Execute(null)`.
- В `SettingsViewModel` открытие настроек инициирует сетевой запрос (`GetSettingsCommand`), поэтому это плохой первый smoke path.
- В репозитории есть `global.json` c SDK `10.0.104` и уже используется `TUnit`, то есть новый test runner не конфликтует с текущим стеком.
- Корневого `NuGet.Config` нет; существует только `SkillChat.Client/nuget.config`, а `AppAutomation doctor` ожидает явный `NuGet.Config` на уровне repo-root.

## 3. Проблема
Проект не имеет deterministic seam для UI automation: отсутствуют canonical test topology и стабильные селекторы, а текущий startup path содержит runtime side effects и network-dependent поведение, из-за чего первый smoke-suite нельзя надёжно запустить ни в `Headless`, ни в `FlaUI`.

## 4. Цели дизайна
- Сохранить существующий UX и структуру приложения, добавив только минимально-нужные seams для автоматизации.
- Сделать первый smoke path полностью deterministic без обязательного поднятия backend.
- Использовать ровно ту responsibility split, которую требует `AppAutomation`.
- Обеспечить повторное использование одних и тех же сценариев между `Headless` и `FlaUI`.
- Собирать инженерную обратную связь по внедрению в отдельный journal-файл, пригодный для отправки разработчикам `AppAutomation`.
- Сохранить обратную совместимость текущего desktop startup path для обычного пользователя.

## 5. Non-Goals (чего НЕ делаем)
- Не автоматизируем реальный chat flow с живым сервером в первой итерации.
- Не покрываем весь UI клиента и не размечаем всё приложение `AutomationId`.
- Не переписываем архитектуру `MainWindowViewModel` шире, чем нужно для deterministic startup/tests.
- Не переносим в consumer repo исходники `AppAutomation`.
- Не делаем полноценный CI/CD pipeline для UI automation в рамках этой задачи.

## 6. Предлагаемое решение (TO-BE)
### 6.1 Распределение ответственности
- `tests/SkillChat.UiTests.Authoring`
  - page objects;
  - `[UiControl(...)]`;
  - shared smoke scenarios;
  - никаких runtime-specific bootstrap деталей.
- `tests/SkillChat.UiTests.Headless`
  - headless session hooks;
  - headless resolver;
  - thin runtime wrappers с `[InheritsTests]`.
- `tests/SkillChat.UiTests.FlaUI`
  - desktop session wiring;
  - `FlaUI` resolver;
  - thin runtime wrappers с `[InheritsTests]`.
- `tests/SkillChat.AppAutomation.TestHost`
  - repo-specific bootstrap;
  - пути до `.sln` и desktop `.csproj`;
  - временные settings/directories;
  - фабрика `MainWindow` для headless runtime.
- `SkillChat.Client`
  - минимальный deterministic startup seam;
  - минимальный `AutomationId` contract для выбранного smoke path.
- `tests/AppAutomation.AdoptionJournal.md`
  - лог всех шероховатостей интеграции;
  - предложения по улучшению документации/tooling/framework API;
  - фиксируется на каждом мейлстоуне перед коммитом.

### 6.2 Детальный дизайн
1. Будет создан canonical test topology в корне репозитория:
   - `tests/SkillChat.UiTests.Authoring`
   - `tests/SkillChat.UiTests.Headless`
   - `tests/SkillChat.UiTests.FlaUI`
   - `tests/SkillChat.AppAutomation.TestHost`

2. Будет добавлен root-level `NuGet.Config`, достаточный для `appautomation doctor` и обычного restore.

3. Startup/bootstrap клиента будет вынесен в переиспользуемый helper внутри `SkillChat.Client`, чтобы:
   - `Program.Main()` продолжал использовать ту же инициализацию;
   - `TestHost` мог поднять тот же `MainWindow`, но на временном `Settings.json`;
   - path к settings можно было подменять на isolated файл в temp-directory;
   - сервисы (`IConfiguration`, `IMapper`, `IClipboard`, `INotify`, `ICanOpenFileDialog`) регистрировались из общего места, а не дублировались.

4. В `MainWindowViewModel` будет устранён startup side effect, который не нужен для рендера окна:
   - сбор runtime environment (`ipAddress`, сведения об ОС, client name) будет выполняться лениво перед реальным `ConnectCommand`, а не в конструкторе;
   - автологин сохранится только для обычного runtime path при непустом токене;
   - для тестового bootstrap будет использоваться temporary settings-файл с пустыми токенами, чтобы приложение стабильно стартовало на login screen.

5. В Avalonia XAML будет добавлен минимальный `AutomationId` contract только для critical smoke path:
   - root window;
   - контейнер login page;
   - login input;
   - password input;
   - login button;
   - register link;
   - контейнер register page;
   - register login/password/name inputs;
   - consent checkbox;
   - register button;
   - back-to-login link;
   - validation/error text blocks, которые проверяются тестами.

6. Первый shared smoke suite будет опираться только на deterministic pre-auth flow:
   - приложение открывается на login screen;
   - переход `Login -> Register -> Login` работает;
   - register form локально валидирует обязательные поля до любого network call;
   - enablement/visibility ключевых controls соответствует ожидаемому состоянию.

7. `Authoring` проект будет содержать один page object для `MainWindow`/pre-auth shell и общий base class со smoke tests.

8. `Headless` и `FlaUI` будут запускать один и тот же shared suite без копирования сценариев.

9. Внедрение будет идти по мейлстоунам, каждый мейлстоун завершится отдельным коммитом и соответствующим обновлением adoption journal.

## 7. Бизнес-правила / Алгоритмы (если есть)
- Первая итерация UI automation не должна требовать запущенного `SkillChat.Server`.
- Любой сценарий, который уходит в сеть до локальной валидации, не входит в initial smoke path.
- `Headless` считается основным стабилизационным runtime; `FlaUI` подключается после него.
- Shared scenarios живут только в `Authoring`; runtime projects не дублируют тестовую логику.
- Любая обнаруженная шероховатость документации/шаблона/CLI должна быть занесена в adoption journal до следующего milestone-коммита.

## 8. Точки интеграции и триггеры
- `SkillChat.Client/Program.cs`
  - должен использовать новый общий bootstrap helper.
- `SkillChat.Client/App.xaml.cs`
  - должен создавать `MainWindow` через общий deterministic startup path либо совместимый factory path.
- `SkillChat.Client.ViewModel/MainWindowViewModel.cs`
  - должен перестать выполнять лишние network-dependent действия в конструкторе.
- `SkillChat.Client/Views/MainWindow.xaml`
  - root `AutomationId` и, при необходимости, якоря pre-auth shell.
- `SkillChat.Client/Views/LoginPage.xaml`
  - `AutomationId` для critical login controls.
- `SkillChat.Client/Views/RegisterPage.xaml`
  - `AutomationId` для critical register controls.
- `SkillChat.sln`
  - должна включать новые test projects.

## 9. Изменения модели данных / состояния
- В production-модели данных изменений не планируется.
- Появится test-only isolated settings файл, создаваемый во временной директории из `TestHost`.
- В runtime state появится возможность создавать `MainWindow` на произвольном settings path для целей тестового bootstrap.

## 10. Миграция / Rollout / Rollback
- Rollout:
  - добавить topology и package references;
  - добавить общий bootstrap helper;
  - добавить `AutomationId`;
  - стабилизировать `Headless`;
  - включить `FlaUI`;
  - зафиксировать usage/journal.
- Rollback:
  - изменения аддитивны и откатываются удалением test projects, test host и новых селекторов;
  - bootstrap extraction может быть откатен возвратом инициализации в `Program`/`App`.
- Обратная совместимость:
  - обычный пользовательский запуск клиента должен остаться без изменения UX;
  - `AutomationId` не должны менять визуальное поведение.

## 11. Тестирование и критерии приёмки
- Acceptance Criteria:
  - в репозитории создан canonical layout `tests/SkillChat.*`.
  - `AppAutomation` подключён через `PackageReference`, а не через source dependency.
  - `appautomation doctor --repo-root .` проходит без ошибок.
  - есть минимум один page object и один shared smoke suite в `Authoring`.
  - smoke suite проходит в `Headless`.
  - тот же suite проходит в `FlaUI`.
  - в проекте есть отдельный adoption journal с логом проблем и предложений по улучшению framework/docs.
  - каждый мейлстоун оформлен отдельным коммитом.
- Какие тесты добавить/изменить:
  - новые UI automation проекты;
  - shared smoke tests для pre-auth flow;
  - при необходимости корректировка существующих тестов не ожидается.
- Команды для проверки:
  - `dotnet build SkillChat.sln`
  - `dotnet test tests/SkillChat.UiTests.Headless/SkillChat.UiTests.Headless.csproj -c Debug`
  - `dotnet test tests/SkillChat.UiTests.FlaUI/SkillChat.UiTests.FlaUI.csproj -c Debug`
  - `dotnet test SkillChat.sln`
  - `.\.tools\appautomation doctor --repo-root .`

## 12. Риски и edge cases
- Документация `AppAutomation` противоречива по версии пакетов:
  - README Fast Path ссылается на `1.1.0`;
  - GitHub releases показывает latest release `1.2.0` от `2026-03-18`;
  - `quickstart.md` в `master` уже использует `2.1.0`.
  - Смягчение: во время EXEC сначала использовать `2.1.0` из quickstart; если restore/install опровергнет это, выбрать latest resolvable version и обязательно занести расхождение в journal.
- Шаблон consumer topology генерирует `net8.0`/старые версии `TUnit`, тогда как репозиторий работает на `net10.0`.
  - Смягчение: после генерации нормализовать TFM и test package versions под репозиторий, сохранив совместимость `AppAutomation`.
- `MainWindowViewModel` содержит startup side effects и авто-connect по токену.
  - Смягчение: isolated settings file + перенос side effects из конструктора в connect path.
- `SettingsViewModel.OpenSettingsCommand` делает network call и не подходит для deterministic smoke.
  - Смягчение: первый smoke suite ограничить pre-auth flow.
- `FlaUI` может быть чувствителен к focus/window timing.
  - Смягчение: сначала стабилизировать `Headless`, затем применить те же selectors в `FlaUI`.

## 13. План выполнения
1. Мейлстоун 1: scaffold и pre-flight
   - установить template/tool по документации;
   - сгенерировать canonical topology;
   - добавить root `NuGet.Config`;
   - завести `tests/AppAutomation.AdoptionJournal.md`;
   - прогнать `doctor`;
   - коммит: `test(appautomation): scaffold canonical ui test topology`

2. Мейлстоун 2: deterministic bootstrap
   - вынести общий bootstrap/helper из `Program`/`App`;
   - добавить isolated settings bootstrap для `TestHost`;
   - убрать startup network side effects из конструктора `MainWindowViewModel`;
   - коммит: `refactor(client): extract deterministic ui automation bootstrap`

3. Мейлстоун 3: selectors + authoring + headless
   - добавить минимальные `AutomationId`;
   - описать page object;
   - написать shared smoke scenarios;
   - подключить headless session hooks и runtime wrapper;
   - стабилизировать `Headless`;
   - коммит: `test(ui): add headless smoke scenarios`

4. Мейлстоун 4: FlaUI runtime
   - подключить thin `FlaUI` runtime project;
   - прогнать те же shared scenarios через desktop runtime;
   - собрать диагностические артефакты при необходимости;
   - коммит: `test(ui): enable flaui smoke runtime`

5. Мейлстоун 5: документация внедрения и финальная верификация
   - дополнить journal итоговыми выводами;
   - описать запуск/поддержку UI automation в repo documentation;
   - прогнать `dotnet build`, targeted UI tests и полный `dotnet test`;
   - коммит: `docs(appautomation): document adoption workflow and feedback`

## 14. Открытые вопросы
- Блокирующих вопросов для старта EXEC нет.
- Неблокирующее уточнение: точная версия `AppAutomation` будет подтверждена во время `restore/install`, потому что документация репозитория противоречива. Это не блокирует реализацию, так как fallback-стратегия описана в разделе рисков.

## 15. Соответствие профилю
- Профиль: `dotnet-desktop-client` + `ui-automation-testing`
- Выполненные требования профиля:
  - UI flow будет сопровождён automation tests.
  - Селекторы будут стабилизированы через `AutomationId`.
  - Будет отдельный smoke-suite UI тестов.
  - `dotnet build` и `dotnet test` включены в план проверки.
  - Shared UI scenarios будут отделены от runtime-specific wiring.

## 16. Таблица изменений файлов
| Файл | Изменения | Причина |
| --- | --- | --- |
| `NuGet.Config` | новый root-level config | совместимость с `appautomation doctor` |
| `SkillChat.sln` | добавление новых test projects | интеграция canonical topology |
| `SkillChat.Client/Program.cs` | переход на общий bootstrap helper | недублируемый startup |
| `SkillChat.Client/App.xaml.cs` | использование общего bootstrap/factory | headless/runtime parity |
| `SkillChat.Client.ViewModel/MainWindowViewModel.cs` | deterministic startup / lazy env collection | убрать side effects из конструктора |
| `SkillChat.Client/Views/MainWindow.xaml` | `AutomationId` для root/shell anchors | стабильные selectors |
| `SkillChat.Client/Views/LoginPage.xaml` | `AutomationId` для login path | smoke selectors |
| `SkillChat.Client/Views/RegisterPage.xaml` | `AutomationId` для register path | smoke selectors |
| `tests/SkillChat.AppAutomation.TestHost/*` | launch/bootstrap + temp settings | repo-specific `AppAutomation` integration |
| `tests/SkillChat.UiTests.Authoring/*` | page objects + shared tests | общая логика smoke suite |
| `tests/SkillChat.UiTests.Headless/*` | headless hooks + thin runtime wrappers | smoke runtime №1 |
| `tests/SkillChat.UiTests.FlaUI/*` | `FlaUI` thin wrappers | smoke runtime №2 |
| `tests/AppAutomation.AdoptionJournal.md` | журнал шероховатостей и предложений | обратная связь разработчикам framework |
| `README.md` или отдельный docs-файл | инструкции по запуску UI automation | поддержка команды |

## 17. Таблица соответствий (было -> стало)
| Область | Было | Стало |
| --- | --- | --- |
| UI automation topology | отсутствует | canonical `tests/SkillChat.*` |
| Startup для тестов | нет test-friendly bootstrap | общий bootstrap + `TestHost` |
| UI selectors | `AutomationId` отсутствуют | минимальный stable selector contract |
| Smoke tests | отсутствуют | shared smoke suite в `Authoring` |
| Runtime coverage | только ручная проверка | `Headless` + `FlaUI` |
| Feedback loop по framework | не фиксируется | отдельный adoption journal |

## 18. Альтернативы и компромиссы
- Вариант: автоматизировать real login/chat flow с живым `SkillChat.Server`.
  - Плюсы: ближе к реальному пользовательскому сценарию.
  - Минусы: нестабильность, зависимость от backend, auth/data preparation, большая стоимость первого внедрения.
  - Почему выбранное решение лучше в контексте этой задачи:
    - документация `AppAutomation` рекомендует начинать с одной deterministic smoke-flow;
    - текущий код клиента не даёт дешёвого и стабильного полного e2e-path без дополнительной инфраструктуры.

- Вариант: сделать только `Headless` и не добавлять `FlaUI`.
  - Плюсы: быстрее внедрение.
  - Минусы: не выполняет canonical consumer flow, хуже desktop-runtime coverage.
  - Почему выбранное решение лучше в контексте этой задачи:
    - пользователь явно просит полноценное внедрение framework;
    - документация позиционирует общие сценарии для `Headless` и `FlaUI`.

- Вариант: подключить `AppAutomation` как source dependency.
  - Плюсы: проще дебажить framework internals.
  - Минусы: прямо противоречит документации framework и усложняет дальнейшее сопровождение.
  - Почему выбранное решение лучше в контексте этой задачи:
    - package-based integration соответствует consumer flow и проще для команды.

## 19. Результат прогона линтера
### SPEC Linter Result

| Блок | Пункты | Статус | Комментарий |
|---|---|---|---|
| A. Полнота спеки | 1-5 | PASS | Цель, AS-IS, проблема, цели и границы заданы явно. |
| B. Качество дизайна | 6-10 | PASS | Responsibility split, bootstrap, selectors, rollout и rollback описаны проверяемо. |
| C. Безопасность изменений | 11-13 | PASS | Есть acceptance criteria, риски, изолированный startup path и milestone-план. |
| D. Проверяемость | 14-16 | PASS | Команды проверки, файловый объём и профиль зафиксированы. |
| E. Готовность к автономной реализации | 17-19 | PASS | План последовательный, блокирующих вопросов нет, компромиссы зафиксированы. |
| F. Соответствие профилю | 20 | PASS | Требования `dotnet-desktop-client` и `ui-automation-testing` покрыты. |

Итог: ГОТОВО

### SPEC Rubric Result

| Критерий | Балл (0/2/5) | Обоснование |
|---|---|---|
| 1. Ясность цели и границ | 5 | Цель, non-goals и acceptance criteria согласованы с запросом пользователя. |
| 2. Понимание текущего состояния | 5 | Зафиксированы текущий startup path, отсутствие selectors и network side effects. |
| 3. Конкретность целевого дизайна | 5 | Описаны topology, bootstrap seam, smoke path и roles проектов. |
| 4. Безопасность (миграция, откат) | 5 | Rollout/rollback и риски задокументированы. |
| 5. Тестируемость | 5 | Есть deterministic smoke strategy и команды проверки для `Headless`/`FlaUI`. |
| 6. Готовность к автономной реализации | 5 | Мейлстоуны и commit plan заданы, блокирующих вопросов нет. |

Итоговый балл: 30 / 30
Зона: готово к автономному выполнению

## Approval
Ожидается фраза: "Спеку подтверждаю"

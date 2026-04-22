# Обновление библиотек UI-тестирования

## 0. Метаданные
- Тип (профиль): `dotnet-desktop-client` + overlay `ui-automation-testing`
- Владелец: Codex
- Масштаб: medium
- Целевой релиз / ветка: текущая рабочая ветка
- Ограничения:
  - QUEST SPEC-first: до подтверждения менять только этот файл.
  - Переход в EXEC только после фразы пользователя `Спеку подтверждаю`.
  - Сохранять текущие пользовательские сценарии и `automation-id`/test selectors.
  - Не делать миграцию приложения на Avalonia 12 в рамках этой задачи.
- Связанные ссылки:
  - `C:\Projects\My\Agents\instructions\profiles\dotnet-desktop-client.md`
  - `C:\Projects\My\Agents\instructions\profiles\ui-automation-testing.md`
  - `C:\Projects\My\Agents\instructions\contexts\testing-dotnet.md`

## 1. Overview / Цель
Обновить библиотеки, которые используются .NET desktop UI-тестами, и внести минимальные изменения в тестовый код/проектные файлы, необходимые для сборки и прохождения smoke UI suite.

## 2. Текущее состояние (AS-IS)
- UI-тесты находятся в:
  - `tests/SkillChat.UiTests.Authoring`
  - `tests/SkillChat.UiTests.Headless`
  - `tests/SkillChat.UiTests.FlaUI`
  - `tests/SkillChat.AppAutomation.TestHost`
- Используемый стек:
  - `AppAutomation.*` `1.2.0`
  - `appautomation.tooling` `1.2.0`
  - `TUnit`, `TUnit.Core`, `TUnit.Assertions` `1.12.111` в UI-test проектах
  - `Avalonia.Headless` `11.3.7`
  - desktop клиент на Avalonia 11.x (`SkillChat.Client` сейчас использует `Avalonia`/`Avalonia.Desktop`/`Avalonia.Themes.Fluent` `11.3.12`)
- `dotnet list ... package --outdated` показывает доступные обновления:
  - `AppAutomation.*` -> `1.4.6`
  - `appautomation.tooling` -> `1.4.6`
  - `TUnit*` -> `1.37.10`
  - `Avalonia.Headless` -> `12.0.1`, при этом последняя найденная 11.x версия в NuGet search: `11.3.14`
- При restore/outdated проверках появляется существующее предупреждение `NU1903` по транзитивному `Tmds.DBus.Protocol 0.21.2` через Avalonia-зависимости.
- Текущие UI-сценарии используют:
  - `AppAutomation.*` session/resolver API
  - `TUnit.Core` атрибуты `[Test]`, `[NotInParallel]`, `[InheritsTests]`
  - `Assert.Multiple()` и fluent assertions
  - стабильные `AutomationId` через authoring page object.

## 3. Проблема
UI-test инфраструктура закреплена на устаревших версиях `AppAutomation.*`, `TUnit` и `Avalonia.Headless`, поэтому сборка и тестовый раннер рискуют расходиться с текущим .NET 10 SDK и актуальными пакетами.

## 4. Цели дизайна
- Разделение ответственности: обновлять package/tool versions отдельно от пользовательской логики приложения.
- Повторное использование: сохранить существующие authoring page objects и общие сценарии.
- Тестируемость: подтвердить изменение через targeted UI test projects и полный `dotnet build`/`dotnet test`.
- Консистентность: привести версии одного семейства пакетов к одной версии.
- Обратная совместимость: не менять публичные API приложения, `automation-id` и ожидаемые пользовательские потоки.

## 5. Non-Goals (чего НЕ делаем)
- Не мигрировать весь клиент на Avalonia 12.
- Не переписывать UI-тестовую архитектуру, page objects или сценарии.
- Не менять `ChatHub`, backend API, модель данных и бизнес-логику.
- Не исправлять unrelated package updates вне UI-test scope, кроме случаев, когда это требуется для сборки UI-тестов.
- Не менять визуальный дизайн приложения.

## 6. Предлагаемое решение (TO-BE)

### 6.1 Распределение ответственности
- `tests/SkillChat.UiTests.*/*.csproj` -> версии UI-test пакетов и test runner пакетов.
- `tests/SkillChat.AppAutomation.TestHost/*.csproj` -> версии AppAutomation test host пакетов.
- `dotnet-tools.json` -> версия `appautomation.tooling`.
- UI-test `.cs` файлы -> точечные изменения API/namespace/attribute/assertion code, если новые версии этого требуют.
- `SkillChat.Client/*.csproj` -> только совместимость Avalonia 11.x, если `Avalonia.Headless 11.3.14` требует alignment для сборки или устраняет restore/build warning.

### 6.2 Детальный дизайн
- Обновить `AppAutomation.*` и `appautomation.tooling` с `1.2.0` до `1.4.6`.
- Обновить UI-test `TUnit`, `TUnit.Core`, `TUnit.Assertions` с `1.12.111` до `1.37.10`.
- Обновить `Avalonia.Headless` не на `12.0.1`, а на `11.3.14`, чтобы остаться в текущей major-линейке Avalonia 11.
- После restore/build исправить только фактические compile/runtime incompatibilities:
  - renamed namespaces/types/methods;
  - analyzer-required signatures;
  - obsolete/removed assertion helper usage;
  - changes in AppAutomation launch/resolver APIs.
- Если TUnit 1.37.10 конфликтует с существующими UI-test packages, сначала попытаться выровнять все TUnit-пакеты в UI-test проектах на одну версию; не трогать non-UI test projects без необходимости.
- Если `Avalonia.Headless 11.3.14` вызывает downgrade/conflict с клиентскими Avalonia 11.3.12 packages, разрешено поднять только Avalonia 11.x пакеты клиента до совместимой 11.3.14-линейки. Avalonia 12 остается вне scope.

## 7. Бизнес-правила / Алгоритмы (если есть)
- Пользовательские сценарии UI-тестов должны остаться теми же: регистрация/login smoke, signed-in shell smoke, sidebar collapse smoke.
- Селекторы остаются основанными на `AutomationId`; текстовые или позиционные селекторы не добавлять.
- Любой новый workaround фиксировать кратким комментарием только при наличии неочевидной причины.

## 8. Точки интеграции и триггеры
- NuGet restore/build/test для UI-test проектов.
- Microsoft.Testing.Platform через `global.json`.
- Desktop/headless launch через `SkillChat.AppAutomation.TestHost`.
- Page object integration через `SkillChat.UiTests.Authoring`.

## 9. Изменения модели данных / состояния
- Новых persisted fields нет.
- Изменения состояния приложения не планируются.
- Возможны изменения только в build/test dependency graph.

## 10. Миграция / Rollout / Rollback
- Rollout: атомарное обновление версий пакетов и совместимых правок кода.
- Обратная совместимость: сохраняется на уровне UI сценариев и selectors.
- Rollback: вернуть версии пакетов/tooling и точечные правки тестового кода.
- Риск от restore cache/obj artifacts не включается в коммит; проверять `git status` перед финалом.

## 11. Тестирование и критерии приёмки
- Acceptance Criteria:
  - `AppAutomation.*` и `appautomation.tooling` обновлены до `1.4.6`.
  - UI-test `TUnit*` пакеты обновлены до `1.37.10`.
  - `Avalonia.Headless` обновлен до актуальной совместимой 11.x версии (`11.3.14`) без миграции на Avalonia 12.
  - Проект собирается без compile errors.
  - UI smoke suite запускается и проходит либо остаточные инфраструктурные ограничения явно отделены от внесенных изменений.
  - `automation-id`/test selectors не изменены.
- Какие тесты добавить/изменить:
  - Новые regression tests не требуются, если пользовательское поведение не меняется.
  - Разрешены правки существующих UI-тестов только для совместимости с новыми библиотеками.
- Characterization tests / contract checks:
  - Существующие `MainWindowScenariosBase`, `MainWindowSignedInScenariosBase`, headless sidebar smoke остаются контрактом поведения.
- Команды для проверки:
  - `dotnet restore SkillChat.sln`
  - `dotnet build SkillChat.sln`
  - `dotnet test tests/SkillChat.UiTests.Headless/SkillChat.UiTests.Headless.csproj`
  - `dotnet test tests/SkillChat.UiTests.FlaUI/SkillChat.UiTests.FlaUI.csproj`
  - `dotnet test SkillChat.sln`
  - `dotnet tool run appautomation --version` или эквивалентная доступная команда проверки tooling, если CLI поддерживает вывод версии.

## 12. Риски и edge cases
- TUnit мог изменить source-generator/analyzer behavior между `1.12.111` и `1.37.10`; смягчение: build first, затем точечные правки тестового кода.
- AppAutomation мог изменить API session/resolver; смягчение: ограничить правки адаптерными слоями `LaunchSession`/`CreatePage`.
- FlaUI tests могут зависеть от desktop session и быть нестабильными в локальном окружении; смягчение: отдельно фиксировать, является ли падение инфраструктурным или связано с апгрейдом.
- `Avalonia.Headless 12.0.1` тянет major migration; смягчение: оставаться на `11.3.14`.
- Существующий `NU1903` по `Tmds.DBus.Protocol` может остаться после targeted UI-test update; смягчение: зафиксировать как отдельный риск, если не блокирует сборку. Если блокирует, поднять только минимально необходимую Avalonia 11.x совместимость.

## 13. План выполнения
1. После подтверждения spec обновить versions в UI-test `.csproj` и `dotnet-tools.json`.
2. Выполнить restore/build targeted UI-test projects.
3. Исправить compile errors, связанные только с API новых UI-test библиотек.
4. Запустить headless UI tests.
5. Запустить FlaUI UI tests, если окружение позволяет desktop automation.
6. Запустить полный `dotnet build SkillChat.sln` и `dotnet test SkillChat.sln`.
7. Проверить `git status`, выполнить post-EXEC review и зафиксировать остаточные риски.

## 14. Открытые вопросы
Нет блокирующих вопросов. Выбран консервативный путь: обновлять UI-test пакеты до latest stable, но оставаться на Avalonia 11.x для headless, чтобы не превращать задачу в миграцию UI framework.

## 15. Соответствие профилю
- Профиль: `.NET Desktop Client`
- Overlay: `UI Automation Testing`
- Выполненные требования профиля:
  - План сохраняет `automation-id`/test selectors.
  - UI flow не меняется.
  - В проверках есть targeted UI smoke и полный `dotnet build`/`dotnet test`.
  - Изменения ограничены тестовой инфраструктурой и совместимостными правками.

## 16. Таблица изменений файлов
| Файл | Изменения | Причина |
| --- | --- | --- |
| `tests/SkillChat.UiTests.Authoring/SkillChat.UiTests.Authoring.csproj` | `AppAutomation.*`, `TUnit.Core`, `TUnit.Assertions` versions | Обновить authoring/test framework |
| `tests/SkillChat.UiTests.Headless/SkillChat.UiTests.Headless.csproj` | `AppAutomation.*`, `Avalonia.Headless`, `TUnit` versions | Обновить headless UI automation |
| `tests/SkillChat.UiTests.FlaUI/SkillChat.UiTests.FlaUI.csproj` | `AppAutomation.*`, `TUnit` versions | Обновить desktop UI automation |
| `tests/SkillChat.AppAutomation.TestHost/SkillChat.AppAutomation.TestHost.csproj` | `AppAutomation.*` versions | Обновить test host contracts |
| `dotnet-tools.json` | `appautomation.tooling` version | Синхронизировать CLI с AppAutomation packages |
| `tests/SkillChat.UiTests.*/*.cs` | Только при необходимости: API compatibility fixes | Сборка с новыми библиотеками |
| `SkillChat.Client/SkillChat.Client.csproj` | Только при необходимости: Avalonia 11.x alignment | Совместимость с `Avalonia.Headless 11.3.14` |

## 17. Таблица соответствий (было -> стало)
| Область | Было | Стало |
| --- | --- | --- |
| AppAutomation packages/tool | `1.2.0` | `1.4.6` |
| UI-test TUnit packages | `1.12.111` | `1.37.10` |
| Avalonia.Headless | `11.3.7` | `11.3.14` |
| Avalonia major | 11.x | 11.x |
| UI selectors | текущие `AutomationId` | без изменений |

## 18. Альтернативы и компромиссы
- Вариант: обновить `Avalonia.Headless` до `12.0.1` и мигрировать весь клиент на Avalonia 12.
  - Плюсы: формально все UI packages на последних major.
  - Минусы: задача превращается в framework migration с риском XAML/API/regression изменений.
  - Почему не выбран: пользователь просит UI-test libraries, а не миграцию приложения.
- Вариант: обновить только AppAutomation, оставить TUnit/Avalonia.Headless.
  - Плюсы: меньше риска.
  - Минусы: TUnit остается существенно устаревшим; часть несовместимостей может всплыть позже.
  - Почему не выбран: задача сформулирована как обновление библиотек UI-тестирования, а TUnit является частью test runner stack.
- Вариант: централизовать версии через `Directory.Packages.props`.
  - Плюсы: меньше дублирования.
  - Минусы: структурное изменение package management всего решения.
  - Почему не выбран: выходит за минимально-достаточный scope.

## 19. Результат quality gate и review

### SPEC Linter Result

| Блок | Пункты | Статус | Комментарий |
|---|---|---|---|
| A. Полнота спеки | 1-5 | PASS | Цель, AS-IS, проблема, цели и Non-Goals зафиксированы. |
| B. Качество дизайна | 6-10 | PASS | Ответственность, интеграция, rollback и границы Avalonia 11 описаны. |
| C. Безопасность изменений | 11-13 | PASS | Нет изменений данных/API; rollback и ограничения scope указаны. |
| D. Проверяемость | 14-16 | PASS | Acceptance Criteria и команды targeted/full проверок указаны. |
| E. Готовность к автономной реализации | 17-19 | PASS | План этапов есть, блокирующих вопросов нет. |
| F. Соответствие профилю | 20 | PASS | Учтены `dotnet-desktop-client` и `ui-automation-testing`. |

Итог: ГОТОВО

### SPEC Rubric Result

| Критерий | Балл (0/2/5) | Обоснование |
|---|---:|---|
| 1. Ясность цели и границ | 5 | Scope ограничен UI-test dependency update и compatibility fixes. |
| 2. Понимание текущего состояния | 5 | Зафиксированы проекты, текущие версии и доступные обновления. |
| 3. Конкретность целевого дизайна | 5 | Целевые версии и подход к Avalonia 11/12 явно определены. |
| 4. Безопасность (миграция, откат) | 5 | Нет миграции данных/API; rollback простым возвратом версий и правок. |
| 5. Тестируемость | 5 | Есть targeted UI и full solution команды. |
| 6. Готовность к автономной реализации | 5 | План исполним без продуктовых решений пользователя. |

Итоговый балл: 30 / 30  
Зона: готово к автономному выполнению

### Post-SPEC Review
- Статус: PASS
- Что исправлено:
  - Добавлена явная граница по Avalonia 12, чтобы update UI-test libs не превратился в framework migration.
  - Добавлен риск существующего `NU1903` по транзитивному `Tmds.DBus.Protocol`.
  - Уточнено, что `SkillChat.Client.csproj` трогается только при необходимости совместимости Avalonia 11.x.
- Что осталось на решение пользователя:
  - Требуется подтверждение спеки фразой `Спеку подтверждаю`.

### Post-EXEC Review
- Статус: PASS
- Что исправлено до завершения:
  - После обновления зависимостей headless suite зависал на втором `SignedInSmoke` тесте. Причина: предыдущий headless `MainWindow` оставался привязанным к visual/binding tree между тестами. В `HeadlessRuntimeSession.Dispose()` добавлено мягкое отсоединение `DataContext` и `Content` на UI thread.
  - Проверен и отклонен вариант с `Window.Close()`: он снимал зависание, но ломал Avalonia Headless renderer ошибкой `Unable to locate 'Avalonia.Platform.IPlatformRenderInterface'`.
- Что проверено дополнительно:
  - `automation-id`/page object selectors не менялись.
  - AppAutomation runtime остаётся на Avalonia 11.x; миграции на Avalonia 12 нет.
  - Временный dump и диагностические папки удалены из `TestResults`.
- Остаточные риски / follow-ups:
  - Существующее предупреждение `NU1903` по транзитивному `Tmds.DBus.Protocol 0.21.2` остается в Avalonia dependency chain и не исправлялось в рамках UI-test update.
  - В решении остаются существующие nullability warnings; они не связаны с изменением UI-test библиотек.

## Approval
Ожидается фраза: "Спеку подтверждаю"

## 20. Журнал действий агента
Заполняется инкрементально после каждого значимого блока работ. Одна строка = один завершённый значимый блок.

| Фаза (SPEC/EXEC) | Тип намерения/сценария | Уверенность в решении (0.0-1.0) | Каких данных не хватает | Следующее действие | Нужна ли передача управления/решения человеку | Было ли фактическое обращение к человеку / решение человека | Короткое объяснение выбора | Затронутые артефакты/файлы |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| SPEC | Сбор instruction stack | 0.95 | Нет | Создать рабочую спецификацию | Нет | Нет | Применены QUEST, testing-dotnet, dotnet-desktop-client и ui-automation-testing | `AGENTS.md`, `C:\Projects\My\Agents\instructions\*` |
| SPEC | Анализ UI-test dependency graph | 0.9 | Возможные breaking changes станут видны только после restore/build на EXEC | Зафиксировать план обновления | Нет | Нет | Найдены UI-test проекты и версии пакетов; выбран conservative Avalonia 11.x path | `tests/SkillChat.UiTests.*/*.csproj`, `tests/SkillChat.AppAutomation.TestHost/*.csproj`, `dotnet-tools.json`, `SkillChat.Client/SkillChat.Client.csproj` |
| SPEC | SPEC quality gate и post-SPEC review | 0.95 | Нет | Запросить подтверждение спеки | Да | Да, ожидается фраза пользователя `Спеку подтверждаю` | Спека проходит linter/rubric, блокирующих вопросов нет | `specs/2026-04-21-ui-test-libraries-update.md` |
| EXEC | Подтверждение и обновление версий | 0.9 | Реальные API incompatibilities будут видны после restore/build | Выполнить restore/build UI-test проектов | Нет | Да, пользователь подтвердил spec фразой `спеку подтверждаю` | Обновлены только версии, перечисленные в spec: AppAutomation/TUnit/Avalonia.Headless/tooling | `tests/SkillChat.UiTests.*/*.csproj`, `tests/SkillChat.AppAutomation.TestHost/*.csproj`, `dotnet-tools.json`, `specs/2026-04-21-ui-test-libraries-update.md` |
| EXEC | Restore и targeted build | 0.9 | Еще не проверен runtime UI smoke | Запустить targeted UI tests | Нет | Нет | `dotnet restore SkillChat.sln`, headless build и FlaUI build проходят без ошибок; C# compatibility правки не потребовались | `tests/SkillChat.UiTests.*/*.csproj`, `tests/SkillChat.AppAutomation.TestHost/*.csproj`, `specs/2026-04-21-ui-test-libraries-update.md` |
| EXEC | Диагностика headless timeout | 0.82 | Нужно повторно проверить после cleanup-правки | Добавить release headless `MainWindow` между тестами | Нет | Нет | Dump показал зависание второго headless теста в `DesktopAppSession.Launch()` при создании второго окна; временный dump удален после диагностики | `tests/SkillChat.UiTests.Headless/Tests/MainWindowHeadlessSignedInTests.cs`, `specs/2026-04-21-ui-test-libraries-update.md` |
| EXEC | Исправление headless cleanup | 0.92 | Нужно подтвердить через `dotnet test --project` и full solution checks | Запустить стандартные targeted UI test команды | Нет | Нет | `Window.Close()` устранял зависание, но ломал Avalonia Headless renderer; заменено на мягкое отсоединение `DataContext`/`Content`, headless exe проходит 2/2 | `tests/SkillChat.UiTests.Headless/Tests/MainWindowHeadlessSignedInTests.cs`, `specs/2026-04-21-ui-test-libraries-update.md` |
| EXEC | Targeted и full verification | 0.96 | Нет | Провести post-EXEC review | Нет | Нет | `dotnet test --project` для Headless и FlaUI проходит; `dotnet build SkillChat.sln --no-restore` и полный `dotnet test --solution SkillChat.sln --no-build` проходят, всего 66/66 | `SkillChat.sln`, `tests/SkillChat.UiTests.*`, `SkillChat.*.Test`, `specs/2026-04-21-ui-test-libraries-update.md` |
| EXEC | Tooling verification | 0.95 | CLI не поддерживает `--version`, версия подтверждена через `dotnet tool list` | Завершить финальный sanity pass | Нет | Нет | `dotnet tool restore`, `dotnet tool list` показывает `appautomation.tooling 1.4.6`; `dotnet appautomation doctor --repo-root .` проходит с 0 errors/0 warnings | `dotnet-tools.json`, `specs/2026-04-21-ui-test-libraries-update.md` |

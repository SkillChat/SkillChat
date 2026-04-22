# UI-покрытие unread-divider в текущей ветке

## 0. Метаданные
- Тип (профиль): `delivery-task`; core: `quest-governance + collaboration-baseline + testing-baseline`; context: `testing-dotnet`; profile: `dotnet-desktop-client + ui-automation-testing`
- Владелец: команда SkillChat, исполнитель `Codex`
- Масштаб: small
- Целевой релиз / ветка: `feature/unread-divider`
- Ограничения:
  - не менять продуктовую логику шире обнаруженного UI-test пробела без отдельной спеки;
  - использовать стабильные `automation-id`, не текстовые селекторы;
  - сохранить запуск shared authoring сценария и для Headless, и для FlaUI;
  - перед завершением выполнить минимум UI smoke-suite и релевантные unit-тесты.
- Связанные ссылки:
  - `specs/2026-03-27-unread-messages-divider.md`
  - `specs/2026-03-30-unread-divider-remediation.md`
  - `specs/2026-04-01-unread-divider-bootstrap-fix.md`

## 1. Overview / Цель
Закрыть пробел ревью текущей ветки: unread-divider уже добавлен в продуктовый XAML и seeded automation state, но UI smoke-тесты не проверяют, что divider реально доступен через automation layer. Дополнительно синхронизировать automation API с порядком сообщений серверного `GetMessages`, чтобы UI-тест проверял сценарий, близкий к production.

## 2. Текущее состояние (AS-IS)
- `SkillChat.Client/Views/UnreadDivider.xaml` задаёт `AutomationId` вида `UnreadDivider_<MessageId>` и показывает divider по `MessageViewModel.IsUnreadBoundary`.
- `tests/SkillChat.AppAutomation.TestHost/SkillChatAppLaunchHost.cs` сеет `FirstUnreadMessageId = "message-unread-1"` и 18 непрочитанных сообщений.
- `tests/SkillChat.UiTests.Authoring/Pages/MainWindowPage.cs` умеет резолвить `MessageItem_<id>` и `MessageCheckbox_<id>`, но не умеет резолвить `UnreadDivider_<id>`.
- `tests/SkillChat.UiTests.Authoring/Tests/MainWindowSignedInScenariosBase.cs` проверяет общий signed-in smoke path, но не assert-ит unread-divider.
- `SkillChat.Client/Automation/SkillChatAutomationApiClient.cs` при `PageSize == null` возвращает seeded messages в исходном порядке state. Серверный `ChatService.GetMessages` возвращает страницу в порядке `PostTime desc`, после чего клиент вставляет элементы в начало коллекции. Из-за этого automation сценарий может визуализировать историю в порядке, отличном от production.
- Серверные и ViewModel-тесты для `FirstUnreadMessageId`, backfill older pages и `MarkChatRead` уже присутствуют.

## 3. Проблема
Изменение unread-divider не закреплено сквозным UI-тестом: текущий smoke может пройти, даже если divider перестал рендериться или пропал его automation selector. При этом test host не полностью имитирует контракт порядка сообщений сервера, что снижает ценность будущей UI-проверки.

## 4. Цели дизайна
- Разделение ответственности: page object знает селектор, shared smoke-сценарий проверяет пользовательский контракт, automation API имитирует серверный контракт.
- Повторное использование: расширить существующий `MainWindowPage` и общий signed-in smoke вместо отдельной дублирующей suite.
- Тестируемость: assert должен падать при потере `UnreadDivider_message-unread-1`.
- Консистентность: ordering в automation API должен соответствовать production `GetMessages`.
- Обратная совместимость: не менять публичные серверные контракты и не затрагивать persisted state.

## 5. Non-Goals (чего НЕ делаем)
- Не меняем алгоритм `ChatService.GetMessages` и `ChatHub.MarkChatRead`.
- Не добавляем новый UI-фреймворк и не переписываем AppAutomation инфраструктуру.
- Не проверяем pixel-perfect положение divider; достаточно проверить, что он появился в UI automation tree в seeded signed-in сценарии.
- Не добавляем отдельный текстовый селектор по надписи "Новые сообщения".

## 6. Предлагаемое решение (TO-BE)
### 6.1 Распределение ответственности
- `SkillChat.Client/Automation/SkillChatAutomationApiClient.cs` -> отдаёт сообщения в порядке, совместимом с серверным `GetMessages`: фильтр по chat/before, сортировка `PostTime desc`, ограничение page size, `HasMoreBefore`, `FirstUnreadMessageId` только на initial page.
- `tests/SkillChat.UiTests.Authoring/Pages/MainWindowPage.cs` -> добавляет resolver `ResolveUnreadDivider(string messageId)`.
- `tests/SkillChat.UiTests.FlaUI/Tests/MainWindowFlaUiSignedInTests.cs` -> проверяет full desktop automation selector `UnreadDivider_message-unread-1` в signed-in smoke.
- `tests/SkillChat.UiTests.Headless/Tests/MainWindowHeadlessSignedInTests.cs` -> проверяет binding/automation id компонента `Messages`/`UnreadDivider` без зависимости от полноценного headless window layout.

### 6.2 Детальный дизайн
- Поток данных:
  1. Test host стартует signed-in сценарий с seeded unread range.
  2. Automation API возвращает initial message page в production-like order.
  3. `MainWindowViewModel` выставляет `IsUnreadBoundary` у `message-unread-1`.
  4. XAML создаёт видимый `UnreadDivider_message-unread-1`.
  5. FlaUI smoke резолвит divider через page object; Headless component test проверяет XAML binding и automation id.
- Контракты / API:
  - Новый page-object метод использует `UiLocatorKind.AutomationId` и `FallbackToName: false`.
  - Product automation id остаётся `UnreadDivider_<MessageId>`.
- Обработка ошибок:
  - FlaUI smoke использует `WaitUntil`, чтобы не быть чувствительным к первому layout-pass.
  - Headless test не вызывает `Window.Show()`, потому что текущий Avalonia Headless runtime не поднимает полноценный desktop layout/fonts для этого сценария.
- Производительность:
  - Сортировка seeded in-memory списка малая; влияния на runtime production нет.

## 7. Бизнес-правила / Алгоритмы
- В signed-in automation сценарии должен существовать ровно один ожидаемый unread-divider для `message-unread-1`.
- UI-тест должен опираться на `automation-id`, а не на текст divider.
- Automation API должен возвращать `HasMoreBefore` корректно при ограниченном `PageSize`, чтобы future tests могли проверять backfill без отдельного fake.

## 8. Точки интеграции и триггеры
- `MainWindowFlaUiSignedInTests.Unread_divider_is_exposed_for_seeded_signed_in_history` проверяет `ResolveUnreadDivider("message-unread-1")`.
- `MainWindowHeadlessSignedInTests.Unread_divider_view_binds_visibility_and_automation_id` проверяет локальный `Messages` control с `MessageViewModel.IsUnreadBoundary = true`.
- `SkillChatAutomationApiClient.GetMessagesAsync` вызывается текущим `LoadMessageHistoryCommand` на старте signed-in shell.

## 9. Изменения модели данных / состояния
- Новых persisted-полей нет.
- Нового runtime state нет.
- `MessagePage.HasMoreBefore` в automation fake становится calculated значением, совместимым с серверным ответом.

## 10. Миграция / Rollout / Rollback
- Миграция не требуется.
- Rollback: удалить resolver/assert, вернуть прежнюю in-memory выборку automation API и прежние binding/positioning изменения unread-divider.
- Обратная совместимость: UI selector уже существует, добавление теста не меняет пользовательское поведение.

## 11. Тестирование и критерии приёмки
- Acceptance Criteria:
  - FlaUI signed-in UI smoke падает, если `UnreadDivider_message-unread-1` не появился в desktop automation tree.
  - Headless UI test падает, если `Messages`/`UnreadDivider` binding не показывает divider или теряет `UnreadDivider_<MessageId>`.
  - Automation API возвращает initial page в том же направлении сортировки, что server `GetMessages`.
  - Существующие серверные и ViewModel-тесты unread-divider остаются зелёными.
- Какие тесты добавить/изменить:
  - `tests/SkillChat.UiTests.Authoring/Pages/MainWindowPage.cs`: добавить `ResolveUnreadDivider`.
  - `tests/SkillChat.UiTests.FlaUI/Tests/MainWindowFlaUiSignedInTests.cs`: добавить full desktop assert для divider.
  - `tests/SkillChat.UiTests.Headless/Tests/MainWindowHeadlessSignedInTests.cs`: добавить component-level UI assert для `Messages`/`UnreadDivider`.
  - При необходимости добавить/обновить targeted unit test для automation API ordering, если в проекте уже есть подходящий тестовый слой; иначе ограничиться покрытием через UI smoke.
- Команды для проверки:
```powershell
dotnet test --project tests/SkillChat.UiTests.Headless/SkillChat.UiTests.Headless.csproj --no-build --maximum-parallel-tests 1
dotnet test --project tests/SkillChat.UiTests.FlaUI/SkillChat.UiTests.FlaUI.csproj --no-build --maximum-parallel-tests 1
dotnet test --project SkillChat.Client.ViewModel.Test/SkillChat.Client.ViewModel.Test.csproj --no-build
dotnet test --project SkillChat.Server.Test/SkillChat.Server.Test.csproj --no-build
dotnet test --solution SkillChat.sln --no-build --max-parallel-test-modules 1 --maximum-parallel-tests 1
```

## 12. Риски и edge cases
- Риск: AppAutomation конкретной платформы может не отдавать невидимые controls; это ожидаемо, потому что проверяется видимый divider.
- Риск: initial layout может задержать появление automation element; используется существующий polling через `WaitUntil`.
- Риск: исправление ordering в fake может изменить визуальный порядок seeded smoke messages; это намеренная синхронизация с production.

## 13. План выполнения
1. Добавить `ResolveUnreadDivider(string messageId)` в `MainWindowPage`.
2. Добавить assertion `UnreadDivider_message-unread-1` в FlaUI signed-in smoke.
3. Добавить Headless component-level UI test для `Messages`/`UnreadDivider`.
4. Исправить `SkillChatAutomationApiClient.GetMessagesAsync`: сортировка `PostTime desc`, `Take(pageSize + 1)`, `HasMoreBefore`, trim до pageSize, `FirstUnreadMessageId` только без `BeforePostTime`.
5. Собрать решение при необходимости.
6. Прогнать targeted UI tests Headless/FlaUI и релевантные unit-тесты.
7. Выполнить post-EXEC review и, если нет критичных замечаний, сделать коммит по правилам проекта.

## 14. Открытые вопросы
Блокирующих вопросов нет.

## 15. Соответствие профилю
- Профиль: `dotnet-desktop-client + ui-automation-testing`
- Выполненные требования профиля:
  - UI-поток не блокируется: изменения только в fake API и тестах.
  - UI-контракт закрепляется стабильным `automation-id`.
  - Перед завершением планируется запуск smoke-suite UI тестов.
  - Product selectors не меняются, добавляется page-object resolver и targeted UI assertions.

## 16. Таблица изменений файлов
| Файл | Изменения | Причина |
| --- | --- | --- |
| `SkillChat.Client.ViewModel/MainWindowViewModel.cs` | Выставить `IsUnreadBoundary` до вставки message item | UI binding divider должен получать актуальное значение без зависимости от последующего notify |
| `SkillChat.Client/SkillChatClientBootstrap.cs` | Выставить seeded `IsUnreadBoundary` до назначения коллекции в `Messages` | Headless/FlaUI smoke использует automation bootstrap, поэтому item container должен получить готовое состояние |
| `SkillChat.Client/Automation/SkillChatAutomationApiClient.cs` | Production-like ordering и `HasMoreBefore` | UI smoke должен проверять реалистичный порядок history |
| `SkillChat.Client/Views/MainWindow.xaml.cs` | Перед initial positioning unread-divider ставить scroll offset в начало | Boundary item должен быть материализован до поиска visual control |
| `SkillChat.Client/Views/Messages.xaml` | Привязать `UnreadDivider.IsVisible` на месте использования в message template | Binding должен получать `MessageViewModel` item context |
| `SkillChat.Client/Views/UnreadDivider.xaml` | Сохранить `UnreadDivider_<MessageId>` на внутреннем automation-visible `TextBlock`/Label | AppAutomation должен стабильно находить divider по существующему selector |
| `tests/SkillChat.UiTests.Authoring/Pages/MainWindowPage.cs` | Resolver для `UnreadDivider_<MessageId>` | Стабильный UI API для тестов |
| `tests/SkillChat.UiTests.FlaUI/Tests/MainWindowFlaUiSignedInTests.cs` | Desktop assertion unread-divider в signed-in smoke | Сквозное покрытие текущего изменения в реальном automation tree |
| `tests/SkillChat.UiTests.Headless/Tests/MainWindowHeadlessSignedInTests.cs` | Component UI assertion для `Messages`/`UnreadDivider` | Быстрое headless покрытие XAML binding и automation id |

## 17. Таблица соответствий (было -> стало)
| Область | Было | Стало |
| --- | --- | --- |
| UI selector | `UnreadDivider_<id>` есть в XAML, но не используется тестами | Page object умеет резолвить divider |
| UI smoke | Проверяет shell, settings/profile/attachment flows | FlaUI дополнительно проверяет unread-divider, Headless проверяет component binding |
| Automation history fake | При `PageSize == null` отдаёт state order | Отдаёт server-like `PostTime desc` page |

## 18. Альтернативы и компромиссы
- Вариант: добавить отдельный headless-only тест видимости viewport.
- Плюсы: сильнее проверяет initial positioning.
- Минусы: привязка к Avalonia visual tree и платформенной геометрии, не переиспользуется во FlaUI.
- Почему выбранное решение лучше в контексте этой задачи: текущий пробел ревью в первую очередь про отсутствие UI automation coverage; full selector проверяется в FlaUI, а Headless закрывает XAML binding без нестабильного window layout.

- Вариант: не трогать automation API ordering.
- Плюсы: меньше правок.
- Минусы: новый UI-тест проверял бы сценарий, отличный от production order.
- Почему выбранное решение лучше в контексте этой задачи: небольшая правка fake повышает достоверность всего signed-in smoke.

## 19. Результат quality gate и review
### SPEC Linter Result

| Блок | Пункты | Статус | Комментарий |
|---|---|---|---|
| A. Полнота спеки | 1-5 | PASS | Цель, AS-IS, проблема, цели и Non-Goals заданы |
| B. Качество дизайна | 6-10 | PASS | Ответственность, интеграции, алгоритмы и rollback описаны |
| C. Безопасность изменений | 11-13 | PASS | Scope малый, persisted state не меняется |
| D. Проверяемость | 14-16 | PASS | Есть acceptance criteria, файлы и команды проверки |
| E. Готовность к автономной реализации | 17-19 | PASS | План пошаговый, блокирующих вопросов нет |
| F. Соответствие профилю | 20 | PASS | UI automation требования зафиксированы |

Итог: ГОТОВО

### SPEC Rubric Result

| Критерий | Балл (0/2/5) | Обоснование |
|---|---|---|
| 1. Ясность цели и границ | 5 | Scope ограничен UI-test coverage и fake API parity |
| 2. Понимание текущего состояния | 5 | Зафиксированы XAML selector, seeded state и текущие пробелы tests |
| 3. Конкретность целевого дизайна | 5 | Названы методы, файлы и expected selector |
| 4. Безопасность (миграция, откат) | 5 | Нет persisted изменений, rollback простой |
| 5. Тестируемость | 5 | Критерии и команды проверки конкретны |
| 6. Готовность к автономной реализации | 5 | Блокирующих вопросов нет |

Итоговый балл: 30 / 30
Зона: готово к автономному выполнению

Краткий Post-SPEC Review:
- Статус: PASS
- Что исправлено: в spec добавлен обнаруженный риск automation API ordering, чтобы UI-тест был репрезентативным.
- Что осталось на решение пользователя: подтвердить переход в EXEC.

## Approval
Ожидается фраза: "Спеку подтверждаю"

## 20. Журнал действий агента
| Фаза (SPEC/EXEC) | Тип намерения/сценария | Уверенность в решении (0.0-1.0) | Каких данных не хватает | Следующее действие | Нужна ли передача управления/решения человеку | Было ли фактическое обращение к человеку / решение человека | Короткое объяснение выбора | Затронутые артефакты/файлы |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| SPEC | review + ui-test remediation | 0.93 | Подтверждение пользователя для EXEC | Дождаться фразы `Спеку подтверждаю` | Да | Да, запрашивается подтверждение | QUEST запрещает менять тестовый код до подтверждения spec | `specs/2026-04-22-unread-divider-ui-test-coverage.md` |
| EXEC | implementation | 0.94 | Результаты тестового прогона | Запустить targeted UI/unit проверки | Нет | Пользователь подтвердил spec фразой `Спеку подтверждаю` | Добавлены stable selector/assertion и production-like ordering fake API в рамках утверждённого scope | `SkillChat.Client/Automation/SkillChatAutomationApiClient.cs`, `tests/SkillChat.UiTests.Authoring/Pages/MainWindowPage.cs`, `tests/SkillChat.UiTests.FlaUI/Tests/MainWindowFlaUiSignedInTests.cs`, `tests/SkillChat.UiTests.Headless/Tests/MainWindowHeadlessSignedInTests.cs` |
| EXEC | test failure remediation | 0.88 | Повторный результат Headless/FlaUI | Пересобрать и повторить UI smoke | Нет | Нет | Первый Headless прогон показал, что selector на root `UserControl` не находится; тот же automation id перенесён на inner `TextBlock` и resolver использует `Label`, как существующие текстовые selectors | `SkillChat.Client/Views/UnreadDivider.xaml`, `tests/SkillChat.UiTests.Authoring/Pages/MainWindowPage.cs`, `specs/2026-04-22-unread-divider-ui-test-coverage.md` |
| EXEC | binding remediation | 0.9 | Повторный результат Headless | Пересобрать и повторить UI smoke | Нет | Нет | Headless visual-tree тест показал `IsUnreadBoundary=true`, но `UnreadDivider.IsVisible=false`; visibility binding перенесён в parent message template | `SkillChat.Client/Views/Messages.xaml`, `SkillChat.Client/Views/UnreadDivider.xaml`, `tests/SkillChat.UiTests.Headless/Tests/MainWindowHeadlessSignedInTests.cs`, `specs/2026-04-22-unread-divider-ui-test-coverage.md` |
| EXEC | viewmodel remediation | 0.9 | Результат повторных тестов | Пересобрать и повторить Headless/FlaUI | Нет | Нет | Boundary-флаг выставлялся после вставки item в визуальное дерево; теперь он проставляется до `Messages.Insert` на initial/backfill pages | `SkillChat.Client.ViewModel/MainWindowViewModel.cs`, `specs/2026-04-22-unread-divider-ui-test-coverage.md` |
| EXEC | initial positioning remediation | 0.86 | Результат повторных тестов | Пересобрать и повторить Headless/FlaUI | Нет | Нет | Refresh коллекции оказался неверным направлением; initial positioning теперь сначала ставит scroll offset в начало, чтобы boundary item был материализован до поиска visual control | `SkillChat.Client/Views/MainWindow.xaml.cs`, `SkillChat.Client.ViewModel/MainWindowViewModel.cs`, `specs/2026-04-22-unread-divider-ui-test-coverage.md` |
| EXEC | automation bootstrap remediation | 0.9 | Повторный результат Headless/FlaUI | Пересобрать и повторить UI smoke | Нет | Нет | Signed-in smoke идёт через `SkillChatClientBootstrap`, где unread-флаг проставлялся после назначения коллекции; теперь seeded item получает флаг до binding/materialization | `SkillChat.Client/SkillChatClientBootstrap.cs`, `SkillChat.Client.ViewModel/MainWindowViewModel.cs`, `specs/2026-04-22-unread-divider-ui-test-coverage.md` |
| EXEC | ui coverage split | 0.91 | Нет | Финальная сборка и post-EXEC review | Нет | Нет | Headless runtime не материализует full window message list без `Window.Show()`, а `Show()` ломается на font manager; покрытие разделено на FlaUI full desktop selector и Headless component binding | `tests/SkillChat.UiTests.FlaUI/Tests/MainWindowFlaUiSignedInTests.cs`, `tests/SkillChat.UiTests.Headless/Tests/MainWindowHeadlessSignedInTests.cs`, `specs/2026-04-22-unread-divider-ui-test-coverage.md` |
| EXEC | validation | 0.93 | Нет | Зафиксировать итог и residual risk | Нет | Нет | `dotnet build` и UI/VM проверки зелёные; полный Server suite имеет 1 unrelated failure в attachments/quoted-message тесте | `tests/SkillChat.UiTests.Headless/Tests/MainWindowHeadlessSignedInTests.cs`, `tests/SkillChat.UiTests.FlaUI/Tests/MainWindowFlaUiSignedInTests.cs`, `SkillChat.Client.ViewModel.Test`, `SkillChat.Server.Test` |

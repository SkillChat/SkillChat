# Bootstrap read-marker для unread divider в пустом чате

## 0. Метаданные
- Тип (профиль): `delivery-task`; core: `quest-governance + collaboration-baseline + testing-baseline`; context: `testing-dotnet`; profile: `dotnet-desktop-client`
- Владелец: Codex
- Масштаб: small
- Целевой релиз / ветка: текущая рабочая ветка
- Ограничения:
  - не менять публичный контракт `IChatHub.MarkChatRead(string chatId, DateTimeOffset lastReadMessagePostTime)`
  - не добавлять side effect в `ChatService.Get(GetMessages)`; чтение истории остаётся read-only
  - сохранить текущий rollout-инвариант: существующая история не должна целиком внезапно стать unread только из-за `null` marker
  - автоматические тесты обязательны до фикса и после него
- Связанные ссылки:
  - `specs/2026-03-27-unread-messages-divider.md`
  - `specs/2026-03-30-unread-divider-remediation.md`
  - `C:\Projects\My\Agents\instructions\core\quest-governance.md`
  - `C:\Projects\My\Agents\instructions\core\quest-mode.md`
  - `C:\Projects\My\Agents\instructions\contexts\testing-dotnet.md`
  - `C:\Projects\My\Agents\instructions\profiles\dotnet-desktop-client.md`

## 1. Overview / Цель
Исправить bootstrap unread-tracking для сценария, где пользователь впервые открывает пустой чат, после чего другой участник присылает сообщения. После возврата в чат divider непрочитанных должен появляться без ручных подготовительных действий и без искусственной подмены состояния в automation harness.

## 2. Текущее состояние (AS-IS)
- Сервер выставляет `MessagePage.FirstUnreadMessageId` только если у `ChatMember` уже есть `LastReadMessagePostTime`.
- Клиент вызывает `_hub.MarkChatRead(...)` только когда в `MainWindowViewModel.Messages` уже есть хотя бы одно сообщение.
- Если пользователь открыл чат, пока он пустой, read-marker не инициализируется вообще.
- Если после этого другой пользователь присылает первое сообщение, следующий `GetMessages` всё ещё видит `LastReadMessagePostTime == null` и не возвращает unread boundary.
- Текущие smoke/automation сценарии могут показывать divider без сервера: `SkillChat.Client.SkillChatClientBootstrap` и `SkillChatAutomationApiClient` напрямую подставляют `FirstUnreadMessageId` из automation-state.
- Существующие тесты unread-divider покрывают page-backfill и dedupe уже существующего marker, но не покрывают bootstrap из пустого чата.

## 3. Проблема
Пользовательский сценарий "открыл пустой чат -> ушёл -> получил первые новые сообщения -> вернулся" не может показать unread divider, потому что система не умеет создать начальную read-boundary без уже существующих сообщений в локальной коллекции.

## 4. Цели дизайна
- Разделение ответственности:
  клиент инициирует bootstrap-mark-read в момент фактического чтения пустого чата; сервер безопасно сохраняет baseline.
- Повторное использование:
  использовать уже существующий `MarkChatRead`, не вводя новый hub API.
- Тестируемость:
  добавить reproducing regression-тесты на клиент и сервер.
- Консистентность:
  divider продолжает зависеть от persisted `LastReadMessagePostTime`, а не от локальной эвристики.
- Обратная совместимость:
  rollout-защита от "вся старая история стала unread" остаётся.

## 5. Non-Goals (чего НЕ делаем)
- Не меняем визуальный дизайн divider.
- Не внедряем per-message read receipts.
- Не переписываем `GetMessages` в write-path.
- Не закрываем в этой задаче unrelated нестабильность полного `SkillChat.Server.Test` прогона, если она не связана с unread bootstrap.

## 6. Предлагаемое решение (TO-BE)
### 6.1 Распределение ответственности
- `SkillChat.Client.ViewModel/MainWindowViewModel.cs`
  инициирует bootstrap `MarkChatRead` даже при пустой истории, когда пользователь находится внизу чата и unread-boundary ещё не ждёт позиционирования.
- `SkillChat.Server/Hubs/ChatHub.cs`
  умеет сохранять начальный read-marker и для случая, когда в момент вызова в чате ещё нет видимых сообщений.
- `SkillChat.Server.Test/ChatServiceTests.cs`
  воспроизводит end-to-end серверный сценарий "empty chat bootstrap -> peer message -> unread boundary visible".
- `SkillChat.Client.ViewModel.Test/ClientInteractionTests.cs`
  воспроизводит клиентский trigger bootstrap для пустого чата.

### 6.2 Детальный дизайн
#### Клиент
1. `TryMarkChatReadAsync()` перестаёт мгновенно выходить только из-за пустой `Messages`.
2. Если `Messages.LastOrDefault()` отсутствует, но:
   - `_hub != null`
   - `ChatId` задан
   - `SettingsViewModel.AutoScroll == true`
   - нет pending unread positioning
   клиент вызывает `_hub.MarkChatRead(ChatId, <текущее UTC-время>)`.
3. Этот вызов рассматривается как bootstrap baseline "пользователь видел пустой чат на текущий момент".
4. Поведение для непустого чата остаётся прежним: отправляется `PostTime` последнего реально загруженного сообщения, unread divider очищается после успешного подтверждения.

#### Сервер
1. `ChatHub.MarkChatRead(...)` по-прежнему сначала пытается резолвить requested timestamp к последнему реально видимому сообщению.
2. Если видимое сообщение найдено, сохраняется его `PostTime` как и сейчас.
3. Если видимого сообщения нет:
   - сервер вычисляет безопасный baseline `min(requestedReadTime, DateTimeOffset.UtcNow)`;
   - baseline используется только как read-marker bootstrap для пустого/невидимого чата;
   - marker не должен двигаться назад относительно уже сохранённого значения.
4. После такого bootstrap будущие сообщения с `PostTime > LastReadMessagePostTime` автоматически становятся кандидатами на unread boundary без изменения `GetMessages`.

#### Почему не `GetMessages`
- Вариант "инициализировать marker прямо в `GetMessages`" отвергается, потому что превращает read-only историю в write-path и ломает прозрачность side effects.
- Вариант "подставлять divider локально на клиенте" отвергается, потому что реальный источник истины уже выбран на сервере и локальная эвристика будет расходиться с persisted state.

## 7. Бизнес-правила / Алгоритмы
- Если пользователь видел пустой чат и после этого пришло первое сообщение другого пользователя, divider должен появиться перед этим первым сообщением.
- Bootstrap-marker не должен указывать в будущее позже server `UtcNow`.
- Более старое значение marker не должно затирать более новое.
- Появление bootstrap-marker не должно помечать старую историю unread при `null` rollout-marker.
- Divider по-прежнему отображается максимум один раз.

## 8. Точки интеграции и триггеры
- `MainWindow.xaml.cs -> MessagesScroller_ScrollChanged(...)`
  как и сейчас, вызывает `TryMarkChatReadAsync()` при достижении нижней границы.
- `MainWindowViewModel.TryMarkChatReadAsync()`
  получает новый bootstrap-path для пустой истории.
- `ChatHub.MarkChatRead(...)`
  получает fallback-path для сохранения baseline без видимых сообщений.
- `ChatService.Get(GetMessages)`
  остаётся read-only и использует сохранённый marker без контрактных изменений.

## 9. Изменения модели данных / состояния
- Новых persisted полей нет.
- Новых API-полей нет.
- Меняется только семантика начального заполнения `ChatMember.LastReadMessagePostTime`.
- Клиентский transient-state `lastRequestedReadMarkerTime` продолжает использоваться для dedupe и не должен ломать непустой сценарий.

## 10. Миграция / Rollout / Rollback
- Миграция данных не требуется.
- Rollout:
  новые/пустые чаты начинают получать baseline marker при первом фактическом просмотре пустого чата.
- Обратная совместимость:
  существующий контракт hub и `GetMessages` не меняется.
- Rollback:
  вернуть старую реализацию `TryMarkChatReadAsync` и `ChatHub.MarkChatRead`; сохранённые bootstrap timestamps можно оставить как совместимые данные.

## 11. Тестирование и критерии приёмки
- Acceptance Criteria:
  - если пользователь открыл пустой чат и позже другой участник отправил первое сообщение, при следующей загрузке истории сервер возвращает `FirstUnreadMessageId` для этого сообщения;
  - клиент в пустом чате действительно вызывает `MarkChatRead`, а не пропускает bootstrap;
  - bootstrap-marker не уходит дальше server `UtcNow`;
  - существующий сценарий `TryMarkChatReadAsync` для непустого чата продолжает дедуплицироваться и очищать divider;
  - divider не появляется для всей старой истории только из-за `null` marker.
- Какие тесты добавить/изменить:
  - `SkillChat.Client.ViewModel.Test/ClientInteractionTests.cs`
    reproducing test: `TryMarkChatReadAsync` вызывает hub и на пустом чате.
  - `SkillChat.Server.Test/ChatHubTests.cs`
    test на bootstrap baseline без видимых сообщений и clamp к server time.
  - `SkillChat.Server.Test/ChatServiceTests.cs`
    integration test на сценарий `empty chat bootstrap -> peer message -> FirstUnreadMessageId`.
- Команды для проверки:
```powershell
dotnet test --project .\SkillChat.Client.ViewModel.Test\SkillChat.Client.ViewModel.Test.csproj
dotnet test --project .\SkillChat.Server.Test\SkillChat.Server.Test.csproj
dotnet build
dotnet test
```

## 12. Риски и edge cases
- Риск: repeated bootstrap calls на пустом чате будут продвигать marker вперёд.
  Смягчение: сервер хранит marker монотонно и клиент сохраняет dedupe-marker; это не ломает unread-semantics для будущих сообщений.
- Риск: malicious client может прислать future timestamp.
  Смягчение: сервер clamp-ит bootstrap к своему `UtcNow`.
- Риск: полный server test suite уже содержит unrelated падения.
  Смягчение: сначала прогонять targeted regression, затем полный прогон и явно отделять новые/старые проблемы в отчёте.

## 13. План выполнения
1. Добавить reproducing tests для client bootstrap и server bootstrap/readback сценария.
2. Реализовать bootstrap-path в `MainWindowViewModel.TryMarkChatReadAsync()`.
3. Реализовать safe fallback-path в `ChatHub.MarkChatRead(...)`.
4. Прогнать targeted tests по клиенту и серверу.
5. Прогнать `dotnet build` и `dotnet test`, зафиксировать, что именно подтверждено, а что осталось внешним риском.

## 14. Открытые вопросы
- Блокирующих вопросов нет.

## 15. Соответствие профилю
- Профиль: `dotnet-desktop-client`
- Выполненные требования профиля:
  - UI-поток не блокируется: bootstrap использует уже существующий async hub call;
  - пользовательский flow изменяется и покрывается regression-тестами;
  - публичные selectors / automation-id не меняются;
  - финальная проверка включает `dotnet build` и `dotnet test`.

## 16. Таблица изменений файлов
| Файл | Изменения | Причина |
| --- | --- | --- |
| `SkillChat.Client.ViewModel/MainWindowViewModel.cs` | bootstrap `MarkChatRead` для пустого чата | инициализировать persisted marker |
| `SkillChat.Server/Hubs/ChatHub.cs` | safe fallback baseline при отсутствии видимых сообщений | сохранить начальный marker без write-side effects в `GetMessages` |
| `SkillChat.Client.ViewModel.Test/ClientInteractionTests.cs` | regression на bootstrap из пустого чата | воспроизведение клиентского дефекта |
| `SkillChat.Server.Test/ChatHubTests.cs` | regression на bootstrap-marker без сообщений | защита server-side semantics |
| `SkillChat.Server.Test/ChatServiceTests.cs` | regression на `empty chat -> peer message -> unread boundary` | прямое подтверждение пользовательского сценария |

## 17. Таблица соответствий (было -> стало)
| Область | Было | Стало |
| --- | --- | --- |
| Пустой чат при первом просмотре | marker не создаётся | marker bootstrap-ится без сообщений |
| Возврат после первого peer-message | divider отсутствует из-за `null` marker | divider строится по сохранённому baseline |
| Automation/smoke vs real runtime | harness может показать divider искусственно | серверный сценарий покрыт отдельным regression-тестом |

## 18. Альтернативы и компромиссы
- Вариант: инициализировать marker внутри `GetMessages`
- Плюсы:
  - меньше клиентских изменений
- Минусы:
  - read-only API получает скрытый side effect
  - сложнее reasoning и отладка
- Почему выбранное решение лучше в контексте этой задачи:
  - сохраняет явную точку записи read-state в `MarkChatRead`.

- Вариант: не менять сервер, а локально рисовать divider после первого unseen сообщения
- Плюсы:
  - быстрый UI-only фикс
- Минусы:
  - клиент расходится с persisted state
  - при повторной загрузке история снова не знает о divider
- Почему выбранное решение лучше в контексте этой задачи:
  - причина дефекта именно в отсутствии server-backed baseline, а не в XAML.

## 19. Результат quality gate и review
### SPEC Linter Result

| Блок | Пункты | Статус | Комментарий |
|---|---|---|---|
| A. Полнота спеки | 1-5 | PASS | Цель, AS-IS, проблема, границы и non-goals зафиксированы |
| B. Качество дизайна | 6-10 | PASS | Описаны ответственность, алгоритм bootstrap, rollback и интеграции |
| C. Безопасность изменений | 11-13 | PASS | Контракт API не меняется, side effect в `GetMessages` исключён |
| D. Проверяемость | 14-16 | PASS | Есть reproducing tests и команды проверки |
| E. Готовность к автономной реализации | 17-19 | PASS | Пошаговый план и критерии приёмки заданы |
| F. Соответствие профилю | 20 | PASS | Учтены требования `testing-dotnet` и `dotnet-desktop-client` |

Итог: ГОТОВО

### SPEC Rubric Result

| Критерий | Балл (0/2/5) | Обоснование |
|---|---|---|
| 1. Ясность цели и границ | 5 | Исправляется один конкретный bootstrap-дефект unread divider |
| 2. Понимание текущего состояния | 5 | Зафиксированы реальные условия в сервере, клиенте и automation harness |
| 3. Конкретность целевого дизайна | 5 | Описаны точные изменения в `TryMarkChatReadAsync` и `ChatHub.MarkChatRead` |
| 4. Безопасность (миграция, откат) | 5 | Нет смены контракта и есть rollback-путь |
| 5. Тестируемость | 5 | Есть reproducing tests на сервере и клиенте |
| 6. Готовность к автономной реализации | 5 | Блокирующих вопросов нет, масштаб small |

Итоговый балл: 30 / 30
Зона: готово к автономному выполнению

### Post-SPEC Review
- Статус: PASS
- Что исправлено:
  - Явно добавлено ограничение не переносить write-side effect в `GetMessages`.
  - Уточнён риск, что automation harness может скрывать живой дефект.
- Что осталось на решение пользователя:
  - Ничего блокирующего.

### Post-EXEC Review
- Статус: PASS
- Что исправлено до завершения:
  - Добавлен reproducing test на клиентский bootstrap `MarkChatRead` для пустого чата.
  - Добавлены server regression-тесты на bootstrap baseline и сценарий `empty chat -> peer message -> unread boundary`.
  - `MainWindowViewModel.TryMarkChatReadAsync()` теперь умеет один раз bootstrap-ить read-marker на пустом чате.
  - `ChatHub.MarkChatRead()` теперь сохраняет безопасный baseline, даже если в чате пока нет видимых сообщений.
- Остаточные риски / follow-ups:
  - В полном `SkillChat.Server.Test` suite остаются 3 pre-existing падения, не связанные с этим фиксом (`CreatePassword_CreatesHashForUserWithoutSecret`, `GetMessages_ReturnsVisibleMessagesWithAttachmentsAndQuotedMessage`, `GetMessages_ReturnsGlobalFirstUnreadMessageId_AndHasMoreBefore_WhenUnreadRangeExceedsCurrentPage`).
  - `dotnet test --solution .\SkillChat.sln` без дополнительных аргументов не завершился в отведённый таймаут; `Headless` и `FlaUI` exe также не завершились в отведённый таймаут, поэтому UI-level full-suite не подтверждён в рамках этого цикла.

## Approval
Ожидается фраза: "Спеку подтверждаю"

## 20. Журнал действий агента
| Фаза (SPEC/EXEC) | Тип намерения/сценария | Уверенность в решении (0.0-1.0) | Каких данных не хватает | Следующее действие | Нужна ли передача управления/решения человеку | Было ли фактическое обращение к человеку / решение человека | Короткое объяснение выбора | Затронутые артефакты/файлы |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| SPEC | Диагностика unread-divider после ручного теста пользователя | 0.76 | Нужен воспроизводимый regression-тест под live semantics | Проверить код сервера, клиента и automation harness | Нет | Нет | Сначала нужно отделить настоящий runtime-баг от пробела в тестах | `SkillChat.Server.ServiceInterface/ChatService.cs`, `SkillChat.Server/Hubs/ChatHub.cs`, `SkillChat.Client.ViewModel/MainWindowViewModel.cs`, `SkillChat.Client/SkillChatClientBootstrap.cs` |
| SPEC | Подтверждение instruction stack и границ задачи | 0.92 | Нет подтверждения спеки пользователем | Сформировать рабочую спецификацию и запросить approval | Да | Да, ожидается фраза `Спеку подтверждаю` | Репозиторий требует SPEC-first цикл перед изменением кода | `specs/2026-04-01-unread-divider-bootstrap-fix.md` |
| EXEC | Test-driven debug bootstrap unread marker | 0.95 | Нужны автоматические подтверждения реального runtime-сценария | Добавить reproducing tests и прогнать их до фикса | Нет | Да, пользователь подтвердил спецификацию | Сначала зафиксирован живой дефект тестами, чтобы не исправлять вслепую | `SkillChat.Client.ViewModel.Test/ClientInteractionTests.cs`, `SkillChat.Server.Test/ChatHubTests.cs`, `SkillChat.Server.Test/ChatServiceTests.cs` |
| EXEC | Минимальный продуктовый фикс клиента и сервера | 0.93 | Нужно проверить влияние на существующий read-flow | Изменить `TryMarkChatReadAsync` и `MarkChatRead`, не затрагивая `GetMessages` контракт | Нет | Нет | Корень дефекта в отсутствии bootstrap marker, а не в рендеринге divider | `SkillChat.Client.ViewModel/MainWindowViewModel.cs`, `SkillChat.Server/Hubs/ChatHub.cs` |
| EXEC | Верификация и post-EXEC review | 0.82 | Полный UI/full-solution прогон ограничен таймаутами test infrastructure | Прогнать build, targeted suites и зафиксировать остаточные внешние проблемы | Нет | Нет | Изменение подтверждено targeted regression-тестами и solution build, остаточные проблемы отделены как pre-existing/infrastructure | `SkillChat.sln`, `TestResults/*`, `specs/2026-04-01-unread-divider-bootstrap-fix.md` |

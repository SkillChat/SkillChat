# Разделитель непрочитанных сообщений на основе legacy-ветки `feature/#62-New-messages-line`

## 0. Метаданные
- Тип (профиль): `delivery-task`; core: `quest-governance + collaboration-baseline + testing-baseline`; context: `testing-dotnet`; profile: `dotnet-desktop-client`
- Владелец: команда SkillChat, исполнитель `TBD`
- Масштаб: medium
- Целевой релиз / ветка: `master`, ближайший спринт после поднятия `fix/#100` и `fix/#111`
- Ограничения:
  - не делать прямой `rebase` legacy-ветки; использовать её только как reference
  - брать в работу только одну стратегическую тему из legacy-наследия
  - первый PR не должен включать delivered/read receipts и полный realtime-пересчёт
  - сохранить стабильность `automation-id` для UI-тестов
  - перед завершением реализации обязательны `dotnet build` и `dotnet test`
- Связанные ссылки:
  - `github.com/feature/#62-New-messages-line`
  - `github.com/feature/#14-message-status`
  - `github.com/fix/#100-name-for-remote-users`
  - `github.com/fix/#111-fix-pop-up-notifications`

## 1. Overview / Цель
Восстановить идею из legacy-ветки `feature/#62-New-messages-line` в актуальной архитектуре `master` и реализовать первый вертикальный slice: пользователь после возврата в чат видит визуальный разделитель между уже просмотренными и новыми сообщениями, а экран открывается в позиции, где этот разделитель уже находится в видимой области.

Решение выбирается как следующая стратегическая тема после двух быстрых фиксов, потому что:
- даёт понятную пользовательскую ценность без полного пересмотра протокола сообщений;
- заметно меньше по риску, чем полная система статусов доставки/прочтения из `feature/#14-message-status`;
- может быть реализовано поверх текущих `MainWindowViewModel`, `ChatService`, `ChatHub` и UI-тестовой инфраструктуры.

## 2. Текущее состояние (AS-IS)
- История сообщений загружается в `SkillChat.Client.ViewModel/MainWindowViewModel.cs` через `LoadMessageHistoryCommand`, который вызывает `GetMessages` и заполняет `ObservableCollection<MessageViewModel>`.
- Серверная история живёт в `SkillChat.Server.ServiceInterface/ChatService.cs` и уже фильтрует сообщения по `MessagesHistoryDateBegin` из `SkillChat.Server.Domain/ChatMember.cs`. Это покрывает сценарий очистки чата, но не хранит границу "прочитано/непрочитано".
- Клиентский `MessageViewModel` не умеет хранить признак boundary-разделителя и коллекция сообщений типизирована как `ObservableCollection<MessageViewModel>`.
- Текущий `master` не содержит ни unread-divider, ни статусов сообщений в протоколе и UI.
- Ветка `feature/#62-New-messages-line` предлагает более тяжёлое решение с отдельными моделями статусов пользователя и отдельным визуальным компонентом, но отстаёт от `master` более чем на 150 коммитов и конфликтует в ключевых файлах клиента и сервера.

## 3. Проблема
Пользователь не может быстро понять, где заканчиваются уже просмотренные сообщения и начинаются новые, а команда не имеет безопасного и небольшого плана, как вытащить полезную часть из legacy-ветки без большого конфликтного `rebase`.

## 4. Цели дизайна
- Разделение ответственности:
  сервер вычисляет unread boundary; клиент только отображает её.
- Повторное использование:
  использовать текущие `GetMessages`, `MainWindowViewModel`, `MessageViewModel`, UI-тестовую инфраструктуру.
- Тестируемость:
  сделать boundary явной частью ответа сервера и явным флагом в `MessageViewModel`.
- Консистентность:
  не вводить параллельную модель списка сообщений ради одного разделителя.
- Предсказуемость initial positioning:
  при наличии unread-divider первый экран должен показывать divider, а не безусловно скроллить к самому низу.
- Обратная совместимость:
  не ломать существующих клиентов и не помечать всю старую историю как непрочитанную после rollout.

## 5. Non-Goals (чего НЕ делаем)
- Не внедряем delivered/read status для каждого сообщения.
- Не переносим ветку `feature/#14-message-status` в этот же scope.
- Не делаем прямой `rebase` `feature/#62-New-messages-line`.
- Не вводим отдельный тип элемента списка вместо `MessageViewModel` в первом PR.
- Не покрываем в первом PR полный realtime-цикл обновления read-marker при каждом входящем сообщении и произвольное отслеживание viewport для каждой прокрутки.
- Не берём в этот scope реакции, emoji-button, `Fixies` и другие legacy-ветки.

## 6. Предлагаемое решение (TO-BE)
### 6.1 Распределение ответственности
- `SkillChat.Server.Domain/ChatMember.cs`
  хранит read-marker пользователя в конкретном чате.
- `SkillChat.Server.ServiceModel/Molds/MessagePage.cs`
  возвращает `FirstUnreadMessageId` как часть ответа истории.
- `SkillChat.Server.ServiceInterface/ChatService.cs`
  вычисляет boundary для текущего пользователя без сайд-эффекта на чтение истории.
- `SkillChat.Interface/IChatHub.cs`
  объявляет явный метод обновления read-marker.
- `SkillChat.Server/Hubs/ChatHub.cs`
  обновляет read-marker в `ChatMember` по явному действию клиента.
- `SkillChat.Client.ViewModel/MainWindowViewModel.cs`
  отмечает первое непрочитанное сообщение флагом и управляет режимом initial positioning / read-marker.
- `SkillChat.Client.ViewModel/MessageViewModel.cs`
  хранит булев флаг boundary и не меняет текущую модель списка.
- `SkillChat.Client/Views/MainWindow.xaml`
  рендерит divider перед отмеченным сообщением.
- `SkillChat.Client/Views/MainWindow.xaml.cs`
  переопределяет текущее поведение `ScrollToEnd()` на первом прогоне, если в истории есть unread-divider.

### 6.2 Детальный дизайн
#### Серверная модель и контракты
- Добавить в `ChatMember` новое опциональное поле `LastReadMessagePostTime`.
- Добавить в `MessagePage` новое опциональное поле `FirstUnreadMessageId`.
- Добавить в `IChatHub` и `ChatHub` метод:
  `Task MarkChatRead(string chatId, DateTimeOffset lastReadMessagePostTime)`.

#### Алгоритм получения history
1. `GetMessages` загружает чат и `ChatMember` текущего пользователя.
2. История по-прежнему фильтруется по `BeforePostTime`, `HideForUsers` и `MessagesHistoryDateBegin`.
3. Если `LastReadMessagePostTime` задан:
   сервер находит самое раннее сообщение в возвращаемой странице, у которого `PostTime > LastReadMessagePostTime` и `UserId != currentUserId`.
4. `MessagePage.FirstUnreadMessageId` получает `Id` этого сообщения.
5. Если `LastReadMessagePostTime == null`:
   сервер не возвращает unread boundary. Это правило rollout-защиты, чтобы после обновления не пометить всю старую историю как непрочитанную.

#### Алгоритм начального позиционирования
1. Если `FirstUnreadMessageId` отсутствует, клиент сохраняет текущее поведение initial load и может открывать чат у нижней границы истории.
2. Если `FirstUnreadMessageId` присутствует, `MainWindowViewModel` включает одноразовый режим initial positioning для unread-divider.
3. На первом layout-pass `MainWindow.xaml.cs` не вызывает безусловный `ScrollToEnd()`, а прокручивает `MessagesScrollViewer` так, чтобы divider оказался внутри viewport.
4. Предпочтительное положение divider: верхняя часть viewport с небольшим отступом; это гарантирует, что пользователь видит начало непрочитанного диапазона.
5. Если непрочитанных сообщений больше высоты viewport, более новые сообщения остаются ниже видимой области. Это ожидаемое и обязательное поведение первого PR.

#### Алгоритм обновления read-marker
1. Клиент не вызывает `MarkChatRead` сразу после initial load, если в открытом чате был показан unread-divider и часть новых сообщений может находиться ниже fold.
2. `MarkChatRead` вызывается только когда пользователь доскроллил чат до нижней границы и последние загруженные сообщения реально попали в видимую область.
3. Клиент передаёт в `MarkChatRead` максимальный `PostTime` среди актуально загруженных сообщений у нижней границы.
4. Сервер обновляет `ChatMember.LastReadMessagePostTime` только если новое значение больше уже сохранённого.
5. При отсутствии сообщений вызов не делается.

#### Клиентское отображение
1. После маппинга `MessageMold -> MessageViewModel` клиент сравнивает `MessagePage.FirstUnreadMessageId` с `Id` сообщений в текущей выборке.
2. Только одно сообщение получает `IsUnreadBoundary = true`.
3. Divider рисуется inline в существующем шаблоне сообщения, а не отдельным элементом коллекции.
4. Для тестов добавляется стабильный `automation-id` вида `UnreadDivider_<MessageId>`.
5. На initial load при наличии divider автоматический скролл в конец должен быть подавлен до завершения одноразового позиционирования divider в viewport.

#### Обработка ошибок
- Если сервер не смог вычислить boundary, история загружается без divider.
- Если вызов `MarkChatRead` завершился ошибкой, история остаётся отображённой; обновление read-marker считается best-effort, ошибка логируется.
- При повторной загрузке той же истории divider может временно повториться только если `MarkChatRead` не сохранился; это допустимый деградирующий сценарий.

#### Производительность
- Вычисление boundary делается по уже выбранной странице сообщений без отдельной полной выборки по чату.
- Новое поле в `ChatMember` не требует дополнительной коллекции/индекса.
- Клиентский boundary-флаг не меняет тип коллекции и не требует отдельной виртуализации/контейнеров.

## 7. Бизнес-правила / Алгоритмы (если есть)
- Unread boundary показывается только перед сообщением другого пользователя.
- В одной отображаемой истории допускается не более одного divider.
- `LastReadMessagePostTime` изменяется монотонно вверх; более старое значение не должно затирать новое.
- При `LastReadMessagePostTime == null` unread-divider не показывается до первого успешного `MarkChatRead`.
- Очистка истории через `MessagesHistoryDateBegin` имеет приоритет: boundary не может ссылаться на сообщение, которое уже скрыто правилами истории.
- Если unread-divider присутствует при первом открытии чата, он обязан быть видим в viewport без ручной прокрутки пользователя.
- Если диапазон новых сообщений длиннее одного экрана, часть более новых сообщений должна оставаться ниже видимой области, а не считаться автоматически прочитанной.

## 8. Точки интеграции и триггеры
- `MainWindowViewModel.LoadMessageHistoryCommand`
  получает `FirstUnreadMessageId`, выставляет `IsUnreadBoundary` и включает режим initial positioning.
- `ChatService.Get(GetMessages request)`
  вычисляет `FirstUnreadMessageId`.
- `ChatHub.MarkChatRead`
  обновляет persisted read-marker только после фактического достижения нижней границы.
- `MainWindow.xaml`
  рендерит divider и `automation-id`.
- `MainWindow.xaml.cs`
  управляет initial scroll positioning и подавляет `ScrollToEnd()` на первом прогоне при наличии divider.
- UI-тесты в `tests/SkillChat.UiTests.*`
  проверяют не только присутствие divider по `automation-id`, но и его видимость в первом экране.

## 9. Изменения модели данных / состояния
- Новые поля:
  - `ChatMember.LastReadMessagePostTime : DateTimeOffset?` persisted
  - `MessagePage.FirstUnreadMessageId : string` calculated/transport
  - `MessageViewModel.IsUnreadBoundary : bool` calculated/client-only
- Новое transient-состояние клиента:
  - одноразовый режим initial positioning unread-divider
- Persisted vs calculated:
  - persisted хранится только read-marker на сервере;
  - id первого unread сообщения вычисляется на лету;
  - boundary-флаг и состояние initial positioning живут только на клиенте.
- Влияние на хранилище:
  RavenDB схемно-совместим; старые документы `ChatMember` останутся валидны с `null` полем.

## 10. Миграция / Rollout / Rollback
- Поведение при первом запуске после rollout:
  для существующих `ChatMember` поле `LastReadMessagePostTime` отсутствует, значит divider не показывается до первого успешного чтения истории и вызова `MarkChatRead`.
- Обратная совместимость:
  новое поле ответа `FirstUnreadMessageId` опционально; старые клиенты его проигнорируют.
- План отката:
  клиент может перестать использовать `FirstUnreadMessageId`, а сервер может перестать заполнять поле и игнорировать `MarkChatRead`; сохранённое поле в `ChatMember` можно оставить без cleanup.

## 11. Тестирование и критерии приёмки
- Acceptance Criteria:
  - Если у пользователя есть новые сообщения после `LastReadMessagePostTime`, в загруженной истории отображается ровно один divider перед первым непрочитанным сообщением.
  - При первом открытии чата divider уже находится в видимой области экрана без ручной прокрутки пользователя.
  - Если непрочитанных сообщений больше высоты viewport, нижняя часть непрочитанного диапазона остаётся ниже видимой области, а чат не открывается сразу в самом низу.
  - Если новых сообщений нет, divider не отображается.
  - После доведения scroll до нижней границы и успешного `MarkChatRead` повторная загрузка той же страницы не показывает divider для уже прочитанного диапазона.
  - После rollout на существующую базу divider не появляется перед всей историей из-за `null` marker.
  - `automation-id` divider стабилен и пригоден для UI-тестов.
- Какие тесты добавить/изменить:
  - `SkillChat.Server.Test/ChatServiceTests.cs`
    тест на вычисление `FirstUnreadMessageId`.
  - `SkillChat.Server.Test/ChatHubTests.cs`
    тест на монотонное обновление `LastReadMessagePostTime`.
  - `SkillChat.Client.ViewModel.Test/ClientInteractionTests.cs`
    тест на выставление `IsUnreadBoundary` только одному сообщению и на включение режима initial positioning.
  - `tests/SkillChat.UiTests.Authoring/Pages/MainWindowPage.cs`
    селектор для `UnreadDivider_<MessageId>`.
  - `tests/SkillChat.UiTests.Headless/...`
    сценарий появления divider после загрузки seeded history и проверки, что divider видим без ручного scroll-to-end.
- Команды для проверки:
```powershell
dotnet build
dotnet test SkillChat.Server.Test/SkillChat.Server.Test.csproj --filter "FullyQualifiedName~ChatServiceTests|FullyQualifiedName~ChatHubTests"
dotnet test SkillChat.Client.ViewModel.Test/SkillChat.Client.ViewModel.Test.csproj --filter "FullyQualifiedName~ClientInteractionTests"
dotnet test tests/SkillChat.UiTests.Headless/SkillChat.UiTests.Headless.csproj --filter "FullyQualifiedName~MainWindow"
dotnet test
```

## 12. Риски и edge cases
- Риск: использование `PostTime` как read-marker при одинаковых timestamps.
  Смягчение: в первом PR принять этот риск как допустимый; при необходимости во второй фазе перейти на `(PostTime, MessageId)`.
- Риск: вызов `MarkChatRead` после каждой загрузки history добавляет запись even for passive load.
  Смягчение: обновлять marker только если найден `maxPostTime` и только монотонно вверх.
- Риск: при scroll-back загрузке старых страниц клиент может вызвать `MarkChatRead` со старым временем.
  Смягчение: сервер не принимает регрессирующее значение.
- Риск: divider может быть не виден в текущем XAML-шаблоне без аккуратной верстки.
  Смягчение: ограничить UI-изменение одним inline-блоком и закрепить UI-тестом.
- Риск: текущее `ScrollToEnd()` на первом прогоне в `MainWindow.xaml.cs` перетрёт unread positioning.
  Смягчение: добавить одноразовый режим initial positioning, который временно подавляет `ScrollToEnd()` при наличии divider.
- Риск: feature может быть воспринята как полноценная read-status система.
  Смягчение: явно держать `Non-Goals` и не добавлять per-message receipts в этот scope.

## 13. План выполнения
1. Создать новую рабочую ветку от `master` под unread-divider.
2. Добавить `LastReadMessagePostTime` в `ChatMember` и `FirstUnreadMessageId` в `MessagePage`.
3. Реализовать вычисление boundary в `ChatServiceTests` через падающий тест, затем в `ChatService`.
4. Добавить `MarkChatRead` в `IChatHub` и `ChatHubTests`, затем реализовать в `ChatHub`.
5. Добавить `IsUnreadBoundary` в `MessageViewModel`.
6. Изменить `MainWindowViewModel` так, чтобы он выставлял boundary-флаг и включал режим initial positioning.
7. Изменить `MainWindow.xaml.cs`, чтобы initial load при наличии divider позиционировал viewport на divider вместо безусловного `ScrollToEnd()`.
8. Добавить inline divider в `MainWindow.xaml` и стабильный `automation-id`.
9. Расширить authoring/headless UI-тесты сценарием видимости divider на первом экране.
10. Прогнать targeted tests, затем полный `dotnet test`.

## 14. Открытые вопросы
- Блокирующих вопросов нет.
- Неблокирующее уточнение на ревью:
  оставить ли `PostTime` единственным read-marker в первой фазе или сразу закладывать composite key `(PostTime, MessageId)`.

## 15. Соответствие профилю
- Профиль: `dotnet-desktop-client`
- Выполненные требования профиля:
  - UI-поток не должен блокироваться: расчёт boundary остаётся на сервере, клиент делает только локальную пометку готовых моделей.
  - Пользовательский поток меняется: в spec включены ViewModel/UI/UI-tests изменения.
  - `automation-id` стабилизируется через `UnreadDivider_<MessageId>`.
  - Initial positioning описан так, чтобы быть совместимым с текущим `ScrollViewer` и проверяемым UI-тестами.
  - Команды `dotnet build` и `dotnet test` включены в секцию проверки.

## 16. Таблица изменений файлов
| Файл | Изменения | Причина |
| --- | --- | --- |
| `SkillChat.Server.Domain/ChatMember.cs` | Новое persisted-поле `LastReadMessagePostTime` | Хранить read-marker по пользователю и чату |
| `SkillChat.Server.ServiceModel/Molds/MessagePage.cs` | Новое поле `FirstUnreadMessageId` | Передать boundary клиенту |
| `SkillChat.Server.ServiceInterface/ChatService.cs` | Вычисление `FirstUnreadMessageId` | Сервер должен знать историю и marker |
| `SkillChat.Interface/IChatHub.cs` | Новый метод `MarkChatRead` | Явный контракт обновления read-marker |
| `SkillChat.Server/Hubs/ChatHub.cs` | Реализация `MarkChatRead` | Сохранение marker |
| `SkillChat.Client.ViewModel/MessageViewModel.cs` | Флаг `IsUnreadBoundary` | Отображение divider без смены типа коллекции |
| `SkillChat.Client.ViewModel/MainWindowViewModel.cs` | Применение boundary и управление initial positioning | Клиентская интеграция |
| `SkillChat.Client/Views/MainWindow.xaml` | Inline divider и `automation-id` | Визуальное поведение и тестируемость |
| `SkillChat.Client/Views/MainWindow.xaml.cs` | Позиционирование viewport на divider при initial load | Обеспечить видимость divider на первом экране |
| `SkillChat.Server.Test/ChatServiceTests.cs` | Новые тесты boundary | Regression на серверную логику |
| `SkillChat.Server.Test/ChatHubTests.cs` | Новые тесты `MarkChatRead` | Regression на persisted read-marker |
| `SkillChat.Client.ViewModel.Test/ClientInteractionTests.cs` | Новые тесты boundary-флага и initial positioning mode | Regression на клиентскую логику |
| `tests/SkillChat.UiTests.Authoring/Pages/MainWindowPage.cs` | Новый resolver для divider | Стабильный UI API для тестов |
| `tests/SkillChat.UiTests.Headless/*` | Сценарий unread-divider и initial viewport | Сквозная проверка пользовательского потока |

## 17. Таблица соответствий (было -> стало)
| Область | Было | Стало |
| --- | --- | --- |
| Стратегическая тема legacy | Нет зафиксированного следующего большого направления | Зафиксирована реализация unread-divider на базе идей `feature/#62` |
| Коллекция сообщений | Только `MessageViewModel` без boundary | Та же коллекция, но с флагом `IsUnreadBoundary` |
| Серверный ответ истории | Только `List<MessageMold>` | `List<MessageMold>` + `FirstUnreadMessageId` |
| Серверное состояние чата | Только `MessagesHistoryDateBegin` | `MessagesHistoryDateBegin` + `LastReadMessagePostTime` |
| Начальное открытие чата | Первый прогон уводит чат в конец истории | При наличии divider первый экран позиционируется на boundary |
| Обновление read-state | Отсутствует | Явный `MarkChatRead` через hub после достижения нижней границы |
| UI-тесты | Нет селектора для unread-divider | Стабильный `UnreadDivider_<MessageId>` |

## 18. Альтернативы и компромиссы
- Вариант: брать `feature/#14-message-status` как следующую тему
  - Плюсы: покрывает более широкий сценарий read/delivery status
  - Минусы: больше конфликтов, больше контрактных изменений, слабее первый UX-win
  - Почему выбранное решение лучше в контексте этой задачи:
    unread-divider даёт меньший и безопасный первый шаг, не блокируя возможную вторую фазу со статусами.

- Вариант: сделать отдельный элемент списка сообщений (`NewMessagesLineViewModel`)
  - Плюсы: более явная UI-модель
  - Минусы: меняет тип коллекции, шаблоны, тесты и логику selection/navigation
  - Почему выбранное решение лучше в контексте этой задачи:
    inline divider через флаг в `MessageViewModel` минимизирует объём изменений и риск регрессий.

- Вариант: после initial load всегда скроллить чат в конец и сразу вызывать `MarkChatRead`
  - Плюсы: проще реализация и меньше кода в scroll-логике
  - Минусы: divider может не попасть в viewport, а сообщения ниже fold будут ошибочно считаться прочитанными
  - Почему выбранное решение лучше в контексте этой задачи:
    требование UX прямо требует видимости divider на первом экране и запрещает автоматически считать скрытые ниже fold сообщения прочитанными.

- Вариант: обновлять read-marker прямо в `GetMessages`
  - Плюсы: меньше API-контрактов
  - Минусы: read-only запрос получает сайд-эффект, сложнее reasoning и rollback
  - Почему выбранное решение лучше в контексте этой задачи:
    явный `MarkChatRead` делает запись состояния прозрачной и пригодной для будущего расширения.

- Вариант: прямой `rebase` legacy-ветки `feature/#62-New-messages-line`
  - Плюсы: гипотетически быстрее использовать старый код
  - Минусы: высокая конфликтность, устаревшие модели, лишний scope
  - Почему выбранное решение лучше в контексте этой задачи:
    свежая реализация от `master` дешевле в сопровождении и проще в ревью.

## 19. Результат прогона линтера
### SPEC Linter Result

| Блок | Пункты | Статус | Комментарий |
|---|---|---|---|
| A. Полнота спеки | 1-5 | PASS | Цель, AS-IS, проблема, цели дизайна и Non-Goals зафиксированы |
| B. Качество дизайна | 6-10 | PASS | Ответственность, контракты, алгоритмы, ошибки и rollout описаны |
| C. Безопасность изменений | 11-13 | PASS | Совместимость, rollout и rollback описаны; scope ограничен |
| D. Проверяемость | 14-16 | PASS | Есть acceptance criteria, тест-план и команды проверки |
| E. Готовность к автономной реализации | 17-19 | PASS | Есть пошаговый план, неблокирующих вопросов минимум |
| F. Соответствие профилю | 20 | PASS | Требования `dotnet-desktop-client` и `testing-dotnet` учтены |

Итог: ГОТОВО

### SPEC Rubric Result

| Критерий | Балл (0/2/5) | Обоснование |
|---|---|---|
| 1. Ясность цели и границ | 5 | Цель первого PR и Non-Goals заданы явно |
| 2. Понимание текущего состояния | 5 | Описаны текущие точки входа клиента, сервера и тестов |
| 3. Конкретность целевого дизайна | 5 | Определены новые поля, методы, алгоритмы и файл-список |
| 4. Безопасность (миграция, откат) | 5 | `null` rollout, backward compatibility и rollback описаны |
| 5. Тестируемость | 5 | Есть серверные, клиентские и UI-проверки с командами |
| 6. Готовность к автономной реализации | 2 | Для старта реализации достаточно, но final naming read-marker может быть уточнён на ревью |

Итоговый балл: 27 / 30
Зона: готово к автономному выполнению

## Approval
Ожидается фраза: "Спеку подтверждаю"

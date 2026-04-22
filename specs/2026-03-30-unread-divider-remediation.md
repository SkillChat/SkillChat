# Ремедиация unread-divider после code review

## 0. Метаданные
- Тип: `delivery-task`
- Базовая спека: `specs/2026-03-27-unread-messages-divider.md`
- Назначение: исправить дефекты дизайна и реализации, найденные на ревью 2026-03-30
- Масштаб: medium
- Целевой результат: корректная граница unread при больших диапазонах, безопасное обновление read-marker, отсутствие stale client state после локальной очистки истории

## 1. Контекст
Первая спека на unread-divider зафиксировала общий UX и базовый протокол. После реализации и ревью выявились три дефекта:

1. `FirstUnreadMessageId` сейчас вычисляется только внутри последней загруженной страницы, а не по реальной границе `read/unread` во всём видимом чате.
2. `MarkChatRead` доверяет клиентскому `DateTimeOffset` без привязки к реально существующим сообщениям чата.
3. Клиент не сбрасывает pending unread state при локальной очистке истории и может оставить висящее состояние позиционирования.

Эта спека не заменяет исходную unread-divider тему, а уточняет её и закрывает найденные ошибки.

## 2. Проблема
Текущая реализация корректно работает только на коротком unread-диапазоне, который полностью помещается в первую страницу истории. При реальном длинном диапазоне:

- divider может указывать не на реальную границу, а на самый старый unread внутри первой страницы;
- клиент может считать прочитанным состояние, которое сервер подтвердит даже при некорректном или злонамеренном timestamp;
- после очистки истории unread state на клиенте может остаться частично активным.

В результате пользователь видит визуально правдоподобное, но неверное состояние чата.

## 3. Цели
- Сделать `FirstUnreadMessageId` глобальной, а не page-local границей.
- Гарантировать, что initial open умеет дотянуть историю до реальной границы unread, даже если она старше первой страницы.
- Исключить запись read-marker позже фактически существующих сообщений.
- Сделать сброс unread state атомарным для всех локальных reset-сценариев.
- Сохранить текущую модель `MessageViewModel` и обратную совместимость существующего API насколько возможно.

## 4. Non-Goals
- Не вводить полноценную per-message read-receipt систему.
- Не менять модель хранения чата радикально.
- Не переписывать целиком pagination API.
- Не пытаться через эту задачу решить все проблемы пагинации по одинаковому `PostTime`.

## 5. Дефекты, которые надо устранить

### D1. Page-local unread boundary
Сейчас сервер:
- выбирает newest-first страницу;
- делает `Take(pageSize)`;
- ищет `LastOrDefault(...)` только внутри этого среза.

Это неверно, если истинная граница unread находится старше первой страницы.

### D2. Невалидный read-marker
Сейчас сервер принимает `MarkChatRead(chatId, lastReadMessagePostTime)` и сохраняет значение монотонно вверх без проверки, соответствует ли timestamp реальному сообщению чата.

### D3. Stale unread state после локального reset
После `CleanChatForMe` и других локальных reset-сценариев unread state должен сбрасываться полностью:
- `InitialUnreadBoundaryMessageId`
- флаги `IsUnreadBoundary`
- локальный dedupe-marker `lastRequestedReadMarkerTime`
- pending one-shot positioning

## 6. Предлагаемое решение

### 6.1 Сервер: вычисление глобальной unread boundary
`MessagePage.FirstUnreadMessageId` меняет смысл:
- это `Id` самого раннего непрочитанного видимого сообщения в чате целиком;
- а не самого раннего непрочитанного сообщения в текущей странице.

Дополнительно `MessagePage` получает новое поле:
- `HasMoreBefore : bool`

Назначение:
- клиент должен понимать, есть ли ещё более старые сообщения, которые не вошли в текущую страницу;
- это нужно для авто-подгрузки истории до тех пор, пока не будет реально загружена boundary-message.

Алгоритм на сервере:
1. Сформировать базовый visible query по чату с учётом:
   - `ChatId`
   - `MessagesHistoryDateBegin`
   - `HideForUsers`
2. Если `LastReadMessagePostTime == null`, `FirstUnreadMessageId = null`.
3. Если `LastReadMessagePostTime != null`, отдельным запросом найти самое раннее видимое сообщение, для которого:
   - `UserId != currentUserId`
   - `PostTime > LastReadMessagePostTime`
   - сортировка по `PostTime ASC`
4. Только после этого загружать текущую страницу сообщений для конкретного окна pagination.
5. `HasMoreBefore` вычислять через запрос `pageSize + 1` либо отдельную проверку, не полагаясь на размер уже отданной страницы как на единственный индикатор конца истории.

Итог:
- сервер всегда знает истинную границу unread;
- клиент получает либо саму boundary-message в текущем окне, либо сигнал, что до неё ещё надо дотянуть историю.

### 6.2 Клиент: initial load с авто-догрузкой до boundary
Первый `LoadMessageHistoryCommand` при открытии чата должен работать в два этапа:

1. Загрузить newest-first страницу как сейчас.
2. Если `FirstUnreadMessageId == null`, завершить initial load по старому сценарию.
3. Если `FirstUnreadMessageId` уже есть в загруженных сообщениях:
   - пометить boundary;
   - выполнить initial positioning.
4. Если `FirstUnreadMessageId` отсутствует в загруженных сообщениях, но `HasMoreBefore == true`:
   - последовательно подгружать более старые страницы с текущим `BeforePostTime`;
   - после каждой страницы проверять, появилась ли boundary-message;
   - остановиться при первом нахождении boundary либо при `HasMoreBefore == false`.
5. После того как boundary-message загружена:
   - выставить ровно один `IsUnreadBoundary = true`;
   - выполнить one-shot positioning так, чтобы boundary была внутри viewport;
   - не автоскроллить в конец.

Это решение сохраняет текущую модель пагинации назад по времени и при этом выполняет ключевое UX-требование:
- divider виден на первом экране;
- если unread длиннее одного экрана, новые сообщения остаются ниже viewport.

### 6.3 Сервер: валидация `MarkChatRead`
Сигнатуру `MarkChatRead(chatId, lastReadMessagePostTime)` в рамках этого remediation не меняем.

Но меняем семантику:
- сервер больше не сохраняет raw client timestamp напрямую;
- сервер резолвит его через реально существующие сообщения видимого чата.

Алгоритм:
1. Найти последнее видимое сообщение чата с `PostTime <= requestedTimestamp`.
2. Если такого сообщения нет, запрос игнорируется.
3. Если `requestedTimestamp` больше фактического последнего видимого сообщения чата, использовать `PostTime` этого последнего сообщения.
4. Обновлять `LastReadMessagePostTime` только монотонно вверх после server-side resolution.

Следствие:
- `DateTimeOffset.MaxValue` и любой другой future timestamp не может продвинуть marker дальше реально существующей истории.
- timestamp между двумя сообщениями будет безопасно “прижат” к последнему реально существующему сообщению не позже него.

### 6.4 Клиент: единый reset unread state
Нужно ввести единый метод, например:
- `ResetUnreadBoundaryState()`

Он должен:
- снять `IsUnreadBoundary` со всех сообщений;
- очистить `InitialUnreadBoundaryMessageId`;
- очистить `lastRequestedReadMarkerTime`.

Этот метод обязан вызываться:
- при `SignOut`;
- при `CleanChatForMe`;
- перед началом initial load нового чата;
- при любом локальном сценарии, который полностью пересобирает `Messages`.

Дополнительно `PositionUnreadDividerOnLayoutUpdated` должен уметь безопасно самозавершаться, если:
- target id очищен;
- коллекция `Messages` уже сброшена;
- boundary-message больше не существует в текущем visual/data state.

Это нужно, чтобы после локальной очистки не оставался висящий `LayoutUpdated`-handler.

## 7. Изменения контрактов и состояния

### Серверные контракты
- `MessagePage.FirstUnreadMessageId`
  - новое значение: глобальная unread boundary по чату
- `MessagePage.HasMoreBefore`
  - новое поле

### Клиентское transient state
- `InitialUnreadBoundaryMessageId`
- `lastRequestedReadMarkerTime`
- одноразовый режим initial positioning

Все эти состояния должны жить только в рамках актуально открытого чата и обнуляться единым reset-path.

## 8. Acceptance Criteria
- Если unread-диапазон длиннее первой страницы, клиент автоматически догружает старые страницы до тех пор, пока не загрузит реальную boundary-message.
- Divider ставится только на истинную границу между прочитанными и непрочитанными сообщениями, а не на page-local approximation.
- При первом открытии чата divider находится в viewport.
- Если unread-диапазон длиннее одного экрана, более новые unread остаются ниже видимой области.
- `MarkChatRead` не может записать timestamp позже реально существующего последнего видимого сообщения.
- `MarkChatRead` с future timestamp не ломает дальнейшее отображение divider.
- После `CleanChatForMe` в клиенте не остаётся ни boundary-флага, ни pending positioning, ни stale read-marker state.

## 9. Тестирование

### Server
- `ChatServiceTests`
  - boundary вычисляется глобально, а не по первой странице
  - сценарий: unread сообщений больше `pageSize`, true boundary не входит в initial page
  - `FirstUnreadMessageId` всё равно указывает на true boundary
- `ChatHubTests`
  - future timestamp clamp
  - timestamp между сообщениями резолвится к последнему допустимому сообщению
  - монотонность сохраняется после server-side resolution

### Client ViewModel
- initial load делает повторные запросы, пока не будет загружена boundary-message
- после нахождения boundary только одно сообщение получает `IsUnreadBoundary`
- `CleanChatForMe` и reset-сценарии очищают unread state полностью
- `TryMarkChatReadAsync` после reset не использует stale `lastRequestedReadMarkerTime`

### UI / integration
- smoke остаётся зелёным
- отдельная проверка должна покрывать сценарий “boundary старше первой страницы”
- для geometry-проверки не опираться на поиск вложенных templated элементов через AppAutomation automation-id; использовать либо ViewModel-level контракт, либо прямой headless/view test поверх Avalonia control tree

## 10. План реализации
1. Расширить `MessagePage` полем `HasMoreBefore`.
2. Переписать серверный расчёт `FirstUnreadMessageId` так, чтобы он работал по глобальному visible query.
3. Добавить server test на unread-range > `pageSize`.
4. Переписать client initial load на auto-backfill older pages до boundary.
5. Выделить единый `ResetUnreadBoundaryState()` и подключить его в `SignOut`, `CleanChatForMe` и reset-path нового initial load.
6. Добавить server-side resolution/clamp в `ChatHub.MarkChatRead`.
7. Добавить tests на future timestamp и cleanup stale state.
8. Прогнать:
```powershell
dotnet test --project SkillChat.Server.Test\SkillChat.Server.Test.csproj
dotnet test --project SkillChat.Client.ViewModel.Test\SkillChat.Client.ViewModel.Test.csproj
dotnet test --project tests\SkillChat.UiTests.Headless\SkillChat.UiTests.Headless.csproj --no-build
```

## 11. Риски
- Авто-догрузка boundary может потребовать нескольких последовательных запросов на initial open длинного чата.
  Смягчение: ограничить только первым открытием, не применять к обычной пагинации.
- `HasMoreBefore` добавляет ещё одно состояние страницы.
  Смягчение: поле простое и обратно совместимое для старых клиентов.
- Валидация `MarkChatRead` через серверный query добавляет одну дешёвую выборку на write-path.
  Смягчение: write-path редкий и безопаснее raw timestamp.

## 12. Результат
После выполнения этой спеки unread-divider перестаёт быть “лучшей попыткой” и становится корректной проекцией реальной границы unread в чате, а write/read state синхронизируется безопасно и предсказуемо.

## Approval
Ожидается фраза: `Спеку подтверждаю`

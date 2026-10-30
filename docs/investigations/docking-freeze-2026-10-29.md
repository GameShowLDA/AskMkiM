# Зависание 29 октября 2026: плавающее окно и аварийная диагностика

Исследовано 30 октября 2026. Ветка: `codex/investigate-docking-freeze`.
Production-код при расследовании не изменён. Добавлен изолированный
[стенд воспроизведения](../../tools/DockingFreezeRepro/README.md).

## Вывод

Воспроизведены два дефекта, образующие правдоподобную цепочку вчерашнего инцидента:

1. После закрытия родительского DockItem с настоящим RunControl его дочернее
   плавающее окно «Итоговый протокол» остаётся видимым. Связанный DockControl уже
   отсоединён от PresentationSource. BeginDrag выбрасывает InvalidOperationException
   в той же точке и с той же нижней цепочкой вызовов, что в архиве.
2. Синхронное ожидание CrashPackageService.CreateAsync в Dispatcher может вызвать
   взаимную блокировку уже на первом сборщике ExceptionCollector. Продолжение
   JsonFileWriter.WriteAsync поставлено в очередь того же Dispatcher, который ждёт
   завершения CreateAsync. Отмена токена не освобождает эту очередь.

Оба механизма доказаны запуском настоящего production-кода на стенде.
Точное состояние потоков процесса 29 октября неизвестно: дампа зависшего процесса
в архиве нет. Поэтому нельзя доказать, что вчера ожидание остановилось именно
на том же продолжении JsonFileWriter, а не на другом этапе диагностики.
Архив не содержит отдельного подтверждения завершения через диспетчер задач.

## Источники и соответствие сборке

Исходный архив: `AskMkiM_2026-10-29.zip`.

- `Logs/all.log`, `Logs/input.log`, `Logs/ui.log`, `Logs/device.log`.
- `CrashReports/20261029_165238_InvalidOperationException/crash.json`.
- Предыдущие полные crash packages с ArgumentException.
- Идентификатор исполнявшейся сборки: `1.1.0+20261026.054317.52d8f866`.
- Commit: `52d8f866404e1005e682de73c1e7b5b9c50c9fc0`, Dirty=False.

На момент исследования `git diff 52d8f866 --` для следующих файлов пуст:

- `MainWindow/App.xaml.cs`;
- `Ask.Diagnostics/Infrastructure/JsonFileWriter.cs`;
- `Ask.Diagnostics/Collectors/ExceptionCollector.cs`;
- `Ask.Diagnostics/Collectors/ScreenshotCollector.cs`;
- `UI/Windows/WpfDocking/Windows/Docking/Primitives/DockManager.WindowHandler.cs`;
- `UI/Windows/WpfDocking/Windows/Docking/Primitives/NativeFloatingWindow.cs`.

Стенд собирается с текущим рабочим деревом. Перечисленные критичные участки
совпадают с исходниками аварийной сборки.

## Хронология из архива

Время 29 октября, локальная зона журнала UTC+04:00.

| Время | Подтверждённое событие |
| --- | --- |
| 16:48:56.7561 | Регистрация глобальных обработчиков в очередном запуске |
| 16:51:54.3658 | ActionExecutor.FinalizeAsync для `211-1.asktrace` |
| 16:51:59.7083 | RunControl.ShowInspectionProtocol показывает «Итоговый протокол» |
| 16:52:00.2441 | Панель выполнения переходит в Ready |
| 16:52:28.9216 | DockItemService.ConfigureDockItemClosing: закрытие `211-1.asktrace` |
| 16:52:28.9470 | Создаётся контейнер текстового редактора и вкладка `211-1.pkw` |
| 16:52:29.0065 | DockItemService.FinalizeDockItemClose: закрытие контейнера «Исполнитель» |
| 16:52:38.6578 | GLOBAL UI EXCEPTION |
| 16:52:38.6853 | InvalidOperationException: This Visual is not connected to a PresentationSource |
| 16:52:38.687845 | Timestamp в частичном crash.json, источник DispatcherUnhandledException |
| 16:53:09.7205 | Crash collector failed: Exception: A task was canceled; стек ExceptionCollector.CollectAsync, строка 32 |
| 16:53:17.4310 | Новая регистрация глобальных обработчиков — новый запуск |

Перед новым запуском нет записи штатного App.OnExit. Последний crash package содержит
только crash.json: нет stacktrace.txt, screenshot, metadata и итогового ZIP.
Это подтверждает незавершённый сбор диагностики, но не указывает само по себе
на конкретный вид блокировки.

## Механизм 1: окно переживает контейнер

Ключевые файлы:

- [RunControl.xaml.cs](../../UI/Controls/Runner/RunControl.xaml.cs): вложенный
  ChildTextEditorContainer; ShowInspectionProtocol создаёт DockItem протокола.
- [DockItemService.cs](../../UI/Services/DockItemService.cs): Closing → отложенный
  FinalizeDockItemClose → ContainerService.RemoveEditorContainer.
- [ControlManager.cs](../../UI/Components/MultiEditorMethods/ControlManager.cs):
  CloseDockItem/RemoveTabAndControl; удаление родительского контрола из ContentPanel.
- [DockManager.WindowHandler.cs](../../UI/Windows/WpfDocking/Windows/Docking/Primitives/DockManager.WindowHandler.cs):
  OnFloatingWindowsChanged создаёт/закрывает NativeFloatingWindow по изменениям
  коллекции FloatingWindows. Отсоединение DockControl не удаляет эту коллекцию.
- [NativeFloatingWindow.cs](../../UI/Windows/WpfDocking/Windows/Docking/Primitives/NativeFloatingWindow.cs):
  RefreshVisibility смотрит на FloatingWindow.IsVisible, а не на подключение
  FloatingWindow.DockControl к PresentationSource.

Воспроизведённая цепочка:

```text
Родительский DockItem.Content = настоящий RunControl
→ DockItem.Show(parentDock, Document)
→ RunControl.ShowInspectionProtocol
→ дочерний DockItem.Show(childDock, Document)
→ дочерний DockItem.Show(childDock, Rect) — перенос в плавающее окно
→ WindowHandler.OnFloatingWindowsChanged(Add)
→ new NativeFloatingWindow(FloatingWindow)
→ закрытие родительского DockItem.Close
→ RunControl и childDock удаляются из visual tree
→ childDock.IsLoaded=False, PresentationSource.FromVisual(childDock)=null
→ NativeFloatingWindow остаётся видимым, childDock.DockItems.Count=1
→ DockManager.BeginDrag(source=floatingWindow, element=floatingWindow, MouseEventArgs)
→ DragHandler.BeginDrag
→ WindowHandler.BeginDrag
→ WindowHandler.BeginDragNative
→ childDock.PointToScreen(e.GetPosition(childDock))
→ InvalidOperationException
```

В реальном обработчике мыши вход идёт через
NativeFloatingWindow.OnMouseLeftButtonDown → DockManager.BeginDrag.
Стенд вызывает этот internal-вход через reflection, без глобального ввода мыши.
Он не доказывает, какая именно дочерняя вкладка была вынесена вчера: архив фиксирует
тип окна и стек, но не его заголовок. Вынесенный итоговый протокол — воспроизводимый
сценарий, согласованный с предшествующими записями.

Контроль: Collapsed сохраняет подключение DockControl к PresentationSource;
такой же BeginDrag возвращается без исключения. Следовательно, простого переключения
видимости недостаточно для воспроизведения найденной ошибки.

## Механизм 2: sync-over-async в обработчике аварии

Production-цепочка:

```text
App.RegisterGlobalExceptionHandlers
→ DispatcherUnhandledException
→ App.CreateCrashPackage
→ CancellationTokenSource(30 секунд)
→ service.CreateAsync(...).GetAwaiter().GetResult() на UI thread
→ CrashPackageService.CreateAsync
→ _gate.WaitAsync (при свободном gate завершается синхронно)
→ CreateCoreAsync
→ первый collector: ExceptionCollector.CollectAsync, Order=100
→ JsonFileWriter.WriteAsync(crash.json)
→ await JsonSerializer.SerializeAsync(...).ConfigureAwait(false)
→ неявный await stream.DisposeAsync() из await using
```

В JsonFileWriter `await using var stream` не задаёт ConfigureAwait(false) для
асинхронного освобождения. Если сериализация завершается синхронно, выполнение
доходит до освобождения ещё в UI-контексте. Если DisposeAsync остаётся незавершённым,
продолжение захватывает этот контекст. ConfigureAwait(false) у других await
не распространяется на это неявное ожидание.

На стенде зафиксировано:

```text
collector Exception: start
UI continuation queued:
  AsyncStateMachineBox<..., Ask.Diagnostics.Infrastructure.JsonFileWriter.<WriteAsync>...>
UI enters GetAwaiter().GetResult()
probe: heartbeat=0; done=False
WATCHDOG: diagnostic wait did not return
```

Получается цикл:

```text
UI thread ждёт CreateAsync
→ CreateAsync ждёт ExceptionCollector
→ ExceptionCollector ждёт JsonFileWriter
→ JsonFileWriter ждёт исполнения продолжения в UI thread
```

На стенде отмена через 3 секунды не разрывает цикл. Через примерно 7,25 секунды
внешняя по отношению к Dispatcher задача-watchdog завершает только процесс стенда.
До этого UI heartbeat остаётся в очереди.

### Почему одного Task.Run недостаточно

Контроль `blocking-worker` запускает CreateAsync через Task.Run, но сохраняет
GetResult в UI thread:

```text
ExceptionCollector завершается в worker thread
→ ScreenshotCollector.CollectAsync
→ dispatcher.InvokeAsync(SaveApplicationWindowScreenshot)
→ operation.Task.WaitAsync(cancellationToken)
→ UI thread всё ещё занят GetResult
```

На стенде heartbeat=0; после трёх секунд ScreenshotCollector получает TaskCanceledException,
общий сбор отменяется без ZIP, UI освобождается. Такой перенос оставляет блокировку
на время таймаута и ломает полноту отчёта.

Контроль `async` выполняет тот же CreateAsync через await: собираются все collectors,
создаётся ZIP, UI heartbeat исполняется. Это проверка диагностического сервиса;
она не заменяет решение о политике обработки необработанных исключений в App.

## Проверки

Команда воспроизводимого набора:

```powershell
pwsh -File tools/DockingFreezeRepro/Verify.ps1
```

| Сценарий | Результат запуска 30 октября |
| --- | --- |
| detach | PASS: отсоединённый DockControl, живое плавающее окно, нужный стек InvalidOperationException |
| collapse | PASS: PresentationSource сохранён, исключения нет |
| close-runner | PASS: настоящий RunControl с протоколом повторяет тот же дефект после Close родителя |
| blocking | PASS: подтверждено захваченное продолжение JsonFileWriter, зависание пережило отмену, watchdog exit=2 |
| blocking-worker | PASS: зависание до таймаута на ScreenshotCollector, ZIP не создаётся |
| async | PASS: ZIP создан, UI heartbeat выполнен |

PASS означает совпадение с ожидаемым дефектным поведением или контрольным сценарием,
а не исправление production-кода. Стенд собран с 0 compile errors. Имеются прежние
предупреждения зависимостей NU1701; их исправление в расследование не входит.
Существующий `Ask.Diagnostics.UnitTests` также проверен: 15 тестов пройдено,
0 ошибок, 0 пропусков.

## Направление исправления

Изменения поведения в этой ветке пока не внесены. При исправлении нужны оба направления:

1. Увязать жизненный цикл дочерних плавающих окон с закрытием RunControl/родительской
   вкладки. Проверять повторную загрузку, обычное скрытие, отмену закрытия и перенос
   вкладок. Один guard перед PointToScreen предотвратит исключение, но оставит окно
   без хозяина и не устранит причину нарушения lifetime.
2. Убрать блокирующее ожидание UI-зависимой диагностики из Dispatcher. Отдельно
   устранить захват контекста в асинхронном освобождении JsonFileWriter.
   Не считать CancellationTokenSource общим жёстким ограничением времени ожидания.
   Сохранить осознанную политику завершения после необработанного исключения:
   сейчас обработчик App не устанавливает e.Handled, и простой перевод в async void
   не гарантирует завершения сбора до аварийного выхода.

После исправления требуются regression-проверки отсутствия осиротевшего окна,
повторного открытия исполнителя и полноценного crash package при ошибке UI.

## Ограничения

- Десктопное приложение целиком с авторизацией и аппаратурой не запускалось.
- NativeFloatingWindow и RunControl настоящие; событие начала drag вызывается
  программно. Физические движения мыши из вчерашнего сеанса не восстановлены.
- Диагностическая блокировка воспроизведена отдельно от глобального App-handler,
  с той же синхронной формой ожидания. Обработчик App сопоставлен по исходникам.
- Timeout в стенде сокращён с production 30 секунд до 3 секунд, чтобы повторные
  запуски не требовали принудительного закрытия пользователем.
- Расположение незавершённого ожидания вчера нельзя установить окончательно
  без дампа потоков; начальный UI-стек и наличие дефектов подтверждены независимо.

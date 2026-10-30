# Воспроизведение зависания docking / crash diagnostics

Изолированный WPF-стенд расследования [инцидента 29 октября 2026](../../docs/investigations/docking-freeze-2026-10-29.md).
Ссылается на настоящие `UI` и `Ask.Diagnostics`, не входит в основной solution.
Не запускает приложение, авторизацию, команды оборудования или инициализацию БД.
Каждый сценарий работает в собственном процессе; закрывает только собственные окна.
Тестовые окна не активируются; диагностические файлы пишутся в уникальные каталоги TEMP.

## Запуск

Из корня репозитория, Windows и .NET SDK 8:

```powershell
pwsh -File tools/DockingFreezeRepro/Verify.ps1
```

После сборки отдельный сценарий:

```powershell
dotnet Bin/DockingFreezeRepro/DockingFreezeRepro.dll close-runner
```

`Verify.ps1 -SkipBuild` использует уже собранный стенд. Сборка следует общему
`Directory.Build.props`: результат находится в `Bin/DockingFreezeRepro/`.

## Сценарии и ожидаемые результаты текущего кода

| Режим | Проверка | Ожидаемый результат |
| --- | --- | --- |
| `detach` | Создать плавающее окно и удалить его DockControl из visual tree | Окно остаётся видимым; BeginDrag выбрасывает InvalidOperationException в BeginDragNative |
| `collapse` | Только скрыть DockControl через Collapsed | PresentationSource сохраняется; BeginDrag не выбрасывает исключение |
| `close-runner` | Настоящий RunControl.ShowInspectionProtocol → вынести протокол в плавающее окно → закрыть родительский DockItem | Дочернее окно переживает RunControl; BeginDrag выбрасывает то же исключение |
| `blocking` | CreateAsync(...).GetAwaiter().GetResult() в Dispatcher, как в App.CreateCrashPackage | Захвачено продолжение JsonFileWriter; heartbeat не исполняется; watchdog завершает стенд с кодом 2 |
| `blocking-worker` | Task.Run(CreateAsync) с сохранением GetResult в Dispatcher | ScreenshotCollector ждёт Dispatcher до отмены; интерфейс заблокирован примерно 3 секунды; архив не создаётся |
| `async` | Асинхронный await того же CreateAsync | Создаётся ZIP; heartbeat исполняется |

`blocking` намеренно воспроизводит взаимную блокировку: токен отменяется через 3 секунды,
watchdog завершает **только процесс стенда** примерно через 7,25 секунды. Код 2 здесь ожидаем.
Это проверки наличия известных дефектов, а не утверждение корректности production-кода.
После исправления дефектов ожидания стенда необходимо пересмотреть.

Для входа в internal DockManager.BeginDrag используется reflection: это позволяет повторить
фактический путь без SendInput и без перемещения пользовательской мыши. Начальное состояние,
выживание плавающего окна, подключение контейнера и стек исключения проверяются явно.
SynchronizationContext-обёртка только записывает тип поставленного в очередь продолжения,
затем делегирует выполнение настоящему DispatcherSynchronizationContext.

Сборщики диагностических данных обёрнуты для трассировки начала/конца; их реализация не меняется.
Диагностические режимы не открывают видимое окно: ScreenshotCollector всё равно должен обратиться
к Dispatcher, чтобы проверить Application.Windows, затем сохраняет screenshot-info.txt.

Полные журналы проверки находятся в каталоге, который печатает Verify.ps1.
Каждый процесс дополнительно печатает собственный каталог `AskMkiM.DockingFreezeRepro/<id>` в TEMP.

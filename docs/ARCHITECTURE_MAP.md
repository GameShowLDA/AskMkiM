# AskMkiM Architecture Map

> This file is a navigation and architecture index, not the source of truth.
> Start here before broad repository exploration.
> For targeted tasks, use this map to identify the relevant code and inspect that code directly.
> If this map conflicts with the current source code, the source code wins and this map must be updated.

Карта описывает текущее состояние production-кода. Целевая архитектура и план
миграции находятся отдельно в `docs/architecture/clean-architecture.md` и не должны
смешиваться с фактическими runtime-связями, зафиксированными здесь.

## Quick Navigation

| Нужно изменить | Сначала смотреть | Затем смотреть |
| --- | --- | --- |
| Запуск приложения | `MainWindow/App.xaml.cs`, `MainWindow/Init/PreStartupInitializer.cs` | `MainWindow/Init/DatabaseInitializer.cs`, `MainWindow/Engine/AppServices.cs`, `MainWindow/MainWindow.xaml.cs` |
| DI и composition root | `MainWindow/Init/PreStartupInitializer.cs` | `Ask.Diagnostics/Extensions/ServiceCollectionExtensions.cs`, `Ask.Core/Services/App/ServiceLocator.cs`, `MainWindow/Engine/AppServices.cs` |
| Трансляция программы контроля | `MainWindow/Services/TranslationServices.cs` | `Ask.Engine/ControlCommandAnalyser/CommandTranslationManager.cs`, `Ask.Engine/ControlCommandAnalyser/Parser/`, `Ask.Engine/ControlCommandAnalyser/Formatter/`, `Ask.Engine/ControlCommandAnalyser/Validation/` |
| Исполнение программы контроля | `UI/Controls/Runner/RunControl.xaml.cs` | `Ask.UI/Features/ProtocolNew/Execution/ActionExecutor.cs`, `Ask.Engine/ControlCommandExecutor/Execution/CommandExecutionManager.cs` |
| Алгоритм конкретной команды | `Ask.Engine/ControlCommandExecutor/Executors/` | `Ask.Engine/ControlCommandExecutor/BaseStrategies/`, `Ask.Engine/ControlCommandExecutor/Execution/EquipmentService.cs` |
| Пауза, шаг, остановка, переход к команде | `Ask.UI/Features/ProtocolNew/Execution/ActionExecutor.cs` | `Ask.Core/Services/App/StepControlManager.cs`, `Ask.Engine/ControlCommandExecutor/Execution/CommandExecutionManager.cs`, `Ask.Engine/ControlCommandExecutor/Execution/BreakpointHandler.cs`, `Ask.Engine/ControlCommandExecutor/Execution/CommandJumpService.cs` |
| Холостой режим и симуляция сбоев | `Ask.Core/Services/Config/AppSettings/ExecutionConfig.cs`, `IdleHardwareErrorSimulator.cs`, `IDevice.IsHardwareFailureSimulationEnabled` | `UI/Controls/Settings/Execution/ExecutionControl.xaml`, DTO шести типов оборудования, целевой manager/adapter в `Ask.Device.*` |
| Ошибка оборудования и интерактивный повтор | `Ask.Core/Services/UI/UserActionHelper.cs` | `Ask.Core/Services/UI/EquipmentExecutionContext.cs`, `Ask.UI/Controls/ProtocolNew/ProtocolUI.cs`, целевой adapter/manager/transport |
| МКР и точки | `Ask.Core/Shared/Interfaces/DeviceInterfaces/RelaySwitchModule/` | `Ask.Device.Application/FunctionAdapters/ModuleRelayControl/`, `Ask.Device.Runtime/Function/ModuleRelayControl/`, `Ask.Device.Emulator/ModuleRelayControl/` |
| Устройство коммутации | `Ask.Core/Shared/Interfaces/DeviceInterfaces/SwitchingDevice/` | `Ask.Device.Application/FunctionAdapters/DeviceBusCommutation/`, `Ask.Device.Runtime/Function/DeviceBusCommutation/` |
| Быстрый мультиметр | `Ask.Core/Shared/Interfaces/DeviceInterfaces/Multimeter/` | `Ask.Device.Runtime/Device/KeysightDevice.cs`, `Ask.Device.Runtime/Device/MultimeterB7783.cs`, `Ask.Device.Runtime/Function/Base/Multimeter/` |
| Пробойная установка GPT | `Ask.Core/Shared/Interfaces/DeviceInterfaces/BreakdownTester/` | `Ask.Device.Application/FunctionAdapters/GPT/`, `Ask.Device.Runtime/Function/GPT/`, `Ask.Device.Runtime/Device/GPT79904.cs` |
| Источник напряжения/тока | `Ask.Core/Shared/Interfaces/DeviceInterfaces/PowerSourceModule/` | `Ask.Device.Application/FunctionAdapters/ModuleVoltageCurrent/`, `Ask.Device.Runtime/Function/ModuleVoltageCurrentSource/` |
| Шасси и питание | `Ask.Core/Shared/Interfaces/DeviceInterfaces/Chassis/` | `Ask.Device.Runtime/Device/ManagerChassis.cs`, `Ask.Device.Runtime/Function/ManagerChassis/`, `Ask.Device.Emulator/Chassis/`, `UI/Components/PowerButton.xaml.cs` |
| UPS | `Ask.Core/Shared/Interfaces/DeviceInterfaces/UninterruptiblePowerSupply/` | `Ask.Device.Application/FunctionAdapters/MikUps1101rRm/`, `Ask.Device.Runtime/Function/MikUps1101rRm/` |
| COM | `Ask.Device.Runtime/Base/Device/DeviceWithCOM.cs` | `Ask.Device.Communication/Com/Protocols/ComProtocol.cs`, `Ask.Device.Communication/Com/Configuration/SerialPortCustom.cs` |
| TCP/UDP/USB | `Ask.Device.Runtime/Base/Device/` | `Ask.Device.Communication/Ethernet/`, `Ask.Device.Communication/Usb/`, runtime `Ask.Device.Runtime/Function/Base/Connected/` |
| Конфигурация устройств | `UI/Controls/Settings/DeviceConfig/` | `Ask.DataBase.Engine/Static/Devices/`, `Ask.DataBase.Engine/Services/DeviceEngine.cs`, `Ask.DataBase.Provider/Services/Devices/` |
| База данных | `Ask.DataBase.Provider/Context/AppDbContext*.cs` | `Ask.DataBase.Provider/Initialization/DatabaseInitializationService.cs`, `Ask.DataBase.Engine/Services/DeviceEngine.cs` |
| Настройки выполнения/протокола/UI | `Ask.Core/Services/Config/` | `Ask.DataBase.Engine/Static/Settings/`, `Ask.DataBase.Provider/Services/Settings/`, `MainWindow/Init/DatabaseInitializer.cs` |
| Протокол выполнения | `Ask.UI/Controls/ProtocolNew/ProtocolUI*.cs` | `Ask.UI/Features/ProtocolNew/Protocol/`, `Ask.Core/Services/Protocols/ExecutionProtocolHistoryService.cs` |
| Формирование унифицированных сообщений протокола | `Ask.Protocol.Messages/EntryPoints/` | `Ask.Protocol.Messages/Builders/`, `Ask.Protocol.Messages/Show/`; сообщения executor-команд, блоков проверки, оборудования, измерений, допустимых диапазонов и ошибок UI-валидации формируются централизованно |
| Форматы `.asktrace/.askresult/.askreport` | `Ask.Core/Services/Protocols/ExecutionProtocolHistoryService.cs` | `Ask.Core/Shared/Metadata/Static/ProtocolFileExtensions.cs`, `Ask.UI/Features/ProtocolNew/Protocol/ProtocolStorageService.cs` |
| Печать протокола | `Ask.UI/Features/ProtocolNew/Protocol/ProtocolCompletionService.cs` | `Ask.UI/Features/ProtocolNew/Execution/ExecutionFinalizer.cs`, `Ask.Core/Services/Config/AppSettings/ProtocolConfig.cs`, `PrintUtility` usages |
| Метрология | `MainWindow/Services/MetrologyService.cs` | `Ask.Core/Services/Metrology/MetrologyControlFactory.cs`, `Ask.UI/Controls/ExecutorControls/MetrologyControls/`, `Ask.Engine/Tests/Metrology/` |
| Самоконтроль и инженерные тесты | `MainWindow/Services/TestService.cs`, `MainWindow/Services/SelfTestServices.cs` | `Ask.Device.Runtime/Function/*/SelfCheck/`, `Ask.Protocol.Messages/EntryPoints/SelfTestMessages.cs`, `Ask.UI/Controls/ExecutorControls/TestsControls/`, `Ask.Engine/Tests/` |
| Ошибки трансляции | `Ask.Core/Services/Errors/Translation/` | целевой parser/validator, `Ask.UI/Controls/ErrorList/`, `UI/Controls/ErrorList/` |
| Crash reports | `MainWindow/App.xaml.cs`, `MainWindow/Init/PreStartupInitializer.cs`, `MainWindow/Services/TranslationServices.cs` | `Ask.Diagnostics/Services/CrashPackageService.cs`, `Ask.Diagnostics/Services/ExceptionDiagnosticReporter.cs`, `Ask.Diagnostics/Collectors/` |
| Архивы APK/APKW | `Ask.UI/Features/Archive/` | `Ask.Core/Services/FileFormats/Apk/`, `MainWindow/Services/Conversion/` |
| Рабочее пространство и вкладки | `UI/Components/MultiEditorControl.xaml.cs` | `UI/Components/MultiEditorMethods/FileManager.cs`, `UI/Services/`, `MainWindow/Services/MultiWindowService.cs` |
| Роли и права | `MainWindow/Init/RoleApplicationConfigurator.cs` | `Ask.Core/Services/Config/AppSettings/RoleAuthorizationConfig.cs`, `Ask.UI/Features/RoleManagement/` |
| Административные и сервисные утилиты | `MainWindow/MainWindow.xaml`, `MainWindow/ViewModels/AdminViewModel.cs`, `MainWindow/Services/AdminServices.cs` | `UI/Controls/AdminPanel/ServiceUtilitiesControl.xaml`, `UI/Controls/AdminPanel/SetCommand.xaml`, `Ask.UI/Features/ServiceTools/{Gpt,Chassis,SwitchingDevice}/`, `UI/Controls/AdminPanel/DataBaseView.xaml`, `UI/Controls/AdminPanel/CheckResistanceControl.xaml` |
| Debug-доступ текущего пользователя | `Ask.Core/Services/Config/AppSettings/DebugAccessConfig.cs` | `RoleAuthorizationConfig.cs`, `SystemStateEvents.DebugRightsChanged`, оба `ErrorListControl.xaml.cs`, `ProtocolEntryOutputService.cs` |
| События между подсистемами | `Ask.Core/Services/EventCore/Services/EventAggregator.cs` | `Ask.Core/Services/EventCore/Adapters/`, `Ask.Core/Services/EventCore/Events/`, `MainWindow/Events/` |
| Встроенная справка | `Ask.Support/HelpServer.cs` | `Ask.Support/HelpProvider.cs`, `Ask.Support/HelpViewerWindow.cs`, `Ask.Support/AppHelp/` |

## Runtime Flow Index

- [Application startup](#application-startup-flow)
- [Database initialization](#database-startup-flow)
- [Translation](#translation-flow)
- [Control-program execution](#control-program-execution-flow)
- [Command dispatch and emergency completion](#command-dispatch-flow)
- [Equipment resolution](#equipment-resolution-flow)
- [Device materialization from SQLite](#device-materialization-flow)
- [Real equipment command](#real-equipment-command-flow)
- [Idle execution](#real-idle)
- [Metrology](#metrology-flow)
- [Protocol generation, save and print](#protocol-flow)
- [Error propagation and retry](#equipment-error-flow)
- [Crash diagnostics](#crash-diagnostics-flow)
- [Authentication and Debug access](#authentication-and-debug-access-flow)

## Solution Structure

Все production-проекты используют `net8.0-windows`. `MainWindowProgram` — единственный
основной `WinExe`; остальные перечисленные проекты — библиотеки. Тестовые проекты,
ручные harness-приложения и `MethodDependencyExplorer` в runtime-карту не входят.

| Проект | Путь | Назначение и основные namespaces | Прямые project references |
| --- | --- | --- | --- |
| `MainWindowProgram` | `MainWindow/MainWindowProgram.csproj` | WPF entry point, shell, startup, ручная композиция UI; `MainWindowProgram.*` | `Ask.Diagnostics`, `Ask.DataBase.Engine`, `Ask.Support`, `Ask.UI`, `ConsoleUI`, `Message`, `UI` |
| `UI` | `UI/UI.csproj` | Legacy WPF workspace, editor, runner, settings, protocol/file services; `UI.*` | `Ask.Core`, `Ask.DataBase.Provider`, `Ask.Engine`, `Ask.Support`, `Ask.UI`, `Message`, `Ask.Device.Runtime` |
| `Ask.UI` | `Ask.UI/Ask.UI.csproj` | Новые reusable WPF features: protocol, archive, notifications, role UI, executor controls, сервисное управление GPT; `Ask.UI.*` | `Ask.Core`, `Ask.Engine`, `Ask.Support`, `Message`, `Ask.Device.Runtime`, `Ask.LogLib` |
| `Ask.Engine` | `Ask.Engine/Ask.Engine.csproj` | Parser/formatter, command execution, strategies, metrology and hardware-test algorithms; `Ask.Engine.*` | `Ask.Core`, `Ask.DataBase.Engine`, `Ask.LogLib`, `Ask.Protocol.Messages`, `Message` |
| `Ask.Core` | `Ask.Core/Ask.Core.csproj` | Shared contracts, DTO, enums, events, config state, errors, file formats; `Ask.Core.*` | `Ask.LogLib` |
| `Ask.Protocol.Messages` | `Ask.Protocol.Messages/Ask.Protocol.Messages.csproj` | Формирование, device-логирование и вывод унифицированных `ShowMessageModel`; содержит фасады и builders для executor-команд, блоков проверки, оборудования и измерений | `Ask.Core`, `Ask.LogLib`; потребители — `Ask.Device.Runtime`, `Ask.Engine` |
| `Ask.Device.Application` | `Ask.Device.Application/Ask.Device.Application.csproj` | Application adapters/decorators over raw device managers, retry and user-facing error conversion; `Ask.Device.Application.*` | `Ask.Core`, `Ask.LogLib`, `Ask.Device.Runtime` |
| `Ask.Device.Runtime` | `Ask.Device.Runtime/Ask.Device.Runtime.csproj` | Concrete devices, low-level managers, device command generation and transports; `Ask.Device.Runtime.*` | `Ask.Core`, `Ask.Device.Communication`, `Ask.Device.Emulator`, `Ask.Protocol.Messages` |
| `Ask.Device.Emulator` | `Ask.Device.Emulator/Ask.Device.Emulator.csproj` | Stateful raw-protocol emulation for chassis and МКР in Idle mode and Real/Idle protocol selection; `Ask.Device.Emulator.*` | `Ask.Core` |
| `Ask.Device.Communication` | `Ask.Device.Communication/Ask.Device.Communication.csproj` | COM/TCP/UDP/USB protocol implementations; `Ask.Device.Communication.*` | `Ask.Core`, `Ask.Diagnostics`, `Ask.LogLib` |
| `Ask.Device.ResponseProcessor` | `Ask.Device.ResponseProcessor/Ask.Device.ResponseProcessor.csproj` | Пустая библиотека, зарезервированная для обработки уже полученных ответов устройств; production-код и зависимости пока отсутствуют | нет |
| `Ask.DataBase.Engine` | `Ask.DataBase.Engine/Ask.DataBase.Engine.csproj` | Runtime device facade, cache, reflection factory, DTO↔device mapping; `Ask.DataBase.Engine.*` | `Ask.Core`, `Ask.Device.Application`, `Ask.DataBase.Provider` |
| `Ask.DataBase.Provider` | `Ask.DataBase.Provider/Ask.DataBase.Provider.csproj` | EF Core/SQLite context, migrations and CRUD services; `Ask.DataBase.Provider.*` | `Ask.Core`, `Ask.LogLib` |
| `Ask.Diagnostics` | `Ask.Diagnostics/Ask.Diagnostics.csproj` | Crash packages, command history, diagnostic collectors; `Ask.Diagnostics.*` | нет |
| `Ask.Support` | `Ask.Support/Ask.Support.csproj` | Local Kestrel help server, Photino help window, WPF help routing; `Ask.Support` | `Ask.LogLib` |
| `ConsoleUI` | `ConsoleUI/ConsoleUI.csproj` | Встроенная сервисная консоль и команды; `ConsoleUI.*` | `Ask.DataBase.Engine` |
| `Message` | `Message/Message.csproj` | Кастомные WPF message boxes; `Message` | нет |
| `Ask.LogLib` | `Ask.LogLib/Ask.LogLib.csproj` | NLog facade, exception bridge and live application-log event; `Ask.LogLib` | нет |

Архитектурно значимые внешние зависимости:

- WPF, AvalonEdit, MaterialDesign/ModernWPF и AvalonDock — desktop UI/editor;
- EF Core + SQLite — локальная конфигурационная БД;
- `Microsoft.Extensions.Hosting`/DI — host в startup и help server;
- `System.IO.Ports` — COM;
- National Instruments VISA — runtime приборов;
- Photino.NET + Kestrel — встроенная справка;
- NLog — логи;
- YamlDotNet — YAML-related core services.

Фактическое дерево compile-time зависимостей:

```text
MainWindowProgram
├─ UI
│  ├─ Ask.Engine
│  │  ├─ Ask.DataBase.Engine
│  │  │  ├─ Ask.DataBase.Provider ── Ask.Core ── Ask.LogLib
│  │  │  └─ Ask.Device.Application
│  │  │     └─ Ask.Device.Runtime
│  │  │        ├─ Ask.Device.Communication
│  │  │        │  ├─ Ask.Diagnostics
│  │  │        │  └─ Ask.LogLib
│  │  │        └─ Ask.Device.Emulator ── Ask.Core
│  │  ├─ Ask.Core
│  │  ├─ Ask.LogLib
│  │  ├─ Ask.Protocol.Messages
│  │  └─ Message
│  ├─ Ask.UI
│  ├─ Ask.Support
│  ├─ Ask.DataBase.Provider
│  ├─ Ask.Device.Runtime
│  └─ Message
├─ Ask.DataBase.Engine
├─ Ask.Diagnostics
├─ Ask.Support
├─ Ask.UI
├─ ConsoleUI
└─ Message

Ask.Protocol.Messages
├─ Ask.Core ── Ask.LogLib
└─ Ask.LogLib

Ask.Device.ResponseProcessor (project references отсутствуют)
```

`Ask.Protocol.Messages` добавлен в solution как отдельная production-библиотека.
`EntryPoints/EquipmentMessages` является публичным фасадом, внутренний
`Builders/EquipmentMessageBuilder` формирует результаты операций, а
`Show/EquipmentMessagePublisher` задаёт политику device log и делегирует вывод общему
`Show/MessagePublisher`. Фасад сохраняет метаданные исходного места вызова, а общий publisher передаёт в экранный протокол
собственный источник и исходный метод в формате `PublishAsync (вызван из File.cs → Method, строка N)`.
`Ask.Device.Runtime.Function.Base.Connected.Transport` уже использует новый фасад для подключения,
отключения, инициализации и сброса. `ExecutorMessageBuilder` удалён: его методы распределены между
`CommandMessages`, `ExecutionMessages`, `EquipmentMessages`, `MeasurementMessages` и `SelfTestMessages`. Допустимые диапазоны
значений независимо от вызывающей подсистемы публикуются через `RangeMessages`. Остальные runtime-потоки
пока продолжают использовать `DeviceMessageBuilder` и локальное создание `ShowMessageModel`.

Все реализации в `Ask.Device.Runtime/Function/*/SelfCheck/` публикуют экранные сообщения через
`SelfTestMessages → SelfTestMessageBuilder → SelfTestMessagePublisher → MessagePublisher → IMessageOutputService`.
В самих SelfCheck-классах остаются управление оборудованием, вычисление результата и регистрация аппаратных ошибок;
`ShowMessageModel` и прямые вызовы `ShowMessageAsync` в этом потоке отсутствуют.

`Directory.Build.props` направляет обычные результаты сборки в
`Bin/<MSBuildProjectName>/`; `MainWindow/MainWindowProgram.csproj` переопределяет output path
на `D:\AskMkiM\Bin\` и содержит publish/copy targets.

## Repository Structure

```text
AskMkiM/
├─ MainWindow/                 основной WPF exe и composition root
│  ├─ Init/                    startup, DB warm-up, roles, single-instance
│  ├─ Engine/                  shell composition and lifecycle
│  ├─ Services/                menu/use-case orchestration
│  ├─ ViewModels/              menu-level ViewModels
│  └─ Events/                  subscriptions between core events and shell
├─ UI/                         legacy WPF workspace/editor/settings
│  ├─ Components/              MultiEditorControl and workspace managers
│  ├─ Controls/                runner, settings, editor, device panels
│  └─ Services/                files, tabs, translation, protocol viewer
├─ Ask.UI/
│  ├─ Controls/ProtocolNew/    ProtocolUI facade/control
│  ├─ Controls/ExecutorControls/
│  ├─ Features/ProtocolNew/    execution lifecycle and protocol feature
│  ├─ Features/Archive/        APK/APKW archive workflows
│  ├─ Features/Notifications/
│  └─ Features/RoleManagement/
├─ Ask.Engine/
│  ├─ ControlCommandAnalyser/  parsing, formatting, post-analysis
│  ├─ ControlCommandExecutor/  execution manager, executors, strategies
│  └─ Tests/                   production metrology/self-control algorithms
├─ Ask.Core/
│  ├─ Shared/Interfaces/       device, UI and execution contracts
│  ├─ Shared/DTO/              persisted and cross-layer data
│  ├─ Services/Config/         runtime settings state
│  ├─ Services/EventCore/      in-process event bus
│  ├─ Services/Errors/         typed errors/warnings/factories
│  ├─ Services/FileFormats/    PK/OPK/APK/APKW and format helpers
│  └─ Services/Protocols/      history protocol persistence
├─ Ask.Protocol.Messages/      унифицированное формирование, логирование и отображение сообщений
│  ├─ EntryPoints/             публичные фасады групп сообщений
│  ├─ Builders/                внутреннее формирование `ShowMessageModel`
│  ├─ Models/                  контракты накопленных результатов и пределов измерений
│  ├─ Extensions/              интеграция результатов сообщений с `ProtocolModel`
│  └─ Show/                    категорийная политика и общий вывод в экранный протокол
├─ Ask.Device.ResponseProcessor/ пустая библиотека для будущей обработки полученных ответов устройств
├─ Ask.Device.Application/     adapters and application composition
├─ Ask.Device.Runtime/         device classes and raw function managers
├─ Ask.Device.Emulator/        stateful chassis and МКР protocol emulation for Idle mode
├─ Ask.Device.Communication/   wire protocols
├─ Ask.DataBase.Engine/        runtime device/data facade
├─ Ask.DataBase.Provider/      EF Core/SQLite provider
├─ Ask.Diagnostics/            crash package feature
├─ Ask.Support/                help server and packaged AppHelp
├─ ConsoleUI/, Message/, Ask.LogLib/
├─ docs/                       maintained documentation and this map
├─ Ask.Device.Emulator.UnitTests/ protocol-level tests for chassis, МКР, УКШ,
│                                multimeter, ППУ and Real/Idle routing
└─ Ask.*.UnitTests/            other automated tests, excluded from runtime map
```

`NewCore/`, `DataBaseConfigruration/`, `Ask.Diagnostics.Video/` have no active
`.csproj` in the solution. Они не считаются production-сборками. `TestConsole/`,
`TestWPF/`, `TestManyWindows/`, `TestArchive/` и `MethodDependencyExplorer/` —
test/manual/tooling projects.

## Entry Points

### Application startup flow

`MainWindow/App.xaml.cs: App` — WPF application class. Фактическая цепочка:

```text
App static constructor
→ Environment.GetCommandLineArgs()
→ SingleInstanceManager.CheckOrSignal(args)
  → mutex + NamedPipe client/server
  → второй процесс передаёт ACTIVATE/OPENFILE и завершается

App.OnStartup()
→ RegisterGlobalExceptionHandlers()
→ FileAssociationRegistrar.RegisterCurrentUserAssociations()
→ ApplicationClockService.Start()
→ Task.Run(PreStartupInitializer.Initialize)
→ RoleLoginWindowManager.Show/WaitForAuthenticationAsync
→ RoleApplicationConfigurator.Apply(role)
→ await startup initialization
→ InitializeTheme()
→ new MainWindow()
  → InitializeComponent()
  → AppServices.Build(this)
  → GuiInitializer.Apply(this)
→ MainWindow.InitializeAsync()
  → ApplicationLifecycleManager.Initialize()
  → CommandLineParser.ProcessCommandLineArgs()
  → ApplicationInitializer.SubscribeToMessageEvents()
  → HotkeyBinderManager.AttachAllHotkeys()
→ ApplicationActivator.FlushPendingFileRequests()
→ show main window
```

### Authentication and Debug access flow

Debug-доступ не является параметром запуска или независимо изменяемым состоянием.
Единственный источник истины — фактически авторизованная текущая роль:

```text
RoleLoginWindow authenticates RoleCredentialModel successfully
→ RoleApplicationConfigurator.Apply(role)
→ RoleAuthorizationConfig.SetCurrentRole(role.Role, role.DisplayName)
→ DebugAccessConfig.IsDebugEnabled
  → true only for RoleType.Root
  → false for every other role and for CurrentRole == null
→ DebugAccessConfig.NotifyCurrentRoleChanged(previousState)
→ SystemStateEventAdapter.RaiseDebugRightsChanged(newState), только если bool изменился
→ EventAggregator.Publish<SystemStateEvents.DebugRightsChanged>
→ уже открытые UI.Controls.ErrorList.ErrorListControl /
   Ask.UI.Controls.ErrorList.ErrorListControl обновляют видимость DEBUG-колонки
```

Смена пользователя без перезапуска проходит через
`MainWindow.SwitchCurrentUserAsync → RoleLoginWindowManager → успешная аутентификация
→ RoleApplicationConfigurator.Apply`. Неуспешная попытка не вызывает `Apply` и не
изменяет текущую роль или Debug-доступ. `RoleAuthorizationConfig.Clear()` при
отсутствии/завершении сессии пересчитывает доступ и публикует выключение после `root`.

Production consumers читают только `DebugAccessConfig.IsDebugEnabled`:

- `Ask.UI/Features/ProtocolNew/Protocol/ProtocolEntryOutputService.cs` добавляет
  source file/member/line в запись протокола;
- `UI/Controls/ErrorList/ErrorListControl.xaml.cs` и
  `Ask.UI/Controls/ErrorList/ErrorListControl.xaml.cs` задают начальную видимость
  DEBUG-колонки и реактивно обновляют её по событию.

`CommandLineParser` по-прежнему обрабатывает файловые пути и совместимый no-op
switch `admin`; аргумент `debug` не распознаётся и не влияет на доступ. Отдельной
консольной команды изменения Debug-состояния нет.

### Database startup flow

```text
PreStartupInitializer.Initialize()
→ MainWindow.Init.DatabaseInitializer.InitializeAsync()
→ DatabaseEngineInitializer.InitializeAsync()
→ DatabaseInitializationService.InitializeAsync()
  → DbPathResolver.Resolve()
  → SQLite integrity_check
  → migrate / adopt existing schema / EnsureCreated fallback
  → compatibility columns and legacy profile storage
  → default settings rows + hotkeys
→ WarmUpDeviceCachesAsync()
  → DeviceRuntime.ClearCache()
  → all eight static device facades GetAllAsync()
→ ProtocolSettings/ExecutionSettings/UserInterfaceSettings/DeviceDisplaySettings.GetAsync()
→ populate ProtocolConfig/ExecutionConfig/UserInterfaceConfig/DeviceDisplayConfig
→ subscribe config Save* events back to DB services
```

Затем `PreStartupInitializer` вызывает `InitializeAppHost()` и
`HelpServer.EnsureStarted()`. Startup БД намеренно ловит ошибку на уровне
`MainWindow.Init.DatabaseInitializer`, пишет лог и возвращает `null`; приложение
может продолжить запуск.

## Dependency Injection

В проекте гибридная композиция: часть graph создаётся `Microsoft.Extensions.DI`,
часть — вручную через `new`, статические facades и `ServiceLocator`.

### Host chain

```text
PreStartupInitializer.InitializeAppHost()
→ Host.CreateDefaultBuilder()
→ ConfigureServices(...)
→ Build()
→ ServiceLocator.Initialize(AppHost)
→ AppHost.StartAsync()
```

Ключевые регистрации:

| Interface/service | Implementation/factory | Lifetime | Registration |
| --- | --- | --- | --- |
| `Dispatcher` | `Application.Current.Dispatcher` factory | Singleton | `PreStartupInitializer.InitializeAppHost` |
| `MetrologyControlFactory` | self | Singleton | там же |
| `ApplicationAutoConfigurationService` | self | Singleton | там же |
| `IRelaySwitchModuleConfigurationValidator` | `RelaySwitchModuleConfigurationValidator` | Singleton | там же |
| `ModuleRelayControlWindow` | self | Transient | там же |
| `IArchivePermissionService` | `RoleArchivePermissionService` | Singleton | там же |
| `IArchiveIntegrityService` | `ArchiveIntegrityService` | Singleton | там же |
| `IArchiveOperationLogger` | `ArchiveOperationLogger` | Singleton | там же |
| `IArchiveOperationService` | `ArchiveOperationService` | Singleton | там же |
| `ICrashPackageLogSink` | `DelegateCrashPackageLogSink` factory | Singleton | `AddCrashDiagnostics` |
| `ICommandHistoryService` | `CommandHistoryService` | Singleton | `AddCrashDiagnostics` |
| `IHostedService` | `CommandHistoryBridgeHostedService` | Hosted singleton | `AddCrashDiagnostics` |
| `ICrashPackageService` | `CrashPackageService` | Singleton | `AddCrashDiagnostics` |
| `IExceptionDiagnosticReporter` | `ExceptionDiagnosticReporter` | Singleton | `AddCrashDiagnostics` |
| `ICrashDataCollector` | 9 collector implementations | Singleton, multiple | `AddCrashDiagnostics` |
| `IDiagnosticStateProvider` | delegate provider `"Application"` | Singleton | `AddDiagnosticStateProvider` |
| `IDiagnosticConfigProvider` | delegate provider `"AppSettings"` | Singleton | `AddDiagnosticConfigProvider` |

`RegisterMetrologyControls()` сканирует загруженные assemblies, выбирает concrete
`UserControl` с `MetrologyModeAttribute` и регистрирует каждый как Transient.
`MetrologyControlFactory` повторяет discovery, строит `MetrologyType → Type/title`
и создаёт control через `GetRequiredService`.

### Manual composition

`MainWindow.Engine.AppServices.Build(window)` вручную создаёт:

```text
MultiWindowService
→ MainWindow.Services.FileService
→ MetrologyService через ActivatorUtilities + ServiceLocator
→ AdminServices / TestService / SettingsService / WindowService
→ SelfTestServices / TranslationServices (с `IExceptionDiagnosticReporter` из DI) / RunServices
→ MainWindowViewModel и дочерние menu ViewModels
```

`UI.Components.MultiEditorMethods.FileManager` вручную создаёт workspace graph:
`ContainerService`, `ProtocolService`, `ControlManagerService`, `DockItemService`,
`FolderService`, `RunControlService`, `TextEditorService`, `TranslationService`,
`UI.Services.FileManager.FileService`.

Device/data services также не зарегистрированы в DI: `DeviceRuntime` держит
singleton-like `new DeviceEngine()`, а тот создаёт cache и DTO services в
конструкторе. Это важный hidden composition path.

## Dependency Map

Фактические runtime-роли:

```text
MainWindowProgram (startup + shell composition)
↓
UI + Ask.UI (workspace, controls, execution/protocol facade)
↓
Ask.Engine (translation and algorithms)
↓
Ask.Core contracts/config/events
↓                         ↘
Ask.DataBase.Engine        Ask.Device.Application
↓                         ↓
Ask.DataBase.Provider      Ask.Device.Runtime
                          ↓
                          Ask.Device.Communication
                          ↓
                          COM / TCP / UDP / USB / VISA
```

Зависимости не образуют строгую clean architecture: UI знает Provider и Runtime,
Engine знает DB Engine и Message, Core содержит WPF/config/application concerns.
При изменении ориентироваться на существующие seams, а не на целевую схему.

## Subsystems

### Translation and command language

#### Purpose

Преобразует `.pk/.pkw/.acs` в список `BaseCommandModel`, форматированный `.opkw`
текст, ошибки, warnings и mapping исходных/форматированных строк.

#### Entry Points

`MainWindow.Services.TranslationServices` из `TranslationViewModel`; для запуска
она создаёт/обновляет `TranslatorItem`, затем передаёт модели в `RunControl`.

#### Key Interfaces and implementations

- `ICommandParser` → concrete parsers в `ControlCommandAnalyser/Parser/`;
- `ICommandFormatter` → formatters в `Formatter/`;
- `ICommandBody` → body builders в `ComandBody/`.

`CommandTranslationManager` находит все три группы через reflection в assembly.

#### Translation flow

```text
TranslationViewModel command
→ TranslationServices.BuildAsync/CreateNewTranslator/EditExistingTranslator
→ BuildTranslationAsync (background parse)
→ CommandTranslationManager.ParseAllAndDisplay/ParseAll
→ PreprocessText.PreprocessTextAndExtractComments
→ command block splitting
→ matching ICommandParser.Parse
→ CheckVshModel
→ CommandPostAnalyzer.Analyze
→ matching ICommandFormatter
→ source/formatted line mapping
→ TranslatorItem.ApplyTranslationModels
→ ErrorList + left/right AvalonEdit editors
```

#### Error flow

Parser/validators add `ErrorItem`/`WarningItem` from
`Ask.Core/Services/Errors/Translation`. `CriticalTranslationErrorClassifier`
определяет блокирующие ошибки. UI отображает snapshot через `TranslatorItem` и
error-list controls.

#### Files

- `MainWindow/Services/TranslationServices.cs`
- `Ask.Engine/ControlCommandAnalyser/CommandTranslationManager.cs`
- `Ask.Engine/ControlCommandAnalyser/Parser/CommandPostAnalyzer.cs`
- `Ask.Engine/ControlCommandAnalyser/Parser/`
- `Ask.Engine/ControlCommandAnalyser/Formatter/`
- `Ask.Engine/ControlCommandAnalyser/RmTranslation/`

### Execution Engine

#### Purpose

Координирует lifetime запуска, последовательность команд, breakpoints, pause/step,
equipment preparation, протокол, stop/finalize и аварийный `КЦ`.

#### Entry Points

`RunControl.Start(models)` строит `ActionSettings { StartDelegate = StartTest }`
и вызывает `ProtocolUI.StartAsync()`.

#### Control-program execution flow

```text
RunControl.Start(models)
→ ProtocolUI.StartAsync()
→ ActionExecutor.StartAsync(ActionSettings)
  → ExecutionRunGuard.TryAcquire
  → clear protocol/errors and reset StepControlManager
  → real mode only: power check
  → ExecutionSystemResetService.ResetAsync clears global execution state
  → ActionSettings.PreActionDelegate (if any)
  → Task.Run(ActionSettings.StartDelegate)
→ RunControl.StartTest(...)
  → new CommandExecutionManager(ProtocolUI, editor, models, opkPath)
  → subscribe AddError/ClearError
  → CommandExecutionManager.ExecuteAllAsync()
```

#### Command dispatch flow

```text
CommandExecutionManager.ExecuteAllCoreAsync loop
→ IUserInteractionService.WaitIfPausedAsync
→ editor.SetActiveLine
→ BreakpointHandler.OnBreakpointHitAsync
→ CommandMessages.ShowBreakpointHitAsync (публикация заголовка без ожидания паузы и проверки пошагового режима)
→ new CommandExecutionContext
→ CommandExecutorRegistry.TryGet(mnemonic)
→ ICommandExecutor.ExecuteAsync(context, ProtocolModel)
→ CompleteCommandAsync(hasErrors)
→ apply JumpToCommandNumber for УП
```

`CommandExecutorRegistry` reflection-сканирует `Ask.Engine` и создаёт все concrete
`ICommandExecutor`. Текущие executors: `ОК`, `РМ`, `СП`, `СК`, `ВШ`, `ПТ`, `ОТ`,
`ЦУ`, `УП`, `КЦ`, `КС`, `ИЕ`, `ЭТ`, `ПР`, `СИ`, `ПИ`, `НЕ`, `ОС`.

#### Addressed reset of test equipment

Широковещательный UDP-сброс удалён. Для каждого запуска `ActionExecutor`
создаёт отдельный `ExecutionSession`, которому принадлежит
`EquipmentUsageSession`:

```text
ActionExecutor.StartAsync
→ ExecutionSession
→ EquipmentUsageTracker.BeginSession
→ StartDelegate / PreActionDelegate / StopDelegate inherit execution context
→ production IDevice built by DeviceBuilder
→ DeviceApplicationComposer
→ EquipmentTrackingConnectable
→ first Initialize/Connect/Disconnect/Reset attempt
→ EquipmentUsageTracker.Register(IDevice)
```

Учёт происходит до фактической операции, поэтому неудачная попытка подключения
тоже делает устройство использованным. Устройство, которое только присутствует
в конфигурации, но до которого выполнение не дошло, в mandatory reset не попадает.
Повторные обращения к тому же runtime-экземпляру не дублируют запись.

Все terminal paths сходятся в одном finalizer:

```text
SUCCESS / FAILURE / EARLY RETURN
USER FINISH / STOP / ABORT
CANCEL / OperationCanceledException
TIMEOUT / HARDWARE ERROR / COMMUNICATION ERROR / EXCEPTION
→ ActionExecutor.FinalizeAsync (isExit prevents duplicate finalizer entry)
→ ExecutionFinalizer.RunMandatoryStepsAsync
  → EquipmentExecutionContext.EnterMandatoryFinalization
  → cancel and await ProcessTask
  → run scenario StopDelegate
  → read the captured EquipmentUsageSession
  → snapshot used equipment
  → DeviceResetService.ResetDevicesAsync(
      usedDevices,
      ProtocolUI,
      CancellationToken.None)
  → clear executor/session state
  → reset global execution state
  → print/display/save protocol
```

`ActionExecutor` захватывает ссылку на `EquipmentUsageSession` перед запуском
финализатора. Остановка фоновой задачи очищает только ссылку на `ProcessTask`;
сам execution session освобождается после обязательного сброса. Поэтому очистка
задачи не может удалить список оборудования до формирования snapshot.

Главный invariant:

```text
Every execution terminal path
→ Mandatory finalization
→ Reset every equipment item used by that execution
→ Finish
```

Финальный reset выполняется независимо от промежуточных reset внутри алгоритма.
Локальные `FinalizeAsync`/`FinalizeMeasurement` сохраняют только сценарные
результаты и внутреннее состояние; аппаратная гарантия принадлежит
`ExecutionFinalizer`.

Обязательный сброс:

```text
EquipmentUsageSession.GetUsedDevices
→ DeviceResetService.ResetDevicesAsync
→ последовательно для каждого уникального устройства
  → IConnectable.ResetAsync
  → Transport → адресный UDP/TCP/COM/USB driver
  → bool/exception проверяется отдельно для устройства
  → результат записывается в лог и протокол
→ false/exception/output error
  → log
  → next device
```

В mandatory finalization интерактивное меню
`Repeat/Continue/Finish` не открывается. Уже отменённый execution token не
передаётся сбросу. В Idle используется тот же публичный `IConnectable.ResetAsync`;
симулированная ошибка одного устройства фиксируется и не прерывает попытки
сброса остальных.

Вне mandatory finalization `DeviceResetService` сохраняет обычное интерактивное
поведение `Repeat/Continue` для промежуточных алгоритмических сбросов (`ОС`,
command jump и self-check stages).

В программе контроля команда `КЦ` оформляет завершающий блок и формирует
протокол, но не сбрасывает оборудование. Единственный Reset выполняет общий
mandatory finalizer. Для `CheckType.ControlProgram` отдельный заголовок
`Завершение теста` не выводится, поэтому результаты финального сброса остаются
в блоке `КЦ`.

При exception:

```text
executor throws
→ CompleteCommandAsync(true)
→ ExecuteKscOnExceptionAsync
→ show execution error
→ find last KscCommandModel
→ KscCommandExecutor.ExecuteAsync
→ rethrow original exception
→ ActionExecutor catches/finalizes
```

Аварийный `КЦ` выполняется в `EquipmentExecutionContext` как обязательное
завершение: ошибки оборудования в этой области протоколируются без нового
интерактивного цикла.

#### Strategies

- `ConnectedPointChecker` — проверяет соединённые цепи, формирует единый `AlgorithmExecutionResult` и передаёт
  создание и публикацию этапов и результатов в `CommandMessages`/`MeasurementMessages`;
- `DisconnectionCheckExecutor` выбирает `MethodExecutor`,
  `NodeAccumulationChecker`, `NodeFullChecker` или pairwise strategy;
- `MethodExecutor`, `NodeAccumulationChecker`, `NodeFullChecker` и `PairwiseFirstPointChecker`
  возвращают единый `AlgorithmExecutionResult`; формирование и публикация их заголовков,
  этапов локализации, диагностических сообщений и готовых результатов измерения проходят
  через `CommandMessages`, `ExecutionMessages` и `MeasurementMessages`;
- `PairwiseFirstPointCheckerAlt` — специальная ЭТ-проверка; возвращает `AlgorithmExecutionResult`, а создание
  и публикацию измерений, ошибок подключения точек и debug-сообщений делегирует `Ask.Protocol.Messages`;
- `FaultChainMeasurementService` — повторно измеряет проблемные цепи и возвращает
  `AlgorithmExecutionResult`; модель ошибки формирует `MeasurementMessages`;
- `EhtCommandExecutor`, `IeCommandExecutor`, `KsCommandExecutor`, `NeCommandExecutor`,
  `PiCommandExecutor`, `PrCommandExecutor` и `SiCommandExecutor` передают единый
  `AlgorithmExecutionResult` в `ProtocolModelExtensions.AddResult`; расширение находится
  в `Ask.Protocol.Messages/Extensions/ProtocolModelExtensions.cs` и внутри раскладывает
  ошибки и информационные сообщения по коллекциям `ProtocolModel`;
- `ParallelTestRunner` публикует этап общего сброса через `ExecutionMessages`, а
  `CiGroupMethodExecutor` передаёт ошибки подключения и результаты измерения в
  `ExecutionMessages`/`MeasurementMessages` и использует логический признак успеха;
- `MeasurementMessages` формирует тексты брака узлового и группового методов через
  `MeasurementFailureMessageBuilder`; `MeasurementLimitKind`, старые
  `GroupMethodProtocolBuilder` и `NodeMethodProtocolBuilder` удалены из `Ask.Engine`;
- исполнители команд передают `SourceLines` в `CommandMessages.FormatSourceLines`;
  `CommandExecutorBase` больше не содержит форматирование текста протокола;
- `DeviceManager` — grouped facade для relay/switch equipment operations.

`ПИ` вызывает `СИ` как вложенный executor до и после основной ACW/DCW-проверки.
`РМ` вызывает `EquipmentService.AnalyzePoints` и готовит equipment state.

#### Pause, stop and command jump

`ActionExecutor` владеет `ExecutionSession`/`CancellationTokenSource`,
`ExecutionPauseController`, `ExecutionRunGuard`, `ExecutionFinalizer`.
`ProtocolUI` реализует `IExecutionController`, `IExecutionPauseGate` и
`IUserInteractionService`. Pause checkpoints проходят через
`WaitAtExecutionCheckpointAsync`; command jump от F4 идёт:

```text
ProtocolUI.RequestCommandJump
→ CommandExecutionManager.RequestPausedCommandJumpAsync
→ CommandJumpService.SelectAsync
→ drawer events
→ ActionExecutor.InterruptPauseForCommandJump
→ CommandJumpRequestedException
→ CommandJumpService.PrepareAsync
→ CommandMessages.ShowCommandJumpAsync
→ DeviceResetService.ResetDevicesAsync
→ CommandExecutionManager resumes at selected command
```

`CommandExecutionManager` передаёт сообщения о неизвестной команде, запуске аварийного
`КЦ` и ошибке аварийного `КЦ` в `ExecutionMessages`; моделей экранного протокола сам не создаёт.

`ExecutionFinalizer` последовательно отменяет текущую задачу, очищает состояние,
сбрасывает оборудование, печатает при включённой настройке, восстанавливает UI,
показывает результат и сохраняет протоколы. Все шаги выполняются внутри
`EquipmentExecutionContext.EnterMandatoryFinalization`; ошибка отдельного шага
логируется и не прерывает оставшиеся обязательные действия.

#### Related configuration

`ExecutionConfig`: idle, step-by-step, delays, reactions and compatibility mode.
`ProtocolConfig`: command headers, step messages, printing and protocol templates.

#### Files

- `Ask.UI/Features/ProtocolNew/Execution/ActionExecutor.cs`
- `Ask.UI/Features/ProtocolNew/Execution/ExecutionFinalizer.cs`
- `UI/Controls/Runner/RunControl.xaml.cs`
- `Ask.Engine/ControlCommandExecutor/Execution/`
- `Ask.Engine/ControlCommandExecutor/Executors/`
- `Ask.Engine/ControlCommandExecutor/BaseStrategies/`

### Equipment resolution and device persistence

#### Purpose

Разрешает configured devices из SQLite, сохраняет identity через cache, создаёт
runtime-классы и навешивает application adapters.

#### Equipment resolution flow

Во время `РМ`:

```text
RmCommandExecutor
→ EquipmentService.AnalyzePoints(points, map, UI)
→ ChassisManagers.GetByNumberAsync
→ SwitchingDevices.GetDevicesByNumberChassisAsync
→ RelaySwitchModules.GetDevicesByNumberChassisAsync
→ ValidationMessages.PublishEquipmentConfigurationErrorAsync при ошибке конфигурации
→ validate module point bounds
→ module.ConnectableManager.InitializeAsync + ResetAsync
→ switchingDevice.ConnectableManager.InitializeAsync + ResetAsync
→ cache AnalyzedPoints/ValidRelayModules/ValidSwitchingDevice
```

Измерительные executors позже используют `GetModuleByPoint`,
`GetBreakdownTesterOrThrow`, `GetFastMeterOrThrow` и `GetSwitchingDevice`.

#### Device materialization flow

```text
Static facade (RelaySwitchModules/FastMeters/etc.)
→ DeviceRuntime.Get*Async<TInterface>()
→ shared DeviceEngine
→ matching Provider CrudService<TDto>
→ AppDbContext / SQLite
→ DeviceEngine.Build<TInterface>(dto)
→ DeviceBuilder.Build
→ DeviceFactory.ResolveDeviceType(dto.DeviceClass)
→ Activator.CreateInstance
→ DeviceMapperRegistry.Apply
→ DeviceApplicationComposer.Compose
→ cache by (requested interface, DTO Id)
```

`DeviceFactory` ищет тип сначала через `Type.GetType`, затем в loaded assemblies,
потом грузит known assembly `Ask.Device.Runtime`. `DeviceClass` — persisted CLR
type name и compatibility-sensitive contract.

Персональный флаг `IDevice.IsHardwareFailureSimulationEnabled` маппится в
одноимённое bool-поле DTO и SQLite-таблиц `ChassisManagers`, `BreakdownTesters`,
`FastMeters`, `RelaySwitchModules`, `PowerSourceModules`, `SwitchingDevices`.
`ExecutionControl` загружает экземпляры через статические фасады, показывает для
каждого отдельную `SettingsCard` и сохраняет изменённые экземпляры через
соответствующий `UpdateAsync`; при неизменном `DeviceClass` обновление применяет
DTO к существующему runtime-экземпляру, сохраняя его identity для уже открытых
тестов. При смене `DeviceClass` запись runtime-cache заменяется новым экземпляром.
После успешных `CreateAsync` / `UpdateAsync` / `DeleteAsync` / `DeleteAllAsync`
`DeviceRuntime` публикует `DeviceConfigurationEvents.Changed`. Полная замена
конфигурации в `DeviceConfigurationService.ApplyConfigurationFileAsync` публикует
то же событие с видом `Replaced`. `ExecutionControl` подписывается на время своей
WPF-жизни, объединяет близкие события и перечитывает шесть списков оборудования;
несохранённые переключатели переносятся по ключу `(device interface type, Id)`.

#### Files

- `Ask.Engine/ControlCommandExecutor/Execution/EquipmentService.cs`
- `Ask.DataBase.Engine/Static/DeviceRuntime.cs`
- `Ask.DataBase.Engine/Services/DeviceEngine.cs`
- `Ask.DataBase.Engine/Builder/DeviceBuilder.cs`
- `Ask.DataBase.Engine/Factory/DeviceFactory.cs`
- `Ask.Device.Application/Composition/DeviceApplicationComposer.cs`

### Metrology and hardware tests

#### Purpose

Production algorithms for metrology, node/group methods, relay switching tests and
self-control. Несмотря на папку `Ask.Engine/Tests`, это runtime production-код.

#### Entry Points

`MetrologyViewModel → MetrologyService.OpenMetrologyMode → MetrologyControlFactory`.
`TestViewModel → TestService`; `SelfTestViewModel → SelfTestServices`.

#### Metrology flow

```text
menu command
→ MetrologyService.OpenMetrologyMode(type)
→ MetrologyControlFactory.Create(type)
→ DI-created attributed UserControl
→ control prepares ActionSettings / ProtocolUI
→ mode algorithm in Ask.Engine.Tests.Metrology
→ BaseMeasurement template
  → CollectDevices
  → ConnectToEquipment
  → SetupCommutation
  → ConfigureMeter
  → PerformMeasurement override
  → FinalizeMeasurement/result protocol
```

Attributed modes: КС, ИЕ, СИ, ПР, ПИ(DCW/ACW), КН(DCW/ACW), ЭТ.
Other runtime branches:

- `Ask.Engine/Tests/MethodExecutor/` — group CI/PI;
- `Ask.Engine/Tests/NodeMethod/` — node CI/PI;
- `Ask.Engine/Tests/RelaySwitchingModule/` — cross-connection/contact resistance;
- `Ask.Engine/Tests/SelfControl/` — module/system self-control.

#### Files

- `Ask.Core/Services/Metrology/MetrologyControlFactory.cs`
- `Ask.UI/Controls/ExecutorControls/MetrologyControls/`
- `Ask.UI/Controls/ExecutorControls/TestsControls/`
- `Ask.Engine/Tests/Metrology/`
- `Ask.Engine/Tests/RelaySwitchingModule/`
- `Ask.Engine/Tests/SelfControl/`

### Protocols and file formats

#### Purpose

`ProtocolUI` одновременно служит execution control, interaction/output service,
error list and host for execution + inspection protocols.

#### Protocol flow

```text
executors/tests
→ IUserInteractionService.ShowMessageAsync
→ ProtocolUI
→ ProtocolEntryOutputService
→ execution protocol text + ShowMessageModel snapshot

ActionExecutor finalization
→ ExecutionFinalizer
→ ProtocolCompletionService.BuildInspectionProtocol
→ InspectionProtocolBuilder
→ ProtocolUI.ShowInspectionProtocol
→ ProtocolStorageService
→ ExecutionProtocolHistoryService
```

Форматы:

- `.asktrace` — записи хода выполнения;
- `.askresult` — итог обычной проверки;
- `.askreport` — итог программы контроля.

`ExecutionProtocolHistoryService.SaveInspectionAsync` выбирает `.askreport` для
`CheckType.ControlProgram`, иначе `.askresult`, и старается использовать basename
соответствующего `.asktrace`. Каталог истории:
`Path.GetFullPath(Path.Combine("..", FileLocations.DataSaveDirectory))`.

Автопечать:

```text
ExecutionFinalizer
→ ProtocolCompletionService.PrintIfRequired
→ only CheckType != ControlProgram
→ ProtocolConfig.GetPrintProtocol()
→ PrintUtility.PrintProtocol(messages)
```

Для программы контроля `KscCommandExecutor.GetProtocol` заполняет базовые поля `ProtocolModel` и
при включенной печати или формировании протокола программы контроля запрашивает дополнительные данные:

```text
KscCommandExecutor.GetProtocol
→ ProtocolConfig.ShouldShowProtocolInfoDialog()
→ AutoPrintProtocol || ShowProtocolInSoftware
├─ false → ветка итогового report не запускается
└─ true → FileInteractionEventAdapter.RaiseGetProtocolInfo
   → MainWindow.Services.FileService.OnGetProtocolInfo
   → ProtocolInfoWindow.ShowDialog
   → FileInteractionEventAdapter.RaiseProtocolInfoClose
   → KscCommandExecutor.OnProtocolInfoClosing
   → FileInteractionEventAdapter.RaiseViewProtocol
   → MainWindow.Services.FileService.ViewProtocol
   → UI.Services.ProtocolManager.ProtocolService.ViewProtocol
      ├─ !ShowProtocolInSoftware → ExportProtocolAsPdf
      └─ AutoPrintProtocol → PrintUtility.PrintProtocol(protocol, protocolText)
```

`ProtocolConfig.ShouldShowProtocolInfoDialog()` — единая точка решения для окна ввода данных. При отключенных
настройках не запускаются ни окно, ни последующая цепочка итогового report. Ручные print
buttons в `Ask.UI/Controls/ProtocolNew/ProtocolUI.xaml.cs` печатают execution или inspection text без этой проверки.

#### Files

- `Ask.UI/Controls/ProtocolNew/ProtocolUI.cs`
- `Ask.UI/Controls/ProtocolNew/ProtocolUI.xaml.cs`
- `Ask.UI/Features/ProtocolNew/Protocol/`
- `Ask.Core/Services/Protocols/ExecutionProtocolHistoryService.cs`
- `Ask.Core/Shared/Metadata/Static/ProtocolFileExtensions.cs`

### Archive and legacy file conversion

APK/APKW archive UI находится в `Ask.UI/Features/Archive`. Форматные операции:
`Ask.Core/Services/FileFormats/Apk/`, `Opk/`; shell conversion commands —
`MainWindow/Services/Conversion/`. Архивные permissions/integrity/logging/operation
services зарегистрированы singleton в startup. `ArchiveControl` выполняет I/O и
проверки вне UI thread через `Task.Run`.

### Support and diagnostics

Справка:

```text
PreStartupInitializer.InitializeHelpServer
→ HelpServer.EnsureStarted
→ Host.CreateDefaultBuilder + Kestrel/static files
→ Ask.Support/AppHelp
F1 → HelpProvider → HelpViewerWindow (Photino)
```

Crash diagnostics:

#### Crash diagnostics flow

```text
DispatcherUnhandledException / AppDomain.UnhandledException /
TaskScheduler.UnobservedTaskException / LoggerUtility.ExceptionLogged
→ IExceptionDiagnosticReporter or App.CreateCrashPackage
→ CrashPackageService
→ ICrashDataCollector collection
→ exception, caller artifacts, screenshot, command history, device state,
  config, logs, system info and metadata
→ Bin/CrashReports
→ NotificationHostService
```

Для необработанного исключения трансляции используется синхронизированный с UI
путь, гарантирующий создание отчёта до окна ошибки:

```text
TranslationServices.CreateNewTranslator/EditExistingTranslator catch
→ ShowTranslationErrorAsync(ex, editor, normalizedSourceText, operation)
→ CrashReportArtifact.Json("translation-parameters.json", ...)
  + CrashReportArtifact.Text("source-program.txt", editor.Text)
  + optional "translation-input.txt" when normalized input differs
→ IExceptionDiagnosticReporter.ReportAsync
→ CrashPackageService.CreateAsync
→ ExceptionCollector + CrashReportArtifactCollector + остальные collectors
→ Bin/CrashReports/<timestamp>_<exception>.zip
→ LoggerUtility.LogError (повторный auto-report подавлен на exception.Data)
→ MessageBoxCustom.Show
```

`CrashReportArtifact` — общий extension point для контекстных JSON/текстовых
файлов без зависимости `Ask.Diagnostics` от вызывающей подсистемы.
`CrashReportArtifactCollector` проверяет, что имя вложения остаётся внутри
каталога пакета. Параметры трансляции содержат операцию, путь/имя/расширение
исходника, длину и число строк. Исходная программа сохраняется без изменения;
если фактический нормализованный вход транслятора отличается, он добавляется
отдельным файлом. Регистрация и путь задаются в
`MainWindow/Init/PreStartupInitializer.cs`; общий `ICrashPackageService` также
используется глобальными WPF/AppDomain/TaskScheduler handlers.

`CommandHistoryBridgeHostedService` connects static
`DiagnosticCommandHistory.RecordCommand/RecordResponse` calls from transport
protocols to bounded `CommandHistoryService`.

## Equipment Architecture

### Shared device model

`Ask.Core.Shared.Interfaces.DeviceInterfaces.IDevice` is the root contract.
`DeviceWithIP`, `DeviceWithCOM`, `DeviceWithUSB` implement common state and expose
`IConnectable`, `IDeviceProtocol`, `IConnectionInfo`, `ConnectionDetails` and the
per-device `IsHardwareFailureSimulationEnabled` flag.

There are no separate concrete “Idle device” classes. Real and idle are branches
inside application adapters, runtime managers and transports, all controlled by
the global `ExecutionConfig`.

### Device matrix

| Interface | Runtime implementation | Managers/adapters | Protocol/transport | DB facade |
| --- | --- | --- | --- | --- |
| `IChassisManager` | `ManagerChassis` | runtime `PowerManager`; no application adapter | `ChassisQueryExecutor` → Real UDP / stateful Idle emulator | `ChassisManagers` |
| `IRelaySwitchModule` | `ModuleRelayControl` | adapters for Point/Bus/Meter; runtime SelfTest | `ModuleRelayControlQueryExecutor` → Real UDP / stateful Idle emulator | `RelaySwitchModules` |
| `IPowerSourceModule` | `ModuleVoltageCurrentSource` | adapters for Voltage/Current/Bus; runtime SelfTest | `Transport` → UDP | `PowerSourceModules` |
| `ISwitchingDevice` | `DeviceBusCommutation` | adapters for Connector/Relay/Resistor/Capacitor; runtime SelfTest | `DeviceBusCommutationQueryExecutor` → Real UDP / Idle emulator | `SwitchingDevices` |
| `IMultimeter` | `KeysightDevice` | runtime measurement profiles/managers | `DeviceProtocolEmulator.QueryMultimeterAsync` → Real `TcpProtocol:5025` / `MultimeterEmulatorProtocol` | `FastMeters` |
| `IMultimeter` | `MultimeterB7783` | shared runtime measurement managers | `DeviceProtocolEmulator.QueryMultimeterAsync` → Real `UsbProtocol` / `MultimeterEmulatorProtocol` | `FastMeters` |
| `IBreakdownTester` | `GPT79904` | application ACW/DCW/IR/System adapters over runtime managers | `BreakdownTesterCommandProtocol` → Real `ComProtocol` / `BreakdownTesterEmulatorProtocol` | `BreakdownTesters` |
| `IUninterruptiblePowerSupply` | `MikUps1101rRmDevice` | application Connectable/Power adapters | `UsbProtocol` → `UsbCommandHandler`/ViewPower | `UninterruptiblePowerSupplies` |
| `IRack` | отдельной реализации в текущем production-коде нет | data/identity role | не определён | `Racks`; сохранённый `DeviceClass` должен указывать на доступный совместимый тип |

### Real equipment command flow

Representative МКР point command:

```text
executor/strategy
→ IRelaySwitchModule.PointManager.ConnectRelayAsync
→ PointManagerAdapter
→ UserActionHelper.GetRunWithUserRepeatAsync
→ runtime PointManager.ConnectRelayAsync
→ DeviceCommand(8, point, bus, action).ToString
→ ModuleRelayControlQueryExecutor.QueryAsync
→ IDeviceProtocol.QueryAsync
→ UdpProtocol.QueryAsync
→ UdpClient.SendAsync/ReceiveAsync
→ ModuleRelayControlQueryExecutor.ThrowIfFirmwareRejectedCommand
  ├─ Status absent/success → BaseResponse.FromJson validation
  → adapter DeviceMessageBuilder or RelayExceptionFactory
  └─ Status = UnknownCommand / InvalidParametr / InvalidParameter
    → ModuleRelayControlProtocolException(device, operation, localized error, firmware status)
    → UserActionHelper catches hardware exception
    → IUserInteractionService.ShowMessageAsync(MessageType.Error, skipPause: true)
    → protocol line "МКР chassis.number: operation. Системная ошибка. reason [БРАК]"
    → existing Retry / Continue / Abort equipment flow
```

Representative Keysight measurement:

```text
executor/metrology
→ IMultimeter.ResistanceManager.MeasureResistanceAsync
→ ResistanceMeasurementBase
→ MeasurementBase.MeasureResistanceAsync
→ SetModeBase / RangeBase → DeviceProtocolEmulator.QueryMultimeterAsync
→ repeat correctMeasurementCount + falseMeasurementCount times
  → AdapterMeasurementExecutor
  → MeasurementBase.MeasureCoreAsync
  → Simulated.GetSimulatedValue builds idleResponse
  → DeviceProtocolEmulator.QueryMultimeterAsync(profile.Measure, idleResponse)
    → Real: TcpProtocol/UsbProtocol.QueryAsync → transport
    → Idle: SCPI-compatible scientific-notation response from MeasurementRange
  → numeric parsing/rounding
→ range verdict and DeviceMessageBuilder
```

Для `MultimeterTypeMode.Continuity` общий `MeasurementBase` не вызывает
`RangeBase`: режим прозвонки задаётся профильной командой `CONF:CONT`, а
измерительный запрос выполняется через `MEAS:CONT?` без установки диапазона.

По умолчанию измерение сопротивления выполняет три замера:
`correctMeasurementCount = 2` и `falseMeasurementCount = 1`. Правильным
считается числовой ответ внутри `MeasurementRange`; аппаратная ошибка
не считается ложным замером и идёт в обычный equipment retry flow. Серия
проходит при двух или трёх правильных замерах. Прошедшая серия
возвращает среднее правильных значений; непрошедшая — значение вне
диапазона, сохраняя существующие verdict/retry semantics в Engine. Перегрузка
`IResistanceMeasurement.MeasureResistanceAsync` позволяет вызывающему коду
явно задать оба количества.

Инициализация обоих мультиметров использует тот же журнал команд:

```text
IMultimeter.ConnectableManager.InitializeAsync()
→ TcpTransport.InitializeAsync() / UsbTransport.InitializeAsync()
→ DeviceProtocolEmulator.QueryMultimeterAsync(ConnectedProfile.Initialize, idleIdentificationResponse)
  → Real: TcpProtocol / UsbProtocol
  → Idle: идентификационный SCPI-ответ
→ проверка непустого ответа
```

`DeviceProtocolEmulator.QueryMultimeterAsync` записывает каждую операцию двумя строками единого формата:
`Команда мультиметра: "..."` и `Ответ мультиметра на "...": "..."`.
Для SCPI-команд мультиметра без `?` этот шлюз передаёт в транспорт `timeout = 0`
и не ждёт ответа; команды с `?` сохраняют заданный `timeout` и `responseDelay`.

При наличии `IUserInteractionService` низкоуровневая измерительная попытка
выполняется один раз. Ошибка обмена поднимается как аппаратная ошибка до
`UserActionHelper`, который повторяет тот же measurement delegate. Без UI-сервиса
сохраняется прежний внутренний fallback с двумя попытками.

Representative GPT command:

```text
executor
→ IBreakdownTester.{Acw,Dcw,Ir}Manger capability
→ application mode adapter
→ UserActionHelper / AdapterMeasurementExecutor
→ runtime GPT mode/management/helper
→ SCPI-like command string
→ ComProtocol.QueryAsync
→ SerialPortExtensions.UsePort
→ SerialPort.Write/ReadExisting
→ adapter maps failure to typed exception/user action
```

### Adapters and error boundary

`DeviceApplicationComposer.Compose` replaces raw managers for GPT, MINT, МКР,
commutation device and UPS with `Ask.Device.Application.FunctionAdapters`.
Adapters add user retry (`UserActionHelper`), device messages, typed exception
factories and typed result handling. `Transport`, source/current adapters and
active measurement paths также входят в общий интерактивный контур. Аппаратный
`false`/exception передаётся с `deviceTask: true`; корректный измерительный ответ
проверяется на норму внешним Engine-слоем и не смешивается с ошибкой оборудования.
Отклонение команды прошивкой МКР — отдельная типизированная ветка: она всегда публикует
ошибку в экранный протокол до интерактивного выбора оператора.

### Real / Idle

Mode source:

```text
SettingsExecutionDto.IdleModeExecution (SQLite)
→ DatabaseInitializer
→ ExecutionConfig.SetExecutionModel/SetIdleMode
→ ExecutionConfig.IdleModeChange
→ StateEventsBinder updates UI/system state
```

Selection is distributed, not DI-based:

- `ActionExecutor.StartAsync` skips power validation and system reset in idle;
- chassis and МКР initialization/reset and runtime commands use
  `DeviceProtocolEmulator`, which selects the real UDP protocol or the matching
  stateful emulator;
  - УКШ runtime-команды `4`, `5`, `6`, `7`, `8`, `9`, `41` проходят через
    `DeviceBusCommutationQueryExecutor` и `DeviceBusCommutationEmulatorProtocol`, формируя журналируемый ответ
    в формате прошивки: JSON для `4/5/7/9`, строковое значение для `6/8/41`;
  - остальные UDP/TCP/COM/USB connectable managers возвращают simulated success или обходят I/O;
- relay/source/switch managers update in-memory state and return success;
- `Simulated.GetSimulatedValue` supplies values to the Idle multimeter SCPI-response path;
- GPT helpers/managers skip commands or return configured/simulated values;
- specific Engine strategies may suppress physical validation.

Idle measurement-error and equipment-failure simulation are configured independently:

```text
ExecutionControl
→ SettingsExecutionDto.IsErrorSimulationMode
→ existing measurement simulation algorithms

ExecutionControl
→ nested Border «Симуляция сбоев оборудования»
→ one SettingsCard per configured device
→ IDevice.IsHardwareFailureSimulationEnabled
→ matching device DTO / SQLite table
→ IdleHardwareErrorSimulator.ShouldSimulateHardwareError(device)
→ ExecutionConfig idle && device flag
→ non-measurement Idle manager/transport contract
→ existing adapter/UserActionHelper equipment-error flow
```

The nested `Выполнение с ошибками` settings group is visible only while Idle is
enabled. Measurement simulation retains its existing generators, probabilities
and tolerance semantics. Equipment-failure simulation is disabled by default,
selected independently for every configured device and affects only Idle
initialization/reset, connection, mode/configuration, range, switching, source
and power operations. Every applicable call of a selected device, including a
`Retry`, fails (100% probability). The simulated failure preserves the
corresponding real contract: `false`, a failed tuple/status, an empty emulator
response or the operation-specific exception path. Real execution never enters
this mechanism. The simulator owns no user-facing/protocol error text: it returns
only the normal failed contract, while existing operation adapters,
`*ExceptionFactory` types and the `UserActionHelper` fallback build the same
messages as for real equipment failures. Neither simulation state nor Idle mode
is included in failure messages. The migration copies the former global flag to
all six device tables once, preserving the old effective selection during the
transition.

Chassis and МКР Idle flows preserve their device command contracts without a
separate response processor:

```text
Transport / target runtime manager
→ ChassisQueryExecutor / ModuleRelayControlQueryExecutor / DeviceBusCommutationQueryExecutor
→ DeviceProtocolEmulator.CreateChassis / CreateModuleRelayControl / CreateDeviceBusCommutation
→ ModeSelectingDeviceProtocol
  → Real: current device protocol
  → Idle: ChassisEmulatorProtocol / ModuleRelayControlEmulatorProtocol / DeviceBusCommutationEmulatorProtocol
→ existing runtime response models and validation
```

The emulator handles initialization (`1.0.0.0`), reset (`2.1.0.0`), power on/off
and power-state query. Reset clears its in-memory power state. Hardware-error
simulation returns an empty response and enters the existing retry/error contract.
The МКР emulator returns firmware-compatible JSON envelopes for bus, point,
verified point, group, meter and self-check commands. Runtime connection stores
are updated only after the existing response models accept the emulated answer.

EHT special case:

```text
EhtCommandExecutor.ShouldValidatePointConnections()
→ false only when ExecutionConfig idle is enabled
→ PairwiseFirstPointAltContext.ValidatePointConnections = false
→ PairwiseFirstPointCheckerAlt skips physical resistance/connectivity verdicts
→ no "Нет подключения точки" errors from those checks
```

Configuration existence and logical point/model validation may still run in idle;
only checks explicitly gated by idle are bypassed. Ordinary execution follows the
same path with gates enabled and performs real transport I/O.

### Transport details

- UDP: ports `8888 + last IP octet` output and `8800 + last octet` input unless
  explicit port; per-device semaphore; timeout returns warning text.
- TCP: persistent `TcpClient`/`NetworkStream`, reconnect on endpoint change or
  I/O failure; per-device semaphore.
- COM: `SerialPortCustom` serialized in `ConnectionDetails`; `ComProtocol` opens
  through `SerialPortExtensions.UsePort`, writes newline-terminated command and
  polls `ReadExisting`.
- USB: `UsbProtocol` delegates discovery/commands to `IUsbCommandHandler`;
  `UsbCommandHandler` includes B7783 and UPS/ViewPower branches.

### Equipment files

- contracts: `Ask.Core/Shared/Interfaces/DeviceInterfaces/`
- composition: `Ask.Device.Application/Composition/DeviceApplicationComposer.cs`
- adapters: `Ask.Device.Application/FunctionAdapters/`
- concrete devices: `Ask.Device.Runtime/Device/`
- managers: `Ask.Device.Runtime/Function/`
- Idle emulation for chassis, МКР, УКШ, multimeters and ППУ:
  `Ask.Device.Emulator/{Chassis,ModuleRelayControl,DeviceBusCommutation,Multimeter,BreakdownTester}/`;
  routing factory: `Ask.Device.Emulator/DeviceProtocolEmulator.cs`;
  protocol and Real/Idle regression tests: `Ask.Device.Emulator.UnitTests/`
- protocols: `Ask.Device.Communication/`
- persistence: `Ask.DataBase.Engine/Static/Devices/`, `Ask.DataBase.Provider/Services/Devices/`

## UI Architecture

`MainWindow` is shell and menu host. `MainWindowViewModel` exposes File,
Translation, Run, Metrology, Test, SelfTest, Settings, Admin and Window ViewModels.
Their services generally route operations into `MultiWindowService`.

### Главное меню и адаптивная верхняя панель

Единственное дерево главного меню объявлено в `MainWindow/MainWindow.xaml`.
Пункты напрямую связываются с дочерними ViewModel из `MainWindowViewModel`;
`UiEventsBinder` изменяет видимость контекстных файловых команд и передаёт меню
в `MenuHotkeyBinder.BindAutoRenumbering`.

Верхняя панель делит доступную ширину между меню (`*`) и блоком пользователя,
темы и оконных кнопок (`Auto`). `Menu.ItemsPanel` использует горизонтальный
`WrapPanel`: при нехватке места существующие пункты переносятся на следующие
строки, а высота строки shell увеличивается автоматически. Механизм работает
от фактического WPF layout и DPI-независимых единиц, не проверяет разрешение или
ширину окна и не создаёт вторую версию меню. `WindowService` управляет только
состоянием окна; адаптация меню не проходит через ViewModel или обработчик
`SizeChanged`. Размер блока пользователя, темы и оконных кнопок привязан к
высоте одного пункта `File`, а не к суммарной высоте `mainMenu`; это исключает
цикл обратной связи «перенос меню → увеличение кнопок → уменьшение места меню».

Ключевые файлы: `MainWindow/MainWindow.xaml`,
`MainWindow/ViewModels/MainWindowViewModel.cs`,
`MainWindow/Events/UiEventsBinder.cs`,
`MainWindow/HotkeyBindings/MenuHotkeyBinder.cs`,
`MainWindow/Services/WindowService.cs`,
`MainWindow/Engine/AppServices.cs`.

`UI.Components.MultiEditorControl` is the main workspace. It exposes:

- `IEditorDocumentService` → `UI.Services.FileManager.FileService`;
- `IRunService` → `RunControlService`;
- `IProtocolViewerService` → `UI.Services.ProtocolManager.ProtocolService`;
- `IWorkspaceService` → `ControlManager`;
- `ITranslationService` → `UI.Services.TranslationService`.

`MainWindow.Services.MultiWindowService` is the shell adapter over these contracts.
`FileManager` and `EditorWorkspaceModel` own containers, dock items, open paths and
user controls. `TextEditorUI` wraps AvalonEdit; `TranslatorItem` holds source and
formatted editors; `RunControl` hosts ProtocolUI, translated source and error list.
В правой области `RunControl` панель действий документа отображается только для
транслированного файла и итогового протокола; вкладка состояния оборудования её скрывает.

`Ask.UI` contains newer feature-oriented code: ProtocolNew, Archive, Notifications,
RoleManagement, ExecutionSelection and reusable controls. Both UI projects are
active; do not assume one replaces the other.

### Валидация конфигурации устройств

Все окна настройки оборудования в `UI/Controls/Settings/DeviceConfig/` используют
общий `DeviceSettingsControl`. Поток сохранения:

```text
DeviceSettingsControl.SaveButton_PreviewMouseDown
→ DeviceSettingsControl.ValidateRequiredParameters
→ DeviceRequiredParameterValidator (тип подключения, IP и числовые поля)
→ проверка видимых общих, transport-specific и model-specific полей
→ при ошибке: подсветка секций + переход к первому полю
  + DeviceConfigNotifications.ShowRequiredParametersMissing
→ при успехе: DeviceSettingsControl.SaveEvent
→ обработчик конкретного *Window.SetSettings
→ DeviceSettingsProcessorBase.ProcessDevice
→ BaseHandler.GetConnectionDetails
→ целевой static device facade CreateAsync/UpdateAsync
→ Ask.DataBase.Engine → Ask.DataBase.Provider → SQLite
```

COM-секция делегирует создание настроек в
`Ask.UI.Components.ComSettingsComponent.CreateSettings`; её общая подсветка
управляется через `SetValidationHighlight`. `InitializeValidationTracking`
связывает редактируемые поля с секциями: изменение значения сразу снимает
подсветку соответствующей ошибки; `ComSettingsComponent.SettingsChanged`
обеспечивает тот же поток для внутренних полей COM. Проверка выполняется только
для видимых секций, выбранных текущей моделью устройства.

Ключевые файлы:
`UI/Controls/Settings/DeviceConfig/Base/BaseSettingsConfig/DeviceSettingsControl.EventHandler.cs`,
`UI/Controls/Settings/DeviceConfig/Base/BaseSettingsConfig/DeviceSettingsControl.Validation.cs`,
`UI/Controls/Settings/DeviceConfig/Base/DeviceRequiredParameterValidator.cs`,
`UI/Controls/Settings/DeviceConfig/DeviceConfigNotifications.cs`,
`Ask.UI/Components/ComSettingsComponent.xaml.cs`.

### Административные утилиты

Меню `MainWindow.xaml:Admin` содержит отдельные команды, каждая из которых открывает
собственную вкладку рабочего пространства:

- `AdminViewModel.ServiceUtilitiesCommand`
  → `AdminServices.OpenServiceUtilities()`
  → `IWorkspaceService.AddControl("Сервисные утилиты", new ServiceUtilitiesControl(GetGptAsync, GetSwitchingDeviceAsync, GetRelaySwitchModulesAsync, GetMultimetersAsync, GetChassisAsync), TypeWindow.Settings)`;
- `AdminViewModel.DatabaseCommand`
  → `AdminServices.OpenDatabase()`
  → `IWorkspaceService.AddControl("База данных", new DataBaseView(), TypeWindow.Settings)`;
- `AdminViewModel.ResistanceCommand`
  → `AdminServices.OpenResistance()`
  → `IWorkspaceService.AddControl("Сопротивление МКР", new CheckResistanceControl(), TypeWindow.Settings)`.

`ServiceUtilitiesControl` сохраняет экземпляры вложенных
  утилит при переключении;
  - `SetCommand` — отправка низкоуровневых команд и отображение общего потока
    `LoggerUtility.LogMessageWritten`; занимает всю область на собственной вкладке
    и переносится в постоянную правую панель при выборе другой утилиты;
  - `Ask.UI.Features.ServiceTools.Gpt.GPTPunchControl` → `GPTController` —
    ручное управление пробойной установкой; feature полностью находится в
    `Ask.UI`, не зависит от legacy `UI` или БД и получает
    `Func<Task<IBreakdownTester?>>` из `AdminServices`;
    каждая открытая вкладка хранит устройство в собственном `GptDeviceContext`,
    поэтому параллельные вкладки не разделяют статическое состояние;
    `GPTController` лениво создаёт и сохраняет контролы режимов ACW/DCW/IR/общих
    настроек; активные ACW/DCW/IR реализуют `IGptModeControl`, поэтому при выборе
    другого режима контроллер сначала переключает реальное устройство, затем
    деактивирует предыдущий режим в UI; при ошибке сохраняет прежнее состояние
    и возвращает выбор на прежнюю вкладку; `AdminServices.GetGptAsync`
    разрешает устройство через `BreakdownTesters.GetDevicesByNumberChassisAsync(1)`,
    `GPTPunchControl.Loaded` асинхронно вызывает переданный provider, а UI-операции проходят
    через `GptUiOperation`: отсутствие устройства, исключения транспорта и
    отрицательные результаты аппаратных команд записываются в
    `LoggerUtility.LogMessageWritten` и не распространяются в WPF UI thread;
  - `Ask.UI.Features.ServiceTools.Chassis.ChassisControl` — ручная сервисная
    утилита контроллера шасси: загрузка первого настроенного шасси через
    `AdminServices.GetChassisAsync`/`ChassisManagers.GetAllAsync`, инициализация,
    полный сброс, включение, выключение и проверка питания. Команды идут через
    обычные `IChassisManager.ConnectableManager` и `PowerManager`, поэтому в
    реальном режиме используются UDP-команды, а в Idle — тот же stateful
    `ChassisEmulatorProtocol`; результат и ошибки отображаются в панели и
    записываются в device log;
  - `Ask.UI.Features.ServiceTools.SwitchingDevice.SwitchingDeviceControl` —
    ручное управление УКШ без ввода протокольных команд: мультиметр по выбранной
    шине, ППУ, совместная коммутация ППУ и мультиметра, все шины, делитель,
    отдельные и общие реле, резисторы, конденсаторы и замыкание/размыкание
    выбранной цепи самоконтроля; список типов и контактов формируется через
    `ISelfTestCheckerDeviceBusCommutation.GetSupportedTestTypes()` и
    `GetValidBusContacts()`, а команда передаётся в `ExecuteSelfTestAsync()`;
    provider
    `AdminServices.GetSwitchingDeviceAsync`
    разрешает первое устройство через
    `SwitchingDevices.GetDevicesByNumberChassisAsync(1)` и передаёт только
    `ISwitchingDevice`; операции вызывают application adapters из properties
    устройства, обновляют программный список подключений, а исключения и
    отрицательные результаты записывают в постоянную консоль SetCommand;
    ПИНТ показан как недоступный, поскольку его runtime-реализация намеренно
    выбрасывает исключение;
  - `Ask.UI.Features.ServiceTools.RelaySwitchModule.RelaySwitchModuleControl` —
    сервисное управление выбранным МКР первого шасси: одиночные точки,
    операции с аппаратной проверкой, диапазоны, перевод точки между шинами,
    коммутация шин, измеритель и общее отключение точек. Provider
    `AdminServices.GetRelaySwitchModulesAsync` получает список через
    `RelaySwitchModules.GetDevicesByNumberChassisAsync(1)`; UI вызывает
    `IPointManager`, `IBusManager` и `IMeterManager`, а текущие подключения
    читает через `GetConnectedPoints()` и `GetConnectedBuses()`; в Idle те же
    операции проходят через `ModuleRelayControlEmulatorProtocol`;
  - `Ask.UI.Features.ServiceTools.Multimeter.MultimeterControl` — сервисное
    управление мультиметрами первого шасси через общий `IMultimeter`: выбор
    Keysight/В7-78/3, подключение, инициализация, сброс, установка режима и
    диапазона, ручные измерения сопротивления, AC/DC-напряжения, ёмкости,
    прозвонки и диода. `AdminServices.GetMultimetersAsync` получает приборы через
    `FastMeters.GetDevicesByNumberChassisAsync(1)`; измерения передаются
    соответствующему capability manager с `MeasurementRange`, результаты и
    ошибки публикуются в постоянную консоль SetCommand;
- `DataBaseView` — административный просмотр таблиц БД;
- `CheckResistanceControl` — настройка сопротивления МКР.

Файлы: `MainWindow/MainWindow.xaml`,
`MainWindow/ViewModels/AdminViewModel.cs`, `MainWindow/Services/AdminServices.cs`,
`UI/Controls/AdminPanel/ServiceUtilitiesControl.xaml(.cs)`,
`UI/Controls/AdminPanel/SetCommand.xaml(.cs)`,
`Ask.UI/Features/ServiceTools/Gpt/GPTController.xaml(.cs)`,
`Ask.UI/Features/ServiceTools/Gpt/GptUiOperation.cs`,
`Ask.UI/Features/ServiceTools/Gpt/IGptModeControl.cs`,
`Ask.UI/Features/ServiceTools/Gpt/Modes/*.xaml(.cs)`,
`Ask.UI/Features/ServiceTools/Chassis/ChassisControl.xaml(.cs)`,
`Ask.UI/Features/ServiceTools/SwitchingDevice/SwitchingDeviceControl.xaml(.cs)`,
`Ask.UI/Features/ServiceTools/RelaySwitchModule/RelaySwitchModuleControl.xaml(.cs)`,
`Ask.UI/Features/ServiceTools/Multimeter/MultimeterControl.xaml(.cs)`,
`Ask.UI/Shared/Controls/NumericComboBox.cs`, `Ask.LogLib/LoggerUtility.cs`.

Авторизация и Debug-зависимый UI описаны в
[Authentication and Debug access flow](#authentication-and-debug-access-flow).

Dialogs:

- `Message.MessageBoxCustom` — generic modal messages;
- ProtocolUI drawers/overlays — pause/retry/selection actions;
- `NotificationHostService` — non-modal notifications;
- `RoleLoginWindow` — authentication/loading startup UI.

UI thread handling uses `Dispatcher` in `MainWindow`, `ProtocolUI`, notification
host and crash state capture. Execution delegates run through `Task.Run`, while
protocol output marshals back into WPF controls.

## Error Handling Architecture

### Input field validation

Локальный формат первой и второй точки проверяет
`Ask.UI.Components.InputField.Controls.PointInput.Validate()`.
`PointInputRole` выбирает существующий `ErrorItem` первой или второй точки.

Числовой формат электрического параметра и активного поля напряжения проверяет
`ElectricalInput.Validate()`. `ElectricalInputRole` выбирает
`InvalidElectricalValue` или `InvalidVoltage`. Напряжение проверяется только при
`InputField.IsVoltageVisible` и должно быть целым числом; в module mode электрические
поля не участвуют.

Числовой формат активных полей времени проверяет `TimeInput.Validate()`.
`TimeInputRole` различает время выполнения и время нарастания и выбирает
`InvalidExecutionTime` или `InvalidRampTime`. Время выполнения проверяется только при
`InputField.IsTimeVisible`, время нарастания — при `InputField.IsTimeRampVisible`;
в module mode поля времени не участвуют. Время выполнения должно быть целым числом
от 1 до 60 секунд, время нарастания — числом от 0,1 до 10 секунд включительно.
Для ramp UI и Engine принимают точку или запятую как десятичный разделитель и
нормализуют значение перед `double.TryParse` с `InvariantCulture`.

`UIValidationHelper.EnsureValidMetrologyInputAsync()`
→ `IInputFieldAccessor.ValidatePoints()`
→ `InputField.ValidatePoints()`
→ оба `PointInput.Validate()` без раннего выхода
→ `IInputFieldAccessor.ValidateElectricalParameters()`
→ активные `ElectricalInput.Validate()` без раннего выхода
→ `IInputFieldAccessor.ValidateTimeParameters()`
→ активные `TimeInput.Validate()` без раннего выхода
→ каждый невалидный control самостоятельно включает визуальное состояние ошибки
→ `InputValidationResult.Errors`
→ каждая ошибка передаётся в `IMessageOutputService`.

После протоколирования `UIValidationHelper` выбрасывает ожидаемый
`InputValidationException`. `ActionExecutor.ExecuteTaskAsync()` обрабатывает его
отдельно без `LogException`, поэтому ошибки пользовательского ввода не создают
crash packages. Остальные исключения сохраняют прежний аварийный путь.

Проверки существования оборудования и уникальности двух точек остаются в Engine.

Для девяти режимов `Ask.Engine.Tests.Metrology.Mode*` в
`EnsureValidMetrologyInputAsync(..., metrologyMode: ...)` после успешной проверки
формируется стартовый блок протокола:
`Запуск "{IInputFieldProvider.GetExecutionTitle()}"` → первая и вторая точки →
заданное значение с единицей из `CommandDisplayInfo` → только активные дополнительные
поля (время выполнения, время нарастания, напряжение, шина или группа шин).
`ProtocolUI.GetExecutionTitle()` возвращает фактический локализованный `Header`
открытого режима (например, `Режим КС`) с маршалингом в UI-поток.
`UIValidationHelper.BuildMetrologyInputMessages()` формирует строки, затем
`ShowMetrologyInputAsync()` выводит их через `IMessageOutputService` до первого
`Mode*.ConnectToEquipment()`.

Тот же стартовый блок включён для инженерных тестов СИ/ПИ (метод узла и групповой),
перекрёстного теста МКР и проверки сопротивления коммутатора. После успешной
валидации `IInputFieldProvider.SetExecutionInputParameters()` сохраняет фактически
выведенные строки в `ActionSettings.InputParameters`. При завершении теста
`InspectionProtocolBuilder.Build()` вставляет раздел `Введённые данные` перед
`Заключением`; потоковый и итоговый протокол используют один набор значений.
Обе темы `Ask.UI/Resources/Assets/SyntaxHighlighting/{Dark,Light}/MKI_RESULT_PROTOCOL.xshd`
подсвечивают заголовок раздела и все формируемые названия входных параметров цветом
`ProtocolMain`, а единицы `Ом/кОм/МОм/ГОм`, `В/мВ/кВ`, `А/мА`,
`пФ/нФ/мкФ` и `с` — цветом `MeasurementUnit`.

### Translation and validation

Typed `ErrorItem`/`WarningItem` originate from parsers, post-analyzers and
`Ask.Core.Services.Errors.Translation`. They retain source/formatted line metadata
and are displayed in translator/runner error lists.

### Equipment error flow

```text
raw manager/protocol failure
→ false/empty response or exception
→ or selected-device IdleHardwareErrorSimulator failure with the same method contract
→ application adapter / MeasurementBase
→ UserActionHelper.GetRunWithUserRepeatAsync
→ IUserInteractionService.WaitUserActionAsync
→ ProtocolUI shows Repeat/Finish
→ Retry invokes the same captured equipment delegate through the same adapter
→ successful retry keeps the dialog and enables Repeat/Continue/Finish
→ Continue returns the last actual typed result without another equipment call
→ Finish raises cancellation into the normal ActionExecutor finalization path
→ executor catches only when local recovery is defined
→ otherwise CommandExecutionManager emergency КЦ
→ ActionExecutor finalization
```

`UserActionHelper` is the central reusable retry/user-decision mechanism.
An initial successful call has no additional UI. Hardware failure always waits
for the operator regardless of `ExecutionConfig.StopOnError`; `Continue` is
available only after the latest attempt produced a valid equipment response.
Retries are unlimited, and each retry passes through the original adapter,
logging, protocol output and driver chain.

Measurement verdicts use a separate branch:

```text
valid measurement response
→ Engine range/tolerance comparison
→ in range: continue without UI
→ out of range + StopOnError OFF: retain failure result and continue
→ out of range + StopOnError ON: Repeat/Continue/Finish
→ Retry hardware failure: Repeat/Finish
→ next valid measurement response: Repeat/Continue/Finish
```

Once measurement interaction has opened, a valid retry stays interactive even
when its value is now in range. `EquipmentExecutionContext` suppresses all
Retry/Continue/Finish requests during emergency `КЦ` and `ExecutionFinalizer`;
errors there are logged and the remaining mandatory actions continue.
`DeviceMessageBuilder` controls device result output using `DeviceDisplayConfig`.
Typed factories are grouped in `Ask.Core/Services/Errors/Device/`.

### Execution errors

Executors add logical failures to `ProtocolModel.Errors` and call
`CommandExecutionManager.AddErrorMethod` for UI issues. Exceptions escape to
`CommandExecutionManager`, trigger emergency `КЦ`, then reach `ActionExecutor`.
Cancellation/step interruption is recognized separately to avoid misleading error
text.

### Database errors

Provider CRUD services throw DB/duplicate exceptions from
`Ask.Core/Services/Errors/DataBase`. Startup initialization logs and rethrows
inside Provider; shell startup wrapper logs and returns `null`. Corrupt SQLite is
moved to `.damaged_<timestamp>` before recreation.

### Global errors

`App` registers WPF dispatcher, AppDomain and TaskScheduler handlers. Crash package
creation has a 30-second timeout and falls back to a plain log file if diagnostics
services are unavailable.

## Events and Callbacks

Central bus:

```text
*EventAdapter.Raise...
→ EventAggregator.Publish<TEvent>
→ static subscriber list
→ MainWindow event binder / UI control / state manager handler
```

Architecturally significant flows:

- `SystemStateEventAdapter.PowerChanged/LockedChanged/ControlProgramActiveChanged`
  → `SystemStateManager` and `StateEventsBinder` → buttons, menu and shell lock;
- `ExecutionEventAdapter.StepByStepModeChanged`
  → `ActionExecutor.StepMode`;
- breakpoint adapters → `RunControl` model/editor synchronization;
- `CommandDrawerEventAdapter` request/result → paused command selection;
- editor/file adapters → `UiEventsBinder`, workspace opening/closing and external
  file activation;
- `ExecutionConfig.IdleModeChange` → `StateEventsBinder.OnIdleModeChange`;
- `ProtocolConfig.SaveProtocolAsyncEvent` and other config save events
  → DB static settings facades;
- успешный CRUD в `DeviceRuntime` или импорт в `DeviceConfigurationService`
  → `DeviceConfigurationEvents.Changed`
  → динамическое перестроение карточек оборудования в `ExecutionControl`;
- `LoggerUtility.ExceptionLogged`/callback → `IExceptionDiagnosticReporter`;
- `Transport.IsReset` → local state reset for points and buses of the addressed device;
- `ActionExecutor.StartProcessing` → execution-state consumers;
- `ThemeSettings.ThemeChanged`/`LanguageSettings.LanguageChanged` → UI refresh.
- `RoleAuthorizationConfig.SetCurrentRole/Clear`
  → `DebugAccessConfig.NotifyCurrentRoleChanged`
  → `SystemStateEvents.DebugRightsChanged`
  → DEBUG-колонки уже открытых ErrorList controls.

`ApplicationLifecycleManager` constructs `SystemEventsBinder`, `UiEventsBinder`
and `StateEventsBinder`, then calls `ApplicationEventsBinder.BindAll`.

## Background Operations

| Mechanism | Start | Work | Stop/lifetime |
| --- | --- | --- | --- |
| Startup initialization | `App.OnStartup` `Task.Run` | DB, host, help startup in parallel with login | awaited before main UI |
| Single-instance pipe server | `SingleInstanceManager.CheckOrSignal` | accept ACTIVATE/OPENFILE requests in loop | process lifetime |
| Application clock | `ApplicationClockService.Start` | `System.Threading.Timer` publishes time state | `App.OnExit → Stop` |
| Host/diagnostic bridge | `AppHost.StartAsync` | connects static command history to service | host/process lifetime |
| Initial chassis lookup | `PreStartupInitializer` fire-and-forget Task | warms first chassis/tester access | one-shot, exceptions caught |
| Execution session | `ActionExecutor.ExecuteTaskAsync` | `Task.Run(StartDelegate)` with cancellation | `FinalizeAsync`/`StopAsync` cancels and disposes session |
| Device protocol waits | COM/TCP/UDP queries | semaphore-protected I/O and timeout polling | per-call cancellation/timeout |
| Help server | `HelpServer.EnsureStarted` | Kestrel static-file host | `App.OnExit → HelpServer.Stop` |
| Archive refresh | `ArchiveControl` DispatcherTimer | refresh archive lists plus background I/O | view lifetime |
| Role keyboard layout | `RoleLoginWindow` DispatcherTimer | keyboard layout monitoring | window lifetime |
| Workspace click timer | `MultiEditorControl` DispatcherTimer | double-click discrimination | control lifetime |
| Logged exception reporter | `ExceptionDiagnosticReporter` bounded Task.Run | asynchronous crash package | throttled/timeout-limited |

Long-running loops in metrology/GPT measurement helpers are bounded by
cancellation, timers or device conditions; inspect the concrete mode before
changing stop semantics.

## Database Architecture

Provider: EF Core 9 + SQLite. Database path is
`AppContext.BaseDirectory/Resources/app.db` via internal `DbPathResolver`.

`AppDbContext` partials:

- `Ask.DataBase.Provider/Context/AppDbContext.Device.cs`: chassis, legacy profiles, relay modules, sources,
  switching devices, meters, breakdown testers, racks, UPS;
- `Ask.DataBase.Provider/Context/AppDbContext.Settings.cs`: protocol, execution, hotkeys, UI and device-display
  settings.

Data path:

```text
UI/Engine
→ Ask.DataBase.Engine static facade or settings facade
→ DeviceEngine / provider DTO service
→ CrudService<TDto>
→ new AppDbContext
→ DbSet<TDto>
→ SQLite Resources/app.db
```

Migrations live in `Ask.DataBase.Provider/Migrations/`.
`DatabaseInitializationService` also contains explicit compatibility DDL for old
schemas; a migration change must account for both normal migration and supported
legacy adoption behavior.

Runtime device cache uses `(requested interface, Id)` and query caches for
GetAll/chassis lists. Create/update/delete invalidate relevant query caches;
ordinary update preserves runtime object identity and applies DTO in-place,
while a `DeviceClass` change replaces the runtime object. Startup clears and
warms the caches.

## Configuration

| Runtime config | Persisted DTO/table | Load/save bridge | Major consumers |
| --- | --- | --- | --- |
| `ExecutionConfig` | `SettingsExecutionDto` / `Execution` | `ExecutionSettings`, `MainWindow.Init.DatabaseInitializer` | ActionExecutor, Engine, all device idle gates; independent measurement/hardware Idle error settings |
| `ProtocolConfig` | `SettingsProtocolDto` / `SettingsProtocol` | `ProtocolSettings` | protocol templates, output visibility, print |
| `UserInterfaceConfig` | `UserInterfaceDto` / `UserInterface` | `UserInterfaceSettings` | MainWindow, theme/menu UI |
| `DeviceDisplayConfig` | `DeviceDisplaySettingsDto` | `DeviceDisplaySettings` | adapters and device messages |
| `ThemeSettings` | value inside UI config | startup/UI save flow | resources and shell |
| `LanguageSettings` | application settings/resources | startup | localization |
| `RoleAuthorizationConfig` | role/credential files | login/configurator | current session role, menu/archive permissions and Debug derivation |
| `DebugAccessConfig` | derived session state | `RoleAuthorizationConfig.CurrentRole` | protocol debug source and ErrorList DEBUG-column visibility |
| `LegacyMkiConfig` | legacy hardware profile/config file + DB storage | LegacyMki services | compatibility execution |

Config managers are static global state. Their save events are subscribed once in
startup; changes from settings controls update the static model and asynchronously
write through to SQLite.

## Shared Contracts and DTO

Main contract groups:

- `Shared/Interfaces/DeviceInterfaces` — `IDevice`, `IConnectable`,
  `IDeviceProtocol`, equipment interfaces and capability interfaces;
- `Shared/Interfaces/UiInterfaces` — interaction, protocol output, input/highlight
  abstractions used by Engine without concrete controls;
- `Shared/Interfaces/ExecutionInterfaces` — execution/pause/run contracts;
- `Shared/Metadata/View/EditorHost` — editor/workspace/run/translation service
  boundaries between `MainWindow` and `UI`;
- `Shared/DTO/Devices` — EF entities, device materialization data and common
  measurement parameters (`Measurements/MeasurementRange`);
- `Shared/DTO/Settings` — persisted configuration;
- `Shared/DTO/Protocol` — `ProtocolModel`, `ShowMessageModel` and action settings;
- `ControlCommandAnalyser/Model` — parsed command models passed from translation
  into execution.

Important data flows:

```text
Settings DTO → static Config → Engine/device branch
Device DTO → DeviceFactory/Mapper/Composer → IDevice runtime object
source text → BaseCommandModel list → CommandExecutionContext → executor
ShowMessageModel → ProtocolUI → history files / inspection builder
ErrorItem → translator/runner ErrorList
```

## Key Types Index

| Type | Kind | Project | Responsibility | Section |
| --- | --- | --- | --- | --- |
| `App` | WPF application | MainWindowProgram | process startup/global failure handling | [Entry Points](#entry-points) |
| `PreStartupInitializer` | bootstrapper | MainWindowProgram | DB, host, DI, help startup | [Dependency Injection](#dependency-injection) |
| `AppServices` | manual composition | MainWindowProgram | shell services/ViewModels | [Dependency Injection](#manual-composition) |
| `MainWindow` | View/shell | MainWindowProgram | top-level UI and lifecycle | [UI Architecture](#ui-architecture) |
| `MultiEditorControl` | workspace View | UI | editors/tabs/user controls | [UI Architecture](#ui-architecture) |
| `FileManager` | service composer | UI | workspace services | [UI Architecture](#ui-architecture) |
| `RunControl` | execution View | UI | launches control programs | [Execution Engine](#execution-engine) |
| `ProtocolUI` | View + adapter | Ask.UI | execution controller and protocol output | [Protocols](#protocols-and-file-formats) |
| `CommandMessages` | static facade | Ask.Protocol.Messages | проверяет настройки видимости этапов, формирует и выводит начало программы контроля, сообщения команд, точек останова, блоков проверки, цепей, точек, подключения точек, направления диода и разрядов; модели наружу не возвращает | [Protocols](#protocols-and-file-formats) |
| `CommandMessageBuilder` | internal static builder | Ask.Protocol.Messages | содержит перенесённую из `ExecutorMessageBuilder` логику начала программы контроля, сообщений команд, точек останова и блоков проверки | [Protocols](#protocols-and-file-formats) |
| `CommandMessagePublisher` | internal static publisher | Ask.Protocol.Messages | передаёт сформированные сообщения команд в `IMessageOutputService` с настройками блока, паузы, пошагового режима и метаданными исходного вызова | [Protocols](#protocols-and-file-formats) |
| `MessagePublisher` | internal static publisher | Ask.Protocol.Messages | единообразно добавляет метаданные исходного вызова, при необходимости пишет сообщение в device log и передаёт его `IMessageOutputService`; категорийные publishers задают только свою политику | [Protocols](#protocols-and-file-formats) |
| `EquipmentMessages` | static facade | Ask.Protocol.Messages | публично формирует, логирует и выводит результаты операций оборудования | [Protocols](#protocols-and-file-formats) |
| `EquipmentMessageBuilder` | internal static builder | Ask.Protocol.Messages | формирует результаты подключения, отключения, инициализации, настройки, сброса и заголовок самоконтроля оборудования | [Protocols](#protocols-and-file-formats) |
| `EquipmentMessagePublisher` | internal static publisher | Ask.Protocol.Messages | записывает сообщения оборудования в device log и передаёт их `IMessageOutputService` | [Protocols](#protocols-and-file-formats) |
| `SelfTestMessages` | static facade | Ask.Protocol.Messages | публикует этапы, команды пошагового режима, ошибки и результаты самоконтроля мультиметра, GPT, МКР, УКШ и модуля напряжения/тока; runtime SelfCheck-классы моделей экранного протокола не создают | [Equipment](#equipment-architecture) |
| `SelfTestMessageBuilder` | internal static builder | Ask.Protocol.Messages | формирует информационные, командные и результирующие сообщения самоконтроля, включая видимость измерений, Overload, погрешность и свойства итогового протокола | [Equipment](#equipment-architecture) |
| `SelfTestMessagePublisher` | internal static publisher | Ask.Protocol.Messages | передаёт сообщения самоконтроля общему `MessagePublisher` с признаками блока, паузы и проверки доступности вывода | [Equipment](#equipment-architecture) |
| `MeasurementMessages` | static facade | Ask.Protocol.Messages | формирует модели для накопления результатов и публикует начало измерения, этап измерений, ток утечки PI, эталонное значение, ошибки подключения точек, выдачу испытательного напряжения PI ACW/DCW, готовые сообщения измерений, итоговые и промежуточные результаты и погрешности | [Protocols](#protocols-and-file-formats) |
| `MeasurementMessageBuilder` | internal static builder | Ask.Protocol.Messages | формирует заголовки измерений, эталонные значения, ошибки подключения точек, переход к методу полного узла, единый формат диапазона, измеренное значение, погрешность, `ПРОБОЙ` и `Overload` | [Protocols](#protocols-and-file-formats) |
| `MeasurementFailureMessageBuilder` | internal static builder | Ask.Protocol.Messages | формирует описания брака для точек и разрядов узлового и группового методов | [Protocols](#protocols-and-file-formats) |
| `MeasurementLimitKind` | enum | Ask.Protocol.Messages | контракт из `Ask.Protocol.Messages/Models/`, задающий минимальный или максимальный предел при формировании описания брака | [Protocols](#protocols-and-file-formats) |
| `MeasurementMessagePublisher` | internal static publisher | Ask.Protocol.Messages | записывает опубликованные измерения в device log и передаёт их `IMessageOutputService` | [Protocols](#protocols-and-file-formats) |
| `MetrologyMessages` | static facade | Ask.Protocol.Messages | публикует сводку максимальной отрицательной и положительной погрешности метрологического режима | [Protocols](#protocols-and-file-formats) |
| `MetrologyMessageBuilder` | internal static builder | Ask.Protocol.Messages | формирует заголовок сводки режима и сообщения о предельных погрешностях | [Protocols](#protocols-and-file-formats) |
| `MetrologyMessagePublisher` | internal static publisher | Ask.Protocol.Messages | передаёт метрологические сводки в `IMessageOutputService` с метаданными исходного вызова | [Protocols](#protocols-and-file-formats) |
| `MeasurementResultEvaluator` | internal static evaluator | Ask.Engine | применяет Idle-симуляцию и проверяет измеренное значение по границам либо ожидаемой перегрузке до передачи результата в `MeasurementMessages` | [Execution Engine](#execution-engine) |
| `AlgorithmExecutionResult` | result container | Ask.Protocol.Messages | контракт из `Ask.Protocol.Messages/Models/`, хранящий накопленные ошибки и информационные `ShowMessageModel` алгоритма | [Execution Engine](#execution-engine) |
| `ProtocolModelExtensions` | static extensions | Ask.Protocol.Messages | расширение из namespace `Ask.Protocol.Messages.Extensions`, добавляющее единый `AlgorithmExecutionResult` в коллекции ошибок и информационных сообщений `ProtocolModel` | [Execution Engine](#execution-engine) |
| `ExecutionMessages` | static facade | Ask.Protocol.Messages | проверяет видимость параметров выполнения и коммутации, публикует накопленные результаты проверки, ошибки, debug-сообщения, задержки, этапы анализа цепей и локализации, границы этапов, инициализацию, настройку оборудования и коммутацию; формирует только накапливаемую ошибку локализации | [Protocols](#protocols-and-file-formats) |
| `ExecutionMessageBuilder` | internal static builder | Ask.Protocol.Messages | содержит заголовок накопленных результатов, ошибки и задержки выполнения, сообщения подготовки, настройки и коммутации устройств, подключения диапазонов, сброса точек, этапов и запуска теста | [Protocols](#protocols-and-file-formats) |
| `ExecutionMessagePublisher` | internal static publisher | Ask.Protocol.Messages | передаёт сообщения этапов выполнения в `IMessageOutputService`, сохраняет признаки начала блока, обхода паузы/пошагового режима и метаданные исходного вызова | [Protocols](#protocols-and-file-formats) |
| `ValidationMessages` | static facade | Ask.Protocol.Messages | публикует ошибки полей ввода, поиска и конфигурации оборудования, зависимости самоконтроля, а также заголовок запуска и введённые параметры проверки | [Protocols](#protocols-and-file-formats) |
| `ValidationMessageBuilder` | internal static builder | Ask.Protocol.Messages | формирует ошибки данных, поиска и конфигурации оборудования, сообщения о зависимостях самоконтроля и представление введённых параметров запуска | [Protocols](#protocols-and-file-formats) |
| `ValidationMessagePublisher` | internal static publisher | Ask.Protocol.Messages | передаёт ошибки в `IMessageOutputService`, поддерживает обход паузы/пошагового режима и добавляет метаданные исходного вызова | [Protocols](#protocols-and-file-formats) |
| `RangeMessages` | static facade | Ask.Protocol.Messages | принимает `MeasurementRange` и типизированную единицу, публикует допустимый диапазон независимо от вызывающей подсистемы | [Protocols](#protocols-and-file-formats) |
| `RangeMessageBuilder` | internal static builder | Ask.Protocol.Messages | формирует единый текст допустимого диапазона значений | [Protocols](#protocols-and-file-formats) |
| `RangeMessagePublisher` | internal static publisher | Ask.Protocol.Messages | записывает сообщения о диапазонах в device log и передаёт их `IMessageOutputService` | [Protocols](#protocols-and-file-formats) |
| `ActionExecutor` | orchestrator | Ask.UI | run/pause/stop/finalize | [Execution Engine](#execution-engine) |
| `ExecutionFinalizer` | coordinator | Ask.UI | mandatory cleanup, reset, output and protocol completion | [Execution Engine](#execution-engine) |
| `CommandTranslationManager` | parser orchestrator | Ask.Engine | reflection parser/formatter pipeline | [Translation](#translation-and-command-language) |
| `CommandExecutionManager` | orchestrator | Ask.Engine | sequential command execution | [Execution Engine](#execution-engine) |
| `ICommandExecutor` | interface | Ask.Engine | executable mnemonic contract | [Execution Engine](#execution-engine) |
| `CommandExecutorRegistry` | reflection registry | Ask.Engine | mnemonic→executor | [Execution Engine](#execution-engine) |
| `EquipmentService` | static coordinator | Ask.Engine | equipment validation/runtime selection | [Equipment](#equipment-architecture) |
| `BaseMeasurement` | template base | Ask.Engine | metrology lifecycle | [Metrology](#metrology-and-hardware-tests) |
| `IDevice` | interface | Ask.Core | root device contract | [Equipment](#equipment-architecture) |
| `IUserInteractionService` | interface | Ask.Core | Engine↔UI interaction | [Shared Contracts](#shared-contracts-and-dto) |
| `UserActionHelper` | static coordinator | Ask.Core | typed equipment retry/continue/finish loop | [Error Handling](#equipment-error-flow) |
| `DeviceResetService` | static coordinator | Ask.Core | sequential addressed reset of devices used by a test | [Execution Engine](#addressed-reset-of-test-equipment) |
| `EquipmentUsageTracker` | async execution context | Ask.Core | execution-scoped registration of actually addressed devices | [Execution Engine](#addressed-reset-of-test-equipment) |
| `EquipmentUsageSession` | execution state | Ask.Core | ordered unique snapshot of equipment used by one run | [Execution Engine](#addressed-reset-of-test-equipment) |
| `EquipmentTrackingConnectable` | decorator | Ask.Device.Application | registers device usage before connection lifecycle operations | [Execution Engine](#addressed-reset-of-test-equipment) |
| `EquipmentExecutionContext` | async context | Ask.Core | suppresses interactive retry during mandatory finalization | [Error Handling](#equipment-error-flow) |
| `ExecutionConfig` | static config | Ask.Core | execution/idle state | [Configuration](#configuration) |
| `RoleAuthorizationConfig` | static session state | Ask.Core | current successfully authenticated role | [Authentication/Debug](#authentication-and-debug-access-flow) |
| `DebugAccessConfig` | derived access state | Ask.Core | central root-only Debug availability and change notification | [Authentication/Debug](#authentication-and-debug-access-flow) |
| `IdleHardwareErrorSimulator` | static decision service | Ask.Core | 100% failure decision for non-measurement Idle calls of an individually selected device | [Real / Idle](#real--idle) |
| `EventAggregator` | event bus | Ask.Core | in-process publish/subscribe | [Events](#events-and-callbacks) |
| `DeviceApplicationComposer` | composer | Ask.Device.Application | replaces raw managers with adapters | [Equipment](#adapters-and-error-boundary) |
| `DeviceProtocolEmulator` | public static factory | Ask.Device.Emulator | returns Real/Idle-selecting protocols for chassis and МКР | [Equipment](#real--idle) |
| `ChassisQueryExecutor` | runtime helper | Ask.Device.Runtime | routes and logs chassis commands through the real protocol or emulator | [Equipment](#real--idle) |
| `ModuleRelayControlQueryExecutor` | runtime helper | Ask.Device.Runtime | routes and logs МКР commands through the real protocol or emulator | [Equipment](#real--idle) |
| `AdapterMeasurementExecutor` | helper | Ask.Device.Application | measured operation retry/logging | [Error Handling](#equipment-error-flow) |
| `ModuleRelayControl` | device | Ask.Device.Runtime | МКР implementation | [Equipment](#device-matrix) |
| `DeviceBusCommutation` | device | Ask.Device.Runtime | switching device implementation | [Equipment](#device-matrix) |
| `DeviceBusCommutationQueryExecutor` | runtime helper | Ask.Device.Runtime | routes and logs УКШ commands through the real protocol or Idle emulator | [Equipment](#real--idle) |
| `MultimeterEmulatorProtocol` | Idle protocol | Ask.Device.Emulator | returns SCPI responses for Keysight/B7-78/3; selected by `DeviceProtocolEmulator.QueryMultimeterAsync` | [Equipment](#device-matrix) |
| `BreakdownTesterCommandProtocol` | Real/Idle protocol router | Ask.Device.Emulator | logs every GPT79904 command/response and selects COM or Idle protocol | [Equipment](#device-matrix) |
| `BreakdownTesterEmulatorProtocol` | stateful Idle protocol | Ask.Device.Emulator | emulates GPT79904 SCPI identification, configuration, test state and measurement responses | [Equipment](#device-matrix) |
| `KeysightDevice` | device | Ask.Device.Runtime | TCP multimeter | [Equipment](#device-matrix) |
| `MultimeterB7783` | device | Ask.Device.Runtime | USB multimeter | [Equipment](#device-matrix) |
| `GPT79904` | device | Ask.Device.Runtime | COM breakdown tester | [Equipment](#device-matrix) |
| `DeviceRuntime` | static facade | Ask.DataBase.Engine | shared runtime device engine entry | [Database](#database-architecture) |
| `DeviceEngine` | service/cache | Ask.DataBase.Engine | DTO queries and runtime identity | [Device persistence](#equipment-resolution-and-device-persistence) |
| `DeviceFactory` | reflection factory | Ask.DataBase.Engine | `DeviceClass`→runtime type | [Device persistence](#device-materialization-flow) |
| `AppDbContext` | EF DbContext | Ask.DataBase.Provider | SQLite model | [Database](#database-architecture) |
| `DatabaseInitializationService` | initializer | Ask.DataBase.Provider | integrity, schema and seed | [Database](#database-architecture) |
| `CrashPackageService` | service | Ask.Diagnostics | diagnostic package collection | [Support](#support-and-diagnostics) |
| `IExceptionDiagnosticReporter` | interface | Ask.Diagnostics | automatic and awaited exception reporting with duplicate suppression | [Support](#support-and-diagnostics) |
| `CrashReportArtifactCollector` | collector | Ask.Diagnostics | contextual JSON/text files supplied by an error source | [Support](#support-and-diagnostics) |
| `HelpServer` | hosted service facade | Ask.Support | local documentation server | [Support](#support-and-diagnostics) |

## Maintenance Checklist

После архитектурно значимой задачи:

1. Сверить затронутый раздел с production-кодом.
2. Обновить Quick Navigation, если изменилась точка входа.
3. Обновить project tree при изменении `.csproj`.
4. Обновить DI table при изменении registrations/lifetimes.
5. Обновить call chain, Real/Idle, error/event/background flow по факту.
6. Проверить относительные paths и внутренние anchors.
7. Не переписывать незатронутые разделы.

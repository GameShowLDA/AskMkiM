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
| Холостой режим и симуляция ошибок | `Ask.Core/Services/Config/AppSettings/ExecutionConfig.cs`, `IdleHardwareErrorSimulator.cs` | `UI/Controls/Settings/Execution/ExecutionControl.xaml`, целевой manager/adapter в `Ask.Device.*`, конкретный executor/strategy |
| Ошибка оборудования и интерактивный повтор | `Ask.Core/Services/UI/UserActionHelper.cs` | `Ask.Core/Services/UI/EquipmentExecutionContext.cs`, `Ask.UI/Controls/ProtocolNew/ProtocolUI.cs`, целевой adapter/manager/transport |
| МКР и точки | `Ask.Core/Shared/Interfaces/DeviceInterfaces/RelaySwitchModule/` | `Ask.Device.Application/FunctionAdapters/ModuleRelayControl/`, `Ask.Device.Runtime/Function/ModuleRelayControl/` |
| Устройство коммутации | `Ask.Core/Shared/Interfaces/DeviceInterfaces/SwitchingDevice/` | `Ask.Device.Application/FunctionAdapters/DeviceBusCommutation/`, `Ask.Device.Runtime/Function/DeviceBusCommutation/` |
| Быстрый мультиметр | `Ask.Core/Shared/Interfaces/DeviceInterfaces/Multimeter/` | `Ask.Device.Runtime/Device/KeysightDevice.cs`, `Ask.Device.Runtime/Device/MultimeterB7783.cs`, `Ask.Device.Runtime/Function/Base/Multimeter/` |
| Пробойная установка GPT | `Ask.Core/Shared/Interfaces/DeviceInterfaces/BreakdownTester/` | `Ask.Device.Application/FunctionAdapters/GPT/`, `Ask.Device.Runtime/Function/GPT/`, `Ask.Device.Runtime/Device/GPT79904.cs` |
| Источник напряжения/тока | `Ask.Core/Shared/Interfaces/DeviceInterfaces/PowerSourceModule/` | `Ask.Device.Application/FunctionAdapters/ModuleVoltageCurrent/`, `Ask.Device.Runtime/Function/ModuleVoltageCurrentSource/` |
| Шасси и питание | `Ask.Core/Shared/Interfaces/DeviceInterfaces/Chassis/` | `Ask.Device.Runtime/Device/ManagerChassis.cs`, `Ask.Device.Runtime/Function/ManagerChassis/`, `UI/Components/PowerButton.xaml.cs` |
| UPS | `Ask.Core/Shared/Interfaces/DeviceInterfaces/UninterruptiblePowerSupply/` | `Ask.Device.Application/FunctionAdapters/MikUps1101rRm/`, `Ask.Device.Runtime/Function/MikUps1101rRm/` |
| COM | `Ask.Device.Runtime/Base/Device/DeviceWithCOM.cs` | `Ask.Device.Communication/Com/Protocols/ComProtocol.cs`, `Ask.Device.Communication/Com/Configuration/SerialPortCustom.cs` |
| TCP/UDP/USB | `Ask.Device.Runtime/Base/Device/` | `Ask.Device.Communication/Ethernet/`, `Ask.Device.Communication/Usb/`, runtime `Ask.Device.Runtime/Function/Base/Connected/` |
| Конфигурация устройств | `UI/Controls/Settings/DeviceConfig/` | `Ask.DataBase.Engine/Static/Devices/`, `Ask.DataBase.Engine/Services/DeviceEngine.cs`, `Ask.DataBase.Provider/Services/Devices/` |
| База данных | `Ask.DataBase.Provider/Context/AppDbContext*.cs` | `Ask.DataBase.Provider/Initialization/DatabaseInitializationService.cs`, `Ask.DataBase.Engine/Services/DeviceEngine.cs` |
| Настройки выполнения/протокола/UI | `Ask.Core/Services/Config/` | `Ask.DataBase.Engine/Static/Settings/`, `Ask.DataBase.Provider/Services/Settings/`, `MainWindow/Init/DatabaseInitializer.cs` |
| Протокол выполнения | `Ask.UI/Controls/ProtocolNew/ProtocolUI*.cs` | `Ask.UI/Features/ProtocolNew/Protocol/`, `Ask.Core/Services/Protocols/ExecutionProtocolHistoryService.cs` |
| Форматы `.asktrace/.askresult/.askreport` | `Ask.Core/Services/Protocols/ExecutionProtocolHistoryService.cs` | `Ask.Core/Shared/Metadata/Static/ProtocolFileExtensions.cs`, `Ask.UI/Features/ProtocolNew/Protocol/ProtocolStorageService.cs` |
| Печать протокола | `Ask.UI/Features/ProtocolNew/Protocol/ProtocolCompletionService.cs` | `Ask.UI/Features/ProtocolNew/Execution/ExecutionFinalizer.cs`, `Ask.Core/Services/Config/AppSettings/ProtocolConfig.cs`, `PrintUtility` usages |
| Метрология | `MainWindow/Services/MetrologyService.cs` | `Ask.Core/Services/Metrology/MetrologyControlFactory.cs`, `Ask.UI/Controls/ExecutorControls/MetrologyControls/`, `Ask.Engine/Tests/Metrology/` |
| Самоконтроль и инженерные тесты | `MainWindow/Services/TestService.cs`, `MainWindow/Services/SelfTestServices.cs` | `Ask.UI/Controls/ExecutorControls/TestsControls/`, `Ask.Engine/Tests/` |
| Ошибки трансляции | `Ask.Core/Services/Errors/Translation/` | целевой parser/validator, `Ask.UI/Controls/ErrorList/`, `UI/Controls/ErrorList/` |
| Crash reports | `MainWindow/App.xaml.cs`, `MainWindow/Init/PreStartupInitializer.cs` | `Ask.Diagnostics/Services/CrashPackageService.cs`, `Ask.Diagnostics/Collectors/` |
| Архивы APK/APKW | `Ask.UI/Features/Archive/` | `Ask.Core/Services/FileFormats/Apk/`, `MainWindow/Services/Conversion/` |
| Рабочее пространство и вкладки | `UI/Components/MultiEditorControl.xaml.cs` | `UI/Components/MultiEditorMethods/FileManager.cs`, `UI/Services/`, `MainWindow/Services/MultiWindowService.cs` |
| Роли и права | `MainWindow/Init/RoleApplicationConfigurator.cs` | `Ask.Core/Services/Config/AppSettings/RoleAuthorizationConfig.cs`, `Ask.UI/Features/RoleManagement/` |
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

## Solution Structure

Все production-проекты используют `net8.0-windows`. `MainWindowProgram` — единственный
основной `WinExe`; остальные перечисленные проекты — библиотеки. Тестовые проекты,
ручные harness-приложения и `MethodDependencyExplorer` в runtime-карту не входят.

| Проект | Путь | Назначение и основные namespaces | Прямые project references |
| --- | --- | --- | --- |
| `MainWindowProgram` | `MainWindow/MainWindowProgram.csproj` | WPF entry point, shell, startup, ручная композиция UI; `MainWindowProgram.*` | `Ask.Diagnostics`, `Ask.DataBase.Engine`, `Ask.Support`, `Ask.UI`, `ConsoleUI`, `Message`, `UI` |
| `UI` | `UI/UI.csproj` | Legacy WPF workspace, editor, runner, settings, protocol/file services; `UI.*` | `Ask.Core`, `Ask.DataBase.Provider`, `Ask.Engine`, `Ask.Support`, `Ask.UI`, `Message`, `Ask.Device.Runtime` |
| `Ask.UI` | `Ask.UI/Ask.UI.csproj` | Новые reusable WPF features: protocol, archive, notifications, role UI, executor controls; `Ask.UI.*` | `Ask.Core`, `Ask.Engine`, `Ask.Support`, `Message`, `Ask.Device.Runtime` |
| `Ask.Engine` | `Ask.Engine/Ask.Engine.csproj` | Parser/formatter, command execution, strategies, metrology and hardware-test algorithms; `Ask.Engine.*` | `Ask.Core`, `Ask.DataBase.Engine`, `Ask.LogLib`, `Message` |
| `Ask.Core` | `Ask.Core/Ask.Core.csproj` | Shared contracts, DTO, enums, events, config state, errors, file formats; `Ask.Core.*` | `Ask.LogLib` |
| `Ask.Device.Application` | `Ask.Device.Application/Ask.Device.Application.csproj` | Application adapters/decorators over raw device managers, retry and user-facing error conversion; `Ask.Device.Application.*` | `Ask.Core`, `Ask.LogLib`, `Ask.Device.Runtime` |
| `Ask.Device.Runtime` | `Ask.Device.Runtime/Ask.Device.Runtime.csproj` | Concrete devices, low-level managers, device command generation and transports; `Ask.Device.Runtime.*` | `Ask.Core`, `Ask.Device.Communication` |
| `Ask.Device.Communication` | `Ask.Device.Communication/Ask.Device.Communication.csproj` | COM/TCP/UDP/USB protocol implementations; `Ask.Device.Communication.*` | `Ask.Core`, `Ask.Diagnostics`, `Ask.LogLib` |
| `Ask.DataBase.Engine` | `Ask.DataBase.Engine/Ask.DataBase.Engine.csproj` | Runtime device facade, cache, reflection factory, DTO↔device mapping; `Ask.DataBase.Engine.*` | `Ask.Core`, `Ask.Device.Application`, `Ask.DataBase.Provider` |
| `Ask.DataBase.Provider` | `Ask.DataBase.Provider/Ask.DataBase.Provider.csproj` | EF Core/SQLite context, migrations and CRUD services; `Ask.DataBase.Provider.*` | `Ask.Core`, `Ask.LogLib` |
| `Ask.Diagnostics` | `Ask.Diagnostics/Ask.Diagnostics.csproj` | Crash packages, command history, diagnostic collectors; `Ask.Diagnostics.*` | нет |
| `Ask.Support` | `Ask.Support/Ask.Support.csproj` | Local Kestrel help server, Photino help window, WPF help routing; `Ask.Support` | `Ask.LogLib` |
| `ConsoleUI` | `ConsoleUI/ConsoleUI.csproj` | Встроенная сервисная консоль и команды; `ConsoleUI.*` | `Ask.DataBase.Engine` |
| `Message` | `Message/Message.csproj` | Кастомные WPF message boxes; `Message` | нет |
| `Ask.LogLib` | `Ask.LogLib/Ask.LogLib.csproj` | NLog facade and exception event bridge; `Ask.LogLib` | нет |

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
│  │  │        └─ Ask.Device.Communication
│  │  │           ├─ Ask.Diagnostics
│  │  │           └─ Ask.LogLib
│  │  ├─ Ask.Core
│  │  ├─ Ask.LogLib
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
```

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
├─ Ask.Device.Application/     adapters and application composition
├─ Ask.Device.Runtime/         device classes and raw function managers
├─ Ask.Device.Communication/   wire protocols
├─ Ask.DataBase.Engine/        runtime device/data facade
├─ Ask.DataBase.Provider/      EF Core/SQLite provider
├─ Ask.Diagnostics/            crash package feature
├─ Ask.Support/                help server and packaged AppHelp
├─ ConsoleUI/, Message/, Ask.LogLib/
├─ docs/                       maintained documentation and this map
└─ Ask.*.UnitTests/            automated tests, excluded from runtime map
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
| `ICrashDataCollector` | 8 collector implementations | Singleton, multiple | `AddCrashDiagnostics` |
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
→ SelfTestServices / TranslationServices / RunServices
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

- `ConnectedPointChecker` — проверки соединённых цепей;
- `DisconnectionCheckExecutor` выбирает `MethodExecutor`,
  `NodeAccumulationChecker`, `NodeFullChecker` или pairwise strategy;
- `PairwiseFirstPointCheckerAlt` — специальная ЭТ-проверка;
- `FaultChainMeasurementService` — повторное измерение проблемных цепей;
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
→ CommandExecutionManager resumes at selected command
```

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

Для программы контроля итоговый протокол показывается/сохраняется как report, но
автоматическая печать в `ProtocolCompletionService` исключена; ручные print buttons
в `Ask.UI/Controls/ProtocolNew/ProtocolUI.xaml.cs` печатают execution или inspection text.

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
→ exception, screenshot, command history, device state, config, logs,
  system info and metadata
→ CrashReports
→ NotificationHostService
```

`CommandHistoryBridgeHostedService` connects static
`DiagnosticCommandHistory.RecordCommand/RecordResponse` calls from transport
protocols to bounded `CommandHistoryService`.

## Equipment Architecture

### Shared device model

`Ask.Core.Shared.Interfaces.DeviceInterfaces.IDevice` is the root contract.
`DeviceWithIP`, `DeviceWithCOM`, `DeviceWithUSB` implement common state and expose
`IConnectable`, `IDeviceProtocol`, `IConnectionInfo`, `ConnectionDetails`.

There are no separate concrete “Idle device” classes. Real and idle are branches
inside application adapters, runtime managers and transports, all controlled by
the global `ExecutionConfig`.

### Device matrix

| Interface | Runtime implementation | Managers/adapters | Protocol/transport | DB facade |
| --- | --- | --- | --- | --- |
| `IChassisManager` | `ManagerChassis` | runtime `PowerManager`; no application adapter | `Transport` → UDP | `ChassisManagers` |
| `IRelaySwitchModule` | `ModuleRelayControl` | adapters for Point/Bus/Meter; runtime SelfTest | `Transport` → `UdpProtocol` | `RelaySwitchModules` |
| `IPowerSourceModule` | `ModuleVoltageCurrentSource` | adapters for Voltage/Current/Bus; runtime SelfTest | `Transport` → UDP | `PowerSourceModules` |
| `ISwitchingDevice` | `DeviceBusCommutation` | adapters for Connector/Relay/Resistor/Capacitor; runtime SelfTest | `Transport` → UDP | `SwitchingDevices` |
| `IMultimeter` | `KeysightDevice` | runtime measurement profiles/managers | `Transport` → `TcpProtocol:5025` | `FastMeters` |
| `IMultimeter` | `MultimeterB7783` | shared runtime measurement managers | `Transport` → `UsbProtocol` → `UsbCommandHandler` | `FastMeters` |
| `IBreakdownTester` | `GPT79904` | application ACW/DCW/IR/System adapters over runtime managers | `Transport` → `ComProtocol` | `BreakdownTesters` |
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
→ IDeviceProtocol.QueryAsync
→ UdpProtocol.QueryAsync
→ UdpClient.SendAsync/ReceiveAsync
→ BaseResponse.FromJson validation
→ adapter DeviceMessageBuilder or RelayExceptionFactory
```

Representative Keysight measurement:

```text
executor/metrology
→ IMultimeter.ResistanceManager.MeasureResistanceAsync
→ ResistanceMeasurementBase
→ MeasurementBase.MeasureAsync
→ simulated check
→ SetModeBase / RangeBase
→ AdapterMeasurementExecutor
→ MeasurementBase.MeasureCoreAsync
→ TcpProtocol.QueryAsync(profile.Measure)
→ TcpClient/NetworkStream
→ numeric parsing/rounding
→ DeviceMessageBuilder
```

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
- UDP/TCP/COM/USB connectable managers return simulated success or bypass I/O;
- relay/source/switch managers update in-memory state and return success;
- `Simulated.GetSimulatedValue` returns values for measurement paths;
- GPT helpers/managers skip commands or return configured/simulated values;
- specific Engine strategies may suppress physical validation.

Idle error simulation has two independent persisted settings:

```text
ExecutionControl
→ SettingsExecutionDto.IsErrorSimulationMode
→ existing measurement simulation algorithms

ExecutionControl
→ SettingsExecutionDto.IsHardwareErrorSimulationMode
→ ExecutionConfig
→ IdleHardwareErrorSimulator.ShouldSimulateHardwareError
→ Random.Shared.Next(2) == 0
→ non-measurement Idle manager/transport contract
→ existing adapter/UserActionHelper equipment-error flow
```

The nested `Выполнение с ошибками` settings group is visible only while Idle is
enabled. Measurement simulation retains its existing generators, probabilities
and tolerance semantics. Hardware simulation is disabled by default and affects
only Idle initialization/reset, connection, mode/configuration, range,
switching, source and power operations. Every equipment call, including a
`Retry`, makes a new independent `1/2` decision. The simulated failure preserves
the corresponding real contract: `false`, a failed tuple/status, or the
operation-specific exception path. Real execution never enters this mechanism.

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
- protocols: `Ask.Device.Communication/`
- persistence: `Ask.DataBase.Engine/Static/Devices/`, `Ask.DataBase.Provider/Services/Devices/`

## UI Architecture

`MainWindow` is shell and menu host. `MainWindowViewModel` exposes File,
Translation, Run, Metrology, Test, SelfTest, Settings, Admin and Window ViewModels.
Their services generally route operations into `MultiWindowService`.

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

`Ask.UI` contains newer feature-oriented code: ProtocolNew, Archive, Notifications,
RoleManagement, ExecutionSelection and reusable controls. Both UI projects are
active; do not assume one replaces the other.

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

### Translation and validation

Typed `ErrorItem`/`WarningItem` originate from parsers, post-analyzers and
`Ask.Core.Services.Errors.Translation`. They retain source/formatted line metadata
and are displayed in translator/runner error lists.

### Equipment error flow

```text
raw manager/protocol failure
→ false/empty response or exception
→ or IdleHardwareErrorSimulator failure with the same method contract
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
- `LoggerUtility.ExceptionLogged`/callback → `IExceptionDiagnosticReporter`;
- `Transport.IsReset` → local state reset for points and buses of the addressed device;
- `ActionExecutor.StartProcessing` → execution-state consumers;
- `ThemeSettings.ThemeChanged`/`LanguageSettings.LanguageChanged` → UI refresh.

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
GetAll/chassis lists. Create/update/delete invalidate relevant caches; startup
clears and warms them.

## Configuration

| Runtime config | Persisted DTO/table | Load/save bridge | Major consumers |
| --- | --- | --- | --- |
| `ExecutionConfig` | `SettingsExecutionDto` / `Execution` | `ExecutionSettings`, `MainWindow.Init.DatabaseInitializer` | ActionExecutor, Engine, all device idle gates; independent measurement/hardware Idle error settings |
| `ProtocolConfig` | `SettingsProtocolDto` / `SettingsProtocol` | `ProtocolSettings` | protocol templates, output visibility, print |
| `UserInterfaceConfig` | `UserInterfaceDto` / `UserInterface` | `UserInterfaceSettings` | MainWindow, theme/menu UI |
| `DeviceDisplayConfig` | `DeviceDisplaySettingsDto` | `DeviceDisplaySettings` | adapters and device messages |
| `ThemeSettings` | value inside UI config | startup/UI save flow | resources and shell |
| `LanguageSettings` | application settings/resources | startup | localization |
| `RoleAuthorizationConfig` | role/credential files | login/configurator | menu and archive permissions |
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
- `Shared/DTO/Devices` — EF entities and device materialization data;
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
| `IdleHardwareErrorSimulator` | static decision service | Ask.Core | independent `1/2` hardware failure decision for non-measurement Idle calls | [Real / Idle](#real--idle) |
| `EventAggregator` | event bus | Ask.Core | in-process publish/subscribe | [Events](#events-and-callbacks) |
| `DeviceApplicationComposer` | composer | Ask.Device.Application | replaces raw managers with adapters | [Equipment](#adapters-and-error-boundary) |
| `AdapterMeasurementExecutor` | helper | Ask.Device.Application | measured operation retry/logging | [Error Handling](#equipment-error-flow) |
| `ModuleRelayControl` | device | Ask.Device.Runtime | МКР implementation | [Equipment](#device-matrix) |
| `DeviceBusCommutation` | device | Ask.Device.Runtime | switching device implementation | [Equipment](#device-matrix) |
| `KeysightDevice` | device | Ask.Device.Runtime | TCP multimeter | [Equipment](#device-matrix) |
| `MultimeterB7783` | device | Ask.Device.Runtime | USB multimeter | [Equipment](#device-matrix) |
| `GPT79904` | device | Ask.Device.Runtime | COM breakdown tester | [Equipment](#device-matrix) |
| `DeviceRuntime` | static facade | Ask.DataBase.Engine | shared runtime device engine entry | [Database](#database-architecture) |
| `DeviceEngine` | service/cache | Ask.DataBase.Engine | DTO queries and runtime identity | [Device persistence](#equipment-resolution-and-device-persistence) |
| `DeviceFactory` | reflection factory | Ask.DataBase.Engine | `DeviceClass`→runtime type | [Device persistence](#device-materialization-flow) |
| `AppDbContext` | EF DbContext | Ask.DataBase.Provider | SQLite model | [Database](#database-architecture) |
| `DatabaseInitializationService` | initializer | Ask.DataBase.Provider | integrity, schema and seed | [Database](#database-architecture) |
| `CrashPackageService` | service | Ask.Diagnostics | diagnostic package collection | [Support](#support-and-diagnostics) |
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

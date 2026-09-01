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
| МКР и точки | `Ask.Core/Shared/Interfaces/DeviceInterfaces/RelaySwitchModule/` | `Ask.Device.Application/FunctionAdapters/ModuleRelayControl/`, `Ask.Device.Runtime/Function/ModuleRelayControl/`, `Ask.Device.Emulator/ModuleRelayControl/` |
| Устройство коммутации | `Ask.Core/Shared/Interfaces/DeviceInterfaces/SwitchingDevice/` | `Ask.Device.Application/FunctionAdapters/DeviceBusCommutation/`, `Ask.Device.Runtime/Function/DeviceBusCommutation/` |
| Быстрый мультиметр | `Ask.Core/Shared/Interfaces/DeviceInterfaces/Multimeter/` | `Ask.Device.Runtime/Device/KeysightDevice.cs`, `Ask.Device.Runtime/Device/MultimeterB7783.cs`, `Ask.Device.Runtime/Function/Base/Multimeter/` |
| Пробойная установка GPT | `Ask.Device.ResponseProcessor/BreakdownTester/`, `Ask.Core/Shared/Interfaces/DeviceInterfaces/BreakdownTester/` | `Ask.Device.Application/FunctionAdapters/GPT/`, `Ask.Device.Runtime/Function/GPT/`, `Ask.Device.Runtime/Device/GPT79904.cs` |
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
| `Ask.Device.Application` | `Ask.Device.Application/Ask.Device.Application.csproj` | Application adapters/decorators over raw device managers, retry and user-facing error conversion; `Ask.Device.Application.*` | `Ask.Core`, `Ask.LogLib`, `Ask.Device.Runtime`, `Ask.Device.ResponseProcessor`, `Ask.Protocol.Messages` |
| `Ask.Device.Runtime` | `Ask.Device.Runtime/Ask.Device.Runtime.csproj` | Concrete devices, low-level managers, device command generation and transports; `Ask.Device.Runtime.*` | `Ask.Core`, `Ask.Device.Communication`, `Ask.Device.Emulator`, `Ask.Device.ResponseProcessor`, `Ask.Protocol.Messages` |
| `Ask.Device.Emulator` | `Ask.Device.Emulator/Ask.Device.Emulator.csproj` | Stateful raw-protocol emulation for chassis and МКР in Idle mode and Real/Idle protocol selection; `Ask.Device.Emulator.*` | `Ask.Core` |
| `Ask.Device.Communication` | `Ask.Device.Communication/Ask.Device.Communication.csproj` | COM/TCP/UDP/USB protocol implementations; `Ask.Device.Communication.*` | `Ask.Core`, `Ask.Diagnostics`, `Ask.LogLib` |
| `Ask.Device.ResponseProcessor` | `Ask.Device.ResponseProcessor/Ask.Device.ResponseProcessor.csproj` | Модели, строгая проверка протокольных ответов и централизованная публикация сообщений МКР, УКШ, мультиметров и пробойной установки GPT | `Ask.Core` (контракты устройства/UI), `Ask.Protocol.Messages` (публикация операций и самоконтроля) |
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
│  │  │        ├─ Ask.Device.Emulator ── Ask.Core
│  │  │        └─ Ask.Device.ResponseProcessor
│  │  │           ├─ Ask.Core
│  │  │           └─ Ask.Protocol.Messages
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

Ask.Device.ResponseProcessor
├─ Ask.Core (контракты `IRelaySwitchModule`, `IMessageOutputService`)
└─ Ask.Protocol.Messages (централизованная публикация операций МКР;
   JSON-модели используют BCL `System.Text.Json`)
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
значений независимо от вызывающей подсистемы публикуются через `RangeMessages`.
`DeviceMessageBuilder` удалён из `Ask.Device.Runtime`: 104 оставшихся вызова device-результатов из
`Ask.Device.Runtime` и `Ask.Device.Application` переведены на
`DeviceMessages.PublishOperationResultAsync`. Формат, статус, отступ, признак device-сообщения
и step-checkpoint формируются внутри `Ask.Protocol.Messages`; условия видимости по
`DeviceDisplayConfig` остаются в существующих adapters/managers.

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
├─ Ask.Device.ResponseProcessor/ контракты ответов оборудования
│  └─ ModuleRelayControl/
│     ├─ ResponseModels/        модели всех JSON-форм, возвращаемых прошивкой МКР
│     └─ ResponseProcessing/    публичная статическая точка входа обработки ответов МКР
│        └─ Checkers/           отдельные internal-проверки форм ответов
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
  → рабочий режим + `ActionSettings.CheckPower`: проверка `SystemStateManager.IsActivePower`
    → питание отсутствует: сообщение об ошибке, кнопка запуска восстанавливается, выполнение не создаётся
    → Idle, специальный запуск с `CheckPower == false` или root-настройка
      `ExecutionConfig.DisablePowerCheck`: проверка пропускается
→ ActionExecutor.StartAsync(ActionSettings)
  → ExecutionRunGuard.TryAcquire
  → clear protocol/errors and reset StepControlManager
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
→ CommandExecutorRegistry.TryGet(mnemonic)
→ capture ProtocolModel snapshot протокола результатов
→ ControlProgramCommandExecutionContext.Enter
  → new CommandExecutionContext
  → ICommandExecutor.ExecuteAsync(context, ProtocolModel)
  → вложенные UserActionHelper и ProtocolPostOutputController не открывают
    Retry/Continue/Finish и не применяют StopOnError внутри команды
→ определить ошибки, добавленные всей попыткой команды
→ StopOnError OFF или ошибок нет: принять попытку
→ StopOnError ON и есть ошибки: MessageBoxCustom показывает количество ошибок
  и вопрос о повторе команды (`YesNoCancel`)
  → Да: сохранить все шаги попытки в левом экранном протоколе,
    восстановить ProtocolModel протокола результатов и отбросить ещё не
    опубликованные ErrorItem → повторить весь ICommandExecutor
  → Нет: принять последнюю попытку и перейти дальше
  → Отмена: закрыть диалог, оставить выполнение в ожидании и показать штатные
    кнопки Repeat/Continue/Finish для изучения левого протокола перед решением
→ CompleteCommandAsync(hasErrors) только для принятой попытки
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
  → для `IRelaySwitchModule` сначала `PointManager.DisconnectingAllPoint`
  → только после успешного физического отключения точек `IConnectable.ResetAsync`
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
  через `CommandMessages`, `ExecutionMessages` и `MeasurementMessages`. Общий делегат
  разобщающих стратегий дополнительно передаёт строку фактически скоммутированных точек;
  `PiCommandExecutor` использует её для качественного результата ACW/DCW, а исполнители
  ПР/СИ принимают параметр без изменения числовой семантики своих измерений;
- `NodeFullChecker` после выполнения алгоритма полного узла снимает цепи с шины `B`,
  чтобы вложенные `ПИ/СИ*` и самостоятельные `СИ` не оставляли МКР физически подключённым;
- `PairwiseFirstPointCheckerAlt` — специальная ЭТ-проверка; обходит все группы, цепи и точки,
  сохраняя брак каждой текущей точки независимо (ошибка текущей точки не блокирует следующую
  точку той же цепи); порог `100 Ом` применяется только к предварительному контролю физического
  подключения отдельных точек, а перегрузка при измерении пары определяется через
  `MeasurementValueFormatter.IsOverloadValue` по фактическому признаку `Overload`;
  `EhtHighResistanceLocalizationService` запускается только после принятого результата выше
  верхней границы, повторно измеряет такие точки и рекурсивно разбивает цепь на связные фрагменты
  аналогично локализации ПР; если повтор не подтверждает точное разбиение, исходный верхний брак
  сохраняется для всей проверяемой цепи; результаты ниже нижней границы остаются обычными ошибками
  пары и не участвуют в разбиении; возвращает
  `AlgorithmExecutionResult`, а создание и публикацию измерений, ошибок подключения точек и
  debug-сообщений делегирует `Ask.Protocol.Messages`;
- измерительные делегаты проверки разобщения ПР используют
  `MeasurementResultEvaluator.EvaluateDisconnection`: разрыв подтверждается только при
  `value > DisconnectedLowerLimitResistance`; состояние `Overload` также подтверждает разрыв,
  а равенство порогу считается браком. Обычная проверка соединения ПР продолжает использовать
  диапазонный `MeasurementResultEvaluator.Evaluate`;
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
- `MeasurementMessages.PublishInsulationStrengthResultAsync` и
  `MeasurementMessageBuilder.BuildInsulationStrengthResult` централизуют пользовательский
  результат прочности изоляции для обычных и программных проверок: заголовок содержит
  проверяемые точки и допустимый диапазон тока, успешный результат получает статус `НОРМА`,
  неуспешный — текст `ПРОБОЙ` и статус `БРАК`. Измеренный ток остаётся во внутреннем
  `BreakdownMeasurementResponse` и алгоритме сравнения, но не выводится как результат.
  Entry point используется узловыми, групповыми и control-program исполнителями PI ACW/DCW;
- все методы публикации `MeasurementMessages` требуют явный `CheckType`: метрологические
  исполнители передают `CheckType.Metrology`, исполнители программ контроля —
  `CheckType.ControlProgram`, обычные тесты — `CheckType.Test`, самоконтроль оборудования —
  `CheckType.SelfTest`. `MeasurementMessagePublisher` централизованно игнорирует настройки
  видимости итоговых и промежуточных успешных результатов для метрологии; для остальных
  типов сохраняет фильтрацию через `DeviceDisplayConfig`. Ошибочные результаты настройками
  видимости не скрываются;
- `BaseMeasurement.MeasurementPointsDisplay` централизованно форматирует обе введённые
  точки из `BaseMeasurement.Points`. Метрологические PI ACW/DCW при штатном измерении
  продолжают публиковать результат по фактически выдаваемому напряжению через KN-проверку;
  при `BreakdownMeasurementStatus.Fail` они отдельно публикуют PI-результат `ПРОБОЙ` и
  прекращают шаг. Остальные метрологические режимы также используют
  `MeasurementMessages.PublishResultAsync`. Отдельные сообщения допустимого диапазона
  (`RangeMessages.PublishAllowedRangeAsync`) метрологические режимы не публикуют;
- исполнители команд передают исходные строки в `CommandMessages.FormatSourceLines`;
  `CommandExecutionContext.ProtocolSourceLines` по умолчанию ссылается на `Command.SourceLines`,
  но позволяет вложенному executor вывести полный текст родительской команды;
  `CommandExecutorBase` больше не содержит форматирование текста протокола;
- `DeviceManager` — grouped facade для relay/switch equipment operations.

`ПИ` вызывает `СИ` как вложенный executor до и после основной ACW/DCW-проверки.
Оба контекста `СИ1`/`СИ2` получают в `ProtocolSourceLines` исходные строки `ПИ`, поэтому
левый протокол сохраняет параметры ПИ и адреса точек из программы контроля; самостоятельная
`СИ` продолжает использовать собственные `SourceLines`. Для вложенных этапов
`CommandMessages.FormatSourceLinesWithHeader` заменяет исходные номер и мнемонику заголовком
`ПИ/СИ1`, `ПИ/ПИ1` или `ПИ/СИ2`, не дублируя `номер ПИ` перед параметрами.
`РМ` вызывает `EquipmentService.AnalyzePoints` и готовит equipment state.

#### Pause, stop and command jump

`ActionExecutor` владеет `ExecutionSession`/`CancellationTokenSource`,
`ExecutionPauseController`, `ExecutionRunGuard`, `ExecutionFinalizer`.
`ProtocolUI` реализует `IExecutionController`, `IExecutionPauseGate` и
`IUserInteractionService`. Pause checkpoints проходят через
`WaitAtExecutionCheckpointAsync`. `ProtocolHotkeyController` не перехватывает
обычный ввод в `TextBox`/`ComboBox`, но разрешает доступные действия `R`
(повтор), `P` (продолжение/пауза) и `Esc` (завершение), даже если после команды
фокус остался в поле ввода. Command jump от F4 идёт:

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
показывает результат, добавляет обязательный финальный блок программы контроля через
`ProtocolCompletionService.AppendControlProgramCompletionAsync` →
`ControlProgramCompletionMessageBuilder.Build` и сохраняет протоколы. Финальный блок
не зависит от настроек протокола и добавляется после остальных сообщений. Перед его выводом
`ProtocolUI.FinalizeCurrentCommandGroupAsync` → `ProtocolListBoxUI.FinalizeCurrentCommandGroupAsync`
закрывает последнюю группу команды, поэтому финальная зелёная запись отображается отдельно и не сворачивается
вместе с `КЦ` или другой последней командой. Все шаги выполняются внутри
`EquipmentExecutionContext.EnterMandatoryFinalization`; ошибка отдельного шага
логируется и не прерывает оставшиеся обязательные действия.

#### Related configuration

`ExecutionConfig`: idle, step-by-step, delays, reactions and compatibility mode.
`ProtocolConfig`: command headers, step messages, printing and protocol templates.

#### Files

- `Ask.UI/Features/ProtocolNew/Execution/ActionExecutor.cs`
- `Ask.UI/Features/ProtocolNew/Execution/ExecutionFinalizer.cs`
- `Ask.UI/Features/ProtocolNew/Protocol/ProtocolCompletionService.cs`
- `Ask.UI/Features/ProtocolNew/Protocol/ControlProgramCompletionMessageBuilder.cs`
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

Метрология, программы контроля, системный и модульный самоконтроль используют стандартное
`ActionSettings.CheckPower == true`; в исполнителях самоконтроля отдельной проверки или обхода нет.

Для сопротивления в метрологических режимах ПР и КС ветка `PerformMeasurement`
имеет общий обязательный этап компенсации:

```text
ModePr.PrMeasurement.PerformMeasurement / ModeKC.KcMeasurement.PerformMeasurement
→ IMultimeter.ContinuityManager.CheckContinuityAsync /
  IMultimeter.ResistanceManager.MeasureResistanceAsync
→ ResistanceCompensation.SubtractSwitchResistance(Rизм, Rкомм,
  subtract: !ExecutionConfig.GetIsIdleModeEnabled())
  → Math.Max(0, Rизм - Rкомм) в Real или Math.Max(0, Rизм) в Idle
→ расчёт метрологической погрешности от ограниченного результата
→ проверка результата по LowerBound/UpperBound
→ MeasurementMessages.PublishResultAsync / PublishMetrologyMeasurementErrorAsync
→ UI и протокол
```

Та же функция компенсации вызывается только из production-алгоритмов команд ПР/КС
(`PrCommandExecutor`, `KsCommandExecutor`) и их метрологических режимов. Ограничение
выполняется до `MeasurementResultEvaluator`/ручной проверки диапазона и публикации.

Для команды СИ обе измерительные ветки `SiCommandExecutor`
(`NodeFullPerformMeasurementAsync` и `NodeAccumulationPerformMeasurementAsync`)
передают в `MeasurementResultEvaluator` диапазон `[rangeFrom, -1]`. Значение `-1`
означает отсутствие верхней границы: проверяется только `value >= rangeFrom`.
При заданной верхней границе evaluator проверяет обе границы включительно.

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
- `Ask.Engine/ControlCommandExecutor/BaseStrategies/Data/ResistanceCompensation.cs`
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
├─→ ProtocolCompletionService.DisplayCompletionAsync
│  → InspectionProtocolBuilder
│  → ProtocolUI.ShowInspectionProtocol
├─→ ProtocolCompletionService.AppendControlProgramCompletionAsync
│  → ProtocolUI.FinalizeCurrentCommandGroupAsync
│  → ProtocolListBoxUI.FinalizeCurrentCommandGroupAsync
│  → ControlProgramCompletionMessageBuilder.Build
│  → ProtocolUI.ShowMessageAsync (отдельная зелёная последняя запись потокового протокола)
└─→ ProtocolCompletionService.SaveAndExposeAsync
→ ProtocolStorageService
→ ExecutionProtocolHistoryService
→ ExecutionProtocolDiagnosticFormatter.FormatForStorage
  (видимая строка + скрытая структурированная диагностика каждой записи)
```

Форматы:

- `.asktrace` — записи хода выполнения;
- `.askresult` — итог обычной проверки;
- `.askreport` — итог программы контроля.

`ExecutionProtocolHistoryService.SaveInspectionAsync` выбирает `.askreport` для
`CheckType.ControlProgram`, иначе `.askresult`, и старается использовать basename
соответствующего `.asktrace`. Каталог истории:
`Path.GetFullPath(Path.Combine("..", FileLocations.DataSaveDirectory))`.

Structured `.asktrace` message metadata is decoded for every role and supplies invisible segment
markers for header/message/time highlighting; only the readable `ROOT` diagnostic expansion is
role-restricted. New trace entries persist exact message/time offsets instead of deriving segment
boundaries from punctuation. Both `Ask.UI/Controls/TextEditorControl/TextEditorUI.xaml.cs` and the
legacy `UI/Controls/TextEditorControl/TextEditorUI.xaml.cs` bind named `MKI_PROTOCOL.xshd` colors
to the current WPF resources used by `ProtocolListBoxUI.ApplyThemeColors`, including custom themes.

Current structured traces also contain `#ASKM_MESSAGE_V2#` snapshots represented by
`ExecutionProtocolMessageSnapshot`. On open, `FileOpenService` and `MainWindow/Services/FileService`
call `ExecutionProtocolDiagnosticFormatter.TryRestoreMessages`; successful restoration is rendered
by `SavedExecutionProtocolUI` through the production `ProtocolListBoxUI` templates and grouping.
Legacy traces without V2 snapshots are converted line-by-line by
`ExecutionProtocolDiagnosticFormatter.RestoreLegacyMessages` and rendered in the same read-only
`ProtocolListBoxUI`; since the legacy format contains no structured status/group metadata, those
lines are restored as `Info` while preserving their complete text and blank-line layout.

New saves use `#ASKM_PROTOCOL_V3_BR#`: `ExecutionProtocolHistoryService.SaveAsync` delegates to
`ExecutionProtocolDiagnosticFormatter.FormatProtocolForStorage`, which writes readable protocol
lines plus one Base64-encoded Brotli block containing the environment and the complete snapshot
array. Readers remain backward-compatible with V2 per-message snapshots, V1 diagnostics and
pre-structured text traces.

При открытии `.asktrace` `ExecutionProtocolDiagnosticFormatter.PrepareForDisplay`
скрывает служебные записи для обычных ролей и раскрывает источник вызова и атрибуты
сообщения для `Root`. Старые текстовые протоколы открываются без преобразования.
Перед сохранением `ActionExecutor.FinalizeAsync` формирует через
`ExecutionProtocolEnvironmentSnapshotFactory` root-снимок настроек выполнения,
протокола и отображения оборудования, версии/роли/режима и устройств, фактически
зарегистрированных в `EquipmentUsageSession`; снимок сохраняется первой скрытой
записью `.asktrace` и раскрывается в начале документа только для `Root`.

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
- `Ask.Core/Services/Protocols/ExecutionProtocolDiagnosticFormatter.cs`
- `Ask.Core/Services/Protocols/ExecutionProtocolEnvironmentSnapshot.cs`
- `Ask.UI/Features/ProtocolNew/Protocol/ExecutionProtocolEnvironmentSnapshotFactory.cs`
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
`IConnectable`, `IDeviceProtocol`, `IConnectionInfo`, `ConnectionDetails`.

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
  ├─ Status absent/success → ModuleRelayControlResponseProcessor validation
  → PointManagerAdapter возвращает результат или создаёт ошибку через RelayExceptionFactory
  └─ Status = UnknownCommand / InvalidParametr / InvalidParameter
    → ModuleRelayControlProtocolException(device, operation, localized error, firmware status)
    → UserActionHelper catches hardware exception
    → IUserInteractionService.ShowMessageAsync(MessageType.Error, skipPause: true)
    → protocol line "МКР chassis.number: operation. Системная ошибка. reason [БРАК]"
    → existing Retry / Continue / Abort equipment flow
```

`Ask.Device.ResponseProcessor.ModuleRelayControl.ResponseProcessing.ModuleRelayControlResponseProcessor`
предоставляет новые проверки ответов подключения/отключения одной точки. Методы
`CheckPointConnectionAsync`, `CheckPointDisconnectionAsync`,
`CheckVerifiedPointConnectionAsync` и `CheckVerifiedPointDisconnectionAsync` принимают
сырой JSON, ожидаемый `IRelaySwitchModule`, номер точки, шины и необязательный
`IMessageOutputService`. Публичный API не содержит булевого переключателя режима проверки.
`PointConnectionResponseChecker` проверяет
`ModuleName == "MKR"`, `NumberChassis`, `NumberDevice`, точную строку `Answer`
для команды `8` или `82`, а для команды с аппаратным контролем также `Checked == true`.
`PointSelfTestChecker` десериализует ответ команды `TEST_MKR` (`6.<point>`), проверяет
идентификатор МКР, ожидаемый `NumberPoint`, прошивочный статус `sucsess` и обязательную
истинность `ConnectPoint`, `DisconnectBusA`, `DisconnectBusB` и `SelfControl`; пустой,
повреждённый или частично неуспешный ответ даёт `false`. Runtime
`SelfTestManager.CheckPoint` после `IRelaySwitchModule.PointManager.CheckPoint` передаёт сырой
ответ в `ModuleRelayControlResponseProcessor.CheckPointSelfTestAsync`. Processor сохраняет
прежние строки `SelfTestMessages` (`Точка N`, детализацию подключения и отключения от шин),
добавляет `ModuleRelayControlError.PointError` в итоговые ошибки и обрабатывает повреждённый
ответ строкой `Ошибка данных!`; прежняя runtime-модель `SelfPointModel` удалена.
Idle `ModuleRelayControlEmulatorProtocol` для команды `6.<point>` учитывает обе настройки
симуляции: `IsErrorSimulationMode` детерминированно делает ложным один из этапов
`ConnectPoint`/`DisconnectBusA`/`DisconnectBusB` (по номеру точки) и возвращает
`SelfControl = false`; `IsHardwareErrorSimulationMode` через
`IdleHardwareErrorSimulator` с вероятностью 50% возвращает пустой ответ до разбора команды.
`ExternalBusSelfTestChecker` обрабатывает ответ команды `AUTOTEST_EXTERNAL_BUS`
(`10.<bus>`): проверяет идентификатор МКР, ожидаемый `NumberBus`, соответствие четырёх номеров
реле таблице прошивки, `ConnectProtect`, `ConnectMain` и `Error == 0`. Runtime
`SelfTestManager.CheckBus` передаёт сырой ответ в
`ModuleRelayControlResponseProcessor.CheckExternalBusSelfTestAsync`; processor сохраняет
прежние строки `Шины ABN`, `Подключение защитных реле(...)` и
`Подключение основных реле(...)`, а повреждённый ответ, чужой адрес или другую шину выводит
как `Ошибка данных!`. Прежняя runtime-модель `SelfBusModel` удалена. В Idle команда `10.<bus>`
при симуляции ошибки измерения случайно выбирает один из трёх равновероятных исходов: оба этапа
исправны, отказ защитных реле или отказ основных реле. При отказе возвращается ненулевой `Error`;
при симуляции ошибки оборудования общий emulator path возвращает пустой ответ.
После проверки processor напрямую вызывает
`EquipmentMessages.PublishPointOperationResultAsync`. `EquipmentMessageBuilder` формирует
device-строку вида `Модуль МКР-350(1.6) - Подключение точки 1 к шине [A] : [НОРМА]`;
решение об отображении принимает `Ask.Protocol.Messages` через общий `ShouldPublish`.
`PointManagerAdapter` для четырёх одиночных операций не выполняет повторную проверку
`DeviceDisplayConfig` и не публикует дублирующее сообщение; он передаёт
`IUserInteractionService` в runtime `PointManager` и сохраняет только user retry/error boundary.
Все используемые production-команды МКР теперь проходят через
`ModuleRelayControlResponseProcessor`: `1` (инициализация), `2` (сброс), `4` (шины),
`5`/`7` (измеритель), `6` (самоконтроль точки), `8`/`82` (одиночная точка),
`10` (самоконтроль внешней шины), `11` (диапазон точек) и `81` (переподключение точки).
`CommandResponseChecker` проверяет идентификатор МКР и точное значение `Answer`;
`CommandStatusChecker` преобразует `UnknownCommand`/`InvalidParametr` в
`ModuleRelayControlProtocolException`. Runtime-менеджеры больше не используют
`BaseResponse.FromJson` для ответов МКР. Публикация результатов этих операций, включая
инициализацию, сброс и агрегированное отключение сохранённых точек, вызывается через processor;
заголовки и информационные строки самоконтроля МКР также маршрутизируются через processor.
Для команды измерителя `7` processor различает оба штатных ответа: `7.1` означает наличие
замыкания, `7.2` — его отсутствие; повреждённый ответ или чужой адрес остаётся аппаратной
ошибкой. `MeterManagerAdapter` считает оба состояния успешно полученным измерением и запускает
интерактивный повтор только при исключении. `CrossConnectionTests` повторно включает измеритель
проверяющего МКР перед каждой точечной частью, поскольку сброс после предыдущей части выключает его.
В `Ask.Device.Runtime/Function/ModuleRelayControl/` и соответствующих application adapters
не осталось прямого разбора JSON или прямых вызовов `DeviceMessages`, `EquipmentMessages` и
`SelfTestMessages`; решение о видимости остаётся внутри `Ask.Protocol.Messages`.
Runtime-менеджеры МКР передают исходные ответы и параметры операции в
`ModuleRelayControlResponseProcessor`; модели и проверки протокола находятся в
`Ask.Device.ResponseProcessor/ModuleRelayControl/`.

Representative Keysight measurement:

```text
executor/metrology
→ IMultimeter.ResistanceManager.MeasureResistanceAsync
→ ResistanceMeasurementBase
→ MeasurementBase.MeasureAsync → MeasureOtherAsync
→ SetModeBase / RangeBase → DeviceProtocolEmulator.QueryMultimeterAsync
→ AdapterMeasurementExecutor
→ MeasurementBase.MeasureCoreAsync
→ Simulated.GetSimulatedValue builds idleResponse
→ DeviceProtocolEmulator.QueryMultimeterAsync(profile.Measure, idleResponse)
  → Real: TcpProtocol/UsbProtocol.QueryAsync → transport
  → Idle: SCPI-compatible scientific-notation response from MeasurementRange
→ MultimeterResponseProcessor.TryParseMeasurement
  → MeasurementResponseChecker → MeasurementResponse
→ rounding
→ range verdict and DeviceMessages.PublishOperationResultAsync
```

Единая обработка ответов Keysight 34465A и В7-78/3 находится в
`Ask.Device.ResponseProcessor/Multimeter/`:

- `MultimeterResponseProcessor.CheckInitialization` проверяет идентификационный ответ;
- `CheckMode` через `ModeResponseChecker` проверяет ответы `FUNCTION?`/профильного `GetMode`;
- `TryParseMeasurement` через `MeasurementResponseChecker` разбирает знак, точку/запятую,
  экспоненту и допустимый текстовый суффикс; SCPI-маркер `9.9E+37` и текстовые ответы
  `OL`/`OVL`/`OVLD`/`OVLOAD`/`OVERLOAD` возвращаются как `MeasurementState.Overload`
  с единым совместимым значением `double.PositiveInfinity`; проверки диапазонов считают
  неожиданную перегрузку браком даже при отсутствующей верхней границе, а UI/протоколы
  форматируют состояние строго строкой `Overload`;
- `TryCheckContinuity` интерпретирует измерение и SCPI-значение перегрузки `9.9E+37`;
- `CheckNoInstrumentError` через `InstrumentErrorResponseChecker` разбирает код и текст
  ответа `SYSTEM:ERROR?`.

Единая обработка ответов GPT-79904 находится в
`Ask.Device.ResponseProcessor/BreakdownTester/`:

- `BreakdownTesterResponseProcessor.CheckInitialization` проверяет идентификационный ответ GPT;
- `CheckMode` проверяет ответы выбора ACW/DCW/IR;
- `TryParseNumber` через `NumericResponseChecker` разбирает знаковые значения, десятичную
  точку/запятую, экспоненту и суффиксы `kV`, `mA`, `Hz`, `GOhm`/`MOhm`;
- `TryParseState` обрабатывает `ON`/`OFF` для режима земли и системных настроек;
- `TryParseMeasurement` через `MeasurementResponseChecker` извлекает последний измерительный
  результат, единицу и статус `PASS`/`FAIL`/`TEST` из составного ответа GPT и возвращает общий
  `Ask.Core.Shared.DTO.Devices.Breakdown.BreakdownMeasurementResponse` для ACW/DCW/IR;
  статус типизирован enum `BreakdownMeasurementStatus` (`Test`, `Fail`, `Pass`), а ответ без
  одного из этих статусов не считается корректным результатом измерения;
- `BreakdownTesterMessages` является фасадом над `Ask.Protocol.Messages` для рабочих операций
  ACW/DCW/IR/System и самоконтроля; существующие тексты сообщений остаются в вызывающем коде.

Runtime-путь GPT:

```text
IBreakdownTester / GPT79904
→ application AcwModeAdapter / DcwModeAdapter / IrModeAdapter / SystemSettingsAdapter
→ runtime manager/helper в Ask.Device.Runtime/Function/GPT
→ BreakdownTesterCommandProtocol.QueryAsync
→ Real ComProtocol / Idle BreakdownTesterEmulatorProtocol
→ BreakdownTesterResponseProcessor (режим/число/состояние/измерение)
→ BreakdownTesterMessages → Ask.Protocol.Messages
```

Подключение, отключение, инициализация и сброс `IBreakdownTester` маршрутизируются в
`Transport` через `BreakdownTesterResponseProcessor`; COM-идентификация дополнительно проверяется
в `ComTransport.IsExpectedInitializeAnswer` по `ConnectedProfile.CheckMode`.

Общий `MeasurementBase`, `SetModeBase`, `RangeBase`, `ContinuityMeasurementBase` и
отдельный legacy-путь `B7783/VoltageMeasurementBase` не разбирают ответы самостоятельно.
Публикация рабочих сообщений и результатов самоконтроля мультиметра централизована в
`MultimeterMessages`; тексты и параметры сообщений сохранены в `Ask.Protocol.Messages`.
Заголовки этапов и результаты измерений самоконтроля мультиметра публикуются с
`isBlockStart: false`. При этом `SelfTestMessageBuilder.BuildCommand()` формирует заголовок как
`MessageType.Info`, а не `Command`: `ProtocolUI.CheckBlockStart()` не открывает логический блок,
а `ProtocolListBoxUI.AppendVisibleMessage()` не создаёт сворачиваемую `ProtocolCommandGroup`. Цвет заголовка
и явная step-mode checkpoint сохраняются. Другие потоки самоконтроля сохраняют свою политику блоков.
Сообщения подключения, отключения, инициализации и сброса для `IMultimeter` проходят через
`MultimeterResponseProcessor`. Retry и проверка измерения по допустимому диапазону остаются
в Runtime/Engine.

Для `MultimeterTypeMode.Continuity` общий `MeasurementBase` не вызывает
`RangeBase`: режим прозвонки задаётся профильной командой `CONF:CONT`, а
измерительный запрос выполняется через `MEAS:CONT?` без установки диапазона.

Сопротивление, напряжение, диод и прозвонка сначала выполняют один замер. Если его
результат вне `MeasurementRange`, он отбрасывается и `MeasureOtherAsync` выполняет второй
замер, результат которого становится итоговым независимо от диапазона. Это не усреднение.
Отдельный путь В7-78/3 в `B7783/VoltageMeasurementBase.MeasureVoltageAsync` применяет то же
правило двух попыток.
Повтор внутри `AdapterMeasurementExecutor` относится только к восстановлению после ошибки
оборудования. Серия измерений с усреднением реализована только для ёмкости в
`MeasurementBase.MeasureCapacitanceAsync`; количество задаётся параметром
`ICapacitanceMeasurement.MeasureCapacitanceAsync.measurementCount`.

Инициализация обоих мультиметров использует тот же журнал команд:

```text
IMultimeter.ConnectableManager.InitializeAsync()
→ TcpTransport.InitializeAsync() / UsbTransport.InitializeAsync()
→ DeviceProtocolEmulator.QueryMultimeterAsync(ConnectedProfile.Initialize, idleIdentificationResponse)
  → Real: TcpProtocol / UsbProtocol
  → Idle: идентификационный SCPI-ответ
→ MultimeterResponseProcessor.CheckInitialization
→ Transport.InitialDeviceSoundConfigurator.ApplyOnceAsync
  → KeysightDevice / MultimeterB7783: `SYST:BEEP:STAT OFF`
  → DeviceProtocolEmulator.QueryMultimeterAsync
    → Real: TcpProtocol / UsbProtocol
    → Idle: команда поглощается эмулятором без обращения к физическому прибору
```

После первого успешного идентификационного обмена `Transport` вызывает
`InitialDeviceSoundConfigurator` для конкретного runtime-экземпляра устройства. Для `GPT79904`
тот же шаг выполняется из `Transport.ConnectAsync` и `Transport.InitializeAsync`, после чего через
`BreakdownTesterCommandProtocol` однократно отправляются `SYST:BUZZ:PSOUND OFF` и
`SYST:BUZZ:FSOUND OFF`. Профили `KeysightDevice` и `MultimeterB7783` задают
`SYST:BEEP:STAT OFF`; остальные устройства наследуют пустой список
`ConnectedBaseProfile.InitialBeeperDisableCommands`, поэтому дополнительных запросов не получают.
`InitialDeviceSoundConfigurator` защищён `SemaphoreSlim` от параллельной первичной инициализации и
фиксирует одну попытку на время жизни экземпляра, не сбрасывая её при измерении, reset или reconnect.
Ошибка неподдерживаемой команды записывается как warning и не превращает успешную инициализацию в
ошибку, а повторно команда не отправляется.

`DeviceProtocolEmulator.QueryMultimeterAsync` записывает каждую операцию двумя строками единого формата:
`Команда мультиметра: "..."` и `Ответ мультиметра на "...": "..."`.
Для SCPI-команд мультиметра без `?` этот шлюз передаёт в транспорт `timeout = 0`
и не ждёт ответа; команды с `?` сохраняют заданный `timeout` и `responseDelay`.

`HardwareWatchdogProtocol` оборачивает реальные COM/TCP/UDP/USB-протоколы при их создании
в `DeviceWithCOM`, `DeviceWithIP`, `KeysightDevice`, `MultimeterB7783` и
`MikUps1101rRmDevice`. Он запускает вызов `IDeviceProtocol.QueryAsync` вне вызывающего потока,
ожидает не более 5 секунд, отменяет связанный `CancellationToken` и выбрасывает
`TimeoutException`. Поэтому защищены как вызовы через `DeviceProtocolEmulator`, так и прямые
обращения к `device.DeviceProtocol`. `ModeSelectingDeviceProtocol` сохраняет вторую watchdog-
границу для реальных обращений через Real/Idle-шлюз; холостой режим ей не ограничивается.
Watchdog ограничивает ожидание вызывающего кода, но не может принудительно завершить уже
зависший нативный вызов VISA внутри процесса.

Для команды `НЭ` токен `IUserInteractionService.GetCancellationToken()` проходит через
`NeCommandExecutor → IDiodeMeasurement → DiodeMeasurementBase → MeasurementBase →
DeviceProtocolEmulator`. Ошибка установки режима публикуется в UI как ошибка выполнения и
завершает только текущую команду. Ошибка или отсутствие ответа при измерении преобразуется
в отрицательный результат точки, поэтому `CommandExecutionManager` может перейти к следующей
команде программы контроля.

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
    затем `DeviceBusCommutationResponseProcessor` выбирает специализированную проверку команды
    (`EquipmentCommandResponseChecker`, `BusCommandResponseChecker`, `RelayCommandResponseChecker`,
    `ChainCommandResponseChecker` или `SelfTestCommandResponseChecker`), проверяет `ModuleName`, адрес УКШ
    и точное поле `Answer` либо ожидаемое числовое значение. Менеджеры `ConnectorManager`, `RelayManager`,
    `ResistorManager`, `CapacitorManager` и UKSH SelfCheck не формируют ожидаемые ответы и не разбирают их
    самостоятельно; сообщения операций и самоконтроля проходят через
    `DeviceBusCommutationResponseProcessor`/`DeviceBusCommutationMessages`.
    Все JSON-значения `Answer` заканчиваются точкой: `2.0.1.`, полные ответы команд `4/5/9`
    и сокращённый ответ команды `7` в формате `7.<action>.` согласно `makeAnswer(..., 2)`;
    `JsonCommandResponseChecker` перед строгим ordinal-сравнением удаляет конечные точки из фактического и
    ожидаемого `Answer`, поэтому принимает оба варианта прошивки, но не произвольное вхождение строки;
    команда `6` возвращает `0` при успехе и код несуществующей цепи при ошибке. Idle-эмулятор использует
    те же форматы и при симуляции ошибок измерения случайно возвращает как успешный, так и ошибочный код;
  - остальные UDP/TCP/COM/USB connectable managers возвращают simulated success или обходят I/O;
- relay/source/switch managers update in-memory state and return success;
- `Simulated.GetSimulatedValue` supplies values to the Idle multimeter SCPI-response path;
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

Chassis, МКР and УКШ Idle flows preserve their device command contracts through
the same response processors used for real devices:

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
  polls `ReadExisting`. `DeviceWithCOM` owns the resulting `SerialPort` and implements
  `IDisposable`: disposal closes and releases the port even when `Close` reports an error.
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
`FileCompareService` сравнивает текст исходного редактора с `SavedTextSnapshot`.
`DockItemService` подписывает редактируемые вкладки на `TextChanged` и добавляет `*`
только в визуальный `DockItem.TabText`; чистый `DockItem.Title` остаётся ключом пути.
`SaveFileManager` обновляет снимок и снимает индикатор после фактической записи,
а неизменённый исходник `TranslatorItem` повторно не записывает и уведомление не показывает.
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
Экранный протокол `CrossConnectionTests` выводит только этап, номер точки и результаты
проверок подключения/отключения. `DeviceMessages` и `EquipmentMessages` используют
общую политику `DeviceDisplayConfig.ShouldDisplayOperationResult()`: ошибки оборудования
выводятся всегда, успешные служебные операции — только при включённом параметре
`ShowDeviceExecutionParameters`. `CrossConnectionTests` передаёт исходный
`IUserInteractionService`, поэтому отмена и интерактивный повтор остаются в общем потоке.
Каждый этап публикуется как командный заголовок начала блока, поэтому его точки и
результаты отображаются в отдельной сворачиваемой секции экранного протокола.
В холостом режиме без измерительной и аппаратной симуляции ошибок
`CrossConnectionTests` локально использует ожидаемое состояние цепи; при включении
любой симуляции ошибок и в реальном режиме проверяется фактический ответ измерителя.
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

Исключение составляет одна попытка команды программы контроля. Пока активен
`ControlProgramCommandExecutionContext`, вложенные adapters/managers выполняются
один раз и возвращают результат без собственного интерактивного цикла, а
`ProtocolPostOutputController` не ставит выполнение на паузу по отдельной записи
`Error`. После возврата `ICommandExecutor.ExecuteAsync` решение принимается в
`CommandExecutionManager` для всей команды. Повтор сохраняет экранный протокол
выполнения со всеми шагами и попытками, но откатывает `ProtocolModel` протокола
результатов и отложенные `ErrorItem` предыдущей попытки, после чего заново
вызывает тот же executor. Метрология, инженерные тесты и самоконтроль этот
контекст не создают и сохраняют прежнее поведение `UserActionHelper`.

В самоконтроле УКШ `SelfTestRetryHelper.CheckRelayStateAsync` публикует результат
проверки реле с `skipPause: true`: строка `[БРАК]` сохраняется, но общая
`ProtocolPostOutputController`-пауза не перехватывает управление до возврата
результата. После этого `UserActionHelper` показывает штатные интерактивные
действия согласно `StopOnError`, не подменяя их кнопками пошагового режима.
`SelfTestProcessManager.PerformRelayTestAsync` передаёт
`deviceTask: true`, поэтому отрицательный результат проверки реле не считается
допустимым продолжением: Repeat заново вызывает `CheckContinuityAsync`.

Вне `ControlProgramCommandExecutionContext` measurement verdicts use a separate branch:

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

Измерительные делегаты передают `measurementTask: true` в `UserActionHelper`.
При включённом `SettingsExecutionDto.RepeatMeasurement` отрицательный результат
принудительно вызывает блокирующий `IUserInteractionService.WaitRetryOrContinueAsync()`
и открывает выбор действия независимо от `StopOnError`. Для такого делегата разрешён вложенный повтор даже внутри
`ControlProgramCommandExecutionContext`: повторно выполняется только измерение,
а не весь `ICommandExecutor`. При выключенной настройке контекст программы
контроля по-прежнему подавляет вложенный интерактивный цикл.
В попарной ветке ЭХТ `PairwiseFirstPointCheckerAlt` сохраняет текущую коммутацию
до решения оператора. Повтор заново читает сопротивление и рассчитывает
компенсированный результат; переключение точек выполняется только после принятия результата.
Adapters/managers сохраняют существующие проверки `DeviceDisplayConfig`, после чего
`DeviceMessages` централизованно формирует и публикует device-результат.
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
| Device protocol waits | Real `ModeSelectingDeviceProtocol` calls plus COM/TCP/UDP/USB queries | 5-second outer watchdog, semaphore-protected I/O and transport timeout polling | linked cancellation; caller resumes with `TimeoutException` |
| Help server | `HelpServer.EnsureStarted` | Kestrel static-file host | `App.OnExit → HelpServer.Stop` |
| Archive refresh | `ArchiveControl` DispatcherTimer | refresh archive lists plus background I/O | view lifetime |
| Role keyboard layout | `RoleLoginWindow` DispatcherTimer | keyboard layout monitoring | window lifetime |
| Workspace click timer | `MultiEditorControl` DispatcherTimer | double-click discrimination | control lifetime |
| Logged exception reporter | `ExceptionDiagnosticReporter` bounded Task.Run | asynchronous crash package | throttled/timeout-limited |

`MeasureHelper.MeasureFastPollingAsync` трактует настроенное время теста как временное окно
для запуска повторных попыток после ответа `FAIL`: каждая попытка ожидает первый непустой
ответ `MEASURE`, ответы `PASS`/`TEST` завершают цикл, а новая попытка после `FAIL` запускается
только до истечения окна. После цикла `StopMeasure` подтверждает состояние `TEST OFF`.
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
clears and warms them. `DeviceCache` disposes resource-owning devices when they are
removed, replaced or cleared. `DeviceEngine.UpdateInternalAsync` removes the old cached
instance in `finally`, so a GPT configuration update releases its COM port after success,
provider error and cancellation; a later query builds a fresh runtime instance from the DB.

## Configuration

| Runtime config | Persisted DTO/table | Load/save bridge | Major consumers |
| --- | --- | --- | --- |
| `ExecutionConfig` | `SettingsExecutionDto` / `Execution` | `ExecutionSettings`, `MainWindow.Init.DatabaseInitializer` | ActionExecutor, Engine, all device idle gates; independent measurement/hardware Idle error settings; `RepeatMeasurement` enables retry of explicitly marked equipment measurements |
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
  measurement parameters (`Measurements/MeasurementRange`); общий результат ответа ППУ
  `Breakdown/BreakdownMeasurementResponse` хранит типизированный
  `BreakdownMeasurementStatus Status`, `Value` и `Unit` для ACW/DCW/IR.
  Парсер GPT и IR runtime используют общий тип, но `IMeasurable.MeasureAsync` пока сохраняет
  прежний возврат только измеренного значения и единицы;
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
| `DeviceMessages` | static facade | Ask.Protocol.Messages | заменяет удалённый runtime `DeviceMessageBuilder`; формирует и публикует унифицированные результаты device-операций с деталями, статусом, отступом и step-checkpoint | [Protocols](#protocols-and-file-formats) |
| `EquipmentMessageBuilder` | internal static builder | Ask.Protocol.Messages | формирует результаты подключения, отключения, инициализации, настройки, сброса и заголовок самоконтроля оборудования | [Protocols](#protocols-and-file-formats) |
| `EquipmentMessagePublisher` | internal static publisher | Ask.Protocol.Messages | записывает сообщения оборудования в device log и передаёт их `IMessageOutputService` | [Protocols](#protocols-and-file-formats) |
| `SelfTestMessages` | static facade | Ask.Protocol.Messages | публикует этапы, команды пошагового режима, ошибки и результаты самоконтроля мультиметра, GPT, МКР, УКШ и модуля напряжения/тока; runtime SelfCheck-классы моделей экранного протокола не создают | [Equipment](#equipment-architecture) |
| `SelfTestMessageBuilder` | internal static builder | Ask.Protocol.Messages | формирует информационные, командные и результирующие сообщения самоконтроля, включая видимость измерений, Overload, погрешность и свойства итогового протокола | [Equipment](#equipment-architecture) |
| `SelfTestMessagePublisher` | internal static publisher | Ask.Protocol.Messages | передаёт сообщения самоконтроля общему `MessagePublisher` с признаками блока, паузы и проверки доступности вывода | [Equipment](#equipment-architecture) |
| `MeasurementMessages` | static facade | Ask.Protocol.Messages | формирует модели для накопления результатов и публикует начало измерения, этап измерений, ток утечки PI, качественный результат прочности изоляции с точками, эталонное значение, ошибки подключения точек, выдачу испытательного напряжения PI ACW/DCW, готовые сообщения измерений, итоговые и промежуточные результаты и погрешности; публикация требует явный `CheckType` | [Protocols](#protocols-and-file-formats) |
| `MeasurementMessageBuilder` | internal static builder | Ask.Protocol.Messages | формирует заголовки измерений, эталонные значения, ошибки подключения точек, переход к методу полного узла, единый формат диапазона, измеренное значение, погрешность, `ПРОБОЙ` и `Overload` | [Protocols](#protocols-and-file-formats) |
| `MeasurementFailureMessageBuilder` | internal static builder | Ask.Protocol.Messages | формирует описания брака для точек и разрядов узлового и группового методов | [Protocols](#protocols-and-file-formats) |
| `MeasurementLimitKind` | enum | Ask.Protocol.Messages | контракт из `Ask.Protocol.Messages/Models/`, задающий минимальный или максимальный предел при формировании описания брака | [Protocols](#protocols-and-file-formats) |
| `MeasurementMessagePublisher` | internal static publisher | Ask.Protocol.Messages | централизованно применяет видимость успешных результатов: `Metrology` выводится всегда, остальные типы учитывают `DeviceDisplayConfig`; опубликованные измерения записывает в device log и передаёт `IMessageOutputService` | [Protocols](#protocols-and-file-formats) |
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
| `ControlProgramCompletionMessageBuilder` | internal static builder | Ask.UI | обязательный финальный блок программы контроля с режимом и длительностью выполнения | [Protocols](#protocols-and-file-formats) |
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
| `InitialDeviceSoundConfigurator` | internal lifecycle helper | Ask.Device.Runtime | однократно отключает звуковую сигнализацию GPT/мультиметра после первой успешной инициализации и сохраняет Real/Idle-маршрутизацию | [Equipment](#equipment-architecture) |
| `EquipmentExecutionContext` | async context | Ask.Core | suppresses interactive retry during mandatory finalization | [Error Handling](#equipment-error-flow) |
| `ExecutionConfig` | static config | Ask.Core | execution/idle state | [Configuration](#configuration) |
| `RoleAuthorizationConfig` | static session state | Ask.Core | current successfully authenticated role | [Authentication/Debug](#authentication-and-debug-access-flow) |
| `DebugAccessConfig` | derived access state | Ask.Core | central root-only Debug availability and change notification | [Authentication/Debug](#authentication-and-debug-access-flow) |
| `IdleHardwareErrorSimulator` | static decision service | Ask.Core | independent `1/2` hardware failure decision for non-measurement Idle calls | [Real / Idle](#real--idle) |
| `EventAggregator` | event bus | Ask.Core | in-process publish/subscribe | [Events](#events-and-callbacks) |
| `DeviceApplicationComposer` | composer | Ask.Device.Application | replaces raw managers with adapters | [Equipment](#adapters-and-error-boundary) |
| `DeviceProtocolEmulator` | public static factory | Ask.Device.Emulator | returns Real/Idle-selecting protocols for chassis and МКР | [Equipment](#real--idle) |
| `ChassisQueryExecutor` | runtime helper | Ask.Device.Runtime | routes and logs chassis commands through the real protocol or emulator | [Equipment](#real--idle) |
| `ModuleRelayControlQueryExecutor` | runtime helper | Ask.Device.Runtime | routes and logs МКР commands through the real protocol or emulator | [Equipment](#real--idle) |
| `AdapterMeasurementExecutor` | helper | Ask.Device.Application | measured operation retry/logging | [Error Handling](#equipment-error-flow) |
| `ModuleRelayControl` | device | Ask.Device.Runtime | МКР implementation | [Equipment](#device-matrix) |
| `DeviceBusCommutation` | device | Ask.Device.Runtime | switching device implementation | [Equipment](#device-matrix) |
| `DeviceBusCommutationQueryExecutor` | runtime helper | Ask.Device.Runtime | routes and logs УКШ commands through the real protocol or Idle emulator | [Equipment](#real--idle) |
| `DeviceBusCommutationResponseProcessor` | response facade | Ask.Device.ResponseProcessor | validates УКШ JSON/numeric firmware responses and publishes operation/connection/reset results | [Equipment](#real--idle) |
| `DeviceBusCommutationMessages` | message facade | Ask.Device.ResponseProcessor | centralizes protocol messages emitted by УКШ self-check flows | [Equipment](#real--idle) |
| `MultimeterResponseProcessor` | response facade | Ask.Device.ResponseProcessor | централизованно разбирает идентификацию, режим, измерения, прозвонку и системные ошибки Keysight/В7-78/3 | [Equipment](#equipment-architecture) |
| `MultimeterMessages` | message facade | Ask.Device.ResponseProcessor | централизует публикацию рабочих сообщений и результатов самоконтроля мультиметров через Ask.Protocol.Messages; для self-test-сообщений отключает `isBlockStart`, чтобы UI не создавал сворачиваемые блоки | [Equipment](#equipment-architecture) |
| `BreakdownMeasurementStatus` | enum | Ask.Core | задаёт допустимые статусы ответа ППУ: `Test`, `Fail`, `Pass` | [Shared Contracts](#shared-contracts-and-dto) |
| `BreakdownMeasurementResponse` | shared response DTO | Ask.Core | хранит типизированный статус, измеренное значение и единицу составного ответа ППУ для ACW/DCW/IR | [Shared Contracts](#shared-contracts-and-dto) |
| `BreakdownTesterResponseProcessor` | response facade | Ask.Device.ResponseProcessor | централизованно проверяет идентификацию, режимы, состояния, числовые параметры и измерительные ответы GPT-79904; возвращает общий `BreakdownMeasurementResponse` | [Equipment](#equipment-architecture) |
| `BreakdownTesterMessages` | message facade | Ask.Device.ResponseProcessor | маршрутизирует рабочие сообщения и результаты самоконтроля GPT через Ask.Protocol.Messages | [Equipment](#equipment-architecture) |
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

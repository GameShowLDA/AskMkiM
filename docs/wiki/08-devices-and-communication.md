# Устройства и коммуникация

## Общая модель

В проекте устройства описываются в `NewCore` и конфигурируются через `DataBaseConfigruration`.

Схема по слоям такая:

- `Ask.Core` задает интерфейсы устройств;
- `NewCore/Device` задает конкретные классы;
- `NewCore/Communication` задает протоколы;
- `NewCore/Function` задает менеджеры и прикладные действия;
- `DataBaseConfigruration/Services/Device` загружает и сохраняет конфигурацию устройств.

## Иерархия устройств

```mermaid
flowchart TD
    IDevice["IDevice"] --> IP["DeviceWithIP"]
    IDevice --> COM["DeviceWithCOM"]
    IDevice --> USB["DeviceWithUSB"]

    IP --> Chassis["ManagerChassis"]
    IP --> Relay["ModuleRelayControl"]
    IP --> Mint["ModuleVoltageCurrentSource"]
    IP --> Switch["DeviceBusCommutation"]
    IP --> Fast["KeysightDevice"]

    COM --> Gpt["GPT79904"]
    USB --> Ups["MikUps1101rRmDevice"]
```

## Конкретные типы оборудования

Сейчас в проекте есть:

- `ManagerChassis`
- `ModuleRelayControl`
- `ModuleVoltageCurrentSource`
- `DeviceBusCommutation`
- `KeysightDevice`
- `GPT79904`
- `MikUps1101rRmDevice`

## Протоколы связи

В `NewCore/Communication` используются:

- `SerialDeviceProtocol`
- `UdpDeviceProtocol`
- `KeysightDeviceProtocol`
- `UninterruptiblePowerSupplyUsbProtocol`

## Как связываются устройство и протокол

### COM-устройства

`DeviceWithCOM`:

- хранит `SerialPort`;
- умеет восстанавливать порт из `ConnectionDetails`;
- автоматически создает `SerialDeviceProtocol`.

### IP-устройства

`DeviceWithIP`:

- хранит `IPAddress`;
- создает `UdpDeviceProtocol`.

### USB-устройства

`DeviceWithUSB`:

- хранит строку подключения;
- использует отдельный USB-протокол в конкретной реализации.

## Менеджеры функций

Большая часть прикладных действий на устройствах вынесена в менеджеры.

Примеры:

- для `ModuleRelayControl`:
  - `PointManager`
  - `BusManager`
  - `MeterManager`
  - `StateManager`
  - `SelfTestManager`
- для `ModuleVoltageCurrentSource`:
  - `VoltageManager`
  - `CurrentManager`
  - `BusManager`
  - `StateManager`
  - `SelfTestManager`
- для `DeviceBusCommutation`:
  - `RelayManager`
  - `ResistorManager`
  - `CapacitorManager`
  - `ConnectorManager`
  - `StateManager`
- для `GPT`:
  - `ConnectableManager`
  - командные менеджеры режимов

## Адаптеры

В `NewCore/FunctionAdapters` есть адаптеры для приведения внутренних менеджеров к интерфейсам из `Ask.Core`.

Это полезно, потому что:

- UI и Engine работают через интерфейсы;
- concrete-логика устройства остается в `NewCore`.

## Где хранится конфигурация устройств

В `DataBaseConfigruration` есть сервисы:

- `ChassisManagerServices`
- `RelaySwitchModuleServices`
- `PowerSourceModuleServices`
- `SwitchingDeviceServices`
- `FastMeterServices`
- `BreakdownTesterServices`
- `RackServices`
- `UninterruptiblePowerSupplyServices`

Они поднимают устройства из SQLite-конфигурации.

## Подключение и reset

Многие алгоритмы используют единый контракт `IConnectable`.

Из него вызываются действия вида:

- `InitializeAsync`
- `ConnectAsync`
- `DisconnectAsync`
- `ResetAsync`

Именно через эти методы движок и метрология приводят оборудование в рабочее состояние.

## Что важно при отладке устройств

- Если устройство “есть в коде”, но не находится в сценарии, сначала проверять базу и сервисы `DataBaseConfigruration`.
- Если устройство найдено, но не отвечает, смотреть `ConnectionDetails`, конкретный `IDeviceProtocol` и `ConnectableManager`.
- Для проверок оборудования значимы не только сами устройства, но и корректность привязки точек, шин и номеров модулей.

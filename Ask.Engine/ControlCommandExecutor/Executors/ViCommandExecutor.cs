using Ask.Core.Services.EventCore.Adapters;
using Ask.Core.Services.Extensions;
using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.PowerSupply;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.SwitchingDevice;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums.Commands;
using Ask.Core.Shared.Metadata.Static.Messages;
using Ask.DataBase.Engine.Static.Devices;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandExecutor.Execution;
using Ask.Engine.Equipment;

namespace Ask.Engine.ControlCommandExecutor.Executors;

/// <summary>
/// Выполняет команду управления внешним источником питания.
/// </summary>
internal sealed class ViCommandExecutor : CommandExecutorBase, ICommandExecutor
{
  private const string PintDeviceName = "ПИНТ";

  private readonly Func<int, int, CancellationToken, Task<IPowerSupply?>> powerSupplyProvider;
  private readonly Func<int, CancellationToken, Task<ISwitchingDevice?>> switchingDeviceProvider;
  private readonly Func<int, CancellationToken, Task<SupportedBusPairs>> busPairProvider;

  /// <summary>
  /// Создаёт исполнитель, использующий оборудование из текущей конфигурации.
  /// </summary>
  public ViCommandExecutor()
    : this(
      PowerSupplies.GetDeviceByNumberChassisAsync,
      async (chassisNumber, cancellationToken) =>
        (await SwitchingDevices.GetDevicesByNumberChassisAsync(chassisNumber, cancellationToken)).FirstOrDefault(),
      async (chassisNumber, cancellationToken) => RelayModuleBusPairResolver.Resolve(
        await RelaySwitchModules.GetDevicesByNumberChassisAsync(chassisNumber, cancellationToken),
        chassisNumber))
  {
  }

  internal ViCommandExecutor(
    Func<int, int, CancellationToken, Task<IPowerSupply?>> powerSupplyProvider,
    Func<int, CancellationToken, Task<ISwitchingDevice?>> switchingDeviceProvider,
    Func<int, CancellationToken, Task<SupportedBusPairs>> busPairProvider)
  {
    this.powerSupplyProvider = powerSupplyProvider ?? throw new ArgumentNullException(nameof(powerSupplyProvider));
    this.switchingDeviceProvider = switchingDeviceProvider ?? throw new ArgumentNullException(nameof(switchingDeviceProvider));
    this.busPairProvider = busPairProvider ?? throw new ArgumentNullException(nameof(busPairProvider));
  }

  /// <inheritdoc />
  public string Mnemonic => EnumExtensions.GetCommandOrganizationalInfo(OrganizationalComands.VI).DisplayName;

  /// <inheritdoc />
  public async Task ExecuteAsync(CommandExecutionContext context, ProtocolModel protocolModel)
  {
    var command = GetRequiredCommand<ViCommandModel>(context);
    SetActiveLine(context, command);

    string nameCommand = $"{command.CommandNumber} {command.Mnemonic}";
    string message = CommandMessages.FormatSourceLines(command.SourceLines);
    await CommandMessages.PublishCommandExecutionAsync(context.Console, nameCommand, message);

    int chassisNumber = command.PowerSupplyChassisNumber
      ?? throw new InvalidOperationException("Для команды ВИ не определён номер тестера источника питания.");
    int pintNumber = command.PowerSupplyNumber
      ?? throw new InvalidOperationException("Для команды ВИ не определён номер ПИНТ.");
    int powerSupplyDeviceNumber = command.PowerSupplyDeviceNumber
      ?? throw new InvalidOperationException("Для команды ВИ не определён аппаратный номер источника питания.");
    CancellationToken cancellationToken = context.Console.GetCancellationToken();
    bool skipSwitchingDelay = command.AlgorithmKey.Contains("Б", StringComparer.OrdinalIgnoreCase);

    var powerSupply = await powerSupplyProvider(chassisNumber, powerSupplyDeviceNumber, cancellationToken)
      ?? throw new InvalidOperationException(
        $"ПИНТ {pintNumber} тестера {chassisNumber} не найден.");
    var switchingDevice = await switchingDeviceProvider(chassisNumber, cancellationToken)
      ?? throw new InvalidOperationException(
        $"Устройство коммутации тестера {chassisNumber} не найдено.");

    if (command.Operation == ViOperation.ConfigureAndConnect)
    {
      SwitchingBusNew busPair = GetBusPair(command);
      SupportedBusPairs modulePairs = await busPairProvider(chassisNumber, cancellationToken);
      if (!powerSupply.SupportedBusPairs.Supports(busPair) ||
          !switchingDevice.SupportedBusPairs.Supports(busPair) ||
          !modulePairs.Supports(busPair))
      {
        throw new InvalidOperationException(
          $"Пара шин {busPair} недоступна для ПИНТ, УКШ или не представлена модулем МКР.");
      }
    }

    EquipmentService.RegisterUsedDevices([powerSupply, switchingDevice]);
    var sourceInitialization = await powerSupply.ConnectableManager.InitializeAsync(context.Console);
    if (!sourceInitialization.Connect)
      throw new InvalidOperationException($"Не удалось инициализировать ПИНТ {pintNumber}.");

    var switchingInitialization = await switchingDevice.ConnectableManager.InitializeAsync(context.Console);
    if (!switchingInitialization.Connect)
      throw new InvalidOperationException($"Не удалось инициализировать устройство коммутации тестера {chassisNumber}.");

    switch (command.Operation)
    {
      case ViOperation.DisconnectAndRelease:
        await DisconnectAndResetAsync(
          powerSupply, switchingDevice, context, cancellationToken, skipSwitchingDelay);
        break;

      case ViOperation.ConfigureWithoutBuses:
        await ConfigureAsync(
          command, powerSupply, switchingDevice, context, cancellationToken,
          connectToBuses: false, skipSwitchingDelay: skipSwitchingDelay);
        break;

      case ViOperation.ConfigureAndConnect:
        await ConfigureAsync(
          command, powerSupply, switchingDevice, context, cancellationToken,
          connectToBuses: true, skipSwitchingDelay: skipSwitchingDelay);
        break;

      default:
        throw new InvalidOperationException("Для команды ВИ не определено выполняемое действие.");
    }

    ExecutionEventAdapter.RaiseDevicesChanged([powerSupply, switchingDevice]);
  }

  private static async Task DisconnectAndResetAsync(
    IPowerSupply powerSupply,
    ISwitchingDevice switchingDevice,
    CommandExecutionContext context,
    CancellationToken cancellationToken,
    bool skipSwitchingDelay)
  {
    await powerSupply.OutputManager.SetOutputStateAsync(false, cancellationToken, context.Console);
    await DisconnectPintAsync(switchingDevice, context, skipSwitchingDelay);
    await powerSupply.VoltageManager.SetVoltageAsync(
      powerSupply.VoltageManager.MinVoltage,
      cancellationToken,
      context.Console);
    await powerSupply.CurrentManager.SetCurrentAsync(
      powerSupply.CurrentManager.MinCurrent,
      cancellationToken,
      context.Console);
  }

  private static async Task ConfigureAsync(
    ViCommandModel command,
    IPowerSupply powerSupply,
    ISwitchingDevice switchingDevice,
    CommandExecutionContext context,
    CancellationToken cancellationToken,
    bool connectToBuses,
    bool skipSwitchingDelay)
  {
    decimal voltage = command.Voltage
      ?? throw new InvalidOperationException("Для включения источника не задано напряжение.");
    decimal current = command.Current
      ?? throw new InvalidOperationException("Для включения источника не задан ток.");

    // Перекоммутация выполняется только при снятом выходном напряжении.
    await powerSupply.OutputManager.SetOutputStateAsync(false, cancellationToken, context.Console);
    await powerSupply.CurrentManager.SetCurrentAsync(current, cancellationToken, context.Console);
    await powerSupply.VoltageManager.SetVoltageAsync(voltage, cancellationToken, context.Console);

    if (connectToBuses)
    {
      bool connected = await switchingDevice.ConnectorManager.ConnectPINT(
        GetBusPair(command), context.Console, skipSwitchingDelay);
      if (!connected)
        throw new InvalidOperationException("Не удалось подключить ПИНТ к выбранной паре шин.");
    }
    else
    {
      await DisconnectPintAsync(switchingDevice, context, skipSwitchingDelay);
    }

    await powerSupply.OutputManager.SetOutputStateAsync(true, cancellationToken, context.Console);

    if (connectToBuses)
    {
      try
      {
        var regulationMode = await powerSupply.OutputManager.GetRegulationModeAsync(cancellationToken, context.Console);
        if (regulationMode != PowerSupplyRegulationMode.ConstantVoltage)
        {
          throw new InvalidOperationException(
            $"Источник питания перешёл в недопустимый режим: {regulationMode}.");
        }
      }
      catch
      {
        await powerSupply.OutputManager.SetOutputStateAsync(false, cancellationToken, context.Console);
        await DisconnectPintAsync(switchingDevice, context, skipSwitchingDelay);
        throw;
      }
    }
  }

  private static async Task DisconnectPintAsync(
    ISwitchingDevice switchingDevice,
    CommandExecutionContext context,
    bool skipSwitchingDelay)
  {
    var connectedBuses = switchingDevice.ConnectorManager
      .GetConnectedDevices()
      .Where(connection => string.Equals(connection.device, PintDeviceName, StringComparison.OrdinalIgnoreCase))
      .Select(connection => connection.bus)
      .Distinct()
      .ToArray();

    foreach (var bus in connectedBuses)
    {
      bool disconnected = await switchingDevice.ConnectorManager.DisconnectPINT(
        bus, context.Console, skipSwitchingDelay);
      if (!disconnected)
        throw new InvalidOperationException($"Не удалось отключить ПИНТ от пары шин {bus}.");
    }
  }

  private static SwitchingBusNew GetBusPair(ViCommandModel command)
  {
    string? positiveBus = command.PositiveBus?.ToString();
    string? negativeBus = command.NegativeBus?.ToString();
    if (positiveBus is null
        || negativeBus is null
        || !positiveBus.StartsWith('A')
        || !negativeBus.StartsWith('B')
        || positiveBus[^1] != negativeBus[^1]
        || !int.TryParse(positiveBus[^1].ToString(), out int pairNumber)
        || !Enum.IsDefined(typeof(SwitchingBusNew), pairNumber))
    {
      throw new InvalidOperationException("Для команды ВИ не определена корректная пара шин.");
    }

    return (SwitchingBusNew)pairNumber;
  }
}

using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.BreakdownTester;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Chassis;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.PowerSourceModule;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Rack;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.RelaySwitchModule;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.SwitchingDevice;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.UninterruptiblePowerSupply;
using Ask.DataBase.Engine.Static.Devices;
using ConsoleUI.ConsoleCommanding.Core;

namespace ConsoleUI.ConsoleCommanding.Commands
{
  public class DeviceStatusCommand : ICommand
  {
    public string Name => "status";

    public async Task ExecuteAsync(string[] args, CommandContext context)
    {
      if (args.Length > 1)
      {
        WriteUsage(context);
        return;
      }

      bool showHelp = args.Length == 1 && IsHelpArgument(args[0]);
      int? deviceNumber = null;
      if (args.Length == 1 && !showHelp)
      {
        if (!int.TryParse(args[0], out int parsedNumber))
        {
          context.Console.WriteLine($"Некорректный номер устройства: {args[0]}");
          WriteUsage(context);
          return;
        }

        deviceNumber = parsedNumber;
      }

      List<IDevice> devices;
      try
      {
        devices = await LoadDevicesAsync();
      }
      catch (Exception ex)
      {
        context.Console.WriteLine($"Не удалось получить список устройств: {ex.Message}");
        return;
      }

      if (devices.Count == 0)
      {
        context.Console.WriteLine("  Устройства не найдены.");
        return;
      }

      if (showHelp)
      {
        WriteHelp(context, devices);
        return;
      }

      var selectedDevices = deviceNumber.HasValue
        ? devices.Where(x => x.Number == deviceNumber.Value).ToList()
        : devices;

      if (selectedDevices.Count == 0)
      {
        context.Console.WriteLine($"Устройство с номером {deviceNumber} не найдено.");
        context.Console.WriteLine("Введите status --help, чтобы посмотреть номера устройств из конфигурации.");
        return;
      }

      context.Console.WriteLine(deviceNumber.HasValue
        ? $"Статусы устройств с номером {deviceNumber.Value}:"
        : "Статусы устройств:");

      foreach (var device in OrderDevices(selectedDevices))
      {
        WriteDeviceStatus(context, device);
      }
    }

    private static bool IsHelpArgument(string arg)
    {
      return string.Equals(arg, "--help", StringComparison.OrdinalIgnoreCase)
        || string.Equals(arg, "-h", StringComparison.OrdinalIgnoreCase)
        || string.Equals(arg, "help", StringComparison.OrdinalIgnoreCase)
        || string.Equals(arg, "/?", StringComparison.OrdinalIgnoreCase);
    }

    private static void WriteUsage(CommandContext context)
    {
      context.Console.WriteLine("Использование:");
      context.Console.WriteLine("  status          - вывести статусы всех устройств");
      context.Console.WriteLine("  status <номер>  - вывести статус устройства с номером из конфигурации");
      context.Console.WriteLine("  status --help   - показать номера устройств из конфигурации");
    }

    private static void WriteHelp(CommandContext context, IReadOnlyCollection<IDevice> devices)
    {
      WriteUsage(context);
      context.Console.WriteLine(string.Empty);
      context.Console.WriteLine("Номера устройств из конфигурации:");

      foreach (var device in OrderDevices(devices))
      {
        context.Console.WriteLine($"  {device.Number}: {device.Name} [{device.DeviceType}]{FormatChassis(device)} Id={device.Id}");
      }
    }

    private static IOrderedEnumerable<IDevice> OrderDevices(IEnumerable<IDevice> devices)
    {
      return devices
        .OrderBy(x => x.DeviceType.ToString())
        .ThenBy(GetChassisNumber)
        .ThenBy(x => x.Number)
        .ThenBy(x => x.Id);
    }

    private static async Task<List<IDevice>> LoadDevicesAsync()
    {
      var chassisTask = ChassisManagers.GetAllAsync();
      var racksTask = Racks.GetAllAsync();
      var fastMetersTask = FastMeters.GetAllAsync();
      var breakdownTestersTask = BreakdownTesters.GetAllAsync();
      var powerSourceModulesTask = PowerSourceModules.GetAllAsync();
      var relaySwitchModulesTask = RelaySwitchModules.GetAllAsync();
      var switchingDevicesTask = SwitchingDevices.GetAllAsync();
      var uninterruptiblePowerSuppliesTask = UninterruptiblePowerSupplies.GetAllAsync();

      await Task.WhenAll(
        chassisTask,
        racksTask,
        fastMetersTask,
        breakdownTestersTask,
        powerSourceModulesTask,
        relaySwitchModulesTask,
        switchingDevicesTask,
        uninterruptiblePowerSuppliesTask);

      var devices = new List<IDevice>();
      devices.AddRange(chassisTask.Result);
      devices.AddRange(racksTask.Result);
      devices.AddRange(fastMetersTask.Result);
      devices.AddRange(breakdownTestersTask.Result);
      devices.AddRange(powerSourceModulesTask.Result);
      devices.AddRange(relaySwitchModulesTask.Result);
      devices.AddRange(switchingDevicesTask.Result);
      devices.AddRange(uninterruptiblePowerSuppliesTask.Result);

      return devices;
    }

    private static void WriteDeviceStatus(CommandContext context, IDevice device)
    {
      context.Console.WriteLine($"[{device.DeviceType}] {device.Name} | Id={device.Id}, Number={device.Number}{FormatChassis(device)}");
      context.Console.WriteLine($"  Подключение: {FormatValue(device.ConnectionDetails)}");
      WriteConnectionStatus(context, device);
      WriteTypedStatus(context, device);
      context.Console.WriteLine(string.Empty);
    }

    private static void WriteConnectionStatus(CommandContext context, IDevice device)
    {
      if (device.ConnectableManager == null)
      {
        context.Console.WriteLine("  Статус подключения: нет менеджера подключения");
        return;
      }

      try
      {
        var status = device.ConnectableManager.GetConnectionStatus();
        WriteMultiline(context, "  Статус подключения", status);
      }
      catch (Exception ex)
      {
        context.Console.WriteLine($"  Статус подключения: ошибка чтения ({ex.Message})");
      }
    }

    private static void WriteTypedStatus(CommandContext context, IDevice device)
    {
      switch (device)
      {
        case IHeadUnit headUnit:
          context.Console.WriteLine($"  Структура шин: {headUnit.BusType}");
          break;
      }

      switch (device)
      {
        case IRelaySwitchModule relayModule:
          context.Console.WriteLine($"  Тип шины МКР: {relayModule.BusType}");
          context.Console.WriteLine($"  Количество точек: {relayModule.PointCount}");
          WriteRelayBusStatus(context, relayModule);
          WriteRelayPointStatus(context, relayModule);
          break;

        case ISwitchingDevice switchingDevice:
          WriteSwitchingDeviceStatus(context, switchingDevice);
          break;

        case IBreakdownTester breakdownTester:
          context.Console.WriteLine($"  Режим: {breakdownTester.Mode}");
          context.Console.WriteLine($"  Пределы: PI(ACW)={breakdownTester.AcwMaxVoltage} В, PI(DCW)={breakdownTester.DcwMaxVoltage} В, IrMax={breakdownTester.IrMaxVoltage} В, IrMin={breakdownTester.IrMinVoltage} В");
          break;

        case IFastMeter fastMeter:
          context.Console.WriteLine($"  Режим измерителя: {fastMeter.TypeMode}");
          context.Console.WriteLine($"  Порог прозвонки: {fastMeter.MaxContinuityResistance} Ом");
          break;

        case IPowerSourceModule powerSourceModule:
          context.Console.WriteLine($"  Калибровка сопротивления: {(string.IsNullOrWhiteSpace(powerSourceModule.ResistanceCalibrationJson) ? "не задана" : "задана")}");
          break;

        case IUninterruptiblePowerSupply ups:
          context.Console.WriteLine($"  Последний путь USB: {FormatValue(ups.LastResolvedDevicePath)}");
          break;

        case IRack:
        case IChassisManager:
          break;
      }
    }

    private static void WriteRelayBusStatus(CommandContext context, IRelaySwitchModule relayModule)
    {
      try
      {
        var buses = relayModule.BusManager?.GetConnectedBuses() ?? [];
        if (buses.Count == 0)
        {
          context.Console.WriteLine("  Подключенные шины МКР: нет");
          return;
        }

        var formatted = string.Join(", ", buses
          .Where(x => x.IsConnected)
          .Select(x => x.Bus.ToString()));

        context.Console.WriteLine($"  Подключенные шины МКР: {FormatCollection(formatted)}");
      }
      catch (Exception ex)
      {
        context.Console.WriteLine($"  Подключенные шины МКР: ошибка чтения ({ex.Message})");
      }
    }

    private static void WriteRelayPointStatus(CommandContext context, IRelaySwitchModule relayModule)
    {
      try
      {
        var points = relayModule.PointManager?.GetConnectedPoints() ?? [];
        if (points.Count == 0)
        {
          context.Console.WriteLine("  Подключенные точки МКР: нет");
          return;
        }

        var formatted = string.Join(", ", points
          .OrderBy(x => x.PointNumber)
          .Select(x => $"{x.PointNumber}->{x.Bus}"));

        context.Console.WriteLine($"  Подключенные точки МКР: {formatted}");
      }
      catch (Exception ex)
      {
        context.Console.WriteLine($"  Подключенные точки МКР: ошибка чтения ({ex.Message})");
      }
    }

    private static void WriteSwitchingDeviceStatus(CommandContext context, ISwitchingDevice switchingDevice)
    {
      try
      {
        var connectedDevices = switchingDevice.ConnectorManager?.GetConnectedDevices() ?? [];
        if (connectedDevices.Count == 0)
        {
          context.Console.WriteLine("  Устройства на шинах УКШ: нет");
          return;
        }

        var formatted = string.Join(", ", connectedDevices
          .OrderBy(x => x.bus.ToString())
          .ThenBy(x => x.device)
          .Select(x => $"{x.bus}->{x.device}"));

        context.Console.WriteLine($"  Устройства на шинах УКШ: {formatted}");
      }
      catch (Exception ex)
      {
        context.Console.WriteLine($"  Устройства на шинах УКШ: ошибка чтения ({ex.Message})");
      }
    }

    private static void WriteMultiline(CommandContext context, string title, string? value)
    {
      if (string.IsNullOrWhiteSpace(value))
      {
        context.Console.WriteLine($"{title}: нет данных");
        return;
      }

      var lines = value
        .Replace("\r\n", "\n")
        .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

      if (lines.Length == 1)
      {
        context.Console.WriteLine($"{title}: {lines[0]}");
        return;
      }

      context.Console.WriteLine($"{title}:");
      foreach (var line in lines)
      {
        context.Console.WriteLine($"    {line}");
      }
    }

    private static string FormatChassis(IDevice device)
    {
      return device is IAttachableDevice attachableDevice
        ? $", Chassis={attachableDevice.NumberChassis}"
        : string.Empty;
    }

    private static int GetChassisNumber(IDevice device)
    {
      return device is IAttachableDevice attachableDevice
        ? attachableDevice.NumberChassis
        : 0;
    }

    private static string FormatValue(string? value)
    {
      return string.IsNullOrWhiteSpace(value)
        ? "не задано"
        : value;
    }

    private static string FormatCollection(string value)
    {
      return string.IsNullOrWhiteSpace(value)
        ? "нет"
        : value;
    }
  }
}

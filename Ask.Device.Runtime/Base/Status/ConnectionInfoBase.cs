using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.BreakdownTester;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.PowerSourceModule;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.RelaySwitchModule;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.SwitchingDevice;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.UninterruptiblePowerSupply;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Device.Runtime.Device;
using System.Text;

namespace Ask.Device.Runtime.Base.Status
{
  /// <summary>
  /// Предоставляет информацию о состоянии подключения устройства.
  /// </summary>
  internal class ConnectionInfoBase : IConnectionInfo
  {
    private readonly IDevice _device;

    /// <summary>
    /// Инициализирует объект информации о подключении устройства.
    /// </summary>
    /// <param name="device">Устройство, для которого формируется информация о подключении.</param>
    /// <param name="connectionType">Тип подключения устройства.</param>
    public ConnectionInfoBase(IDevice device, ConnectionType connectionType)
    {
      _device = device;
      ConnectionType = connectionType;
    }

    /// <inheritdoc />
    public bool IsConnected { get; set; } = false;

    /// <inheritdoc />
    public ConnectionType ConnectionType { get; init; }

    /// <inheritdoc />
    public string GetConnectionStatus()
    {
      var mode = $"Статус подключения {_device.Name}: ";
      if (IsConnected)
      {
        mode += "Подключено\r\n";
      }
      else
      {
        mode += "Отключено";
      }

      if (!IsConnected)
        return mode;

      switch (_device)
      {
        case IMultimeter:
          mode += GetMultimeterStatus((IMultimeter)_device);
          break;

        case IBreakdownTester:
          mode += GetBreakdownTesterStatus((IBreakdownTester)_device);
          break;

        case IRelaySwitchModule:
          mode += GetRelaySwitchModuleStatus((IRelaySwitchModule)_device);
          break;

        case ISwitchingDevice:
          mode += GetSwitchingDeviceStatus((ISwitchingDevice)_device);
          break;

        case IPowerSourceModule:
          mode += GetPowerSourceModuleStatus((IPowerSourceModule)_device);
          break;

        default:
          break;
      }

      return mode;
    }

    /// <summary>
    /// Формирует информацию о модуля источник напряжения и тока.
    /// </summary>
    /// <param name="device">Источник питания.</param>
    /// <returns>Строка с информацией о состоянии устройства.</returns>
    private string GetPowerSourceModuleStatus(IPowerSourceModule device)
    {
      throw new NotImplementedException();
    }

    /// <summary>
    /// Формирует информацию о подключённых устройствах устройства коммутации шин.
    /// </summary>
    /// <param name="device">Коммутационное устройство.</param>
    /// <returns>Строка со списком подключённых устройств.</returns>
    private string GetSwitchingDeviceStatus(ISwitchingDevice device)
    {
      var devices = device.ConnectorManager.GetConnectedDevices();

      if (devices.Count() == 0)
        return "Подключенные устройства:\n  Нет подключённых устройств.";

      var sb = new StringBuilder();
      sb.AppendLine("Подключенные устройства:");

      foreach (var d in devices)
      {
        sb.AppendLine($"  {d.device} — {d.bus}");
      }

      return sb.ToString();
    }

    /// <summary>
    /// Формирует информацию о подключённых шинах и точках релейного модуля коммутации.
    /// </summary>
    /// <param name="device">Релейный модуль.</param>
    /// <returns>Строка с информацией о подключённых шинах и точках.</returns>
    private string GetRelaySwitchModuleStatus(IRelaySwitchModule device)
    {
      var buses = device.BusManager.GetConnectedBuses();
      var points = device.PointManager.GetConnectedPoints();

      var sb = new StringBuilder();

      sb.AppendLine("Подключенные шины:");
      if (buses.Count == 0)
      {
        sb.AppendLine("  Нет подключённых шин.");
      }
      else
      {
        foreach (var b in buses)
          sb.AppendLine($"  {b.Bus}");
      }

      sb.AppendLine();
      sb.AppendLine("Подключенные точки:");

      if (points.Count == 0)
      {
        sb.AppendLine("  Нет подключённых точек.");
      }
      else
      {
        foreach (var p in points)
          sb.AppendLine($"  Точка {p.PointNumber} = {p.Bus}");
      }

      return sb.ToString();
    }

    /// <summary>
    /// Формирует информацию о текущем режиме пробойной установки.
    /// </summary>
    /// <param name="device">Пробойная установка.</param>
    /// <returns>Строка с описанием режима работы.</returns>
    private string GetBreakdownTesterStatus(IBreakdownTester device)
    {
      return device?.Mode switch
      {
        BreakdownTypeMode.ACW => "Режим: ACW",
        BreakdownTypeMode.DCW => "Режим: DCW",
        BreakdownTypeMode.IR => "Режим: IR",
        _ => "Режим не определён",
      };
    }

    /// <summary>
    /// Формирует информацию о текущем режиме мультиметра.
    /// </summary>
    /// <param name="device">Мультиметр.</param>
    /// <returns>Строка с описанием режима измерения.</returns>
    private string GetMultimeterStatus(IMultimeter device)
    {
      var mode = "Режим: ";
      switch (device.TypeMode)
      {
        case MultimeterTypeMode.None:
          mode += "Не задан";
          break;
        case MultimeterTypeMode.AcVoltage:
          mode += "Измерение переменного напряжения";
          break;
        case MultimeterTypeMode.DcVoltage:
          mode += "Измерение постоянного напряжения";
          break;
        case MultimeterTypeMode.Capacitance:
          mode += "Измерение ёмкости.";
          break;
        case MultimeterTypeMode.Continuity:
          mode += "Прозвонка.";
          break;
        case MultimeterTypeMode.Resistance:
          mode += "Измерение электрического сопротивления.";
          break;
        case MultimeterTypeMode.Diode:
          mode += "Проверка диода.";
          break;
      }

      return mode;
    }
  }
}

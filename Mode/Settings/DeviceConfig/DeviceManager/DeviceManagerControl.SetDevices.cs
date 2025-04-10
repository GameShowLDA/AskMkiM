using DataBaseConfiguration.Models.Device;
using NewCore.Base.Device;

namespace Mode.Settings.DeviceConfig.DeviceManager
{
  public partial class DeviceManagerControl
  {
    /// <summary>
    /// Добавление устройства.
    /// </summary>
    /// <typeparam name="T">Тип устройства.</typeparam>
    /// <param name="device">Модель устройства.</param>
    public void AddDevice<T>(T device) where T : IDevice
    {
      switch (device)
      {
        case BreakdownTesterEntity breakdownTester:
          BreakdownTesterControl.AddDevice(breakdownTester);
          break;

        case FastMeterEntity fastMeter:
          FastMeterControl.AddDevice(fastMeter);
          break;

        case PowerSourceModuleEntity powerSource:
          PowerSourceModuleControl.AddDevice(powerSource);
          break;

        case PrecisionMeterEntity precisionMeter:
          PrecisionMeterControl.AddDevice(precisionMeter);
          break;

        case RelaySwitchModuleEntity relaySwitch:
          RelaySwitchModuleControl.AddDevice(relaySwitch);
          break;

        case SwitchingDeviceEntity switchingDevice:
          SwitchingDeviceControl.AddDevice(switchingDevice);
          break;

        default:
          Console.WriteLine("Неизвестный тип устройства.");
          break;
      }
    }

    /// <summary>
    /// Добавление устройства.
    /// </summary>
    /// <typeparam name="T">Тип устройства.</typeparam>
    /// <param name="typeDevices">Тип устройств.</param>
    public void ClearDevice<T>(T typeDevices) where T : IDevice
    {
      switch (typeDevices)
      {
        case BreakdownTesterEntity breakdownTester:
          BreakdownTesterControl.ClearItems();
          break;

        case FastMeterEntity fastMeter:
          FastMeterControl.ClearItems();
          break;

        case PowerSourceModuleEntity powerSource:
          PowerSourceModuleControl.ClearItems();
          break;

        case PrecisionMeterEntity precisionMeter:
          PrecisionMeterControl.ClearItems();
          break;

        case RelaySwitchModuleEntity relaySwitch:
          RelaySwitchModuleControl.ClearItems();
          break;

        case SwitchingDeviceEntity switchingDevice:
          SwitchingDeviceControl.ClearItems();
          break;

        default:
          Console.WriteLine("Неизвестный тип устройства.");
          break;
      }
    }
  }
}

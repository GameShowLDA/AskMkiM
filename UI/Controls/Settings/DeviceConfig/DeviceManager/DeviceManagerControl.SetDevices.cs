using Ask.Core.Shared.DTO.Devices.Base;
using Ask.Core.Shared.DTO.Devices.Breakdown;
using Ask.Core.Shared.DTO.Devices.FastMeter;
using Ask.Core.Shared.DTO.Devices.PowerSourceModule;
using Ask.Core.Shared.DTO.Devices.RelaySwitchModule;
using Ask.Core.Shared.DTO.Devices.SwitchingDevice;
using Ask.Core.Shared.DTO.Devices.UninterruptiblePowerSupply;
using Ask.Core.Shared.Entity.Devices;
using Ask.Core.Shared.Interfaces.DeviceInterfaces;

namespace UI.Controls.Settings.DeviceConfig.DeviceManager
{
  public partial class DeviceManagerControl
  {
    /// <summary>
    /// Добавление устройства.
    /// </summary>
    /// <typeparam name="T">Тип устройства.</typeparam>
    /// <param name="device">Модель устройства.</param>
    public void AddDevice<T>(T device) where T : DeviceDto
    {
      switch (device)
      {
        case BreakdownTesterDto breakdownTester:
          BreakdownTesterControl.AddDevice(breakdownTester);
          break;

        case FastMeterDto fastMeter:
          FastMeterControl.AddDevice(fastMeter);
          break;

        case PowerSourceModuleDto powerSource:
          PowerSourceModuleControl.AddDevice(powerSource);
          break;

        case RelaySwitchModuleDto relaySwitch:
          RelaySwitchModuleControl.AddDevice(relaySwitch);
          break;

        case SwitchingDeviceDto switchingDevice:
          SwitchingDeviceControl.AddDevice(switchingDevice);
          break;

        case UninterruptiblePowerSupplyDto uninterruptiblePowerSupply:
          UninterruptiblePowerSupplyControl.AddDevice(uninterruptiblePowerSupply);
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
    public void ClearDevice<T>(T typeDevices) where T : DeviceDto
    {
      switch (typeDevices)
      {
        case BreakdownTesterDto breakdownTester:
          BreakdownTesterControl.ClearItems();
          break;

        case FastMeterDto fastMeter:
          FastMeterControl.ClearItems();
          break;

        case PowerSourceModuleDto powerSource:
          PowerSourceModuleControl.ClearItems();
          break;

        case RelaySwitchModuleEntity relaySwitch:
          RelaySwitchModuleControl.ClearItems();
          break;

        case SwitchingDeviceEntity switchingDevice:
          SwitchingDeviceControl.ClearItems();
          break;

        case UninterruptiblePowerSupplyEntity uninterruptiblePowerSupply:
          UninterruptiblePowerSupplyControl.ClearItems();
          break;

        default:
          Console.WriteLine("Неизвестный тип устройства.");
          break;
      }
    }
  }
}

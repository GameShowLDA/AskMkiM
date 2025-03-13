using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using AppConfig.DataBase.Models;
using AppConfig.DataBase.Services;
using NewCore.Base;

namespace Mode.Settings.DeviceConfig.DeviceManager
{
  public partial class DeviceManagerControl
  {
    public void AddDevice<T>(T device) where T : IDevice
    {
      var type = device.GetType();
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
  }
}

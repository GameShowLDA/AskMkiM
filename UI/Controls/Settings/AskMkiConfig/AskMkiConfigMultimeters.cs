using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using Ask.Device.Runtime.Base.DeviceProtocol;
using Ask.Device.Runtime.Device.Multimeters;
using System;
using System.Linq;

namespace UI.Controls.Settings.AskMkiConfig;

public partial class AskMkiConfigControl
{
  private static readonly Type[] RuntimeMultimeterTypes =
  [
    typeof(MultimeterB7783),
    typeof(KeysightDevice),
    typeof(MultiAgilent34401A),
    typeof(MultiAgilent34450A),
    typeof(MultiAgilentRigol),
    typeof(MultiAgilentCom),
    typeof(MultiDmm4040),
    typeof(MultiDmm4050)
  ];

  private static bool IsUsbVoltmeter(string? deviceClass)
  {
    var type = ResolveVoltmeterType(deviceClass);
    return type != null && typeof(DeviceWithUSB).IsAssignableFrom(type);
  }

  private static bool IsIpVoltmeter(string? deviceClass)
  {
    var type = ResolveVoltmeterType(deviceClass);
    return type != null && typeof(DeviceWithIP).IsAssignableFrom(type);
  }

  private static bool IsComVoltmeter(string? deviceClass)
  {
    var type = ResolveVoltmeterType(deviceClass);
    return type != null && typeof(DeviceWithCOM).IsAssignableFrom(type);
  }

  private static Type? ResolveVoltmeterType(string? deviceClass)
  {
    if (string.IsNullOrWhiteSpace(deviceClass))
    {
      return null;
    }

    return RuntimeMultimeterTypes.FirstOrDefault(item =>
      string.Equals(item.FullName, deviceClass, StringComparison.Ordinal)
      || string.Equals(item.Name, deviceClass, StringComparison.Ordinal));
  }

  private static IDevice? CreateVoltmeterDevice(string? deviceClass)
  {
    var type = ResolveVoltmeterType(deviceClass);
    if (type == null || Activator.CreateInstance(type) is not IDevice device)
    {
      return null;
    }

    return device;
  }
}

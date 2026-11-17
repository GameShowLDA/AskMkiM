using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Device.Runtime.Device;

namespace Ask.Engine.UnitTests.DeviceRuntime;

public sealed class SupportedBusPairsTests
{
  [Theory]
  [MemberData(nameof(DeviceCapabilities))]
  public void Device_DefaultBusPairsMatchHardwareCapabilities(
    IAttachableDevice device,
    SupportedBusPairs expected)
  {
    try
    {
      Assert.Equal(expected, device.SupportedBusPairs);
    }
    finally
    {
      (device as IDisposable)?.Dispose();
    }
  }

  public static TheoryData<IAttachableDevice, SupportedBusPairs> DeviceCapabilities => new()
  {
    { new ModuleRelayControl(), SupportedBusPairs.AB1 },
    { new DeviceBusCommutation(), SupportedBusPairs.All },
    { new B5108(), SupportedBusPairs.AB2 | SupportedBusPairs.AB3 | SupportedBusPairs.AB4 },
    { new GPT79904(), SupportedBusPairs.AB1 },
    { new MultimeterB7783(), SupportedBusPairs.All },
    { new KeysightDevice(), SupportedBusPairs.All },
    { new ModuleVoltageCurrentSource(), SupportedBusPairs.All },
    { new MikUps1101rRmDevice(), SupportedBusPairs.None },
  };
}

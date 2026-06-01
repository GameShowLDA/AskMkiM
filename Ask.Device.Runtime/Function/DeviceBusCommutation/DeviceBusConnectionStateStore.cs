using Ask.Core.Shared.DTO.Devices.RelaySwitchModule;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;

namespace Ask.Device.Runtime.Function.DeviceBusCommutation
{
  internal enum DeviceBusConnectionType
  {
    Multimeter,
    PINT,
    BreakdownTester,
    BreakdownTesterAndMultimeter,
  }

  internal sealed class DeviceBusConnectionStateStore
  {
    private readonly object syncRoot = new object();
    private readonly ConnectionState[,] states;

    public DeviceBusConnectionStateStore()
    {
      states = new ConnectionState[
        Enum.GetValues(typeof(DeviceBusConnectionType)).Length,
        Enum.GetValues(typeof(SwitchingBusNew)).Length];

      Reset();
    }

    public void Reset()
    {
      lock (syncRoot)
      {
        for (int device = 0; device < states.GetLength(0); device++)
        {
          for (int bus = 0; bus < states.GetLength(1); bus++)
          {
            states[device, bus] = ConnectionState.Disconnected;
          }
        }
      }
    }

    public void Set(DeviceBusConnectionType device, SwitchingBusNew bus, bool connected)
    {
      lock (syncRoot)
      {
        states[GetDeviceIndex(device), GetBusIndex(bus)] = connected ? ConnectionState.Connected : ConnectionState.Disconnected;
      }
    }

    public IReadOnlyList<(DeviceBusConnectionType Device, SwitchingBusNew Bus)> GetConnected(DeviceBusConnectionType device)
    {
      lock (syncRoot)
      {
        var result = new List<(DeviceBusConnectionType Device, SwitchingBusNew Bus)>();

        foreach (SwitchingBusNew bus in Enum.GetValues(typeof(SwitchingBusNew)))
        {
          if (states[GetDeviceIndex(device), GetBusIndex(bus)] == ConnectionState.Connected)
          {
            result.Add((device, bus));
          }
        }

        return result;
      }
    }

    public IReadOnlyList<DeviceConnectionInfo> GetConnectedDevices()
    {
      lock (syncRoot)
      {
        var result = new List<DeviceConnectionInfo>();

        foreach (DeviceBusConnectionType device in Enum.GetValues(typeof(DeviceBusConnectionType)))
        {
          foreach (SwitchingBusNew bus in Enum.GetValues(typeof(SwitchingBusNew)))
          {
            if (states[GetDeviceIndex(device), GetBusIndex(bus)] == ConnectionState.Connected)
            {
              result.Add(new DeviceConnectionInfo(bus, DeviceTypeToText(device)));
            }
          }
        }

        return result
          .OrderBy(x => x.bus)
          .ThenBy(x => x.device)
          .ToList();
      }
    }

    private static string DeviceTypeToText(DeviceBusConnectionType type) => type switch
    {
      DeviceBusConnectionType.Multimeter => "Мультиметр",
      DeviceBusConnectionType.PINT => "ПИНТ",
      DeviceBusConnectionType.BreakdownTester => "Пробойная установка",
      DeviceBusConnectionType.BreakdownTesterAndMultimeter => "Пробойная установка + мультиметр",
      _ => type.ToString()
    };

    private int GetDeviceIndex(DeviceBusConnectionType device)
    {
      int index = (int)device;
      if (index < 0 || index >= states.GetLength(0))
      {
        throw new ArgumentOutOfRangeException(nameof(device));
      }

      return index;
    }

    private int GetBusIndex(SwitchingBusNew bus)
    {
      int index = (int)bus - 1;
      if (index < 0 || index >= states.GetLength(1))
      {
        throw new ArgumentOutOfRangeException(nameof(bus));
      }

      return index;
    }
  }
}

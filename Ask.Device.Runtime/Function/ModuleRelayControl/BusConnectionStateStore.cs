using Ask.Core.Shared.DTO.Devices.RelaySwitchModule;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;

namespace Ask.Device.Runtime.Function.ModuleRelayControl
{
  internal sealed class BusConnectionStateStore
  {
    private readonly object syncRoot = new object();
    private readonly ConnectionState[] buses;

    public BusConnectionStateStore()
    {
      buses = new ConnectionState[Enum.GetValues(typeof(SwitchingBus)).Length];
      Reset();
    }

    public void Reset()
    {
      lock (syncRoot)
      {
        Array.Fill(buses, ConnectionState.Disconnected);
      }
    }

    public void Set(SwitchingBus bus, bool connected)
    {
      lock (syncRoot)
      {
        buses[GetIndex(bus)] = connected ? ConnectionState.Connected : ConnectionState.Disconnected;
      }
    }

    public IReadOnlyList<BusConnectionInfo> GetConnectedBuses()
    {
      lock (syncRoot)
      {
        var result = new List<BusConnectionInfo>();

        foreach (SwitchingBus bus in Enum.GetValues(typeof(SwitchingBus)))
        {
          if (buses[GetIndex(bus)] == ConnectionState.Connected)
          {
            result.Add(new BusConnectionInfo(bus, true));
          }
        }

        return result;
      }
    }

    private int GetIndex(SwitchingBus bus)
    {
      int index = (int)bus;
      if (index < 0 || index >= buses.Length)
      {
        throw new ArgumentOutOfRangeException(nameof(bus));
      }

      return index;
    }
  }
}

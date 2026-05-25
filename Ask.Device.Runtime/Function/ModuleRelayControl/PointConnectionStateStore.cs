using Ask.Core.Shared.DTO.Devices.RelaySwitchModule;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;

namespace Ask.Device.Runtime.Function.ModuleRelayControl
{
  internal sealed class PointConnectionStateStore
  {
    private readonly object syncRoot = new object();
    private ConnectionState[] busA;
    private ConnectionState[] busB;

    public PointConnectionStateStore(int pointCount)
    {
      busA = CreatePointStateArray(pointCount);
      busB = CreatePointStateArray(pointCount);
      Reset(pointCount);
    }

    public void Reset(int pointCount)
    {
      lock (syncRoot)
      {
        if (busA.Length != pointCount + 1)
        {
          busA = CreatePointStateArray(pointCount);
          busB = CreatePointStateArray(pointCount);
        }

        Array.Fill(busA, ConnectionState.Disconnected);
        Array.Fill(busB, ConnectionState.Disconnected);
      }
    }

    public void Set(int number, BusPoint bus, bool connected)
    {
      Set(number, bus, connected ? ConnectionState.Connected : ConnectionState.Disconnected);
    }

    public void Set(int number, BusPoint bus, ConnectionState state)
    {
      lock (syncRoot)
      {
        ValidatePointNumber(number);

        if (bus is BusPoint.A or BusPoint.AB)
        {
          busA[number] = state;
        }

        if (bus is BusPoint.B or BusPoint.AB)
        {
          busB[number] = state;
        }
      }
    }

    public void SetRange(int firstPoint, int lastPoint, BusPoint bus, bool connected)
    {
      lock (syncRoot)
      {
        ValidatePointNumber(firstPoint);
        ValidatePointNumber(lastPoint);

        for (int number = firstPoint; number <= lastPoint; number++)
        {
          if (bus is BusPoint.A or BusPoint.AB)
          {
            busA[number] = connected ? ConnectionState.Connected : ConnectionState.Disconnected;
          }

          if (bus is BusPoint.B or BusPoint.AB)
          {
            busB[number] = connected ? ConnectionState.Connected : ConnectionState.Disconnected;
          }
        }
      }
    }

    public IReadOnlyList<int> GetConnectedPointNumbers(BusPoint bus)
    {
      lock (syncRoot)
      {
        var source = bus == BusPoint.A ? busA : busB;
        var result = new List<int>();

        for (int number = 1; number < source.Length; number++)
        {
          if (source[number] == ConnectionState.Connected)
          {
            result.Add(number);
          }
        }

        return result;
      }
    }

    public IReadOnlyList<PointConnectionInfo> GetConnectedPoints()
    {
      lock (syncRoot)
      {
        var result = new List<PointConnectionInfo>();

        for (int number = 1; number < busA.Length; number++)
        {
          if (busA[number] == ConnectionState.Connected)
          {
            result.Add(new PointConnectionInfo(number, BusPoint.A));
          }
        }

        for (int number = 1; number < busB.Length; number++)
        {
          if (busB[number] == ConnectionState.Connected)
          {
            result.Add(new PointConnectionInfo(number, BusPoint.B));
          }
        }

        return result;
      }
    }

    private static ConnectionState[] CreatePointStateArray(int pointCount)
    {
      if (pointCount < 0)
      {
        throw new ArgumentOutOfRangeException(nameof(pointCount));
      }

      return new ConnectionState[pointCount + 1];
    }

    private void ValidatePointNumber(int number)
    {
      if (number <= 0 || number >= busA.Length)
      {
        throw new ArgumentOutOfRangeException(nameof(number));
      }
    }
  }
}

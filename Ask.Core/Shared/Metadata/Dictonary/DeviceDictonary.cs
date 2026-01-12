using Ask.Core.Shared.Metadata.Enums.DeviceEnums;

namespace Ask.Core.Shared.Metadata.Dictonary
{
  public static class DeviceDictonary
  {
    /// <summary>
    /// Словарь соответствий шин и их параметров (группа, номер).
    /// </summary>
    public static readonly Dictionary<SwitchingBus, Tuple<int, int>> BusParameters = new Dictionary<SwitchingBus, Tuple<int, int>>
    {
      { SwitchingBus.A1, Tuple.Create(1, 1) },
      { SwitchingBus.A2, Tuple.Create(1, 2) },
      { SwitchingBus.A3, Tuple.Create(1, 3) },
      { SwitchingBus.A4, Tuple.Create(1, 4) },
      { SwitchingBus.B1, Tuple.Create(2, 1) },
      { SwitchingBus.B2, Tuple.Create(2, 2) },
      { SwitchingBus.B3, Tuple.Create(2, 3) },
      { SwitchingBus.B4, Tuple.Create(2, 4) },
      { SwitchingBus.AB1, Tuple.Create(3, 1) },
      { SwitchingBus.AB2, Tuple.Create(3, 2) },
      { SwitchingBus.AB3, Tuple.Create(3, 3) },
      { SwitchingBus.AB4, Tuple.Create(3, 4) },
    };
  }
}

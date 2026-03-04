using Ask.Core.Shared.Metadata.Enums.DeviceEnums;

namespace Ask.Core.Services.Extensions
{
  public static class BusConverter
  {
    /// <summary>
    /// Пытается преобразовать строку в коммутационную шину.
    /// Поддерживает кириллицу (А/В).
    /// </summary>
    public static bool TryParseSwitchingBus(
      string value,
      out SwitchingBus bus)
    {
      bus = default;

      if (string.IsNullOrWhiteSpace(value))
        return false;

      var normalized = NormalizeBusToken(value);

      return EnumConverter.TryParseEnum(normalized, out bus);
    }

    /// <summary>
    /// Преобразует объединённую шину ABx в соответствующие шины Ax и Bx.
    /// </summary>
    public static bool TrySplitAbBus(
      SwitchingBusNew abBus,
      out SwitchingBus busA,
      out SwitchingBus busB)
    {
      busA = default;
      busB = default;

      switch (abBus)
      {
        case SwitchingBusNew.AB1:
          busA = SwitchingBus.A1;
          busB = SwitchingBus.B1;
          return true;

        case SwitchingBusNew.AB2:
          busA = SwitchingBus.A2;
          busB = SwitchingBus.B2;
          return true;

        case SwitchingBusNew.AB3:
          busA = SwitchingBus.A3;
          busB = SwitchingBus.B3;
          return true;

        case SwitchingBusNew.AB4:
          busA = SwitchingBus.A4;
          busB = SwitchingBus.B4;
          return true;

        default:
          return false;
      }
    }

    private static string NormalizeBusToken(string value)
    {
      return value
        .Trim()
        .Replace('А', 'A')
        .Replace('В', 'B')
        .Replace('а', 'A')
        .Replace('в', 'B');
    }
  }
}

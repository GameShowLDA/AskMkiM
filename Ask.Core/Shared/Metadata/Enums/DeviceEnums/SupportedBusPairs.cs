namespace Ask.Core.Shared.Metadata.Enums.DeviceEnums;

/// <summary>
/// Задаёт пары структурных шин, доступные устройству.
/// </summary>
[Flags]
public enum SupportedBusPairs
{
  /// <summary>Доступные пары шин отсутствуют.</summary>
  None = 0,

  /// <summary>Пара шин A1/B1.</summary>
  AB1 = 1 << 0,

  /// <summary>Пара шин A2/B2.</summary>
  AB2 = 1 << 1,

  /// <summary>Пара шин A3/B3.</summary>
  AB3 = 1 << 2,

  /// <summary>Пара шин A4/B4.</summary>
  AB4 = 1 << 3,

  /// <summary>Все пары структурных шин.</summary>
  All = AB1 | AB2 | AB3 | AB4
}

/// <summary>
/// Преобразует номера пар структурных шин в набор поддерживаемых пар.
/// </summary>
public static class SupportedBusPairsExtensions
{
  /// <summary>
  /// Возвращает признак доступности указанной пары шин.
  /// </summary>
  public static bool Supports(this SupportedBusPairs supportedPairs, SwitchingBusNew bus) =>
    (supportedPairs & FromBus(bus)) != 0;

  /// <summary>
  /// Возвращает набор с одной указанной парой шин.
  /// </summary>
  public static SupportedBusPairs FromBus(SwitchingBusNew bus) => bus switch
  {
    SwitchingBusNew.AB1 => SupportedBusPairs.AB1,
    SwitchingBusNew.AB2 => SupportedBusPairs.AB2,
    SwitchingBusNew.AB3 => SupportedBusPairs.AB3,
    SwitchingBusNew.AB4 => SupportedBusPairs.AB4,
    _ => SupportedBusPairs.None
  };
}

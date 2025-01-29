namespace Core.ModuleVoltageCurrentSource
{
  /// <summary>
  /// Перечисления различных данных "Модуля источника напряжения и тока".
  /// </summary>
  public static class Enums
  {
    /// <summary>
    /// Шины МИНТ.
    /// </summary>
    public enum BusModuleVoltageCurrentSource
    {
      /// <summary>
      /// Шина А1 МИНТ.
      /// </summary>
      A1,

      /// <summary>
      /// Шина А2 МИНТ.
      /// </summary>
      A2,

      /// <summary>
      /// Шина А3 МИНТ.
      /// </summary>
      A3,

      /// <summary>
      /// Шина А4 МИНТ.
      /// </summary>
      A4,

      /// <summary>
      /// Шина В1 МИНТ.
      /// </summary>
      B1,

      /// <summary>
      /// Шина В2 МИНТ.
      /// </summary>
      B2,

      /// <summary>
      /// Шина В3 МИНТ.
      /// </summary>
      B3,

      /// <summary>
      /// Шина В4 МИНТ.
      /// </summary>
      B4,

      /// <summary>
      /// Шина А1 и В1 МИНТ.
      /// </summary>
      AB1,

      /// <summary>
      /// Шина А2 и В2 МИНТ.
      /// </summary>
      AB2,

      /// <summary>
      /// Шина А3 и В3 МИНТ.
      /// </summary>
      AB3,

      /// <summary>
      /// Шина А4 и В4 МИНТ.
      /// </summary>
      AB4,
    }

    /// <summary>
    /// Словарь данных о шинах.
    /// </summary>
    internal static readonly Dictionary<BusModuleVoltageCurrentSource, Tuple<int, int>> KeyValuePairsModuleVoltageCurrentSource = new Dictionary<BusModuleVoltageCurrentSource, Tuple<int, int>>
    {
      { BusModuleVoltageCurrentSource.A1, Tuple.Create(1, 1) },
      { BusModuleVoltageCurrentSource.A2, Tuple.Create(1, 2) },
      { BusModuleVoltageCurrentSource.A3, Tuple.Create(1, 3) },
      { BusModuleVoltageCurrentSource.A4, Tuple.Create(1, 4) },
      { BusModuleVoltageCurrentSource.B1, Tuple.Create(2, 1) },
      { BusModuleVoltageCurrentSource.B2, Tuple.Create(2, 2) },
      { BusModuleVoltageCurrentSource.B3, Tuple.Create(2, 3) },
      { BusModuleVoltageCurrentSource.B4, Tuple.Create(2, 4) },
      { BusModuleVoltageCurrentSource.AB1, Tuple.Create(3, 1) },
      { BusModuleVoltageCurrentSource.AB2, Tuple.Create(3, 2) },
      { BusModuleVoltageCurrentSource.AB3, Tuple.Create(3, 3) },
      { BusModuleVoltageCurrentSource.AB4, Tuple.Create(3, 4) },
    };

    /// <summary>
    /// Источники напряжения.
    /// </summary>
    public enum VoltageSources
    {
      Supply12V,
      Supply5V,
    }
  }
}

using Ask.Core.Shared.Metadata.Atributes;

namespace Ask.Core.Shared.Metadata.Enums.UnitEnums
{
  /// <summary>
  /// Единицы измерения ёмкости.
  /// </summary>
  public enum CapacitanceUnit
  {
    /// <summary>
    /// Пикофарад (пФ).
    /// </summary>
    [UnitDisplay("пФ", QuantitySymbol.C)]
    PicoFarad,

    /// <summary>
    /// Нанофарад (нФ).
    /// </summary>
    [UnitDisplay("нФ", QuantitySymbol.C)]
    NanoFarad,

    /// <summary>
    /// Микрофарад (мкФ).
    /// </summary>
    [UnitDisplay("мкФ", QuantitySymbol.C)]
    MicroFarad
  }
}

using Ask.Core.Shared.Metadata.Atributes;

namespace Ask.Core.Shared.Metadata.Enums.UnitEnums
{
  /// <summary>
  /// Единицы измерения тока.
  /// </summary>
  public enum CurrentUnit
  {
    /// <summary>
    /// Ампер (А).
    /// </summary>
    [UnitDisplay("А", QuantitySymbol.I)]
    Ampere,

    /// <summary>
    /// Миллиампер (мА).
    /// </summary>
    [UnitDisplay("мА", QuantitySymbol.I)]
    MilliAmpere,
  }
}

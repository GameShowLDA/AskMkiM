using Ask.Core.Shared.Metadata.Atributes;

namespace Ask.Core.Shared.Metadata.Enums.UnitEnums

{
  /// <summary>
  /// Единицы измерения напряжения.
  /// </summary>
  public enum VoltageUnit
  {
    /// <summary>
    /// Вольт (В).
    /// </summary>
    [UnitDisplay("В", QuantitySymbol.U)]
    Volt,

    /// <summary>
    /// Милливольт (мВ).
    /// </summary>
    [UnitDisplay("мВ", QuantitySymbol.U)]
    MilliVolt,

    /// <summary>
    /// Киловольт (кВ).
    /// </summary>
    [UnitDisplay("кВ", QuantitySymbol.U)]
    KiloVolt
  }
}

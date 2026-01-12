using Ask.Core.Shared.Metadata.Atributes;

namespace Ask.Core.Shared.Metadata.Enums.UnitEnums
{
  /// <summary>
  /// Единицы измерения сопротивления.
  /// </summary>
  public enum ResistanceUnit
  {
    /// <summary>
    /// Ом (Ω).
    /// </summary>
    [UnitDisplay("Ом", QuantitySymbol.R)]
    Ohm,

    /// <summary>
    /// Килоом (кΩ).
    /// </summary>
    [UnitDisplay("КОм", QuantitySymbol.R)]
    KiloOhm,

    /// <summary>
    /// Мегаом (МΩ).
    /// </summary>
    [UnitDisplay("МОм", QuantitySymbol.R)]
    MegaOhm,

    /// <summary>
    /// Гигаом (ГΩ).
    /// </summary>
    [UnitDisplay("ГОм", QuantitySymbol.R)]
    GigaOhm
  }
}

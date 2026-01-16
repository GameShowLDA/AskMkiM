using Ask.Core.Shared.Metadata.Atributes;

namespace Ask.Core.Shared.Metadata.Enums.TranslationEnums.Commands
{
  /// <summary>
  /// Перечисление, представляющее различные типы команд в системе.
  /// </summary>
  public enum MeasurementTypeCommand
  {
    [CommandDisplayInfo("Сопротивления", "КС", UnitEnums.QuantitySymbol.R, "Ом", 0, 10_000_000)]
    /// <summary>
    /// Тип команды KC.
    /// </summary>
    KC,

    [CommandDisplayInfo("Сопротивления", "ПР", UnitEnums.QuantitySymbol.R, "Ом", 1, 100_000)]
    /// <summary>
    /// Тип команды PR.
    /// </summary>
    PR,

    [CommandDisplayInfo("Сопротивления изоляции", "СИ", UnitEnums.QuantitySymbol.R, "МОм", 1, 1000)]
    /// <summary>
    /// Тип команды CI.
    /// </summary>
    SI,

    [CommandDisplayInfo("Ёмкости", "ИЕ", UnitEnums.QuantitySymbol.C, "нФ", 0.2, 100000)]
    /// <summary>
    /// Тип команды IE.
    /// </summary>
    IE,

    [CommandDisplayInfo("Переменного напряжения", "КН_ACW", UnitEnums.QuantitySymbol.U, "В", 0.1, 250)]
    /// <summary>
    /// Тип команды KN переменным током.
    /// </summary>
    KN_ACW,

    [CommandDisplayInfo("Постоянного напряжения", "КН_DCW", UnitEnums.QuantitySymbol.U, "В", 0.1, 250)]

    /// <summary>
    /// Тип команды KN постоянным током.
    /// </summary>
    KN_DCW,

    [CommandDisplayInfo("Прочности изоляции переменного тока", "ПИ_ACW", UnitEnums.QuantitySymbol.I, "мА", 50, 700)]
    /// <summary>
    /// Тип команды PI переменным током.
    /// </summary>
    PI_ACW,

    [CommandDisplayInfo("Прочности изоляции постоянного тока", "ПИ_DCW", UnitEnums.QuantitySymbol.I, "мА", 50, 1000)]
    /// <summary>
    /// Тип команды PI постоянным током.
    /// </summary>
    PI_DCW,

    [CommandDisplayInfo("Прочности изоляции", "ПИ", UnitEnums.QuantitySymbol.U, "В", 0, 0)]
    /// <summary>
    /// Тип команды PI постоянным током.
    /// </summary>
    PI,

    [CommandDisplayInfo("Сопротивления", "ЭТ", UnitEnums.QuantitySymbol.R, "Ом", 0, 100)]
    /// <summary>
    /// Тип команды EHT постоянным током.
    /// </summary>
    EHT,
  }
}

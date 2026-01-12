using Ask.Core.Shared.Metadata.Enums.UnitEnums;

namespace Ask.Core.Shared.Metadata.Converters
{
  /// <summary>
  /// Предоставляет методы для преобразования значений сопротивления
  /// между различными единицами измерения (Ом, кОм, МОм, ГОм).
  /// </summary>
  public static class ResistanceConverter
  {
    /// <summary>
    /// Преобразует значение сопротивления в Омы (Ω).
    /// </summary>
    /// <param name="value">Исходное значение сопротивления.</param>
    /// <param name="fromUnit">Единица измерения исходного значения.</param>
    /// <returns>Значение сопротивления в Омах.</returns>
    public static double ToOhms(double value, ResistanceUnit fromUnit)
    {
      return value * GetUnitMultiplier(fromUnit);
    }

    /// <summary>
    /// Преобразует значение сопротивления в Мегаомы (МΩ).
    /// </summary>
    /// <param name="value">Исходное значение сопротивления.</param>
    /// <param name="fromUnit">Единица измерения исходного значения.</param>
    /// <returns>Значение сопротивления в Мегаомах.</returns>
    public static double ToMegaOhms(double value, ResistanceUnit fromUnit)
    {
      double ohms = ToOhms(value, fromUnit);
      return ohms / 1_000_000.0;
    }

    /// <summary>
    /// Преобразует значение сопротивления в Гигаомы (ГΩ).
    /// </summary>
    /// <param name="value">Исходное значение сопротивления.</param>
    /// <param name="fromUnit">Единица измерения исходного значения.</param>
    /// <returns>Значение сопротивления в Гигаомах.</returns>
    public static double ToGigaOhms(double value, ResistanceUnit fromUnit)
    {
      double ohms = ToOhms(value, fromUnit);
      return ohms / 1_000_000_000.0;
    }

    /// <summary>
    /// Возвращает множитель для перевода значения в Омы.
    /// </summary>
    /// <param name="unit">Единица измерения.</param>
    /// <returns>Множитель для перевода в Омы.</returns>
    private static double GetUnitMultiplier(ResistanceUnit unit)
    {
      return unit switch
      {
        ResistanceUnit.Ohm => 1.0,
        ResistanceUnit.KiloOhm => 1_000.0,
        ResistanceUnit.MegaOhm => 1_000_000.0,
        ResistanceUnit.GigaOhm => 1_000_000_000.0,
        _ => 1.0
      };
    }

    /// <summary>
    /// Преобразует строковое обозначение единицы измерения сопротивления
    /// в соответствующее значение перечисления <see cref="ResistanceUnit"/>.
    /// Поддерживаются как русские, так и английские обозначения.
    /// </summary>
    /// <param name="unitText">Строковое обозначение единицы (например: "Ом", "kΩ", "МОм", "GOhm").</param>
    /// <returns>Соответствующее значение <see cref="ResistanceUnit"/>.</returns>
    /// <exception cref="ArgumentException">
    /// Выбрасывается, если передана неизвестная единица измерения.
    /// </exception>
    public static ResistanceUnit ParseUnit(string unitText, string defaultUnit)
    {
      if (string.IsNullOrWhiteSpace(unitText))
      {
        if (string.IsNullOrWhiteSpace(defaultUnit))
        {
          throw new ArgumentException("Строка единицы измерения не может быть пустой.", nameof(unitText));
        }
        unitText = defaultUnit;
      }

      string normalized = unitText
        .Trim()
        .ToLowerInvariant()
        .Replace(" ", string.Empty);

      return normalized switch
      {
        "ом" or "омы" or "ohm" or "om" or "ω" =>
          ResistanceUnit.Ohm,

        "ком" or "килоом" or "kohm" or "kω" or "kOm" or "kΩ" =>
          ResistanceUnit.KiloOhm,

        "мом" or "мегаом" or "mohm" or "mω" or "mOm" or "mΩ" =>
          ResistanceUnit.MegaOhm,

        "гом" or "гигаом" or "gohm" or "gω" or "gOm" or "gΩ" =>
          ResistanceUnit.GigaOhm,

        _ => throw new ArgumentException(
          $"Неизвестная единица сопротивления: '{unitText}'.",
          nameof(unitText))
      };
    }
  }
}

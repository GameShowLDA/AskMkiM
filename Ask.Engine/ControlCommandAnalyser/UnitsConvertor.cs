namespace Ask.Engine.ControlCommandAnalyser
{
  public class UnitsConvertor
  {
    /// <summary>
    /// Безопасно парсит строку и переводит в Омы.
    /// Возвращает null, если строка пустая или содержит ошибку.
    /// </summary>
    public static double? TryParseValue(string valueStr, string unit)
    {
      if (string.IsNullOrWhiteSpace(valueStr))
        return null;

      if (double.TryParse(valueStr.Replace(',', '.'),
                          System.Globalization.NumberStyles.Any,
                          System.Globalization.CultureInfo.InvariantCulture,
                          out double value))
      {
        if (unit.ToLowerInvariant().Contains("ом"))
        {
          return ConvertToOhms(value, unit);
        }
        else if (unit.ToLowerInvariant().Contains("ф"))
        {
          return ConvertToFarads(value, unit);
        }
        else if (unit.ToLowerInvariant().Contains("в"))
        {
          return ConvertToVolts(value, unit);
        }
        else if (unit.ToLowerInvariant().Contains("а"))
        {
          return ConvertToAmpers(value, unit);
        }
      }

      return null;
    }

    /// <summary>
    /// Безопасно парсит строку и переводит в Омы.
    /// Возвращает null, если строка пустая или содержит ошибку.
    /// </summary>
    public static (double?, string) TryConvertBack(double value, string unit)
    {
      if (unit.ToLowerInvariant().Contains("ом"))
      {
        return FormatOhms(value);
      }
      else if (unit.ToLowerInvariant().Contains("ф"))
      {
        return FormatFarads(value);
      }
      else if (unit.ToLowerInvariant().Contains("в"))
      {
        return FormatVolts(value);
      }

      return (value, unit);
    }

    /// <summary>
    /// Преобразует значение сопротивления в Омы (СИ).
    /// </summary>
    internal static double ConvertToOhms(double value, string unit)
    {
      unit = unit.ToLowerInvariant();
      return unit switch
      {
        "ом" => value,
        "ком" => value * 1_000,
        "мом" => value * 1_000_000,
        "гом" => value * 1_000_000_000,
        _ => value
      };
    }

    /// <summary>
    /// Преобразует значение сопротивления в Омы (СИ).
    /// </summary>
    internal static double ConvertToMOhms(double value, string unit)
    {
      unit = unit.ToLowerInvariant();
      return unit switch
      {
        "ом" => value / 1_000_000,
        "ком" => value / 1_000,
        "мом" => value,
        "гом" => value * 1_000,
        _ => value
      };
    }

    /// <summary>
    /// Преобразует значение ёмкости в фарады (СИ).
    /// </summary>
    private static double ConvertToFarads(double value, string unit)
    {
      unit = unit.ToLowerInvariant();
      return unit switch
      {
        "ф" => value,                 // Фарад — базовая единица
        "мф" => value * 1e-3,          // миллифарад
        "мкф" => value * 1e-6,          // микрофарад
        "нф" => value * 1e-9,          // нанофарад
        "пф" => value * 1e-12,         // пикофарад
        _ => value                      // если не распознали, возвращаем как есть
      };
    }

    /// <summary>
    /// Преобразует значение напряжения в вольты (СИ).
    /// </summary>
    private static double ConvertToVolts(double value, string unit)
    {
      // Вольты
      if (unit == "МВ") return value * 1e6;

      unit = unit.ToLowerInvariant();
      return unit switch
      {
        "в" => value,
        "мв" => value * 1e-3,
        "кв" => value * 1e3,
        _ => value
      };
    }

    /// <summary>
    /// Преобразует значение силы тока в амперы (СИ).
    /// </summary>
    private static double ConvertToAmpers(double value, string unit)
    {
      // Вольты
      if (unit == "МА") return value * 1e6;

      unit = unit.ToLowerInvariant();
      return unit switch
      {
        "а" => value,
        "ма" => value * 1e-3,
        "ка" => value * 1e3,
        _ => value
      };
    }

    // === ОБРАТНЫЙ КОНВЕРТЕР (в удобную форму) ===

    /// <summary>
    /// Переводит сопротивление из Ом в удобную запись (например: 1500000 → 1.5 МОм).
    /// </summary>
    public static (double value, string unit) FormatOhms(double ohms)
    {
      if (ohms >= 1e9) return (Math.Round(ohms / 1e9, 3), "ГОм");
      if (ohms >= 1e6) return (Math.Round(ohms / 1e6, 3), "МОм");
      if (ohms >= 1e3) return (Math.Round(ohms / 1e3, 3), "кОм");
      return (Math.Round(ohms, 3), "Ом");
    }

    /// <summary>
    /// Переводит ёмкость из фарад в удобную запись (например: 0.00000022 → 220 нФ).
    /// </summary>
    public static (double value, string unit) FormatFarads(double farads)
    {
      if (farads >= 1) return (Math.Round(farads, 3), "Ф");
      if (farads >= 1e-3) return (Math.Round(farads * 1e3, 3), "мФ");
      if (farads >= 1e-6) return (Math.Round(farads * 1e6, 3), "мкФ");
      if (farads >= 1e-9) return (Math.Round(farads * 1e9, 3), "нФ");
      if (farads >= 1e-12) return (Math.Round(farads * 1e12, 3), "пФ");
      return (farads, "Ф"); // fallback
    }

    /// <summary>
    /// Переводит ёмкость из фарад в удобную запись (например: 0.00000022 → 220 нФ).
    /// </summary>
    public static (double value, string unit) FormatNanoFarads(double farads)
    {
      return (Math.Round(farads * 1e9, 3), "нФ");
    }

    /// <summary>
    /// Переводит напряжение из вольт в удобную запись (например: 5000 → 5 кВ).
    /// </summary>
    public static (double value, string unit) FormatVolts(double volts)
    {
      if (volts >= 1e6) return (Math.Round(volts / 1e6, 3), "МВ");
      if (volts >= 1e3) return (Math.Round(volts / 1e3, 3), "кВ");
      if (volts >= 1) return (Math.Round(volts, 3), "В");
      return (Math.Round(volts * 1e3, 3), "мВ");
    }
  }
}

using Ask.Core.Services.Errors.Translation;
using Ask.Core.Shared.Interfaces.ParserInterfaces;
using Ask.Core.Shared.ParserContext;
using Ask.Engine.ControlCommandAnalyser.Attributes;
using Ask.Engine.ControlCommandAnalyser.Model.Pr;
using Ask.Engine.ControlCommandAnalyser.Parser.Common.Helpers;
using Ask.Engine.ControlCommandAnalyser.Parser.Common.HelperParserParametr;
using static Ask.LogLib.LoggerUtility;

namespace Ask.Engine.ControlCommandAnalyser.Parser.Common.Processors.Pr
{
  /// <summary>
  /// Процессор параметров сопротивления для команды ПР.
  /// Извлекает диапазон сопротивления и распределяет значения
  /// между состояниями "разомкнуто" и "замкнуто".
  /// </summary>
  internal class PrResistanceProcessor : IParameterProcessor<PrCommandModel>
  {
    /// <summary>
    /// Выполняет разбор параметров сопротивления и обновляет модель команды.
    /// </summary>
    /// <param name="model">Модель команды.</param>
    /// <param name="remainder">Оставшаяся часть строки команды.</param>
    /// <param name="ctx">Контекст парсинга параметров.</param>
    /// <returns>Строка без обработанных параметров.</returns>
    public string Process(PrCommandModel model, string remainder, ParameterContext ctx)
    {
      string lowerRaw, higherRaw, unit;

      (lowerRaw, higherRaw, unit, remainder) =
          CommonParameterParser.ResistanceParser.ParseResistanceRangeWithR(remainder);

      LogDebug($"После парсинга сопротивления: lower='{lowerRaw}', higher='{higherRaw}', unit='{unit}', remainder='{remainder}'");

      double? lower = !string.IsNullOrWhiteSpace(lowerRaw)
          ? CommonParameterParser.ParseToDouble(lowerRaw)
          : null;

      double? higher = !string.IsNullOrWhiteSpace(higherRaw)
          ? CommonParameterParser.ParseToDouble(higherRaw)
          : null;

      ApplyPrResistance(model, unit, lower, higher);

      return remainder;
    }

    /// <summary>
    /// Применяет параметры сопротивления к модели ПР
    /// с учётом допустимого диапазона.
    /// </summary>
    private static void ApplyPrResistance(
        PrCommandModel model,
        string? unit,
        double? lower,
        double? higher)
    {
      var rangeAttr = GetResistanceRangeAttr(model);

      if (rangeAttr == null)
      {
        LogError("Для PrCommandModel не найден атрибут ResistanceRange.");
        model.Errors.Add(
            PrErrors.CannotParseParameters(
                "Ошибка конфигурации обработчика ПР",
                model.StartLineNumber,
                $"{model.CommandNumber} {model.Mnemonic}"));
        return;
      }

      double PR_MIN = rangeAttr.Min;
      double PR_MAX = rangeAttr.Max;
      double PR_DEFAULT_LOWER = rangeAttr.DefaultLower;
      char infinity = '\u221E';

      (double? valLower, string lowerUnit) =
          lower.HasValue ? UnitsConvertor.TryConvertBack(lower.Value, unit) : (null, unit);

      (double? valHigher, string higherUnit) =
          higher.HasValue ? UnitsConvertor.TryConvertBack(higher.Value, unit) : (null, unit);

      if (valLower.HasValue && valHigher.HasValue)
      {
        ApplyRange(model, lower, higher, PR_MIN, PR_MAX, infinity, valLower, lowerUnit, valHigher, higherUnit);
        model.ResistanceUnit = unit;
        return;
      }

      if (!valLower.HasValue && valHigher.HasValue)
      {
        ApplyUpper(model, higher, PR_MAX, infinity, valHigher, higherUnit);
        model.ResistanceUnit = unit;
        return;
      }

      if (valLower.HasValue && !valHigher.HasValue)
      {
        ApplyLower(model, lower, PR_MIN, infinity, valLower, lowerUnit);
        model.ResistanceUnit = unit;
        return;
      }

      ApplyDefault(model, PR_MIN, PR_DEFAULT_LOWER, infinity);
    }

    /// <summary>
    /// Получает атрибут диапазона сопротивления для модели.
    /// </summary>
    private static ResistanceRangeAttribute? GetResistanceRangeAttr(PrCommandModel model)
        => (ResistanceRangeAttribute?)Attribute.GetCustomAttribute(
            model.GetType(),
            typeof(ResistanceRangeAttribute));

    /// <summary>
    /// Применяет значения сопротивления по умолчанию.
    /// </summary>
    private static void ApplyDefault(PrCommandModel model, double min, double defLower, char infinity)
    {
      model.DisconnectedLowerLimitResistance = defLower;
      model.DisconnectedLowerLimitResistanceSource = MeasurementSourceValueFormatter.FormatWithSpace(defLower, "Ом");
      model.DisconnectedHigherLimitResistance = null;
      model.DisconnectedHigherLimitResistanceSource = $"{infinity} Ом";

      model.ConnectedLowerLimitResistance = min;
      model.ConnectedLowerLimitResistanceSource = MeasurementSourceValueFormatter.FormatWithSpace(min, "Ом");
      model.ConnectedHigherLimitResistance = defLower;
      model.ConnectedHigherLimitResistanceSource = MeasurementSourceValueFormatter.FormatWithSpace(defLower, "Ом");

      model.ResistanceUnit = "Ом";

    }

    /// <summary>
    /// Применяет только нижнюю границу сопротивления.
    /// </summary>
    private static void ApplyLower(
        PrCommandModel model,
        double? lower,
        double min,
        char infinity,
        double? valLower,
        string unit)
    {
      if (lower < min)
      {
        model.Errors.Add(
            PrErrors.ResistanceLimitsConflict(
                model.StartLineNumber,
                $"{model.CommandNumber} {model.Mnemonic}",
                $"Нижняя граница ({valLower} {unit}) меньше минимально допустимой ({min} Ом)"));
        return;
      }

      model.DisconnectedLowerLimitResistance = lower;
      model.DisconnectedLowerLimitResistanceSource = MeasurementSourceValueFormatter.FormatWithSpace(valLower.Value, unit);
      model.DisconnectedHigherLimitResistance = null;
      model.DisconnectedHigherLimitResistanceSource = $"{infinity} Ом";

      model.ConnectedLowerLimitResistance = min;
      model.ConnectedLowerLimitResistanceSource = MeasurementSourceValueFormatter.FormatWithSpace(min, "Ом");
      model.ConnectedHigherLimitResistance = lower;
      model.ConnectedHigherLimitResistanceSource = MeasurementSourceValueFormatter.FormatWithSpace(valLower.Value, unit);
    }

    /// <summary>
    /// Применяет только верхнюю границу сопротивления.
    /// </summary>
    private static void ApplyUpper(
        PrCommandModel model,
        double? upper,
        double max,
        char infinity,
        double? valUpper,
        string unit)
    {
      if (upper > max)
      {
        model.Errors.Add(
            PrErrors.ResistanceLimitsConflict(
                model.StartLineNumber,
                $"{model.CommandNumber} {model.Mnemonic}",
                $"Верхняя граница ({valUpper} {unit}) больше максимально допустимой ({max} Ом)"));
        return;
      }

      model.DisconnectedLowerLimitResistance = upper;
      model.DisconnectedLowerLimitResistanceSource = MeasurementSourceValueFormatter.FormatWithSpace(valUpper.Value, unit);
      model.DisconnectedHigherLimitResistance = null;
      model.DisconnectedHigherLimitResistanceSource = $"{infinity} Ом";

      model.ConnectedLowerLimitResistance = 0;
      model.ConnectedLowerLimitResistanceSource = MeasurementSourceValueFormatter.FormatWithSpace(0, "Ом");
      model.ConnectedHigherLimitResistance = upper;
      model.ConnectedHigherLimitResistanceSource = MeasurementSourceValueFormatter.FormatWithSpace(valUpper.Value, unit);
    }

    /// <summary>
    /// Применяет диапазон сопротивления.
    /// </summary>
    private static void ApplyRange(
        PrCommandModel model,
        double? lower,
        double? upper,
        double min,
        double max,
        char infinity,
        double? valLower,
        string lowerUnit,
        double? valUpper,
        string upperUnit)
    {
      if (lower > upper || lower < min || upper > max)
      {
        model.Errors.Add(
            PrErrors.ResistanceLimitsConflict(
                model.StartLineNumber,
                $"{model.CommandNumber} {model.Mnemonic}",
                $"Некорректный диапазон сопротивления"));
        return;
      }

      model.DisconnectedLowerLimitResistance = upper;
      model.DisconnectedLowerLimitResistanceSource = MeasurementSourceValueFormatter.FormatWithSpace(valUpper.Value, upperUnit);
      model.DisconnectedHigherLimitResistance = null;
      model.DisconnectedHigherLimitResistanceSource = $"{infinity} Ом";

      model.ConnectedLowerLimitResistance = lower;
      model.ConnectedLowerLimitResistanceSource = MeasurementSourceValueFormatter.FormatWithSpace(valLower.Value, lowerUnit);
      model.ConnectedHigherLimitResistance = upper;
      model.ConnectedHigherLimitResistanceSource = MeasurementSourceValueFormatter.FormatWithSpace(valUpper.Value, upperUnit);
    }
  }
}

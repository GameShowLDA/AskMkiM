using Ask.Core.Services.Errors.Translation;
using Ask.Core.Services.Extensions;
using Ask.Core.Shared.DTO.Executor;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums.Commands;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandAnalyser.Model.Ks;
using Ask.Engine.ControlCommandAnalyser.Parser.Common.HelperParserParametr;
using static Ask.LogLib.LoggerUtility;

namespace Ask.Engine.ControlCommandAnalyser.Parser.Common.Helpers
{
  /// <summary>
  /// Менеджер обработки параметров сопротивления.
  /// Выполняет парсинг, валидацию диапазонов и запись значений в модели команд.
  /// </summary>
  public static class ResistanceManager
  {
    /// <summary>
    /// Обрабатывает параметры сопротивления для команды ЭТ.
    /// </summary>
    public static void ProcessResistance(
    EhtCommandModel model,
    string lowerLimitResistance,
    string higherLimitResistance,
    string cabelLimitResistance,
    string unit,
    string cabelUnit,
    string commandNumber,
    string mnemonic,
    int numberLine)
    {
      var defaults = GetResistanceDefaults(MeasurementTypeCommand.EHT);
      var parsed = ParseResistanceInputs(lowerLimitResistance, higherLimitResistance, unit, defaults.DefaultUnit, cabelLimitResistance, cabelUnit);

      if (!ValidateResistanceLimits(model, parsed, defaults, commandNumber, mnemonic, numberLine))
      {
        return;
      }
      ApplyResistanceToModel(model, parsed, defaults, commandNumber, mnemonic);
    }

    /// <summary>
    /// Обрабатывает параметры сопротивления для команды КС.
    /// </summary>
    public static void ProcessResistance(
    KsCommandModel model,
    string lower,
    string higher,
    string unit,
    string commandNumber,
    string mnemonic,
    int numberLine)
    {
      var defaults = GetResistanceDefaults(MeasurementTypeCommand.KC);

      var parsed = ParseResistanceInputs(lower, higher, unit, defaults.DefaultUnit);

      if (!ValidateResistanceLimits(model, parsed, defaults, unit, commandNumber, mnemonic, numberLine))
        return;

      ApplyResistanceToModel(model, parsed, defaults);
    }

    /// <summary>
    /// Получает значения сопротивления по умолчанию из метаданных команды.
    /// </summary>
    private static ResistanceDefaults GetResistanceDefaults(MeasurementTypeCommand typeCommand)
    {
      var commandInfo = EnumExtensions.GetDisplayInfo(typeCommand);

      return new ResistanceDefaults(
          commandInfo.LowerLimit,
          commandInfo.UpperLimit,
          commandInfo.Unit);
    }

    /// <summary>
    /// Значения сопротивления по умолчанию.
    /// </summary>
    private record ResistanceDefaults(double DefaultLower, double DefaultHigher, string DefaultUnit);

    /// <summary>
    /// Парсит входные параметры сопротивления.
    /// </summary>
    private static ParsedResistance ParseResistanceInputs(string lowerRaw, string higherRaw, string unit, string defaultUnit, string cabelRaw = null, string cabelUnit = null)
    {
      double? lower = !string.IsNullOrWhiteSpace(lowerRaw)
          ? CommonParameterParser.ParseToDouble(lowerRaw)
          : null;

      double? higher = !string.IsNullOrWhiteSpace(higherRaw)
          ? CommonParameterParser.ParseToDouble(higherRaw)
          : null;

      double? cabel = !string.IsNullOrWhiteSpace(cabelRaw)
          ? CommonParameterParser.ParseToDouble(cabelRaw)
          : null;

      return new ParsedResistance(lower, higher, cabel,
          string.IsNullOrWhiteSpace(unit) ? defaultUnit : unit,
          string.IsNullOrWhiteSpace(cabelUnit) ? defaultUnit : cabelUnit);
    }

    /// <summary>
    /// Результат парсинга параметров сопротивления.
    /// </summary>
    private record ParsedResistance( double? Lower, double? Higher, double? Cabel, string Unit, string CabelUnit);

    /// <summary>
    /// Проверяет корректность диапазона сопротивления.
    /// </summary>
    private static bool ValidateResistanceLimits(BaseCommandModel model, ParsedResistance parsed, ResistanceDefaults defaults, 
      string commandNumber, string mnemonic, int numberLine)
    {
      if (!parsed.Lower.HasValue || !parsed.Higher.HasValue)
        return true;

      double lower = parsed.Lower.Value;
      double higher = parsed.Higher.Value;

      if (lower > higher)
        return AddError(model, numberLine, commandNumber, mnemonic,
            "Нижняя граница больше верхней.");

      if (lower < defaults.DefaultLower)
        return AddError(model, numberLine, commandNumber, mnemonic,
            "Нижняя граница меньше минимально допустимой.");

      if (higher > defaults.DefaultHigher)
        return AddError(model, numberLine, commandNumber, mnemonic,
            "Верхняя граница больше максимально допустимой.");

      return true;
    }

    /// <summary>
    /// Проверяет корректность диапазона сопротивления для команды КС.
    /// </summary>
    private static bool ValidateResistanceLimits(
    KsCommandModel model,
    ParsedResistance parsed,
    ResistanceDefaults defaults,
    string unit,
    string commandNumber,
    string mnemonic,
    int numberLine)
    {
      double minResistance = defaults.DefaultLower;
      double maxResistance = defaults.DefaultHigher;

      bool hasErrors = false;

      if (parsed.Lower.HasValue && parsed.Higher.HasValue)
      {
        if (parsed.Lower.Value >= parsed.Higher.Value)
        {
          var lowerValue = UnitsConvertor.TryConvertBack(parsed.Lower.Value, unit);
          var higherValue = UnitsConvertor.TryConvertBack(parsed.Higher.Value, unit);

          LogWarning($"В команде {commandNumber} {mnemonic} (строка {numberLine}) нижняя граница больше или равна верхней.");

          model.Errors.Add(KsErrors.ResistanceLimitsConflict(
              numberLine,
              $"{commandNumber} {mnemonic}",
              $"Нижняя граница сопротивления ({lowerValue.Item1} {lowerValue.Item2}) больше или равна верхней ({higherValue.Item1} {higherValue.Item2})."));

          hasErrors = true;
        }
      }

      if (parsed.Lower.HasValue && !hasErrors)
      {
        var lowerValue = UnitsConvertor.TryConvertBack(parsed.Lower.Value, unit);

        if (parsed.Lower.Value < minResistance)
        {
          var minValue = UnitsConvertor.TryConvertBack(minResistance, "Ом");

          LogWarning($"В команде {commandNumber} {mnemonic} (строка {numberLine}) нижняя граница меньше минимальной.");

          model.Errors.Add(KsErrors.ResistanceLimitsConflict(
              numberLine,
              $"{commandNumber} {mnemonic}",
              $"Нижняя граница сопротивления ({lowerValue.Item1} {lowerValue.Item2}) меньше минимально измеряемого ({minValue.Item1} {minValue.Item2})."));

          hasErrors = true;
        }

        if (parsed.Lower.Value > maxResistance)
        {
          var maxValue = UnitsConvertor.TryConvertBack(maxResistance, "Ом");

          LogWarning($"В команде {commandNumber} {mnemonic} (строка {numberLine}) нижняя граница больше максимальной.");

          model.Errors.Add(KsErrors.ResistanceLimitsConflict(
              numberLine,
              $"{commandNumber} {mnemonic}",
              $"Нижняя граница сопротивления ({lowerValue.Item1} {lowerValue.Item2}) больше максимально возможной ({maxValue.Item1} {maxValue.Item2})."));

          hasErrors = true;
        }
      }

      if (parsed.Higher.HasValue && !hasErrors)
      {
        var higherValue = UnitsConvertor.TryConvertBack(parsed.Higher.Value, unit);

        if (parsed.Higher.Value > maxResistance)
        {
          var maxValue = UnitsConvertor.TryConvertBack(maxResistance, "Ом");

          LogWarning($"В команде {commandNumber} {mnemonic} (строка {numberLine}) верхняя граница больше максимальной.");

          model.Errors.Add(KsErrors.ResistanceLimitsConflict(
              numberLine,
              $"{commandNumber} {mnemonic}",
              $"Верхняя граница сопротивления ({higherValue.Item1} {higherValue.Item2}) больше максимально возможной ({maxValue.Item1} {maxValue.Item2})."));

          hasErrors = true;
        }

        if (parsed.Higher.Value < minResistance)
        {
          var minValue = UnitsConvertor.TryConvertBack(minResistance, "Ом");

          LogWarning($"В команде {commandNumber} {mnemonic} (строка {numberLine}) верхняя граница меньше минимальной.");

          model.Errors.Add(KsErrors.ResistanceLimitsConflict(
              numberLine,
              $"{commandNumber} {mnemonic}",
              $"Верхняя граница сопротивления ({higherValue.Item1} {higherValue.Item2}) меньше минимально измеряемого ({minValue.Item1} {minValue.Item2})."));

          hasErrors = true;
        }
      }

      return !hasErrors;
    }

    /// <summary>
    /// Добавляет ошибку валидации сопротивления.
    /// </summary>
    private static bool AddError(BaseCommandModel model, int numberLine, string commandNumber, string mnemonic, string message)
    {
      LogWarning($"В команде {commandNumber} {mnemonic} (строка {numberLine}) {message}");

      model.Errors.Add(
          EhtErrors.ResistanceLimitsConflict(
              numberLine,
              $"{commandNumber} {mnemonic}",
              message));

      return false;
    }

    /// <summary>
    /// Записывает значения сопротивления в модель ЭТ.
    /// </summary>
    private static void ApplyResistanceToModel(EhtCommandModel model, ParsedResistance parsed, ResistanceDefaults defaults, string commandNumber, string mnemonic)
    {
      double lowerFinal = parsed.Lower ?? defaults.DefaultLower;
      double higherFinal = parsed.Higher ?? defaults.DefaultHigher;

      model.LowerLimitResistance = lowerFinal;
      model.LowerLimitResistanceSource = $"{lowerFinal} {parsed.Unit}";

      model.HigherLimitResistance = higherFinal;
      model.HigherLimitResistanceSource = $"{higherFinal} {parsed.Unit}";

      model.ResistanceUnit = parsed.Unit;

      if (parsed.Cabel.HasValue)
      {
        model.CabelResistance = parsed.Cabel.Value;
        model.CabelResistanceSource =
            $"{parsed.Cabel.Value} {parsed.CabelUnit}";

        model.CabelResistanceUnit = parsed.CabelUnit;
      }
    }

    /// <summary>
    /// Записывает значения сопротивления в модель КС.
    /// </summary>
    private static void ApplyResistanceToModel(
    KsCommandModel model,
    ParsedResistance parsed,
    ResistanceDefaults defaults)
    {
      char infinity = '\u221E';

      double minResistance = defaults.DefaultLower;

      model.ResistanceUnit = parsed.Unit ?? string.Empty;

      double lowerFinal = parsed.Lower ?? minResistance;

      model.LowerLimitResistance = lowerFinal;
      model.LowerLimitResistanceSource = $"{lowerFinal} {parsed.Unit}";

      if (parsed.Higher == null)
      {
        model.HigherLimitResistance = null;
        model.HigherLimitResistanceSource = $"{infinity} {parsed.Unit}";
      }
      else
      {
        model.HigherLimitResistance = parsed.Higher.Value;
        model.HigherLimitResistanceSource = $"{parsed.Higher.Value} {parsed.Unit}";
      }
    }

  }
}

using Ask.Core.Services.Errors.Translation;
using Ask.Core.Shared.Interfaces.ParserInterfaces;
using Ask.Core.Shared.ParserContext;
using Ask.Engine.ControlCommandAnalyser.Attributes;
using Ask.Engine.ControlCommandAnalyser.Model.Pr;
using Ask.Engine.ControlCommandAnalyser.Parser.Common.HelperParserParametr;
using static Ask.LogLib.LoggerUtility;

namespace Ask.Engine.ControlCommandAnalyser.Parser.Common.Processors.Pr
{
  internal class PrResistanceProcessor : IParameterProcessor<PrCommandModel>
  {
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
        return;
      }

      if (!valLower.HasValue && valHigher.HasValue)
      {
        ApplyUpper(model, higher, PR_MAX, infinity, valHigher, higherUnit);
        return;
      }

      if (valLower.HasValue && !valHigher.HasValue)
      {
        ApplyLower(model, lower, PR_MIN, infinity, valLower, lowerUnit);
        return;
      }

      ApplyDefault(model, PR_MIN, PR_DEFAULT_LOWER, infinity);
    }

    private static ResistanceRangeAttribute? GetResistanceRangeAttr(PrCommandModel model)
        => (ResistanceRangeAttribute?)Attribute.GetCustomAttribute(
            model.GetType(),
            typeof(ResistanceRangeAttribute));

    private static void ApplyDefault(PrCommandModel model, double min, double defLower, char infinity)
    {
      model.DisconnectedLowerLimitResistance = defLower;
      model.DisconnectedLowerLimitResistanceSource = $"{defLower} Ом";
      model.DisconnectedHigherLimitResistance = null;
      model.DisconnectedHigherLimitResistanceSource = $"{infinity} Ом";

      model.ConnectedLowerLimitResistance = min;
      model.ConnectedLowerLimitResistanceSource = $"{min} Ом";
      model.ConnectedHigherLimitResistance = defLower;
      model.ConnectedHigherLimitResistanceSource = $"{defLower} Ом";
    }

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
      model.DisconnectedLowerLimitResistanceSource = $"{valLower} {unit}";
      model.DisconnectedHigherLimitResistance = null;
      model.DisconnectedHigherLimitResistanceSource = $"{infinity} Ом";

      model.ConnectedLowerLimitResistance = min;
      model.ConnectedLowerLimitResistanceSource = $"{min} Ом";
      model.ConnectedHigherLimitResistance = lower;
      model.ConnectedHigherLimitResistanceSource = $"{valLower} {unit}";
    }

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
      model.DisconnectedLowerLimitResistanceSource = $"{valUpper} {unit}";
      model.DisconnectedHigherLimitResistance = null;
      model.DisconnectedHigherLimitResistanceSource = $"{infinity} Ом";

      model.ConnectedLowerLimitResistance = 0;
      model.ConnectedLowerLimitResistanceSource = $"0 Ом";
      model.ConnectedHigherLimitResistance = upper;
      model.ConnectedHigherLimitResistanceSource = $"{valUpper} {unit}";
    }

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
      model.DisconnectedLowerLimitResistanceSource = $"{valUpper} {upperUnit}";
      model.DisconnectedHigherLimitResistance = null;
      model.DisconnectedHigherLimitResistanceSource = $"{infinity} Ом";

      model.ConnectedLowerLimitResistance = lower;
      model.ConnectedLowerLimitResistanceSource = $"{valLower} {lowerUnit}";
      model.ConnectedHigherLimitResistance = upper;
      model.ConnectedHigherLimitResistanceSource = $"{valUpper} {upperUnit}";
    }
  }
}

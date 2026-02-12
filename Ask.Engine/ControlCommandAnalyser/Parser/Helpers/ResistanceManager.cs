using Ask.Core.Services.Errors.Translation;
using Ask.Core.Services.Extensions;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums.Commands;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandAnalyser.Parser.HelperParserParametr;
using static Ask.LogLib.LoggerUtility;


namespace Ask.Engine.ControlCommandAnalyser.Parser.Helpers
{
  public static class ResistanceManager
  {
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
      var defaults = GetResistanceDefaults();
      var parsed = ParseResistanceInputs(lowerLimitResistance, higherLimitResistance, cabelLimitResistance, unit, cabelUnit, defaults.DefaultUnit);

      if (!ValidateResistanceLimits(model, parsed, defaults, commandNumber, mnemonic, numberLine))
      {
        return;
      }

      ApplyResistanceToModel(model, parsed, defaults, commandNumber, mnemonic);
    }
    private static ResistanceDefaults GetResistanceDefaults()
    {
      var commandInfo = EnumExtensions.GetDisplayInfo(MeasurementTypeCommand.EHT);

      return new ResistanceDefaults(
          commandInfo.LowerLimit,
          commandInfo.UpperLimit,
          commandInfo.Unit);
    }

    private record ResistanceDefaults(double DefaultLower, double DefaultHigher, string DefaultUnit);

    private static ParsedResistance ParseResistanceInputs(string lowerRaw, string higherRaw, string cabelRaw, string unit, string cabelUnit, string defaultUnit)
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

    private record ParsedResistance( double? Lower, double? Higher, double? Cabel, string Unit, string CabelUnit);

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

    private static void ApplyResistanceToModel(EhtCommandModel model, ParsedResistance parsed, ResistanceDefaults defaults, string commandNumber, string mnemonic)
    {
      double lowerFinal = parsed.Lower ?? defaults.DefaultLower;
      double higherFinal = parsed.Higher ?? defaults.DefaultHigher;

      model.LowerLimitResistance = lowerFinal;
      model.LowerLimitResistanceSource = $"{lowerFinal} {parsed.Unit}";

      model.HigherLimitResistance = higherFinal;
      model.HigherLimitResistanceSource = $"{higherFinal} {parsed.Unit}";

      if (parsed.Cabel.HasValue)
      {
        model.CabelResistance = parsed.Cabel.Value;
        model.CabelResistanceSource =
            $"{parsed.Cabel.Value} {parsed.CabelUnit}";
      }
    }
  }
}

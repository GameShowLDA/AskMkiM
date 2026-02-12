using Ask.Core.Services.Errors.Translation;
using Ask.Engine.ControlCommandAnalyser.Model;
using static Ask.LogLib.LoggerUtility;

namespace Ask.Engine.ControlCommandAnalyser.Parser.Helpers
{
  public static class UnparsedParameters
  {
    public static void HandleUnparsedParameters(EhtCommandModel model, int numberLine, string? remainder)
    {
      if (!string.IsNullOrEmpty(remainder))
      {
        model.UnparsedParameters = "! Не распознанные параметры: ";
        model.UnparsedParameters += remainder;
        model.Errors.Add(GeneralErrors.UnrecognizedParameters(remainder, numberLine, $"{model.CommandNumber} {model.Mnemonic}"));
      }

      // Валидация
      if (string.IsNullOrWhiteSpace(model.LowerLimitResistanceSource) && string.IsNullOrWhiteSpace(model.HigherLimitResistanceSource))
      {
        LogError($"Не удалось распознать параметры в строке: '{remainder}' (строка {numberLine})");
        model.Errors.Add(EhtErrors.CannotParseParameters(
          $"сопротивление было неправильно задано, или неверно указаны границы сопроитвления", numberLine, $"{model.CommandNumber} {model.Mnemonic}"));
      }
    }
  }
}

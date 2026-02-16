using Ask.Core.Services.Errors.Translation;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandAnalyser.Model.Ie;
using Ask.Engine.ControlCommandAnalyser.Model.Ks;
using System.Timers;
using static Ask.LogLib.LoggerUtility;

namespace Ask.Engine.ControlCommandAnalyser.Parser.Helpers
{
  public static class UnparsedParametersManager
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
    public static void HandleUnparsedParameters(OtCommandModel model, int numberLine, string? remainder)
    {
      if (!string.IsNullOrEmpty(remainder))
      {
        model.UnparsedParameters = "! Не распознанные параметры: ";
        model.UnparsedParameters += remainder;
        model.Errors.Add(GeneralErrors.UnrecognizedParameters(remainder, numberLine, $"{model.CommandNumber} {model.Mnemonic}"));
      }

      if (string.IsNullOrWhiteSpace(model.TimeSource) && string.IsNullOrWhiteSpace(model.PointsSourse))
      {
        LogWarning($"В команде {model.CommandNumber} {model.Mnemonic} не указано ни время, ни точки (строка {numberLine})");
        model.Errors.Add(OtErrors.EmptyCommandBody(model.StartLineNumber, $"{model.CommandNumber}   {model.Mnemonic}"));
      }
    }
    public static void HandleUnparsedParameters(KsCommandModel model, int numberLine, string? remainder)
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
        model.Errors.Add(KsErrors.CannotParseParameters(remainder, numberLine, $"{model.CommandNumber} {model.Mnemonic}"));
      }
    }
    public static void HandleUnparsedParameters(CkCommandModel model, int numberLine, string? remainder)
    {
      if (!string.IsNullOrEmpty(remainder))
      {
        model.UnparsedParameters = "! Не распознанные параметры: ";
        model.UnparsedParameters += remainder;
        model.Errors.Add(GeneralErrors.UnrecognizedParameters(remainder, numberLine, $"{model.CommandNumber} {model.Mnemonic}"));
      }
    }
    public static void HandleUnparsedParameters(SiCommandModel model, int numberLine, string? remainder)
    {
      if (!string.IsNullOrEmpty(remainder))
      {
        model.UnparsedParameters = "! Не распознанные параметры: ";
        model.UnparsedParameters += remainder;
        model.Errors.Add(GeneralErrors.UnrecognizedParameters(remainder, numberLine, $"{model.CommandNumber} {model.Mnemonic}"));
      }
    }
    public static void HandleUnparsedParameters(PiCommandModel model, int numberLine, string? remainder)
    {
      if (!string.IsNullOrEmpty(remainder))
      {
        model.UnparsedParameters = "! Не распознанные параметры: ";
        model.UnparsedParameters += remainder;
        model.Errors.Add(GeneralErrors.UnrecognizedParameters(remainder, numberLine, $"{model.CommandNumber} {model.Mnemonic}"));
      }
    }
    public static void HandleUnparsedParameters(IeCommandModel model, int numberLine, string? remainder)
    {

      if (!string.IsNullOrEmpty(remainder))
      {
        model.UnparsedParameters = "! Не распознанные параметры: ";
        model.UnparsedParameters += remainder;
        model.Errors.Add(GeneralErrors.UnrecognizedParameters(remainder, numberLine, $"{model.CommandNumber} {model.Mnemonic}"));
      }
    }
  }
}

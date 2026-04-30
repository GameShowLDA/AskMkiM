using Ask.Core.Services.Errors.Translation;
using Ask.Core.Shared.DTO.Executor;
using System.Text.RegularExpressions;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandAnalyser.Model.Ie;
using Ask.Engine.ControlCommandAnalyser.Model.Ks;
using Ask.Engine.ControlCommandAnalyser.Model.Pr;
using static Ask.LogLib.LoggerUtility;

namespace Ask.Engine.ControlCommandAnalyser.Parser.Common.Helpers
{
  /// <summary>
  /// Менеджер обработки нераспознанных параметров команд.
  /// Записывает остаток строки в модель и выполняет базовую валидацию,
  /// специфичную для типа команды.
  /// </summary>
  public static class UnparsedParametersManager
  {
    private static void AddUnparsedParameters(BaseCommandModel model, int numberLine, string? remainder)
    {
      remainder = NormalizeUnparsedRemainder(remainder);

      if (string.IsNullOrWhiteSpace(remainder))
        return;

      SetUnparsedParameters(model, remainder);
      model.Errors.Add(GeneralErrors.UnrecognizedParameters(remainder, numberLine, $"{model.CommandNumber} {model.Mnemonic}"));
    }

    private static void SetUnparsedParameters(BaseCommandModel model, string remainder)
    {
      var property = model.GetType().GetProperty("UnparsedParameters");
      if (property?.CanWrite == true)
        property.SetValue(model, "! Не распознанные параметры: " + remainder);
    }
    private static string NormalizeUnparsedRemainder(string? remainder)
    {
      if (string.IsNullOrWhiteSpace(remainder))
        return string.Empty;

      var normalized = remainder.Trim();
      return Regex.IsMatch(normalized, @"^[\s,;]+$")
        ? string.Empty
        : normalized;
    }
    /// <summary>
    /// Обрабатывает нераспознанные параметры команды ЭТ.
    /// </summary>
    public static void HandleUnparsedParameters(EhtCommandModel model, int numberLine, string? remainder)
    {
      AddUnparsedParameters(model, numberLine, remainder);

      // Валидация
      if (string.IsNullOrWhiteSpace(model.LowerLimitResistanceSource) && string.IsNullOrWhiteSpace(model.HigherLimitResistanceSource))
      {
        LogError($"Не удалось распознать параметры в строке: '{remainder}' (строка {numberLine})");
        model.Errors.Add(EhtErrors.CannotParseParameters(
          $"сопротивление было неправильно задано, или неверно указаны границы сопроитвления", numberLine, $"{model.CommandNumber} {model.Mnemonic}"));
      }
    }
    /// <summary>
    /// Обрабатывает нераспознанные параметры команды ПТ.
    /// </summary>
    public static void HandleUnparsedParameters(PtCommandModel model, int numberLine, string? remainder)
    {
      AddUnparsedParameters(model, numberLine, remainder);

      if (string.IsNullOrWhiteSpace(model.TimeSource) && string.IsNullOrWhiteSpace(model.PointsSourse))
      {
        LogWarning($"Пустое тело команды: {model.CommandNumber} {model.Mnemonic} (строка {numberLine})");
        model.Errors.Add(PtErrors.EmptyCommandBody(model.StartLineNumber, $"{model.CommandNumber}   {model.Mnemonic}"));
      }
    }

    /// <summary>
    /// Обрабатывает нераспознанные параметры команды ОТ.
    /// </summary>
    public static void HandleUnparsedParameters(OtCommandModel model, int numberLine, string? remainder)
    {
      AddUnparsedParameters(model, numberLine, remainder);

      if (string.IsNullOrWhiteSpace(model.TimeSource) && string.IsNullOrWhiteSpace(model.PointsSourse))
      {
        LogWarning($"В команде {model.CommandNumber} {model.Mnemonic} не указано ни время, ни точки (строка {numberLine})");
        model.Errors.Add(OtErrors.EmptyCommandBody(model.StartLineNumber, $"{model.CommandNumber}   {model.Mnemonic}"));
      }
    }
    /// <summary>
    /// Обрабатывает нераспознанные параметры команды КС.
    /// </summary>
    public static void HandleUnparsedParameters(KsCommandModel model, int numberLine, string? remainder)
    {
      AddUnparsedParameters(model, numberLine, remainder);

      // Валидация
      if (string.IsNullOrWhiteSpace(model.LowerLimitResistanceSource) && string.IsNullOrWhiteSpace(model.HigherLimitResistanceSource))
      {
        LogError($"Не удалось распознать параметры в строке: '{remainder}' (строка {numberLine})");
        model.Errors.Add(KsErrors.CannotParseParameters(remainder, numberLine, $"{model.CommandNumber} {model.Mnemonic}"));
      }
    }

    /// <summary>
    /// Обрабатывает нераспознанные параметры команды НЭ.
    /// </summary>
    public static void HandleUnparsedParameters(NeCommandModel model, int numberLine, string? remainder)
    {
      AddUnparsedParameters(model, numberLine, remainder);

      // Валидация
      if (string.IsNullOrWhiteSpace(model.HigherLimitVoltageSource) && string.IsNullOrWhiteSpace(model.LowerLimitVoltageSource))
      {
        LogError($"Не удалось распознать параметры в строке: '{remainder}' (строка {numberLine})");
        model.Errors.Add(NeErrors.CannotParseParameters(
          $"Диапазон напряжения был неправильно задан или неверно указаны его границы",
          model.StartLineNumber,
          $"{model.CommandNumber}   {model.Mnemonic}"));
      }
    }

    /// <summary>
    /// Обрабатывает нераспознанные параметры команды СК.
    /// </summary>
    public static void HandleUnparsedParameters(CkCommandModel model, int numberLine, string? remainder)
    {
      AddUnparsedParameters(model, numberLine, remainder);
    }

    /// <summary>
    /// Обрабатывает нераспознанные параметры команды СИ.
    /// </summary>
    public static void HandleUnparsedParameters(SiCommandModel model, int numberLine, string? remainder)
    {
      remainder = NormalizeSiUnparsedRemainder(remainder);

      AddUnparsedParameters(model, numberLine, remainder);
    }

    private static string NormalizeSiUnparsedRemainder(string? remainder)
    {
      if (string.IsNullOrWhiteSpace(remainder))
        return string.Empty;

      var normalized = remainder.Trim();

      normalized = Regex.Replace(
        normalized,
        @"(?<=^|[\s,;])\d+(?:[.,]\d+)?\s*(?:м[сc]|ms|с|c)(?=$|[\s,;])",
        string.Empty,
        RegexOptions.IgnoreCase);

      return NormalizeUnparsedRemainder(normalized);
    }

    /// <summary>
    /// Обрабатывает нераспознанные параметры команды ПИ.
    /// </summary>
    public static void HandleUnparsedParameters(PiCommandModel model, int numberLine, string? remainder)
    {
      AddUnparsedParameters(model, numberLine, remainder);
    }

    /// <summary>
    /// Обрабатывает нераспознанные параметры команды IE.
    /// </summary>
    public static void HandleUnparsedParameters(IeCommandModel model, int numberLine, string? remainder)
    {

      AddUnparsedParameters(model, numberLine, remainder);
    }

    /// <summary>
    /// Обрабатывает нераспознанные параметры команды ПР.
    /// </summary>
    public static void HandleUnparsedParameters(PrCommandModel model, int numberLine, string? remainder)
    {

      AddUnparsedParameters(model, numberLine, remainder);

      // Валидация
      if (string.IsNullOrWhiteSpace(model.DisconnectedLowerLimitResistanceSource) && string.IsNullOrWhiteSpace(model.DisconnectedHigherLimitResistanceSource)
        && string.IsNullOrWhiteSpace(model.ConnectedLowerLimitResistanceSource) && string.IsNullOrWhiteSpace(model.ConnectedHigherLimitResistanceSource))
      {
        LogError($"Не удалось распознать параметры в строке: '{remainder}' (строка {numberLine})");
        model.Errors.Add(PrErrors.CannotParseParameters(
          $"Сопротивление было неправильно задано, или неверно указаны его границы",
          model.StartLineNumber,
          $"{model.CommandNumber}   {model.Mnemonic}"));
      }

      if (string.IsNullOrWhiteSpace(model.ConnectedHigherLimitResistanceSource) && !model.AlgorithmKey.Contains(AlgorithmKey.ЗС.ToString()))
      {
        LogError($"Не удалось распознать параметры в строке: '{remainder}' (строка {numberLine})");
        model.Errors.Add(PrErrors.ResistanceLimitsConflict(
          model.StartLineNumber,
          $"{model.CommandNumber}   {model.Mnemonic}",
          $"Не указана верхняя граница при проверке на сообщение"));
      }

      if (string.IsNullOrWhiteSpace(model.DisconnectedLowerLimitResistanceSource) && !model.AlgorithmKey.Contains(AlgorithmKey.ЗР.ToString()))
      {
        LogError($"Не удалось распознать параметры в строке: '{remainder}' (строка {numberLine})");
        model.Errors.Add(PrErrors.ResistanceLimitsConflict(
          model.StartLineNumber,
          $"{model.CommandNumber}   {model.Mnemonic}",
          $"Не указана нижняя граница при проверке на разобщение"));
      }
    }
  }
}

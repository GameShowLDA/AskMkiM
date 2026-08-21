using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Metadata.Static.Messages;

namespace Ask.Protocol.Messages.Builders;

/// <summary>
/// Формирует сообщения этапов и результатов самоконтроля оборудования.
/// </summary>
internal static class SelfTestMessageBuilder
{
  private const double RelativeErrorMarker = -1;

  internal static ShowMessageModel BuildInformation(
    string header,
    string? message = null,
    int indentLevel = 0)
    => new(header, message: message)
    {
      IndentLevel = indentLevel,
    };

  internal static ShowMessageModel BuildError(
    string details,
    string header = "Ошибка",
    int indentLevel = 0)
    => new(header, message: details, type: ShowMessageModel.MessageType.Error)
    {
      IndentLevel = indentLevel,
    };

  internal static ShowMessageModel BuildResult(
    string header,
    bool isSuccessful,
    string? message = null,
    int indentLevel = 0,
    string? executionErrorMessage = null,
    bool? executionError = null,
    bool? canBeDeleted = null,
    bool isStepModeCheckpoint = false)
  {
    bool hasExecutionError = executionError ?? false;
    return new ShowMessageModel(
      header,
      message: message,
      type: isSuccessful
        ? ShowMessageModel.MessageType.Success
        : ShowMessageModel.MessageType.Error)
    {
      IndentLevel = indentLevel,
      ExecutionError = hasExecutionError,
      ExecutionErrorMessage = executionErrorMessage,
      CanBeDeleted = canBeDeleted ?? false,
      IsStepModeCheckpoint = isStepModeCheckpoint,
    };
  }

  internal static ShowMessageModel BuildCommand(
    string header,
    string? message,
    int indentLevel)
  {
    var headerColor = new ShowMessageModel(type: ShowMessageModel.MessageType.CommandBlock)
      .GetColorMessage();

    return new ShowMessageModel(
      header,
      headerColor: headerColor,
      message: message,
      type: ShowMessageModel.MessageType.Command)
    {
      IndentLevel = indentLevel,
      IsStepModeCheckpoint = true,
      IsControlProgramCommandHeader = true,
    };
  }

  internal static ShowMessageModel BuildMultimeterMeasurementResult(
    bool isSuccessful,
    double result,
    string parameter,
    string unit,
    double idealResult,
    int percentageError,
    bool showResult)
  {
    string resultMessage = showResult
      ? $"{FormatResult(result)}{unit}"
      : string.Empty;
    string tolerance = idealResult == RelativeErrorMarker
      ? $"(± {percentageError}%)"
      : $"({idealResult} ± {percentageError}%)";

    return BuildResult(
      $"Тест {parameter}{unit} {tolerance}",
      isSuccessful,
      resultMessage,
      indentLevel: 1,
      executionError: false,
      canBeDeleted: false,
      isStepModeCheckpoint: true);
  }

  internal static ShowMessageModel BuildActiveResistanceResult(
    double result,
    bool isSuccessful,
    string capacitanceValue,
    double minimumResistance,
    string resistanceUnit)
  {
    return BuildResult(
      $"Тест активного сопротивления конденсатора {capacitanceValue} " +
      $"(>{minimumResistance:N0}{resistanceUnit})",
      isSuccessful,
      $"{FormatResult(result)}{resistanceUnit}",
      indentLevel: 1,
      executionError: false,
      canBeDeleted: false,
      isStepModeCheckpoint: true);
  }

  private static string FormatResult(double result)
    => MeasurementValueFormatter.IsOverloadValue(result)
      ? "Overload"
      : MeasurementValueFormatter.Format(result);
}

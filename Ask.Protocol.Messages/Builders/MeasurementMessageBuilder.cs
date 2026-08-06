using Ask.Core.Shared.DTO.Devices.Measurements;
using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Services.Extensions;
using Ask.Core.Shared.Metadata.Atributes;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums.Commands;
using Ask.Core.Shared.Metadata.Static.Messages;
using System.Globalization;
using System.Reflection;

namespace Ask.Protocol.Messages.Builders;

/// <summary>
/// Формирует сообщения об измеренных значениях, допустимых диапазонах и результатах измерений.
/// </summary>
internal static class MeasurementMessageBuilder
{
  internal static ShowMessageModel BuildPointConnectionError(
    string measurementTarget,
    string details)
  {
    return new ShowMessageModel(
      measurementTarget,
      message: details,
      type: ShowMessageModel.MessageType.Error)
    {
      IndentLevel = 1,
    };
  }
  private static readonly CultureInfo RussianDisplayCulture = CultureInfo.GetCultureInfo("ru-RU");

  /// <summary>
  /// Формирует заголовок начала измерения.
  /// </summary>
  /// <param name="measurementTypeCommand">Тип выполняемого измерения.</param>
  /// <returns>Сообщение о начале измерения.</returns>
  internal static ShowMessageModel BuildStart(MeasurementTypeCommand measurementTypeCommand)
  {
    CommandDisplayInfoAttribute displayInfo = measurementTypeCommand.GetCommandDisplayInfo();
    return new ShowMessageModel(
      $"Измерение {displayInfo.MeasurementDescription.ToLowerInvariant()}");
  }

  /// <summary>
  /// Формирует заголовок измерения тока утечки в режиме проверки прочности изоляции.
  /// </summary>
  /// <param name="measurementTypeCommand">Режим проверки прочности изоляции.</param>
  /// <returns>Сообщение о начале измерения тока утечки.</returns>
  /// <exception cref="ArgumentOutOfRangeException">
  /// Выбрасывается, если <paramref name="measurementTypeCommand"/> не соответствует режиму ACW или DCW.
  /// </exception>
  internal static ShowMessageModel BuildLeakageCurrentStart(MeasurementTypeCommand measurementTypeCommand)
  {
    string mode = GetInsulationStrengthMode(measurementTypeCommand);
    return new ShowMessageModel($"Измерение тока утечки {mode}");
  }

  /// <summary>
  /// Формирует заголовок этапа выполнения измерений.
  /// </summary>
  /// <returns>Сообщение о начале этапа измерений.</returns>
  internal static ShowMessageModel BuildMeasurementStage()
  {
    return new ShowMessageModel("Выполнение измерений");
  }

  /// <summary>
  /// Формирует сообщение об эталонном измеренном значении.
  /// </summary>
  /// <param name="measurementUnit">Единица измерения эталонного значения.</param>
  /// <param name="value">Измеренное эталонное значение.</param>
  /// <returns>Сообщение об эталонном значении.</returns>
  internal static ShowMessageModel BuildReferenceValue(Enum measurementUnit, double value)
  {
    ArgumentNullException.ThrowIfNull(measurementUnit);

    return new ShowMessageModel(
      "Эталонное значение",
      message: MeasurementValueFormatter.FormatWithUnit(value, measurementUnit.GetUnit()))
    {
      IndentLevel = 1,
    };
  }

  /// <summary>
  /// Формирует сообщение о выдаче испытательного напряжения.
  /// </summary>
  /// <param name="measurementTypeCommand">Режим испытательного напряжения.</param>
  /// <param name="voltage">Заданное испытательное напряжение.</param>
  /// <returns>Сообщение о выдаче испытательного напряжения.</returns>
  /// <exception cref="ArgumentOutOfRangeException">
  /// Выбрасывается, если <paramref name="measurementTypeCommand"/> не относится
  /// к режиму прочности изоляции ACW или DCW.
  /// </exception>
  internal static ShowMessageModel BuildTestVoltageOutput(
    MeasurementTypeCommand measurementTypeCommand,
    double voltage)
  {
    string mode = GetInsulationStrengthMode(measurementTypeCommand);

    return new ShowMessageModel(
      $"Выдача испытательного напряжения {mode}",
      message: MeasurementValueFormatter.FormatWithUnit(voltage, "В"));
  }

  /// <summary>
  /// Формирует сообщение о неуспешном измерении разряда и переходе к методу полного узла.
  /// </summary>
  /// <param name="measurementTypeCommand">Тип выполненного измерения.</param>
  /// <param name="measurementRange">Измеренное значение и границы допустимого диапазона.</param>
  /// <param name="dischargeNumber">Порядковый номер проверяемого разряда.</param>
  /// <param name="dischargeView">Двоичное представление проверяемого разряда.</param>
  /// <returns>Сообщение о неуспешном измерении и смене алгоритма проверки.</returns>
  internal static ShowMessageModel BuildFullNodeFallbackResult(
    MeasurementTypeCommand measurementTypeCommand,
    MeasurementRange measurementRange,
    int dischargeNumber,
    string dischargeView)
  {
    ShowMessageModel message = BuildResult(
      measurementTypeCommand,
      measurementRange,
      $"Разряд {dischargeNumber} ({dischargeView})");
    message.Message = $"{message.Message}. Переход к методу полного узла";
    message.Status = ShowMessageModel.MessageType.Error;
    return message;
  }

  internal static ShowMessageModel BuildResult(
    MeasurementTypeCommand measurementTypeCommand,
    MeasurementRange measurementRange,
    string? chains = null,
    string comparisonSign = "=")
  {
    CommandDisplayInfoAttribute? displayInfo = typeof(MeasurementTypeCommand)
      .GetMember(measurementTypeCommand.ToString())
      .FirstOrDefault()?
      .GetCustomAttribute<CommandDisplayInfoAttribute>();

    if (displayInfo == null)
    {
      return new ShowMessageModel(
        "Ошибка формирования сообщения измерения",
        message: "Атрибут CommandDisplayInfoAttribute не найден.");
    }

    ArgumentNullException.ThrowIfNull(measurementRange);

    string chainDisplay = chains ?? string.Empty;
    string header = BuildMeasurementHeader(chainDisplay, measurementRange, displayInfo.Unit);

    string measuredValue = BuildMeasuredValue(
      measurementTypeCommand,
      measurementRange,
      displayInfo,
      comparisonSign);

    return new ShowMessageModel(header, message: measuredValue);
  }

  internal static ShowMessageModel BuildResult(
    Enum measurementUnit,
    MeasurementRange measurementRange,
    string? measurementTarget = null,
    string comparisonSign = "=")
  {
    ArgumentNullException.ThrowIfNull(measurementUnit);
    ArgumentNullException.ThrowIfNull(measurementRange);

    string unit = measurementUnit.GetUnit();
    string symbol = measurementUnit.GetQuantitySymbol().ToString();
    string header = BuildMeasurementHeader(measurementTarget ?? string.Empty, measurementRange, unit);
    string message = $"{symbol}изм{comparisonSign} " +
      $"{MeasurementValueFormatter.Format(measurementRange.TargetValue)} {unit}";

    return new ShowMessageModel(header, message: message);
  }

  /// <summary>
  /// Формирует сообщение о погрешности измерения.
  /// </summary>
  /// <param name="measurementUnit">Единица измерения.</param>
  /// <param name="measurementRange">Погрешность и допустимые границы измерения.</param>
  /// <param name="showAllowedRange">Признак включения допустимого диапазона в заголовок.</param>
  /// <returns>Сообщение о погрешности измерения.</returns>
  /// <exception cref="ArgumentNullException">
  /// Выбрасывается, если <paramref name="measurementUnit"/> или
  /// <paramref name="measurementRange"/> равен <see langword="null"/>.
  /// </exception>
  internal static ShowMessageModel BuildError(
    Enum measurementUnit,
    MeasurementRange measurementRange,
    bool showAllowedRange)
  {
    ArgumentNullException.ThrowIfNull(measurementUnit);
    ArgumentNullException.ThrowIfNull(measurementRange);

    string unit = measurementUnit.GetUnit();
    string header = "Погрешность измерения";

    if (showAllowedRange)
    {
      header += $" ({FormatMeasurementLimit(measurementRange.LowerBound)}<{unit}<" +
        $"{FormatMeasurementLimit(measurementRange.UpperBound)})";
    }

    string message = $"{MeasurementValueFormatter.Format(measurementRange.TargetValue)} {unit}";
    return new ShowMessageModel(header, message: message);
  }

  private static string BuildMeasuredValue(
    MeasurementTypeCommand measurementTypeCommand,
    MeasurementRange measurementRange,
    CommandDisplayInfoAttribute displayInfo,
    string comparisonSign)
  {
    string prefix = $"{displayInfo.Symbol}изм{comparisonSign} ";

    if ((measurementRange.TargetValue < measurementRange.LowerBound ||
         measurementRange.TargetValue > measurementRange.UpperBound) &&
        measurementTypeCommand is MeasurementTypeCommand.PI_ACW or MeasurementTypeCommand.PI_DCW)
    {
      return $"{prefix}ПРОБОЙ";
    }

    if (MeasurementValueFormatter.IsOverloadValue(measurementRange.TargetValue) &&
        measurementTypeCommand is MeasurementTypeCommand.EHT or
          MeasurementTypeCommand.KC or
          MeasurementTypeCommand.PR or
          MeasurementTypeCommand.NE)
    {
      return $"{prefix}Overload";
    }

    if (MeasurementValueFormatter.IsOverloadValue(measurementRange.TargetValue, 9.899999999999999E+46) &&
        measurementTypeCommand == MeasurementTypeCommand.IE)
    {
      return $"{prefix}Overload";
    }

    return $"{prefix}{MeasurementValueFormatter.Format(measurementRange.TargetValue)} {displayInfo.Unit}";
  }

  private static string BuildMeasurementHeader(
    string chains,
    MeasurementRange measurementRange,
    string unit)
  {
    string range = measurementRange.UpperBound == -1
      ? $"{FormatMeasurementLimit(measurementRange.LowerBound)}<{unit}"
      : measurementRange.LowerBound == 0
        ? $"{unit}<{FormatMeasurementLimit(measurementRange.UpperBound)}"
        : $"{FormatMeasurementLimit(measurementRange.LowerBound)}<{unit}<" +
          FormatMeasurementLimit(measurementRange.UpperBound);

    return string.IsNullOrWhiteSpace(chains)
      ? $"({range})"
      : $"{chains} ({range})";
  }

  private static string FormatMeasurementLimit(double value)
  {
    return value.ToString("G", RussianDisplayCulture);
  }

  private static string GetInsulationStrengthMode(MeasurementTypeCommand measurementTypeCommand)
  {
    return measurementTypeCommand switch
    {
      MeasurementTypeCommand.PI_ACW => "ACW",
      MeasurementTypeCommand.PI_DCW => "DCW",
      _ => throw new ArgumentOutOfRangeException(
        nameof(measurementTypeCommand),
        measurementTypeCommand,
        "Режим не относится к проверке прочности изоляции ACW или DCW."),
    };
  }
}

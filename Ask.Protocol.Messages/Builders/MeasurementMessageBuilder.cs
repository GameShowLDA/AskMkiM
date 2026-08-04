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
  private static readonly CultureInfo RussianDisplayCulture = CultureInfo.GetCultureInfo("ru-RU");

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
}

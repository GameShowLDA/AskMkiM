using Ask.Core.Services.Extensions;
using Ask.Core.Shared.DTO.Devices.RelaySwitchModule;
using Ask.Core.Shared.Metadata.Static.Messages;
using Ask.Protocol.Messages.Models;

namespace Ask.Protocol.Messages.Builders;

/// <summary>
/// Формирует описания брака для итогового протокола измерений.
/// </summary>
internal static class MeasurementFailureMessageBuilder
{
  internal static string BuildGroupFailure(
    int dischargeIndex,
    string bitString,
    double limit,
    double result,
    Enum unit,
    MeasurementLimitKind limitKind)
  {
    string condition = BuildLimitCondition(limit, unit, limitKind);
    string formattedResult = FormatValue(result, unit);
    var quantitySymbol = unit.GetQuantitySymbol();
    return $"Разряд-{dischargeIndex}[{bitString}] ({condition}). " +
      $"{quantitySymbol}изм = {formattedResult}";
  }

  internal static string BuildNodeFailure(
    PointModel point,
    double limit,
    double result,
    Enum unit,
    MeasurementLimitKind limitKind)
  {
    string condition = BuildLimitCondition(limit, unit, limitKind);
    string formattedResult = FormatValue(result, unit);
    return $"Точка[{point}]({condition}). {unit.GetQuantitySymbol()}изм = {formattedResult}";
  }

  internal static string BuildNodeRangeFailure(
    PointModel point,
    double lowerLimit,
    double upperLimit,
    double result,
    Enum unit)
  {
    string lower = MeasurementValueFormatter.Format(lowerLimit);
    string upper = MeasurementValueFormatter.Format(upperLimit);
    string formattedResult = FormatValue(result, unit);
    string unitName = unit.GetUnit();
    var quantitySymbol = unit.GetQuantitySymbol();
    return $"Точка[{point}]({lower}<{quantitySymbol}<{upper} {unitName}). " +
      $"{quantitySymbol}изм = {formattedResult}";
  }

  private static string BuildLimitCondition(
    double limit,
    Enum unit,
    MeasurementLimitKind limitKind)
  {
    string formattedLimit = MeasurementValueFormatter.Format(limit);
    string unitName = unit.GetUnit();
    var quantitySymbol = unit.GetQuantitySymbol();
    return limitKind switch
    {
      MeasurementLimitKind.Minimum => $"{formattedLimit}<{quantitySymbol} {unitName}",
      MeasurementLimitKind.Maximum => $"{quantitySymbol}<{formattedLimit} {unitName}",
      _ => throw new ArgumentOutOfRangeException(nameof(limitKind), limitKind, null),
    };
  }

  private static string FormatValue(double value, Enum unit)
    => MeasurementValueFormatter.FormatWithUnit(value, unit.GetUnit());
}

using Ask.Core.Shared.Interfaces.ExecutionInterfaces;

namespace Ask.Engine.ControlCommandAnalyser.Formatter.Base
{
  internal class ResistanceFormatter
  {
    public static string FormatResistanceLowerLimit(
      IHasResistanceLimits model,
      string? indent = "\t")
    {
      return FormatResistanceLowerLimit(model.LowerLimitResistanceSource, indent);
    }

    public static string FormatResistanceLowerLimit(
      string? resistance,
      string? indent = "\t")
    {
      return SourceValueFormatter.Format(resistance, "Нижний порог сопротивления", "Нижний порог сопротивления не задан.", indent);
    }

    public static string FormatHigherLimitResistance(
      IHasResistanceLimits model,
      string? indent = "\t")
    {
      return FormatHigherLimitResistance(model.HigherLimitResistanceSource, indent);
    }

    public static string FormatHigherLimitResistance(
      string? resistance,
      string? indent = "\t")
    {
      return SourceValueFormatter.Format(resistance, "Верхний порог сопротивления", "Верхний порог сопротивления не задан.", indent);
    }

    public static string FormatResistance(
      IHasResistance model,
      string? indent = "\t")
    {
      return FormatResistance(model.ResistanceSource, indent);
    }

    public static string FormatResistance(
      string? resistance,
      string? indent = "\t")
    {
      return SourceValueFormatter.Format(resistance, "Сопротивление", "Сопротивление не задано!", indent);
    }

    public static string FormatCableResistance(
      IHasCableResistance model,
      string? indent = "\t")
    {
      return FormatCableResistance(model.CabelResistanceSource, indent);
    }

    public static string FormatCableResistance(
      string? resistance,
      string? indent = "\t")
    {
      return SourceValueFormatter.Format(resistance, "Сопротивление проводов", "Сопротивление проводов не задано.", indent);
    }
  }
}

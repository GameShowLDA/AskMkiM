using Ask.Core.Shared.Interfaces.ExecutionInterfaces;

namespace Ask.Engine.ControlCommandAnalyser.Formatter.Base
{
  internal class CapacityFormatter
  {
    public static string FormatCapacityLowerLimit(
      IHasCapacityLimits model,
      string? indent = "\t")
    {
      return FormatCapacityLowerLimit(model.LowerLimitCapacitySource, indent);
    }

    public static string FormatCapacityLowerLimit(
      string? capacity,
      string? indent = "\t")
    {
      return SourceValueFormatter.Format(capacity, "Нижний порог электрической емкости", "Нижний порог электрической емкости не задан!", indent);
    }

    public static string FormatCapacityHigherLimit(
      IHasCapacityLimits model,
      string? indent = "\t")
    {
      return FormatCapacityHigherLimit(model.HigherLimitCapacitySource, indent);
    }

    public static string FormatCapacityHigherLimit(
      string? capacity,
      string? indent = "\t")
    {
      return SourceValueFormatter.Format(capacity, "Верхний порог электрической емкости", "Верхний порог электрической емкости не задан!", indent);
    }
  }
}

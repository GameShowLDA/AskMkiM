using Ask.Core.Shared.Interfaces.ExecutionInterfaces;
using Ask.Engine.ControlCommandAnalyser.Model;

namespace Ask.Engine.ControlCommandAnalyser.Formatter.Base
{
  internal class VoltageFormatter
  {
    public static string FormatVoltage(
      IHasVoltage model,
      string? indent = "\t",
      bool showNotSet = true)
    {
      return FormatVoltage(model.VoltageSource, indent, showNotSet);
    }

    public static string FormatVoltage(
      string? voltage,
      string? indent = "\t",
      bool showNotSet = true)
    {
      return SourceValueFormatter.Format(voltage, "Напряжение", "Напряжение не задано!", indent, showNotSet);
    }

    public static string FormatVoltageLowerLimit(
      IHasVoltageLimits model,
      string? indent = "\t")
    {
      return FormatVoltageLowerLimit(model.LowerLimitVoltageSource, indent);
    }

    public static string FormatVoltageLowerLimit(
      string? voltage,
      string? indent = "\t")
    {
      return SourceValueFormatter.Format(voltage, "Нижний порог напряжения", "Нижний порог напряжения не задан!", indent);
    }

    public static string FormatVoltageHigherLimit(
      IHasVoltageLimits model,
      string? indent = "\t")
    {
      return FormatVoltageHigherLimit(model.HigherLimitVoltageSource, indent);
    }

    public static string FormatVoltageHigherLimit(
      string? voltage,
      string? indent = "\t")
    {
      return SourceValueFormatter.Format(voltage, "Верхний порог напряжения", "Верхний порог напряжения не задан!", indent);
    }

    public static string FormatVoltageType(
      VoltageEnum.Type voltageType,
      string? indent = "\t")
    {
      return voltageType == VoltageEnum.Type.ACW
        ? $"{indent}Тип тока: переменный"
        : $"{indent}Тип тока: постоянный";
    }
  }
}

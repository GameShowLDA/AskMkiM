using Ask.Core.Services.Errors.Translation;
using Ask.Engine.ControlCommandAnalyser.Model;

namespace Ask.Engine.ControlCommandAnalyser.Parser.Common.Helpers
{
  public static class VoltageManager
  {
    public static void ApplyRange(
        NeCommandModel model,
        string? unit,
        double? lower,
        double? higher,
        double defaultLower,
        double defaultHigher)
    {
      (double? valLower, string lowerUnit) =
          lower.HasValue ? UnitsConvertor.TryConvertBack(lower.Value, unit) : (null, unit);

      (double? valHigher, string higherUnit) =
          higher.HasValue ? UnitsConvertor.TryConvertBack(higher.Value, unit) : (null, unit);

      if (!valLower.HasValue || !valHigher.HasValue)
      {
        model.Errors.Add(
            NeErrors.VoltageLimitsConflict(
                model.StartLineNumber,
                $"{model.CommandNumber} {model.Mnemonic}",
                "Не указан диапазон напряжения"));
        return;
      }

      if (lower > higher)
      {
        model.Errors.Add(
            NeErrors.VoltageLimitsConflict(
                model.StartLineNumber,
                $"{model.CommandNumber} {model.Mnemonic}",
                $"Нижняя граница ({valLower} {lowerUnit}) больше верхней ({valHigher} {higherUnit})"));
        return;
      }

      if (lower < defaultLower)
      {
        model.Errors.Add(
            NeErrors.VoltageLimitsConflict(
                model.StartLineNumber,
                $"{model.CommandNumber} {model.Mnemonic}",
                $"Нижняя граница ({valLower} {lowerUnit}) меньше минимально допустимой ({defaultLower} В)"));
        return;
      }

      if (higher > defaultHigher)
      {
        model.Errors.Add(
            NeErrors.VoltageLimitsConflict(
                model.StartLineNumber,
                $"{model.CommandNumber} {model.Mnemonic}",
                $"Верхняя граница ({valHigher} {higherUnit}) больше максимально допустимой ({defaultHigher} В)"));
        return;
      }

      model.LowerLimitVoltage = lower;
      model.LowerLimitVoltageSource = $"{valLower}{lowerUnit}";

      model.HigherLimitVoltage = higher;
      model.HigherLimitVoltageSource = $"{valHigher}{higherUnit}";
    }

    public static void ApplyDefaultVoltage(
        NeCommandModel model,
        double defaultVoltage,
        string unit)
    {
      model.Voltage = defaultVoltage;
      model.VoltageSource = $"{defaultVoltage}{unit}";
      model.VoltageUnit = unit;
    }

    public static void ApplySingleVoltage(
        SiCommandModel model,
        double value,
        string unit,
        double min,
        double max,
        int line,
        string command)
    {
      if (value > max)
      {
        model.Errors.Add(
            GeneralErrors.VoltageConflict(line, command, "Напряжение превышает максимально допустимое"));
      }
      else if (value < min)
      {
        model.Errors.Add(
            GeneralErrors.VoltageConflict(line, command, "Напряжение меньше минимально допустимого"));
      }

      model.Voltage = value;
      model.VoltageSource = $"{value}{unit}";
    }

    public static void ApplyOperatingVoltage(
    NeCommandModel model,
    double value,
    string unit)
    {
      model.Voltage = value;
      model.VoltageSource = $"{value}{unit}";
      model.VoltageUnit = unit;
    }
  }
}

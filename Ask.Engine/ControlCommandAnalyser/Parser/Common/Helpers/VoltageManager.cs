using Ask.Core.Services.Errors.Translation;
using Ask.Engine.ControlCommandAnalyser.Model;

namespace Ask.Engine.ControlCommandAnalyser.Parser.Common.Helpers
{
  /// <summary>
  /// Менеджер обработки параметров напряжения.
  /// Выполняет применение диапазона, одиночного и значений по умолчанию к моделям команд.
  /// </summary>
  public static class VoltageManager
  {
    /// <summary>
    /// Применяет диапазон напряжения к модели и выполняет проверки границ.
    /// </summary>
    /// <param name="model">Модель команды НЕ.</param>
    /// <param name="unit">Единица измерения.</param>
    /// <param name="lower">Нижняя граница.</param>
    /// <param name="higher">Верхняя граница.</param>
    /// <param name="defaultLower">Минимально допустимое значение.</param>
    /// <param name="defaultHigher">Максимально допустимое значение.</param>
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

    /// <summary>
    /// Применяет значение напряжения по умолчанию.
    /// </summary>
    /// <param name="model">Модель команды НЕ.</param>
    /// <param name="defaultVoltage">Значение напряжения.</param>
    /// <param name="unit">Единица измерения.</param>
    public static void ApplyDefaultVoltage(
        NeCommandModel model,
        double defaultVoltage,
        string unit)
    {
      model.Voltage = defaultVoltage;
      model.VoltageSource = $"{defaultVoltage}{unit}";
      model.VoltageUnit = unit;
    }

    /// <summary>
    /// Применяет одиночное значение напряжения с проверкой диапазона.
    /// </summary>
    /// <param name="model">Модель команды СИ.</param>
    /// <param name="value">Значение напряжения.</param>
    /// <param name="unit">Единица измерения.</param>
    /// <param name="min">Минимально допустимое значение.</param>
    /// <param name="max">Максимально допустимое значение.</param>
    /// <param name="line">Номер строки.</param>
    /// <param name="command">Идентификатор команды.</param>
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

    /// <summary>
    /// Применяет рабочее напряжение к модели.
    /// </summary>
    /// <param name="model">Модель команды НЕ.</param>
    /// <param name="value">Значение напряжения.</param>
    /// <param name="unit">Единица измерения.</param>
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

using Ask.Core.Services.Errors.Models;

namespace Ask.Core.Services.Errors.Translation
{
  /// <summary>
  /// Предоставляет набор методов для генерации общих предупреждений,
  /// возникающих при анализе и проверке последовательности команд
  /// в управляющих программах, не зависящих от конкретной команды.
  /// Каждый метод возвращает объект <see cref="WarningItem"/>,
  /// описывающий конкретную ошибочную ситуацию с указанием строки,
  /// команды и дополнительного описания.
  /// </summary>
  public static class GeneralWarnings
  {
    /// <summary>
    /// Предупреждение: значение сопротивления установлено по умолчанию.
    /// </summary>
    public static WarningItem DefaultResistance(int startLineNumber, string command, string resistance) => new()
    {
      SourceLineNumber = startLineNumber,
      Command = command,
      Code = WarningCode.Gen_DefaultResistaince,
      Description = $"В команде {command} значение сопротивления установлено по умолчанию: {resistance}."
    };

    /// <summary>
    /// Предупреждение: значение времени выполнения установлено по умолчанию.
    /// </summary>
    public static WarningItem DefaultTime(int startLineNumber, string command, string time) => new()
    {
      SourceLineNumber = startLineNumber,
      Command = command,
      Code = WarningCode.Gen_DefaultTime,
      Description = $"В команде {command} значение времени выполнения установлено по умолчанию: {time}."
    };

    /// <summary>
    /// Предупреждение: значение напряжения установлено по умолчанию.
    /// </summary>
    public static WarningItem DefaultVoltage(int startLineNumber, string command, string voltage) => new()
    {
      SourceLineNumber = startLineNumber,
      Command = command,
      Code = WarningCode.Gen_DefaultVoltage,
      Description = $"В команде {command} значение напряжения установлено по умолчанию: {voltage}."
    };

    /// <summary>
    /// Предупреждение: значение нижней границы сопротивления установлено по умолчанию.
    /// </summary>
    public static WarningItem DefaultResistainceLowLimit(int startLineNumber, string command, string resistance) => new()
    {
      SourceLineNumber = startLineNumber,
      Command = command,
      Code = WarningCode.Gen_DefaultResistainceLowLimit,
      Description = $"В команде {command} значение нижней границы сопротивления установлено по умолчанию:{resistance}."
    };

    /// <summary>
    /// Предупреждение: значение верхней границы сопротивления установлено по умолчанию.
    /// </summary>
    public static WarningItem DefaultResistainceHighLimit(int startLineNumber, string command, string resistance) => new()
    {
      SourceLineNumber = startLineNumber,
      Command = command,
      Code = WarningCode.Gen_DefaultResistainceHighLimit,
      Description = $"В команде {command} значение верхней границы сопротивления установлено по умолчанию:{resistance}."
    };

    /// <summary>
    /// Предупреждение: значение нижней границы элктрической емкости установлено по умолчанию.
    /// </summary>
    public static WarningItem DefaultCapacityLowLimit(int startLineNumber, string command, string сapacity) => new()
    {
      SourceLineNumber = startLineNumber,
      Command = command,
      Code = WarningCode.Gen_DefaultResistainceLowLimit,
      Description = $"В команде {command} значение нижней границы элктрической емкости установлено по умолчанию:{сapacity}."
    };

    /// <summary>
    /// Предупреждение: значение верхней границы элктрической емкости установлено по умолчанию.
    /// </summary>
    public static WarningItem DefaultCapacityHighLimit(int startLineNumber, string command, string сapacity) => new()
    {
      SourceLineNumber = startLineNumber,
      Command = command,
      Code = WarningCode.Gen_DefaultResistainceHighLimit,
      Description = $"В команде {command} значение верхней границы элктрической емкости установлено по умолчанию:{сapacity}."
    };

    /// <summary>
    /// Предупреждение: значение верхней границы элктрической емкости установлено по умолчанию.
    /// </summary>
    public static WarningItem DuplicateKey(int startLineNumber, string command, string key) => new()
    {
      SourceLineNumber = startLineNumber,
      Command = command,
      Code = WarningCode.Gen_DuplicateKey,
      Description = $"В команде {command} найден дублирующийся ключ:{key}."
    };

    /// <summary>
    /// Предупреждение: значение верхней границы элктрической емкости установлено по умолчанию.
    /// </summary>
    public static WarningItem KeyZR(int startLineNumber, string command) => new()
    {
      SourceLineNumber = startLineNumber,
      Command = command,
      Code = WarningCode.Gen_KeyZR,
      Description = $"В команде {command} нет точек для проверки на разобщение. Добавлен ключ ЗР.",
    };

    /// <summary>
    /// Предупреждение: команда ВШ была добавлена автоматически.
    /// </summary>
    public static WarningItem VshCommandAddedAutomatically(int startLineNumber, string command) => new()
    {
      SourceLineNumber = startLineNumber,
      Command = command,
      Code = WarningCode.Gen_VshCommandAddedAutomatically,
      Description = $"Команда ВШ не была указана в программе контроля и была добавлена автоматически.",
    };
  }
}

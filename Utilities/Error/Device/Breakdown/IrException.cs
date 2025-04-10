using Utilities.Error.Device;

namespace Utilities.Error.Device.Breakdown
{
  /// <summary>
  /// Фабрика исключений для ошибок, возникающих при работе в режиме измерения сопротивления изоляции (IR).
  /// Предоставляет готовые исключения с корректными сообщениями об ошибках.
  /// </summary>
  public static class IrExceptionFactory
  {
    /// <summary>
    /// Создаёт исключение при невозможности подключения к устройству.
    /// </summary>
    /// <param name="name">Имя устройства.</param>
    /// <param name="chassis">Номер шасси.</param>
    /// <param name="number">Номер устройства.</param>
    public static DeviceException ConnectionFailed(string name, int chassis, int number) =>
        new($"Нет подключения к {name}({chassis}.{number})");

    /// <summary>
    /// Создаёт исключение при ошибке установки режима IR.
    /// </summary>
    /// <param name="name">Имя устройства.</param>
    /// <param name="chassis">Номер шасси.</param>
    /// <param name="number">Номер устройства.</param>
    /// <param name="reason">Дополнительное сообщение об ошибке.</param>
    public static DeviceException SetModeFailed(string name, int chassis, int number, string reason = null) =>
        new($"Ошибка установки режима IR {name}({chassis}.{number}){Format(reason)}");

    /// <summary>
    /// Создаёт исключение при ошибке установки напряжения IR.
    /// </summary>
    public static DeviceException SetVoltageFailed(string name, int chassis, int number, string reason = null) =>
        new($"Ошибка установки напряжения IR {name}({chassis}.{number}){Format(reason)}");

    /// <summary>
    /// Создаёт исключение при ошибке установки верхнего предела сопротивления IR.
    /// </summary>
    public static DeviceException SetHighLimitFailed(string name, int chassis, int number, string reason = null) =>
        new($"Ошибка установки верхнего предела сопротивления IR {name}({chassis}.{number}){Format(reason)}");

    /// <summary>
    /// Создаёт исключение при ошибке установки нижнего предела сопротивления IR.
    /// </summary>
    public static DeviceException SetLowLimitFailed(string name, int chassis, int number, string reason = null) =>
        new($"Ошибка установки нижнего предела сопротивления IR {name}({chassis}.{number}){Format(reason)}");

    /// <summary>
    /// Создаёт исключение при ошибке установки времени теста IR.
    /// </summary>
    public static DeviceException SetTestTimeFailed(string name, int chassis, int number, string reason = null) =>
        new($"Ошибка установки времени теста IR {name}({chassis}.{number}){Format(reason)}");

    /// <summary>
    /// Создаёт исключение при ошибке установки смещения IR.
    /// </summary>
    public static DeviceException SetOffsetFailed(string name, int chassis, int number, string reason = null) =>
        new($"Ошибка установки смещения IR {name}({chassis}.{number}){Format(reason)}");

    /// <summary>
    /// Форматирует текст ошибки, добавляя пояснение, если оно указано.
    /// </summary>
    /// <param name="reason">Дополнительное описание ошибки.</param>
    /// <returns>Строка в формате ": сообщение" или пустая строка.</returns>
    private static string Format(string reason) =>
        string.IsNullOrWhiteSpace(reason) ? string.Empty : $": {reason}";
  }
}

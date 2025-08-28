using AppConfiguration.Error.Device;

namespace AppConfiguration.Error.Device.Breakdown
{
  /// <summary>
  /// Фабрика исключений для ошибок, возникающих при работе в режиме ACW.
  /// </summary>
  public static class AcwExceptionFactory
  {
    /// <summary>
    /// Исключение при ошибке установки режима ACW.
    /// </summary>
    public static DeviceException SetModeFailed(string name, int chassis, int number, string reason = null) =>
        new($"Ошибка установки режима ACW {name}({chassis}.{number}){Format(reason)}");

    /// <summary>
    /// Исключение при ошибке установки напряжения ACW.
    /// </summary>
    public static DeviceException SetVoltageFailed(string name, int chassis, int number, string reason = null) =>
        new($"Ошибка установки напряжения ACW {name}({chassis}.{number}){Format(reason)}");

    /// <summary>
    /// Исключение при ошибке установки верхнего предела тока ACW.
    /// </summary>
    public static DeviceException SetHighLimitFailed(string name, int chassis, int number, string reason = null) =>
        new($"Ошибка установки верхнего предела тока ACW {name}({chassis}.{number}){Format(reason)}");

    /// <summary>
    /// Исключение при ошибке установки нижнего предела тока ACW.
    /// </summary>
    public static DeviceException SetLowLimitFailed(string name, int chassis, int number, string reason = null) =>
        new($"Ошибка установки нижнего предела тока ACW {name}({chassis}.{number}){Format(reason)}");

    /// <summary>
    /// Исключение при ошибке установки времени теста ACW.
    /// </summary>
    public static DeviceException SetTestTimeFailed(string name, int chassis, int number, string reason = null) =>
        new($"Ошибка установки времени теста ACW {name}({chassis}.{number}){Format(reason)}");

    /// <summary>
    /// Исключение при ошибке установки времени нарастания (Ramp Time) ACW.
    /// </summary>
    public static DeviceException SetRampTimeFailed(string name, int chassis, int number, string reason = null) =>
        new($"Ошибка установки времени нарстания ACW {name}({chassis}.{number}){Format(reason)}");

    /// <summary>
    /// Исключение при ошибке установки частоты ACW.
    /// </summary>
    public static DeviceException SetFrequencyFailed(string name, int chassis, int number, string reason = null) =>
        new($"Ошибка установки частоты ACW {name}({chassis}.{number}){Format(reason)}");

    /// <summary>
    /// Исключение при ошибке установки смещения ACW.
    /// </summary>
    public static DeviceException SetOffsetFailed(string name, int chassis, int number, string reason = null) =>
        new($"Ошибка установки смещения ACW {name}({chassis}.{number}){Format(reason)}");

    /// <summary>
    /// Исключение при ошибке установки дугового тока ACW.
    /// </summary>
    public static DeviceException SetArcCurrentFailed(string name, int chassis, int number, string reason = null) =>
        new($"Ошибка установки дугового тока ACW {name}({chassis}.{number}){Format(reason)}");

    private static string Format(string reason) =>
        string.IsNullOrWhiteSpace(reason) ? string.Empty : $": {reason}";
  }
}

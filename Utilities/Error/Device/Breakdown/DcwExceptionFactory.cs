namespace Utilities.Error.Device.Breakdown
{
  /// <summary>
  /// Фабрика исключений для ошибок, возникающих при работе в режиме DCW.
  /// </summary>
  public static class DcwExceptionFactory
  {
    public static DeviceException SetModeFailed(string name, int chassis, int number, string reason = null) =>
        new($"Ошибка установки режима DCW {name}({chassis}.{number}){Format(reason)}");

    public static DeviceException SetVoltageFailed(string name, int chassis, int number, string reason = null) =>
        new($"Ошибка установки напряжения DCW {name}({chassis}.{number}){Format(reason)}");

    public static DeviceException SetHighLimitFailed(string name, int chassis, int number, string reason = null) =>
        new($"Ошибка установки верхнего предела тока DCW {name}({chassis}.{number}){Format(reason)}");

    public static DeviceException SetLowLimitFailed(string name, int chassis, int number, string reason = null) =>
        new($"Ошибка установки нижнего предела тока DCW {name}({chassis}.{number}){Format(reason)}");

    public static DeviceException SetTestTimeFailed(string name, int chassis, int number, string reason = null) =>
        new($"Ошибка установки времени теста DCW {name}({chassis}.{number}){Format(reason)}");

    public static DeviceException SetRampTimeFailed(string name, int chassis, int number, string reason = null) =>
        new($"Ошибка установки Ramp Time DCW {name}({chassis}.{number}){Format(reason)}");

    public static DeviceException SetOffsetFailed(string name, int chassis, int number, string reason = null) =>
        new($"Ошибка установки смещения DCW {name}({chassis}.{number}){Format(reason)}");

    public static DeviceException SetArcCurrentFailed(string name, int chassis, int number, string reason = null) =>
        new($"Ошибка установки дугового тока DCW {name}({chassis}.{number}){Format(reason)}");

    private static string Format(string reason) =>
        string.IsNullOrWhiteSpace(reason) ? string.Empty : $": {reason}";
  }
}
namespace Ask.Core.Services.Errors.Device.Multimeter
{
  public static class CapacitanceExceptionFactory
  {
    /// <summary>
    /// Исключение при ошибке установки режима  измерения ёмкости.
    /// </summary>
    public static DeviceException SetModeFailed(string name, int chassis, int number, string reason = null) =>
        new($"Ошибка установки режима измерения ёмкости {name}({chassis}.{number}){Format(reason)}");

    /// <summary>
    /// Исключение при ошибке измерения режима измерения ёмкости.
    /// </summary>
    public static DeviceException SetMeasureFailed(string name, int chassis, int number, string reason = null) =>
        new($"Ошибка измеренния при режиме измерения ёмкости {name}({chassis}.{number}){Format(reason)}");

    private static string Format(string reason) => string.IsNullOrWhiteSpace(reason) ? string.Empty : $": {reason}";
  }
}

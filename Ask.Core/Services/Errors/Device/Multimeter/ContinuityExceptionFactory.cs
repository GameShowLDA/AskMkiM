namespace Ask.Core.Services.Errors.Device.Multimeter
{
  public static class ContinuityExceptionFactory
  {
    /// <summary>
    /// Исключение при ошибке установки режима  Continuity.
    /// </summary>
    public static DeviceException SetModeFailed(string name, int chassis, int number, string reason = null) =>
        new($"Ошибка установки режима прозвонки {name}({chassis}.{number}){Format(reason)}");

    /// <summary>
    /// Исключение при ошибке измерения режима  Continuity.
    /// </summary>
    public static DeviceException SetContinuityFailed(string name, int chassis, int number, string reason = null) =>
        new($"Ошибка проверки при режиме прозвонки {name}({chassis}.{number}){Format(reason)}");

    private static string Format(string reason) => string.IsNullOrWhiteSpace(reason) ? string.Empty : $": {reason}";
  }
}

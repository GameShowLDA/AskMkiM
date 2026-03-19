namespace Ask.Core.Services.Errors.Device.Multimeter
{
  public static class DiodeExceptionFactory
  {
    /// <summary>
    /// Исключение при ошибке установки режима проверки диода.
    /// </summary>
    public static DeviceException SetModeFailed(string name, int chassis, int number, string reason = null) =>
        new($"Ошибка установки режима проверки диода {name}({chassis}.{number}){Format(reason)}");

    /// <summary>
    /// Исключение при ошибке проверки диода.
    /// </summary>
    public static DeviceException SetCheckFailed(string name, int chassis, int number, string reason = null) =>
        new($"Ошибка проверки диода {name}({chassis}.{number}){Format(reason)}");

    private static string Format(string reason) => string.IsNullOrWhiteSpace(reason) ? string.Empty : $": {reason}";
  }
}

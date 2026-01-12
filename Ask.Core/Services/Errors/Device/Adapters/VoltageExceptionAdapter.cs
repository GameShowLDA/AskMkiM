namespace Ask.Core.Services.Errors.Device.Adapters
{
  public static class VoltageExceptionAdapter
  {
    /// <summary>
    /// Исключение при ошибке установки источника напряжения или null, если подавление включено.
    /// </summary>
    public static DeviceException SetSourceFailed(string source, string reason = null)
    {
      return new($"Ошибка выбора источника напряжения ({source}){Format(reason)}");
    }

    /// <summary>
    /// Исключение при ошибке установки уровня напряжения или null, если подавление включено.
    /// </summary>
    public static DeviceException SetLevelFailed(string value, string reason = null)
    {
      return new($"Ошибка установки напряжения {value} В{Format(reason)}");
    }

    private static string Format(string reason) =>
    string.IsNullOrWhiteSpace(reason) ? string.Empty : $": {reason}";
  }
}

namespace Ask.Core.Services.Errors.Device.Adapters
{
  public static class ConnectionExceptionAdapter
  {
    /// <summary>  
    /// Исключение при ошибке подключения устройства.  
    /// </summary>  
    public static DeviceException ConnectFailed(string name, int chassis, int number, string? reason = null)
    {
      return new($"Ошибка подключения к {name}({chassis}.{number}){Format(reason)}");
    }

    /// <summary>  
    /// Исключение при ошибке отключения устройства.  
    /// </summary>  
    public static DeviceException DisconnectFailed(string name, int chassis, int number, string? reason = null)
    {
      return new($"Ошибка отключения от {name}({chassis}.{number}){Format(reason)}");
    }

    /// <summary>  
    /// Исключение при ошибке инициализации устройства.  
    /// </summary>  
    public static DeviceException InitializeFailed(string name, int chassis, int number, string? reason = null)
    {
      return new($"Ошибка инициализации {name}({chassis}.{number}){Format(reason)}");
    }

    /// <summary>  
    /// Исключение при ошибке сброса устройства.  
    /// </summary>  
    public static DeviceException ResetFailed(string name, int chassis, int number, string? reason = null)
    {
      return new($"Ошибка сброса {name}({chassis}.{number}){Format(reason)}");
    }

    /// <summary>
    /// Исключение при невозможности подключить устройство с определённой ролью.
    /// </summary>
    /// <param name="role">Роль устройства.</param>
    /// <param name="reason">Причина ошибки (опционально).</param>
    public static DeviceException ConnectByRoleFailed(string role, string? reason = null)
    {
      return new($"Не удалось подключить устройство с ролью {role}{Format(reason)}");
    }

    /// <summary>
    /// Исключение: устройство не найдено в конфигурации системы.
    /// </summary>
    /// <param name="deviceName">Имя или тип устройства (например, "мультиметр").</param>
    /// <param name="reason">Дополнительная причина, если известна.</param>
    public static DeviceException NotFoundInConfiguration(string deviceName, string? reason = null) =>
      new($"Устройство \"{deviceName}\" не найдено в конфигурации{Format(reason)}.");

    /// <summary>  
    /// Форматирует дополнительное сообщение.  
    /// </summary>  
    private static string Format(string? reason) =>
        string.IsNullOrWhiteSpace(reason) ? string.Empty : $": {reason}";
  }
}

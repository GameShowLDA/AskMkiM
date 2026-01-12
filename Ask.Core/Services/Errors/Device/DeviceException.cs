namespace Ask.Core.Services.Errors.Device
{
  /// <summary>
  /// Базовое исключение, возникающее при ошибках, связанных с оборудованием.
  /// Используется для всех режимов и компонентов устройств (IR, DCW, PR и др.).
  /// При создании исключения автоматически вызывает глобальное уведомление.
  /// </summary>
  public class DeviceException : Exception
  {
    /// <summary>
    /// Глобальное событие, возникающее при создании исключения устройства.
    /// </summary>
    public static event Action<DeviceException>? OnDeviceExceptionRaised;

    /// <summary>
    /// Создаёт новое исключение устройства с заданным сообщением.
    /// </summary>
    /// <param name="message">Описание ошибки.</param>
    public DeviceException(string message) : base(message)
    {
      Notify(this);
    }

    /// <summary>
    /// Создаёт новое исключение устройства с сообщением и вложенным исключением.
    /// </summary>
    /// <param name="message">Описание ошибки.</param>
    /// <param name="innerException">Внутреннее исключение.</param>
    public DeviceException(string message, Exception innerException)
      : base(message, innerException)
    {
      Notify(this);
    }

    /// <summary>
    /// Уведомляет всех подписчиков о возникновении исключения.
    /// </summary>
    /// <param name="exception">Экземпляр возникшего исключения.</param>
    private static void Notify(DeviceException exception)
    {
      try
      {
        OnDeviceExceptionRaised?.Invoke(exception);
      }
      catch
      {

      }
    }
  }
}

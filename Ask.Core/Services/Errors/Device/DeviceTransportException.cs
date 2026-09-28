namespace Ask.Core.Services.Errors.Device;

/// <summary>
/// Представляет ошибку транспорта оборудования с сохранением исходного исключения.
/// </summary>
public sealed class DeviceTransportException : DeviceException
{
  /// <summary>
  /// Создаёт исключение для неуспешного обмена с устройством.
  /// </summary>
  /// <param name="message">Описание ошибки и устройства.</param>
  /// <param name="innerException">Исходная ошибка транспорта.</param>
  public DeviceTransportException(string message, Exception innerException)
    : base(message, innerException)
  {
  }
}

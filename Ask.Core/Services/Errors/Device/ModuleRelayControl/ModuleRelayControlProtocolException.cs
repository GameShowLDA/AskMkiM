namespace Ask.Core.Services.Errors.Device.ModuleRelayControl;

/// <summary>
/// Представляет системную ошибку команды МКР, возвращённую прошивкой устройства.
/// </summary>
public sealed class ModuleRelayControlProtocolException : DeviceException
{
  /// <summary>
  /// Создаёт исключение для отклонённой прошивкой команды МКР.
  /// </summary>
  /// <param name="deviceDisplayName">Отображаемое имя и адрес МКР.</param>
  /// <param name="operation">Название операции с МКР.</param>
  /// <param name="protocolError">Локализованное описание ошибки протокола.</param>
  /// <param name="firmwareStatus">Исходный статус, возвращённый прошивкой.</param>
  public ModuleRelayControlProtocolException(
    string deviceDisplayName,
    string operation,
    string protocolError,
    string firmwareStatus)
    : base($"{deviceDisplayName}: {operation}. Системная ошибка. {protocolError}")
  {
    DeviceDisplayName = deviceDisplayName;
    Operation = operation;
    ProtocolError = protocolError;
    FirmwareStatus = firmwareStatus;
  }

  /// <summary>
  /// Отображаемое имя и адрес МКР.
  /// </summary>
  public string DeviceDisplayName { get; }

  /// <summary>
  /// Название операции с МКР.
  /// </summary>
  public string Operation { get; }

  /// <summary>
  /// Локализованное описание ошибки протокола.
  /// </summary>
  public string ProtocolError { get; }

  /// <summary>
  /// Исходный статус, возвращённый прошивкой.
  /// </summary>
  public string FirmwareStatus { get; }
}

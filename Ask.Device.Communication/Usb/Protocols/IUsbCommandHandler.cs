using Ask.Core.Shared.Interfaces.DeviceInterfaces;

namespace Ask.Device.Communication.Usb
{
  /// <summary>
  /// Определяет обработчик USB-команд, который интерпретирует команды конкретного устройства поверх общего USB-транспорта.
  /// </summary>
  public interface IUsbCommandHandler
  {
    /// <summary>
    /// Выполняет USB-команду для указанного устройства.
    /// </summary>
    /// <param name="device">Устройство, для которого выполняется команда.</param>
    /// <param name="command">Команда транспорта.</param>
    /// <param name="responseDelay">Задержка перед возвратом ответа в миллисекундах.</param>
    /// <param name="timeout">Пользовательский таймаут операции.</param>
    /// <param name="port">Пользовательский порт операции.</param>
    /// <param name="delayBeforeCall">Задержка перед выполнением команды в миллисекундах.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Строка ответа транспорта.</returns>
    Task<string> ExecuteAsync(
      IDevice device,
      string command,
      double responseDelay = 0,
      int timeout = 0,
      int port = 0,
      int delayBeforeCall = 0,
      CancellationToken cancellationToken = default);
  }
}

using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using Ask.Device.Communication.Common.Threading;

namespace Ask.Device.Communication.Usb.Protocols
{
  /// <summary>
  /// Реализует универсальный транспортный протокол обмена с устройствами по USB.
  /// </summary>
  public class UsbProtocol : IDeviceProtocol
  {
    /// <summary>
    /// Устройство, для которого выполняется обмен.
    /// </summary>
    private readonly IDevice _device;

    /// <summary>
    /// Обработчик USB-команд конкретного транспорта или семейства устройств.
    /// </summary>
    private readonly IUsbCommandHandler _commandHandler;

    /// <summary>
    /// Получает или задаёт семафор, запрещающий параллельное выполнение USB-команд.
    /// </summary>
    public SemaphoreSlim OperationLock { get; set; }

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="UsbProtocol"/>.
    /// </summary>
    /// <param name="device">Устройство, использующее протокол.</param>
    /// <param name="commandHandler">Обработчик USB-команд конкретного транспорта или устройства.</param>
    public UsbProtocol(IDevice device, IUsbCommandHandler commandHandler)
    {
      _device = device ?? throw new ArgumentNullException(nameof(device));
      _commandHandler = commandHandler ?? throw new ArgumentNullException(nameof(commandHandler));
      OperationLock = new SemaphoreSlim(1, 1);
    }

    /// <summary>
    /// Выполняет USB-команду через подключённый обработчик.
    /// </summary>
    /// <param name="command">Команда транспорта.</param>
    /// <param name="responseDelay">Задержка перед возвратом ответа в миллисекундах.</param>
    /// <param name="timeout">Пользовательский таймаут операции.</param>
    /// <param name="port">Пользовательский порт операции.</param>
    /// <param name="delayBeforeCall">Задержка перед выполнением команды в миллисекундах.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Строка ответа транспорта.</returns>
    public async Task<string> QueryAsync(
      string command,
      double responseDelay = 0,
      int timeout = 0,
      int port = 0,
      int delayBeforeCall = 0,
      CancellationToken cancellationToken = default)
    {
      using (await OperationLock.LockAsync(cancellationToken))
      {
        return await _commandHandler.ExecuteAsync(
          _device,
          command,
          responseDelay,
          timeout,
          port,
          delayBeforeCall,
          cancellationToken);
      }
    }
  }
}

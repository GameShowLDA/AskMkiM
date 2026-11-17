using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Interfaces.DeviceInterfaces;

namespace Ask.Core.Services.Errors.Device;

/// <summary>
/// Сообщает об отсутствии ожидаемого ответа устройства.
/// </summary>
public sealed class DeviceNoResponseException : DeviceException
{
  /// <summary>
  /// Создаёт ошибку отсутствия ответа на команду устройства.
  /// </summary>
  /// <param name="device">Устройство, от которого ожидался ответ.</param>
  /// <param name="command">Отправленная команда.</param>
  /// <param name="timeout">Время ожидания ответа в миллисекундах.</param>
  /// <param name="innerException">Исходная ошибка ожидания ответа.</param>
  public DeviceNoResponseException(IDevice device, string command, int timeout, Exception? innerException = null)
    : base($"Нет ответа от {GetDeviceLabel(device)} на команду «{command}» за {timeout} мс.", innerException!)
  {
    DeviceDisplayName = GetDeviceLabel(device);
  }

  /// <summary>
  /// Имя и адрес устройства для строки результата операции.
  /// </summary>
  public string DeviceDisplayName { get; }

  /// <summary>
  /// Название операции, для которой не получен ответ.
  /// </summary>
  public string Operation { get; set; } = "Обмен с устройством";

  /// <summary>
  /// Формирует строку результата операции со статусом отсутствия связи.
  /// </summary>
  /// <returns>Сообщение об отсутствии ответа устройства.</returns>
  public ShowMessageModel ToMessage() => new(DeviceDisplayName, message: Operation,
    type: ShowMessageModel.MessageType.NoResponse) { IsDeviceMessage = true, IndentLevel = 1 };

  private static string GetDeviceLabel(IDevice device) => device is IAttachableDevice attached
    ? $"{device.Name}({attached.NumberChassis}.{attached.Number})"
    : $"{device.Name}(№{device.Number})";
}

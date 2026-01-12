using Ask.Core.Shared.Interfaces.UiInterfaces;

namespace Ask.Core.Shared.Interfaces.DeviceInterfaces.SwitchingDevice.Capabilities
{
  /// <summary>
  /// Управление коммутацией конденсаторов.
  /// </summary>
  public interface ICapacitorDeviceBusCommutation
  {
    /// <summary>
    /// Подключение конденсаторов.
    /// </summary>
    /// <param name="number">Номер конденсатора.</param>
    /// <returns>Возвращает результат подключения.</returns>
    Task<bool> ConnectCapacitor(int number, IUserInteractionService? userMessageService = null);

    /// <summary>
    /// Отключение конденсаторов.
    /// </summary>
    /// <param name="number">Номер конденсатора.</param>
    /// <returns>Возвращает результат отключения.</returns>
    Task<bool> DisconnectCapacitor(int number, IUserInteractionService? userMessageService = null);
  }
}

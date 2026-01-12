using Ask.Core.Shared.Interfaces.UiInterfaces;

namespace Ask.Core.Shared.Interfaces.DeviceInterfaces.SwitchingDevice.Capabilities
{
  /// <summary>
  /// Интерфейс для управления реле в УКШ.
  /// </summary>
  public interface IRelayDeviceBusCommutation
  {
    /// <summary>
    /// Подключает реле с указанным номером.
    /// </summary>
    /// <param name="numberRelay">Номер реле, которое необходимо подключить.</param>
    /// <returns>Возвращает <c>true</c>, если операция выполнена успешно, иначе <c>false</c>.</returns>
    Task<bool> ConnectRelay(int numberRelay, IUserInteractionService? userMessageService = null);

    /// <summary>
    /// Отключает реле с указанным номером.
    /// </summary>
    /// <param name="numberRelay">Номер реле, которое необходимо отключить.</param>
    /// <returns>Возвращает <c>true</c>, если операция выполнена успешно, иначе <c>false</c>.</returns>
    Task<bool> DisconnectRelay(int numberRelay, IUserInteractionService? userMessageService = null);
  }
}

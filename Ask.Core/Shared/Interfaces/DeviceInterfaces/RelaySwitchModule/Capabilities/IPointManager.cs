using Ask.Core.Shared.DTO.Devices.RelaySwitchModule;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;

namespace Ask.Core.Shared.Interfaces.DeviceInterfaces.RelaySwitchModule.Capabilities
{
  /// <summary>
  /// Интерфейс для управления точками (реле) в модуле МКР.
  /// </summary>
  public interface IPointManager
  {
    /// <summary>
    /// Подключает точку (реле) МКР.
    /// </summary>
    /// <param name="bus">Шина подключения.</param>
    /// <param name="number">Номер точки (реле).</param>
    /// <returns>True, если успешно.</returns>
    Task<bool> ConnectRelayAsync(BusPoint bus, int number, IUserInteractionService? userMessageService = null);

    /// <summary>
    /// Отключает точку (реле) МКР.
    /// </summary>
    /// <param name="bus">Шина подключения.</param>
    /// <param name="number">Номер точки (реле).</param>
    /// <returns>True, если успешно.</returns>
    Task<bool> DisconnectRelayAsync(BusPoint bus, int number, IUserInteractionService? userMessageService = null);

    /// <summary>
    /// Подключает диапазон точек МКР.
    /// </summary>
    /// <param name="bus">Подключаемая шина.</param>
    /// <param name="firstPoint">Первая точка в диапазоне.</param>
    /// <param name="lastPoint">Последняя точка в диапазоне.</param>
    /// <returns>True, если успешно.</returns>
    Task<bool> ConnectRelayGroupAsync(BusPoint bus, int firstPoint, int lastPoint, IUserInteractionService? userMessageService = null);

    /// <summary>
    /// Отключает диапазон точек МКР.
    /// </summary>
    /// <param name="bus">Подключаемая шина.</param>
    /// <param name="firstPoint">Первая точка в диапазоне.</param>
    /// <param name="lastPoint">Последняя точка в диапазоне.</param>
    /// <returns>True, если успешно.</returns>
    Task<bool> DisconnectRelayGroupAsync(BusPoint bus, int firstPoint, int lastPoint, IUserInteractionService? userMessageService = null);

    /// <summary>
    /// Проверяет точку на работоспособность у МКР.
    /// </summary>
    /// <param name="numberPoint">Номер точки.</param>
    /// <returns>Ответ от устройства.</returns>
    Task<string> CheckPoint(int numberPoint, IUserInteractionService? userMessageService = null);

    /// <summary>
    /// Переподключение точки к другой шине.
    /// </summary>
    /// <param name="bus">Подключаемая шина.</param>
    /// <param name="nubmerPoint">Номер точки.</param>
    /// <returns>True, если успешно.</returns>
    Task<bool> ConnectingPointToNewBus(BusPoint bus, int nubmerPoint, IUserInteractionService? userMessageService = null);

    /// <summary>
    /// Отключает все точки от шин.
    /// </summary>
    /// <param name="userMessageService">Сервис для отображения сообщений пользователю (необязательный).</param>
    /// <returns>Асинхронная операция завершения отключения всех точек.</returns>
    Task<bool> DisconnectingAllPoint(IUserInteractionService? userMessageService = null);

    /// <summary>
    /// Возвращает список всех точек, которые подключены к шинам.
    /// Возвращаются только точки, имеющие состояние "подключено".
    /// </summary>
    /// <returns>Список подключённых точек с указанием соответствующей шины.</returns>
    IReadOnlyList<PointConnectionInfo> GetConnectedPoints();
  }
}

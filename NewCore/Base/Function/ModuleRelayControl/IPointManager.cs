using static NewCore.Enum.DeviceEnum;

namespace NewCore.Base.Function.ModuleRelayControl
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
    Task<bool> ConnectRelayAsync(BusPoint bus, int number);

    /// <summary>
    /// Отключает точку (реле) МКР.
    /// </summary>
    /// <param name="bus">Шина подключения.</param>
    /// <param name="number">Номер точки (реле).</param>
    /// <returns>True, если успешно.</returns>
    Task<bool> DisconnectRelayAsync(BusPoint bus, int number);

    /// <summary>
    /// Подключает диапазон точек МКР.
    /// </summary>
    /// <param name="bus">Подключаемая шина.</param>
    /// <param name="firstPoint">Первая точка в диапазоне.</param>
    /// <param name="lastPoint">Последняя точка в диапазоне.</param>
    /// <returns>True, если успешно.</returns>
    Task<bool> ConnectRelayGroupAsync(BusPoint bus, int firstPoint, int lastPoint);

    /// <summary>
    /// Отключает диапазон точек МКР.
    /// </summary>
    /// <param name="bus">Подключаемая шина.</param>
    /// <param name="firstPoint">Первая точка в диапазоне.</param>
    /// <param name="lastPoint">Последняя точка в диапазоне.</param>
    /// <returns>True, если успешно.</returns>
    Task<bool> DisconnectRelayGroupAsync(BusPoint bus, int firstPoint, int lastPoint);

    /// <summary>
    /// Проверяет точку на работоспособность у МКР.
    /// </summary>
    /// <param name="numberPoint">Номер точки.</param>
    /// <returns>Ответ от устройства.</returns>
    Task<string> CheckPoint(int numberPoint);
  }
}

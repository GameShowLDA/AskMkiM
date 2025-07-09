using static NewCore.Enum.DeviceEnum;

namespace NewCore.Base.Function.ModuleRelayControl
{
  /// <summary>
  /// Интерфейс для управления коммутацией релейного модуля.
  /// </summary>
  public interface IBusManager
  {
    /// <summary>
    /// Подключает шину МКР.
    /// </summary>
    /// <param name="bus">Замыкаемая шина.</param>
    /// <param name="lowVoltage">true - низковольтная шина, false - высоковольтная.</param>
    /// <returns>Результат замыкания шины.</returns>
    Task<bool> ConnectBusAsync(SwitchingBus bus, bool lowVoltage = true);

    /// <summary>
    /// Отключает шину МКР.
    /// </summary>
    /// <param name="bus">Размыкаемая шина.</param>
    /// <param name="lowVoltage">true - низковольтная шина, false - высоковольтная.</param>
    /// <returns>Результат размыкания шины.</returns>
    Task<bool> DisconnectBusAsync(SwitchingBus bus, bool lowVoltage = true);

    /// <summary>
    /// Пытается получить номер шины на основе значения перечисления SwitchingBus.
    /// </summary>
    /// <param name="bus">Значение перечисления SwitchingBus.</param>
    /// <param name="busNumber">Выходной параметр, содержащий номер шины.</param>
    /// <returns>True, если номер успешно получен; иначе false.</returns>
    bool TryGetBusNumber(SwitchingBus bus, out int busNumber);

    /// <summary>
    /// Пытается преобразовать шину в тип (A, B, AB) на основе значения перечисления SwitchingBus.
    /// </summary>
    /// <param name="bus">Значение перечисления SwitchingBus.</param>
    /// <param name="busType">Выходной параметр, содержащий тип шины (1 - A, 2 - B, 3 - AB).</param>
    /// <returns>True, если тип успешно получен; иначе false.</returns>
    bool TryGetBusType(SwitchingBus bus, out int busType);


  }
}

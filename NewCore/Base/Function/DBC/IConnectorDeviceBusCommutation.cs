using static NewCore.Enum.DeviceEnum;

/// <summary>
/// Интерфейс для управления коммутацией устройств на шинах.
/// </summary>
public interface IConnectorDeviceBusCommutation
{
  /// <summary>
  /// Подключает мультиметр к указанной шине.
  /// </summary>
  /// <param name="bus">Шина, к которой подключается мультиметр.</param>
  /// <returns>Возвращает <c>true</c>, если операция выполнена успешно, иначе <c>false</c>.</returns>
  Task<bool> ConnectMultimeter(SwitchingBusNew bus);

  /// <summary>
  /// Отключает мультиметр от указанной шины.
  /// </summary>
  /// <param name="bus">Шина, от которой отключается мультиметр.</param>
  /// <returns>Возвращает <c>true</c>, если операция выполнена успешно, иначе <c>false</c>.</returns>
  Task<bool> DisconnectMultimeter(SwitchingBusNew bus);

  /// <summary>
  /// Подключает прецизионный источник напряжения и тока (ПИНТ) к указанной шине.
  /// </summary>
  /// <param name="bus">Шина, к которой подключается ПИНТ.</param>
  /// <returns>Возвращает <c>true</c>, если операция выполнена успешно, иначе <c>false</c>.</returns>
  Task<bool> ConnectPINT(SwitchingBusNew bus);

  /// <summary>
  /// Отключает прецизионный источник напряжения и тока (ПИНТ) от указанной шины.
  /// </summary>
  /// <param name="bus">Шина, от которой отключается ПИНТ.</param>
  /// <returns>Возвращает <c>true</c>, если операция выполнена успешно, иначе <c>false</c>.</returns>
  Task<bool> DisconnectPINT(SwitchingBusNew bus);

  /// <summary>
  /// Подключает пробойную установку к системе.
  /// </summary>
  /// <returns>Возвращает <c>true</c>, если операция выполнена успешно, иначе <c>false</c>.</returns>
  Task<bool> ConnectBreakdownTester();

  /// <summary>
  /// Отключает пробойную установку от системы.
  /// </summary>
  /// <returns>Возвращает <c>true</c>, если операция выполнена успешно, иначе <c>false</c>.</returns>
  Task<bool> DisconnectBreakdownTester();

  /// <summary>
  /// Подлючает все шины устрйоства.
  /// </summary>
  /// <returns></returns>
  Task<bool> ConnectAllBuses();

  /// <summary>
  /// Отключает все шины устройства.
  /// </summary>
  /// <returns></returns>
  Task<bool> DisconnectAllBuses();
}

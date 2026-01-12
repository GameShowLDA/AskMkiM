using Ask.Core.Shared.Metadata.Enums.DeviceEnums;

namespace Ask.Core.Shared.Interfaces.DeviceInterfaces.SwitchingDevice.Capabilities
{
  /// <summary>
  /// Интерфейс для выполнения проверки цепочек самоконтроля.
  /// Позволяет реализовать логику для разных устройств УКШ.
  /// </summary>
  public interface ISelfTestChecker
  {
    /// <summary>
    /// Выполняет проверку цепи самоконтроля.
    /// </summary>
    /// <param name="testType">Тип проверки.</param>
    /// <param name="busContact">Выбор шины и контакта.</param>
    /// <param name="action">Действие (1 - замкнуть, 2 - разомкнуть).</param>
    /// <returns><c>true</c>, если команда успешно отправлена, иначе <c>false</c>.</returns>
    Task<bool> ExecuteSelfTestAsync(SwitchingDeviceTypeConnector testType, int busContact, int action);

    /// <summary>
    /// Получает список допустимых контактов для указанного типа теста.
    /// </summary>
    /// <param name="testType">Тип проверки.</param>
    /// <returns>Список номеров контактов или <c>null</c>, если данные отсутствуют.</returns>
    List<int>? GetValidBusContacts(SwitchingDeviceTypeConnector testType);

    /// <summary>
    /// Получает название цепи по её типу.
    /// </summary>
    /// <param name="testType">Тип проверки.</param>
    /// <param name="busContact">Выбор шины и контакта.</param>
    /// <returns>Возвращает название цепочки по её типу.</returns>
    string GetCircuitName(SwitchingDeviceTypeConnector testType, int busContact);

    /// <summary>
    /// Получает количество реле в проверяемой цепи.
    /// </summary>
    /// <param name="testType">Тип проверки.</param>
    /// <param name="busContact">Выбор шины и контакта.</param>
    /// <returns>Количество реле.</returns>
    Task<int> GetRelayCountAsync(SwitchingDeviceTypeConnector testType, int busContact);

    /// <summary>
    /// Управляет реле в цепи самоконтроля.
    /// </summary>
    /// <param name="testType">Тип проверки.</param>
    /// <param name="relayNumber">Номер реле (0 - запросить количество реле).</param>
    /// <param name="busContact">Выбор шины и контакта.</param>
    /// <param name="action">Действие (1 - замкнуть, 2 - разомкнуть).</param>
    /// <returns><c>true</c>, если команда успешно отправлена, иначе <c>false</c>.</returns>
    Task<bool> ControlRelayAsync(SwitchingDeviceTypeConnector testType, int relayNumber, int busContact, int action);
  }
}

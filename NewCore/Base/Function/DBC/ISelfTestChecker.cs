namespace NewCore.Base.Function.DBC
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
    Task<bool> ExecuteSelfTestAsync(SelfTestType testType, int busContact, int action);

    /// <summary>
    /// Получает список допустимых контактов для указанного типа теста.
    /// </summary>
    /// <param name="testType">Тип проверки.</param>
    /// <returns>Список номеров контактов или <c>null</c>, если данные отсутствуют.</returns>
    List<int>? GetValidBusContacts(SelfTestType testType);

    /// <summary>
    /// Получает название цепи по её типу.
    /// </summary>
    string GetCircuitName(SelfTestType testType, int busContact);

    /// <summary>
    /// Получает количество реле в проверяемой цепи.
    /// </summary>
    /// <param name="testType">Тип проверки.</param>
    /// <returns>Количество реле.</returns>
    Task<int> GetRelayCountAsync(SelfTestType testType, int busContact);

    /// <summary>
    /// Управляет реле в цепи самоконтроля.
    /// </summary>
    /// <param name="testType">Тип проверки.</param>
    /// <param name="relayNumber">Номер реле (0 - запросить количество реле).</param>
    /// <param name="busContact">Выбор шины и контакта.</param>
    /// <param name="action">Действие (1 - замкнуть, 2 - разомкнуть).</param>
    /// <returns><c>true</c>, если команда успешно отправлена, иначе <c>false</c>.</returns>
    Task<bool> ControlRelayAsync(SelfTestType testType, int relayNumber, int busContact, int action);
  }
}

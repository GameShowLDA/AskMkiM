using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AppConfiguration.Interface;
using NewCore.Base.Function.DBC;
using NewCore.Base.Interface.Main;
using Utilities;

namespace NewCore.Base.Interface.Additionally
{
  /// <summary>
  /// Интерфейс для выполнения проверки цепочек самоконтроля.
  /// Позволяет реализовать логику для разных устройств УКШ.
  /// </summary>
  public interface ISelfTestCheckerDeviceBusCommutation
  {
    /// <summary>
    /// Запуск самоконтроля устройства коммутации шин для выбранного типа проверки.
    /// </summary>
    /// <param name="messageService">Сервис отображения сообщений.</param>
    /// <param name="selectedType">Выбранное значение перечисления.</param>
    /// <param name="device">Устройство коммутации шин (необязательно).</param>
    /// <param name="meter">Измеритель (необязательно).</param>
    Task StartSelfCheck(CancellationToken cancellationToken, IUserMessageService messageService, System.Enum selectedType, ISwitchingDevice device = null, IFastMeter meter = null);

    /// <summary>
    /// Выполняет проверку цепи самоконтроля.
    /// </summary>
    /// <param name="testType">Тип проверки.</param>
    /// <param name="busContact">Выбор шины и контакта.</param>
    /// <param name="action">Действие (1 - замкнуть, 2 - разомкнуть).</param>
    /// <returns><c>true</c>, если команда успешно отправлена, иначе <c>false</c>.</returns>
    Task<bool> ExecuteSelfTestAsync(CancellationToken cancellationToken, TypeConnector testType, int busContact, int action);

    /// <summary>
    /// Получает список допустимых контактов для указанного типа теста.
    /// </summary>
    /// <param name="testType">Тип проверки.</param>
    /// <returns>Список номеров контактов или <c>null</c>, если данные отсутствуют.</returns>
    List<int>? GetValidBusContacts(TypeConnector testType);

    /// <summary>
    /// Получает название цепи по её типу.
    /// </summary>
    /// <param name="testType">Тип проверки.</param>
    /// <param name="busContact">Выбор шины и контакта.</param>
    /// <returns>Возвращает название цепочки по её типу.</returns>
    string GetCircuitName(TypeConnector testType, int busContact);

    /// <summary>
    /// Получает количество реле в проверяемой цепи.
    /// </summary>
    /// <param name="testType">Тип проверки.</param>
    /// <param name="busContact">Выбор шины и контакта.</param>
    /// <returns>Количество реле.</returns>
    Task<int> GetRelayCountAsync(TypeConnector testType, int busContact);

    /// <summary>
    /// Управляет реле в цепи самоконтроля.
    /// </summary>
    /// <param name="testType">Тип проверки.</param>
    /// <param name="relayNumber">Номер реле (0 - запросить количество реле).</param>
    /// <param name="busContact">Выбор шины и контакта.</param>
    /// <param name="action">Действие (1 - замкнуть, 2 - разомкнуть).</param>
    /// <returns><c>true</c>, если команда успешно отправлена, иначе <c>false</c>.</returns>
    Task<bool> ControlRelayAsync(CancellationToken cancellationToken, TypeConnector testType, int relayNumber, int busContact, int action);

    /// <summary>
    /// Возвращает список поддерживаемых значений перечисления.
    /// </summary>
    IEnumerable<object> GetSupportedTestTypes();

    /// <summary>
    /// Возвращает тип перечисления, используемый как тип проверки.
    /// </summary>
    Type GetTestTypeEnum();
  }
}

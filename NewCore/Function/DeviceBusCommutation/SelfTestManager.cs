using System.Net;
using NewCore.Base.Function.DBC;
using NewCore.Communication;
using static Utilities.LoggerUtility;
using static AppConfiguration.Execution.ExecutionConfig;

namespace NewCore.Function.DeviceBusCommutation
{
  /// <summary>
  /// Класс для управления самоконтроля устройства коммутации шин.
  /// </summary>
  public class SelfTestManager : ISelfTestChecker
  {
    /// <summary>
    /// Устройство коммутации шин.
    /// </summary>
    private readonly Device.DeviceBusCommutation _deviceBusCommutation;

    /// <summary>
    /// Словарь допустимых комбинаций шины и контактов для каждого типа проверки.
    /// </summary>
    public static readonly Dictionary<TypeConnector, List<int>> ValidBusContacts = new()
    {
            { TypeConnector.BlockingRelay, new List<int> { 11, 21 } },
            { TypeConnector.Multimeter, new List<int> { 11,12,13,14,21,22,23,24} },
            { TypeConnector.ADC, new List<int> { 11,12,13,14,21,22,23,24} },
            { TypeConnector.ADCReversed, new List<int> { 11,12,13,14,21,22,23,24} },
            { TypeConnector.PINT, new List<int> { 12, 13, 22, 23 } },
            { TypeConnector.Shunt, new List<int> { 1, 2 } },
            { TypeConnector.BreakdownTester, new List<int> { 11, 21 } },
    };

    /// <summary>
    /// Словарь, содержащий названия цепей для каждого типа проверки.
    /// </summary>
    public static readonly Dictionary<TypeConnector, string> CircuitNames = new()
    {
            { TypeConnector.BlockingRelay, "Блокировочное реле" },
            { TypeConnector.Multimeter, "Мультиметр" },
            { TypeConnector.ADC, "АЦП" },
            { TypeConnector.ADCReversed, "АЦП с переполюсовкой" },
            { TypeConnector.PINT, "ПИНТ" },
            { TypeConnector.Shunt, "Шунт" },
            { TypeConnector.BreakdownTester, "ППУ" },
    };

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="BusManager"/>.
    /// </summary>
    /// <param name="deviceBusCommutation">Экземпляр устройства коммутации шин.</param>
    public SelfTestManager(Device.DeviceBusCommutation deviceBusCommutation) => _deviceBusCommutation = deviceBusCommutation;

    /// <inheritdoc />
    public async Task<bool> ExecuteSelfTestAsync(TypeConnector testType, int busContact, int action)
    {
      if (!ValidateParameters(testType, busContact, action))
      {
        LogError($"Некорректные параметры: Тип проверки - {testType}, Контакт - {busContact}, Действие - {action}.");
        return false;
      }

      if (await GetIsIdleModeEnabled())
      {
        return true;
      }

      DeviceCommand cmd = new DeviceCommand(4, (int)testType, busContact, action);
      LogInformation($"Отправка команды самоконтроля: {cmd}");

      if (!IPAddress.TryParse(_deviceBusCommutation.ConnectionDetails, out IPAddress ipAddress))
      {
        LogError("Некорректный IP-адрес устройства коммутации шин.");
        return false;
      }

      await _deviceBusCommutation.DeviceProtocol.QueryAsync(cmd.ToString());
      return true;
    }

    /// <summary>
    /// Проверяет корректность переданных параметров.
    /// </summary>
    /// <param name="testType">Тип проверки.</param>
    /// <param name="busContact">Выбор шины и контакта.</param>
    /// <param name="action">Действие.</param>
    /// <returns><c>true</c>, если параметры корректны, иначе <c>false</c>.</returns>
    private bool ValidateParameters(TypeConnector testType, int busContact, int action)
    {
      if (!ValidBusContacts.ContainsKey(testType) || action < 1 || action > 2)
      {
        return false;
      }

      return ValidBusContacts[testType].Contains(busContact);
    }

    /// <inheritdoc />
    public List<int>? GetValidBusContacts(TypeConnector testType)
    {
      return ValidBusContacts.TryGetValue(testType, out var contacts) ? contacts : null;
    }

    /// <inheritdoc />
    public string GetCircuitName(TypeConnector testType, int busContact)
    {
      if (CircuitNames.TryGetValue(testType, out string? circuitName))
      {
        return $"{circuitName}, контакт {busContact}";
      }

      return $"Неизвестная цепь, контакт {busContact}";
    }

    /// <inheritdoc />
    public async Task<int> GetRelayCountAsync(TypeConnector testType, int busContact)
    {
      DeviceCommand cmd = new DeviceCommand(41, (int)testType * 10, busContact, 0);
      string response = await _deviceBusCommutation.DeviceProtocol.QueryAsync(cmd.ToString(), timeout: 2000);

      if (int.TryParse(response, out int relayCount))
      {
        LogInformation($"Количество реле в цепи {testType}: {relayCount}");
        return relayCount;
      }

      LogError($"Ошибка получения количества реле для {testType}");
      return -1;
    }

    /// <inheritdoc />
    public async Task<bool> ControlRelayAsync(TypeConnector testType, int relayNumber, int busContact, int action)
    {
      if (relayNumber < 0)
      {
        LogError("Некорректный номер реле.");
        return false;
      }

      DeviceCommand cmd = new DeviceCommand(41, (int)testType * 10 + relayNumber, busContact, action);
      LogInformation($"Управление реле {relayNumber} в цепи {testType}, контакт {busContact}, действие {action} : команда {cmd.ToString()}");

      if (!IPAddress.TryParse(_deviceBusCommutation.ConnectionDetails, out IPAddress ipAddress))
      {
        LogError("Некорректный IP-адрес устройства коммутации шин.");
        return false;
      }

      await _deviceBusCommutation.DeviceProtocol.QueryAsync(cmd.ToString());
      return true;
    }
  }
}

using System.Net;
using NewCore.Base.Function.DBC;
using NewCore.Base.Interface.Main;
using NewCore.Communication;
using static Utilities.LoggerUtility;

namespace NewCore.Function.DeviceBusCommutation
{
  /// <summary>
  /// Класс для проверки цепочек самоконтроля с помощью мультиметра.
  /// </summary>
  public class SelfTestCircuitChecker : ISelfTestChecker
  {
    /// <summary>
    /// Устройство коммутации шин.
    /// </summary>
    private readonly ISwitchingDevice _deviceBusCommutation;

    /// <summary>
    /// Словарь допустимых комбинаций шины и контактов для каждого типа проверки.
    /// </summary>
    public static readonly Dictionary<SelfTestType, List<int>> ValidBusContacts = new()
        {
            { SelfTestType.BlockingRelay, new List<int> { 11, 21 } },
            { SelfTestType.Multimeter, new List<int> { 11,12,13,14,21,22,23,24} },
            { SelfTestType.ADC, new List<int> { 11,12,13,14,21,22,23,24} },
            { SelfTestType.ADCReversed, new List<int> { 11,12,13,14,21,22,23,24} },
            { SelfTestType.PINT, new List<int> { 12, 13, 22, 23 } },
            { SelfTestType.Shunt, new List<int> { 1, 2 } }
        };

    /// <summary>
    /// Словарь, содержащий названия цепей для каждого типа проверки.
    /// </summary>
    public static readonly Dictionary<SelfTestType, string> CircuitNames = new()
        {
            { SelfTestType.BlockingRelay, "Блокировочное реле" },
            { SelfTestType.Multimeter, "Мультиметр" },
            { SelfTestType.ADC, "АЦП" },
            { SelfTestType.ADCReversed, "АЦП с переполюсовкой" },
            { SelfTestType.PINT, "ПИНТ" },
            { SelfTestType.Shunt, "Шунт" }
        };

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="SelfTestCircuitChecker"/>.
    /// </summary>
    /// <param name="deviceBusCommutation">Экземпляр устройства коммутации шин.</param>
    public SelfTestCircuitChecker(ISwitchingDevice deviceBusCommutation)
    {
      _deviceBusCommutation = deviceBusCommutation;
    }

    /// <summary>
    /// Выполняет команду проверки цепочки самоконтроля.
    /// </summary>
    /// <param name="testType">Тип проверки.</param>
    /// <param name="busContact">Выбор шины и контакта.</param>
    /// <param name="action">Действие (1 - замкнуть, 2 - разомкнуть).</param>
    /// <returns><c>true</c>, если команда успешно отправлена, иначе <c>false</c>.</returns>
    public async Task<bool> ExecuteSelfTestAsync(SelfTestType testType, int busContact, int action)
    {
      if (!ValidateParameters(testType, busContact, action))
      {
        LogError($"Некорректные параметры: Тип проверки - {testType}, Контакт - {busContact}, Действие - {action}.");
        return false;
      }

      DeviceCommand command = new DeviceCommand(4, (int)testType, busContact, action);
      LogInformation($"Отправка команды самоконтроля: {command}");

      if (!IPAddress.TryParse(_deviceBusCommutation.ConnectionDetails, out IPAddress ipAddress))
      {
        LogError("Некорректный IP-адрес устройства коммутации шин.");
        return false;
      }

      await DeviceCommandSender.SendCommandAsync(ipAddress, command);
      return true;
    }

    /// <summary>
    /// Проверяет корректность переданных параметров.
    /// </summary>
    /// <param name="testType">Тип проверки.</param>
    /// <param name="busContact">Выбор шины и контакта.</param>
    /// <param name="action">Действие.</param>
    /// <returns><c>true</c>, если параметры корректны, иначе <c>false</c>.</returns>
    private bool ValidateParameters(SelfTestType testType, int busContact, int action)
    {
      if (!ValidBusContacts.ContainsKey(testType) || action < 1 || action > 2)
      {
        return false;
      }

      return ValidBusContacts[testType].Contains(busContact);
    }

    /// <summary>
    /// Получает список допустимых контактов для указанного типа теста.
    /// </summary>
    public List<int>? GetValidBusContacts(SelfTestType testType)
    {
      return ValidBusContacts.TryGetValue(testType, out var contacts) ? contacts : null;
    }

    /// <summary>
    /// Получает название цепи по её типу.
    /// </summary>
    public string GetCircuitName(SelfTestType testType, int busContact)
    {
      if (CircuitNames.TryGetValue(testType, out string? circuitName))
      {
        return $"{circuitName}, контакт {busContact}";
      }
      return $"Неизвестная цепь, контакт {busContact}";
    }

    /// <summary>
    /// Получает количество реле в указанной цепи самоконтроля.
    /// </summary>
    /// <param name="testType">Тип цепи (BlockingRelay, ADC, Multimeter и т. д.).</param>
    /// <returns>Количество реле в цепи, или -1 в случае ошибки.</returns>
    public async Task<int> GetRelayCountAsync(SelfTestType testType, int busContact)
    {
      DeviceCommand command = new DeviceCommand(41, (int)testType * 10, busContact, 0);
      string response = await DeviceCommandSender.SendCommandAsync(IPAddress.Parse(_deviceBusCommutation.ConnectionDetails), command, 2000);

      if (int.TryParse(response, out int relayCount))
      {
        LogInformation($"Количество реле в цепи {testType}: {relayCount}");
        return relayCount;
      }

      LogError($"Ошибка получения количества реле для {testType}");
      return -1;
    }

    /// <summary>
    /// Управляет реле в указанной цепи самоконтроля (включает или выключает реле).
    /// </summary>
    /// <param name="testType">Тип цепи (BlockingRelay, ADC, Multimeter и т. д.).</param>
    /// <param name="relayNumber">Номер реле (0 для запроса количества реле, 1+ для управления).</param>
    /// <param name="busContact">Контакт шины, на котором выполняется проверка.</param>
    /// <param name="action">Действие: 1 - замкнуть, 2 - разомкнуть.</param>
    /// <returns>True, если команда успешно отправлена, иначе false.</returns>
    public async Task<bool> ControlRelayAsync(SelfTestType testType, int relayNumber, int busContact, int action)
    {
      if (relayNumber < 0)
      {
        LogError("Некорректный номер реле.");
        return false;
      }

      DeviceCommand command = new DeviceCommand(41, (int)testType * 10 + relayNumber, busContact, action);
      LogInformation($"Управление реле {relayNumber} в цепи {testType}, контакт {busContact}, действие {action} : команда {command.ToString()}");

      if (!IPAddress.TryParse(_deviceBusCommutation.ConnectionDetails, out IPAddress ipAddress))
      {
        LogError("Некорректный IP-адрес устройства коммутации шин.");
        return false;
      }

      await DeviceCommandSender.SendCommandAsync(ipAddress, command);
      return true;
    }

  }
}

using System.Net;
using AppConfiguration.Interface;
using NewCore.Base.Function.DBC;
using NewCore.Base.Interface.Additionally;
using NewCore.Base.Interface.Main;
using NewCore.Communication;
using Utilities.Models;
using static AppConfiguration.Execution.ExecutionConfig;
using static Utilities.LoggerUtility;

namespace NewCore.Function.DeviceBusCommutation.SelfCheck
{
  public class SelfTestManager : ISelfTestCheckerDeviceBusCommutation
  {
    private static bool meterConnect = false;
    private static bool dbcConnect = false;

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
        LogError($"Некорректные параметры: Тип проверки - {testType}, Контакт - {busContact}, Действие - {action}.", isDeviceLog: true);
        return false;
      }

      if (await GetIsIdleModeEnabled())
      {
        return true;
      }

      DeviceCommand cmd = new DeviceCommand(4, (int)testType, busContact, action);
      LogInformation($"Отправка команды самоконтроля: {cmd}", isDeviceLog: true);

      if (!IPAddress.TryParse(_deviceBusCommutation.ConnectionDetails, out IPAddress ipAddress))
      {
        LogError("Некорректный IP-адрес устройства коммутации шин.", isDeviceLog: true);
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
      if (await GetIsIdleModeEnabled())
      {
        return 0;
      }

      DeviceCommand cmd = new DeviceCommand(41, (int)testType * 10, busContact, 0);
      string response = await _deviceBusCommutation.DeviceProtocol.QueryAsync(cmd.ToString(), timeout: 2000);

      if (int.TryParse(response, out int relayCount))
      {
        LogInformation($"Количество реле в цепи {testType}: {relayCount}", isDeviceLog: true);
        return relayCount;
      }

      LogError($"Ошибка получения количества реле для {testType}", isDeviceLog: true);
      return -1;
    }

    /// <inheritdoc />
    public async Task<bool> ControlRelayAsync(TypeConnector testType, int relayNumber, int busContact, int action)
    {
      if (relayNumber < 0)
      {
        LogError("Некорректный номер реле.", isDeviceLog: true);
        return false;
      }

      DeviceCommand cmd = new DeviceCommand(41, (int)testType * 10 + relayNumber, busContact, action);
      LogInformation($"Управление реле {relayNumber} в цепи {testType}, контакт {busContact}, действие {action} : команда {cmd.ToString()}", isDeviceLog: true);

      if (!IPAddress.TryParse(_deviceBusCommutation.ConnectionDetails, out IPAddress ipAddress))
      {
        LogError("Некорректный IP-адрес устройства коммутации шин.", isDeviceLog: true);
        return false;
      }

      if (await GetIsIdleModeEnabled())
      {
        return true;
      }

      await _deviceBusCommutation.DeviceProtocol.QueryAsync(cmd.ToString());
      return true;
    }

    /// <summary>
    /// Запуск самоконтроля устройства.
    /// </summary>
    /// <param name="userMessageService">Элемент управления для вывода информации.</param>
    /// <returns></returns>l
    /// <exception cref="NotImplementedException"></exception>
    public async Task StartSelfCheck(IUserMessageService messageService, System.Enum selectedType, ISwitchingDevice device = null, IFastMeter meter = null)
    {
      if (selectedType is not TypeConnector type)
      {
        await messageService.ShowMessageAsync(new ShowMessageModel(
          "Ошибка",
          message: "Неверный тип проверки: требуется TypeConnector",
          type: ShowMessageModel.MessageType.Error));
        return;
      }

      await SettingsMeter(meter);

      switch (type)
      {
        case TypeConnector.FullCheck:
          await RunSelfCheckBlockingRelayAsync(messageService, device, meter);
          await RunSelfCheckMultimeterAsync(messageService, device, meter);
          await RunSelfCheckAdcAsync(messageService, device, meter);
          await RunSelfCheckAdcReversedAsync(messageService, device, meter);
          await RunSelfCheckPintAsync(messageService, device, meter);
          await RunSelfCheckShuntAsync(messageService, device, meter);
          await RunSelfCheckBreakdownTesterAsync(messageService, device, meter);
          break;

        case TypeConnector.BlockingRelay:
          await RunSelfCheckBlockingRelayAsync(messageService, device, meter);
          break;

        case TypeConnector.Multimeter:
          await RunSelfCheckMultimeterAsync(messageService, device, meter);
          break;

        case TypeConnector.ADC:
          await RunSelfCheckAdcAsync(messageService, device, meter);
          break;

        case TypeConnector.ADCReversed:
          await RunSelfCheckAdcReversedAsync(messageService, device, meter);
          break;

        case TypeConnector.PINT:
          await RunSelfCheckPintAsync(messageService, device, meter);
          break;

        case TypeConnector.Shunt:
          await RunSelfCheckShuntAsync(messageService, device, meter);
          break;

        case TypeConnector.BreakdownTester:
          await RunSelfCheckBreakdownTesterAsync(messageService, device, meter);
          break;

        default:
          await messageService.ShowMessageAsync(new ShowMessageModel(
            "Ошибка",
            message: $"Тип проверки {type} не распознан.",
            type: ShowMessageModel.MessageType.Error));
          break;

      }

      await meter.ConnectableManager.DisconnectAsync();
    }

    /// <summary>
    /// Выполняет самопроверку цепи блокирующего реле.
    /// </summary>
    private async Task RunSelfCheckBlockingRelayAsync(IUserMessageService messageService, ISwitchingDevice device = null, IFastMeter meter = null)
    {
      await SelfCheckCircuitAsync(TypeConnector.BlockingRelay, messageService, device, meter);
    }

    /// <summary>
    /// Выполняет самопроверку цепи мультиметра.
    /// </summary>
    private async Task RunSelfCheckMultimeterAsync(IUserMessageService messageService, ISwitchingDevice device = null, IFastMeter meter = null)
    {
      await SelfCheckCircuitAsync(TypeConnector.Multimeter, messageService, device, meter);
    }

    /// <summary>
    /// Выполняет самопроверку цепи АЦП.
    /// </summary>
    private async Task RunSelfCheckAdcAsync(IUserMessageService messageService, ISwitchingDevice device = null, IFastMeter meter = null)
    {
      await SelfCheckCircuitAsync(TypeConnector.ADC, messageService, device, meter);
    }

    /// <summary>
    /// Выполняет самопроверку цепи АЦП в инверсной конфигурации.
    /// </summary>
    private async Task RunSelfCheckAdcReversedAsync(IUserMessageService messageService, ISwitchingDevice device = null, IFastMeter meter = null)
    {
      await SelfCheckCircuitAsync(TypeConnector.ADCReversed, messageService, device, meter);
    }

    /// <summary>
    /// Выполняет самопроверку цепи программируемого источника тока и напряжения (ПИНТ).
    /// </summary>
    private async Task RunSelfCheckPintAsync(IUserMessageService messageService, ISwitchingDevice device = null, IFastMeter meter = null)
    {
      await SelfCheckCircuitAsync(TypeConnector.PINT, messageService, device, meter);
    }

    /// <summary>
    /// Выполняет самопроверку цепи с шунтом.
    /// </summary>
    private async Task RunSelfCheckShuntAsync(IUserMessageService messageService, ISwitchingDevice device = null, IFastMeter meter = null)
    {
      await SelfCheckCircuitAsync(TypeConnector.Shunt, messageService, device, meter);
    }

    /// <summary>
    /// Выполняет самопроверку цепи пробойной установки (ПКИ).
    /// </summary>
    private async Task RunSelfCheckBreakdownTesterAsync(IUserMessageService messageService, ISwitchingDevice device = null, IFastMeter meter = null)
    {
      await SelfCheckCircuitAsync(TypeConnector.BreakdownTester, messageService, device, meter);
    }

    /// <summary>
    /// Выполняет самоконтроль указанной цепи, включая проверку главных реле на каждой шине.
    /// </summary>
    /// <param name="testType">Тип цепи для проверки.</param>
    /// <returns>True, если проверка успешна, иначе false.</returns>
    private static async Task<bool> SelfCheckCircuitAsync(TypeConnector testType, IUserMessageService messageService, ISwitchingDevice device = null, IFastMeter meter = null)
    {

      if (!meterConnect && !dbcConnect)
      {
        if (!await CheckConnectionsAsync(device, meter))
        {
          return false;
        }
      }
      await device.ConnectableManager.ResetAsync();


      var selfTestChecker = device.SelfTestManager;

      if (selfTestChecker == null)
      {
        await messageService.ShowMessageAsync(new ShowMessageModel("Ошибка", message: "Устройство не поддерживает самоконтроль.", type: ShowMessageModel.MessageType.Error));
        LogError("Ошибка: Устройство не поддерживает самоконтроль.", isDeviceLog: true);
        return false;
      }

      var contacts = selfTestChecker.GetValidBusContacts(testType);
      if (contacts == null || contacts.Count == 0)
      {
        LogError($"Ошибка: Не удалось получить список контактов для {testType}.", isDeviceLog: true);
        return false;
      }

      bool allTestsPassed = true;

      foreach (int busContact in contacts)
      {

        Console.WriteLine();

        string circuitName = selfTestChecker.GetCircuitName(testType, busContact);

        if (!await PerformCircuitTestAsync(messageService, selfTestChecker, meter, testType, circuitName, busContact))
        {
          await messageService.ShowMessageAsync(new ShowMessageModel($"{circuitName}", type: ShowMessageModel.MessageType.Error) { IndentLevel = 3 });
          LogError($"Проверка {circuitName} завершилась с ошибкой!", isDeviceLog: true);
          allTestsPassed = false;
          continue;
        }
      }

      if (allTestsPassed)
      {
        LogDebug($"Самоконтроль {testType} завершен успешно.", isDeviceLog: true);
        return true;
      }
      else
      {
        LogError($"Самоконтроль {testType} завершен с ошибками.", isDeviceLog: true);
        return false;
      }
    }

    /// <summary>
    /// Выполняет проверку указанной цепи: замыкает, проверяет целостность цепи и размыкает.
    /// </summary>
    /// <param name="selfTestChecker">Объект для тестирования.</param>
    /// <param name="meter">Измерительный прибор.</param>
    /// <param name="testType">Тип цепи (BlockingRelay, ADC, Multimeter и т. д.).</param>
    /// <param name="circuitName">Название цепи.</param>
    /// <param name="busContact">Контакт шины.</param>
    /// <returns>True, если тест пройден успешно, иначе false.</returns>
    private static async Task<bool> PerformCircuitTestAsync(IUserMessageService messageService, ISelfTestCheckerDeviceBusCommutation selfTestChecker, IFastMeter meter, TypeConnector testType, string circuitName, int busContact)
    {
      await messageService.ShowMessageAsync(new ShowMessageModel($"Запуск теста {circuitName}"), true);

      if (!await selfTestChecker.ExecuteSelfTestAsync(testType, busContact, 1))
      {
        await messageService.ShowMessageAsync(new ShowMessageModel($"Ошибка при замыкании: {circuitName}.", type: ShowMessageModel.MessageType.Error));
        return false;
      }

      await messageService.ShowMessageAsync(new ShowMessageModel($"Проверка целостности цепи {circuitName}...") { IndentLevel = 1 });

      // Выполняем проверку цепи (если прибор поддерживает тест целостности)
      await Task.Delay(25);
      bool continuityResult = false;

      if (meter.ContinuityManager != null)
      {
        continuityResult = await meter.ContinuityManager.CheckContinuityAsync();
        if (continuityResult)
        {
          await messageService.ShowMessageAsync(new ShowMessageModel($"Цепь {circuitName}", type: ShowMessageModel.MessageType.Success) { IndentLevel = 3 });

          if (!await PerformRelayCheck(messageService, selfTestChecker, testType, circuitName, busContact, meter))
          {
            await messageService.ShowMessageAsync(new ShowMessageModel($"Реле цепи {circuitName}", type: ShowMessageModel.MessageType.Error));
          }
        }
        else
        {
          await messageService.ShowMessageAsync(new ShowMessageModel($"Цепь {circuitName}", type: ShowMessageModel.MessageType.Error) { IndentLevel = 3 });
        }
      }
      else
      {
        await messageService.ShowMessageAsync(new ShowMessageModel($"Прибор не поддерживает самоконтроль для {circuitName}. Пропуск теста."));
      }

      // Размыкаем цепь
      if (!await selfTestChecker.ExecuteSelfTestAsync(testType, busContact, 2))
      {
        await messageService.ShowMessageAsync(new ShowMessageModel($"Размыкание цепи {circuitName}", type: ShowMessageModel.MessageType.Error));
        return false;
      }

      await messageService.ShowMessageAsync(new ShowMessageModel($"Цепь {circuitName} успешно разомкнута.", type: ShowMessageModel.MessageType.Success));
      return continuityResult;
    }


    /// <summary>
    /// Проверяет главные реле в цепи самоконтроля для указанного типа проверки.
    /// </summary>
    /// <param name="selfTestChecker">Объект для тестирования.</param>
    /// <param name="testType">Тип тестируемой цепи (например, BlockingRelay, ADC, Multimeter).</param>
    /// <param name="circuitName">Название цепи.</param>
    /// <param name="busContact">Контакт шины.</param>
    /// <returns>True, если все реле прошли проверку, иначе false.</returns>
    private static async Task<bool> PerformRelayCheck(IUserMessageService messageService, ISelfTestCheckerDeviceBusCommutation selfTestChecker, TypeConnector testType, string circuitName, int busContact, IFastMeter meter)
    {
      int relayCount = await selfTestChecker.GetRelayCountAsync(testType, busContact);
      if (relayCount < 0)
      {
        await messageService.ShowMessageAsync(new ShowMessageModel($"Ошибка", message: $"Невозможно получить количество реле для {circuitName}.", type: ShowMessageModel.MessageType.Error));
        return false;
      }

      LogInformation($"Обнаружено {relayCount} реле в цепи {circuitName}.", isDeviceLog: true);
      for (int relay = 1; relay <= relayCount; relay++)
      {
        await messageService.ShowMessageAsync(new ShowMessageModel($"Проверка реле {relay} в цепи {circuitName}") { IndentLevel = 1 });

        await Task.Delay(1);
        if (!await selfTestChecker.ControlRelayAsync(testType, relay, busContact, 2))
        {
          await messageService.ShowMessageAsync(new ShowMessageModel($"Включении реле {relay} в цепи {circuitName}", type: ShowMessageModel.MessageType.Error));
          return false;
        }

        LogInformation($"Реле {relay} выключено, проверяем целостность цепи...", isDeviceLog: true);

        await Task.Delay(25);
        var result = await meter.ContinuityManager.CheckContinuityAsync();
        if (!result)
        {
          await messageService.ShowMessageAsync(new ShowMessageModel($"Реле {relay}", type: ShowMessageModel.MessageType.Success) { IndentLevel = 3 });
        }
        else
        {
          await messageService.ShowMessageAsync(new ShowMessageModel($"Реле {relay}", type: ShowMessageModel.MessageType.Error) { IndentLevel = 3 });
        }

        await Task.Delay(1);
        if (!await selfTestChecker.ControlRelayAsync(testType, relay, busContact, 1))
        {
          LogError($"Ошибка при выключении реле {relay} в цепи {circuitName}.", isDeviceLog: true);
          return false;
        }

      }

      return true;
    }


    private static async Task SettingsMeter(IFastMeter meter)
    {
      await meter.ConnectableManager.ConnectAsync();
      await meter.ContinuityManager.SetContinuityModeAsync();
    }
    private static async Task<bool> CheckConnectionsAsync(ISwitchingDevice device, IFastMeter meter)
    {
      var result1 = await device.ConnectableManager.InitializeAsync();
      var result2 = await meter.ConnectableManager.InitializeAsync();

      if (result1.Connect && result2.Connect)
      {
        meterConnect = true;
        dbcConnect = true;
        return true;
      }
      return false;
    }

    public IEnumerable<object> GetSupportedTestTypes()
    {
      return ValidBusContacts.Keys.Cast<object>();
    }

    public Type GetTestTypeEnum()
    {
      return typeof(TypeConnector);
    }
  }
}

using System.Net;
using AppConfiguration.Interface;
using NewCore.Base.Function.DBC;
using NewCore.Base.Interface.Additionally;
using NewCore.Base.Interface.Main;
using NewCore.Communication;
using NewCore.Device;
using Utilities.Models;
using static AppConfiguration.Execution.ExecutionConfig;
using static Utilities.LoggerUtility;


namespace NewCore.Function.DeviceBusCommutation.SelfCheck
{
  /// <summary>
  /// Управляет выполнением полного процесса самотестирования устройства коммутации шин,
  /// включая запуск, проверку цепей, тестирование реле и вывод результатов.
  /// </summary>
  internal static class SelfTestProcessManager
  {
    /// <summary>
    /// Запуск самоконтроля устройства.
    /// </summary>
    /// <param name="userMessageService">Элемент управления для вывода информации.</param>
    /// <returns></returns>l
    /// <exception cref="NotImplementedException"></exception>
    static public async Task StartSelfCheck(IUserMessageService messageService, System.Enum selectedType, ISwitchingDevice device = null, IFastMeter meter = null)
    {
      if (selectedType is not TypeConnector type)
      {
        await messageService.ShowMessageAsync(new ShowMessageModel(
          "Ошибка",
          message: "Неверный тип проверки: требуется TypeConnector",
          type: ShowMessageModel.MessageType.Error));
        return;
      }

      await SelfTestConnectionHelper.SettingsMeter(meter);

      switch (type)
      {
        case TypeConnector.FullCheck:
          await SelfTestRunner.RunSelfCheckBlockingRelayAsync(messageService, device, meter);
          await SelfTestRunner.RunSelfCheckMultimeterAsync(messageService, device, meter);
          await SelfTestRunner.RunSelfCheckAdcAsync(messageService, device, meter);
          await SelfTestRunner.RunSelfCheckAdcReversedAsync(messageService, device, meter);
          await SelfTestRunner.RunSelfCheckPintAsync(messageService, device, meter);
          await SelfTestRunner.RunSelfCheckShuntAsync(messageService, device, meter);
          await SelfTestRunner.RunSelfCheckBreakdownTesterAsync(messageService, device, meter);
          break;

        case TypeConnector.BlockingRelay:
          await SelfTestRunner.RunSelfCheckBlockingRelayAsync(messageService, device, meter);
          break;

        case TypeConnector.Multimeter:
          await SelfTestRunner.RunSelfCheckMultimeterAsync(messageService, device, meter);
          break;

        case TypeConnector.ADC:
          await SelfTestRunner.RunSelfCheckAdcAsync(messageService, device, meter);
          break;

        case TypeConnector.ADCReversed:
          await SelfTestRunner.RunSelfCheckAdcReversedAsync(messageService, device, meter);
          break;

        case TypeConnector.PINT:
          await SelfTestRunner.RunSelfCheckPintAsync(messageService, device, meter);
          break;

        case TypeConnector.Shunt:
          await SelfTestRunner.RunSelfCheckShuntAsync(messageService, device, meter);
          break;

        case TypeConnector.BreakdownTester:
          await SelfTestRunner.RunSelfCheckBreakdownTesterAsync(messageService, device, meter);
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
    /// Выполняет самоконтроль указанной цепи, включая проверку главных реле на каждой шине.
    /// </summary>
    /// <param name="testType">Тип цепи для проверки.</param>
    /// <returns>True, если проверка успешна, иначе false.</returns>
    internal static async Task<bool> SelfCheckCircuitAsync(TypeConnector testType, IUserMessageService messageService, ISwitchingDevice device = null, IFastMeter meter = null)
    {
      if (!SelfTestManager.MeterConnect && !SelfTestManager.DbcConnect)
      {
        if (!await SelfTestConnectionHelper.CheckConnectionsAsync(device, meter))
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

      if (!await SelfTestRetryHelper.TryCloseCircuitWithRetryAsync(messageService, selfTestChecker, testType, busContact, circuitName))
      {
        return false;
      }

      await messageService.ShowMessageAsync(new ShowMessageModel($"Проверка целостности цепи {circuitName}...") { IndentLevel = 1 });

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

    /// <inheritdoc />
    static public async Task<bool> ControlRelayAsync(Device.DeviceBusCommutation _deviceBusCommutation, TypeConnector testType, int relayNumber, int busContact, int action)
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

    static public async Task<bool> ExecuteSelfTestAsync(Device.DeviceBusCommutation _deviceBusCommutation,  TypeConnector testType, int busContact, int action)
    {
      if (!SelfTestManager.ValidateParameters(testType, busContact, action))
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
  }
}

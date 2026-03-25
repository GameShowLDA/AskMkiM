using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.UI;
using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.SwitchingDevice;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.SwitchingDevice.Capabilities;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Core.Shared.Metadata.Static.Messages;
using Ask.Device.Runtime.Commands;
using System.Net;
using static Ask.LogLib.LoggerUtility;


namespace Ask.Device.Runtime.Function.DeviceBusCommutation.SelfCheck
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
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    static public async Task StartSelfCheck(CancellationToken cancellationToken, IUserInteractionService messageService, System.Enum selectedType, ISwitchingDevice device = null, IFastMeter meter = null)
    {
      SelfTestManager.MeterConnect = false;
      SelfTestManager.DbcConnect = false;

      if (selectedType is not SwitchingDeviceTypeConnector type)
      {
        await messageService.ShowMessageAsync(new ShowMessageModel(
          "Ошибка",
          message: "Неверный тип проверки: требуется TypeConnector",
          type: ShowMessageModel.MessageType.Error));
        return;
      }

      await messageService.ShowMessageAsync(ExecutorMessageBuilder.BuildDeviceHealthCheckTitle(device));
      await messageService.ShowMessageAsync(new ShowMessageModel("Настройка оборудования"));

      if (!await SelfTestConnectionHelper.SettingsMeter(meter, messageService))
      {
        return;
      }

      switch (type)
      {
        case SwitchingDeviceTypeConnector.FullCheck:
          await SelfTestRunner.RunSelfCheckBlockingRelayAsync(cancellationToken, messageService, device, meter);
          await SelfTestRunner.RunSelfCheckMultimeterAsync(cancellationToken, messageService, device, meter);
          await SelfTestRunner.RunSelfCheckAdcAsync(cancellationToken, messageService, device, meter);
          await SelfTestRunner.RunSelfCheckAdcReversedAsync(cancellationToken, messageService, device, meter);
          await SelfTestRunner.RunSelfCheckPintAsync(cancellationToken, messageService, device, meter);
          await SelfTestRunner.RunSelfCheckShuntAsync(cancellationToken, messageService, device, meter);
          await SelfTestRunner.RunSelfCheckBreakdownTesterAsync(cancellationToken, messageService, device, meter);
          break;

        case SwitchingDeviceTypeConnector.BlockingRelay:
          await SelfTestRunner.RunSelfCheckBlockingRelayAsync(cancellationToken, messageService, device, meter);
          break;

        case SwitchingDeviceTypeConnector.Multimeter:
          await SelfTestRunner.RunSelfCheckMultimeterAsync(cancellationToken, messageService, device, meter);
          break;

        case SwitchingDeviceTypeConnector.ADC:
          await SelfTestRunner.RunSelfCheckAdcAsync(cancellationToken, messageService, device, meter);
          break;

        case SwitchingDeviceTypeConnector.ADCReversed:
          await SelfTestRunner.RunSelfCheckAdcReversedAsync(cancellationToken, messageService, device, meter);
          break;

        case SwitchingDeviceTypeConnector.PINT:
          await SelfTestRunner.RunSelfCheckPintAsync(cancellationToken, messageService, device, meter);
          break;

        case SwitchingDeviceTypeConnector.Shunt:
          await SelfTestRunner.RunSelfCheckShuntAsync(cancellationToken, messageService, device, meter);
          break;

        case SwitchingDeviceTypeConnector.BreakdownTester:
          await SelfTestRunner.RunSelfCheckBreakdownTesterAsync(cancellationToken, messageService, device, meter);
          break;

        default:
          await messageService.ShowMessageAsync(new ShowMessageModel(
            "Ошибка",
            message: $"Тип проверки {type} не распознан.",
            type: ShowMessageModel.MessageType.Error));
          break;

      }

      await meter.ConnectableManager.DisconnectAsync(messageService);
    }


    /// <summary>
    /// Выполняет самоконтроль указанной цепи, включая проверку главных реле на каждой шине.
    /// </summary>
    /// <param name="testType">Тип цепи для проверки.</param>
    /// <returns>True, если проверка успешна, иначе false.</returns>
    internal static async Task<bool> SelfCheckCircuitAsync(CancellationToken cancellationToken, SwitchingDeviceTypeConnector testType, IUserInteractionService messageService, ISwitchingDevice device = null, IFastMeter meter = null)
    {
      if (!SelfTestManager.MeterConnect && !SelfTestManager.DbcConnect)
      {
        if (!await SelfTestConnectionHelper.CheckConnectionsAsync(device, meter, messageService))
        {
          return false;
        }
      }
      await device.ConnectableManager.ResetAsync(messageService);

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

        cancellationToken.ThrowIfCancellationRequested();

        string circuitName = selfTestChecker.GetCircuitName(testType, busContact);

        if (!await PerformCircuitTestAsync(cancellationToken, messageService, selfTestChecker, meter, testType, circuitName, busContact))
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
    private static async Task<bool> PerformCircuitTestAsync(CancellationToken cancellationToken, IUserInteractionService messageService, ISelfTestCheckerDeviceBusCommutation selfTestChecker, IFastMeter meter, SwitchingDeviceTypeConnector testType, string circuitName, int busContact)
    {
      await messageService.ShowMessageAsync(new ShowMessageModel($"Запуск теста {circuitName}"), true);

      if (!await UserActionHelper.GetRunWithUserRepeatAsync(() => SelfTestRetryHelper.TryCloseCircuitWithRetryAsync(cancellationToken, messageService, selfTestChecker, testType, busContact, circuitName), messageService))
      {
        return false;
      }

      await messageService.ShowMessageAsync(new ShowMessageModel($"Проверка целостности цепи {circuitName}...") { IndentLevel = 1 });

      bool continuityResult = false;

      if (meter.ContinuityManager != null)
      {
        await UserActionHelper.RunWithUserRepeatAsync(async () =>
        {
          continuityResult = await meter.ContinuityManager.CheckContinuityAsync(true, messageService);
          if (continuityResult)
          {
            await messageService.ShowMessageAsync(new ShowMessageModel($"\"{circuitName}\"", type: ShowMessageModel.MessageType.Success) { IndentLevel = 3 });
            await PerformRelayCheck(cancellationToken, messageService, selfTestChecker, testType, circuitName, busContact, meter);
            return true;
          }
          else
          {
            await messageService.ShowMessageAsync(new ShowMessageModel($"\"{circuitName}\"", type: ShowMessageModel.MessageType.Error) { IndentLevel = 3 }, skipPause: true);
            return false;
          }
        }, messageService);
      }
      else
      {
        await messageService.ShowMessageAsync(new ShowMessageModel($"Прибор не поддерживает самоконтроль для {circuitName}. Пропуск теста."));
      }

      if (!await selfTestChecker.ExecuteSelfTestAsync(cancellationToken, testType, busContact, 2))
      {
        await messageService.ShowMessageAsync(new ShowMessageModel($"Размыкание цепи {circuitName}", type: ShowMessageModel.MessageType.Error));
        return false;
      }

      await messageService.ShowMessageAsync(new ShowMessageModel($"\"{circuitName}\" отключен.", type: ShowMessageModel.MessageType.Success) { IndentLevel = 2 });
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
    private static async Task<bool> PerformRelayCheck(CancellationToken cancellationToken, IUserInteractionService messageService, ISelfTestCheckerDeviceBusCommutation selfTestChecker, SwitchingDeviceTypeConnector testType, string circuitName, int busContact, IFastMeter meter)
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
        cancellationToken.ThrowIfCancellationRequested();
        await messageService.ShowMessageAsync(new ShowMessageModel($"Проверка реле {relay} в цепи {circuitName}") { IndentLevel = 1 });

        if (!await selfTestChecker.ControlRelayAsync(cancellationToken, testType, relay, busContact, 2))
        {
          await messageService.ShowMessageAsync(new ShowMessageModel($"Включении реле {relay} в цепи {circuitName}", type: ShowMessageModel.MessageType.Error));
          return false;
        }

        LogInformation($"Реле {relay} выключено, проверяем целостность цепи...", isDeviceLog: true);

        if (!await UserActionHelper.GetRunWithUserRepeatAsync(() => SelfTestRetryHelper.CheckRelayStateAsync(cancellationToken, messageService, meter, relay), messageService))
        {
          return false;
        }

        if (!await selfTestChecker.ControlRelayAsync(cancellationToken, testType, relay, busContact, 1))
        {
          LogError($"Ошибка при выключении реле {relay} в цепи {circuitName}.", isDeviceLog: true);
          return false;
        }

      }

      return true;
    }

    /// <inheritdoc />
    static public async Task<bool> ControlRelayAsync(CancellationToken cancellationToken, Device.DeviceBusCommutation _deviceBusCommutation, SwitchingDeviceTypeConnector testType, int relayNumber, int busContact, int action)
    {
      if (relayNumber < 0)
      {
        LogError("Некорректный номер реле.", isDeviceLog: true);
        return false;
      }

      DeviceCommand cmd = new DeviceCommand(41, ((int)testType * 10) + relayNumber, busContact, action);
      LogInformation($"Управление реле {relayNumber} в цепи {testType}, контакт {busContact}, действие {action} : команда {cmd.ToString()}", isDeviceLog: true);

      if (!IPAddress.TryParse(_deviceBusCommutation.ConnectionDetails, out IPAddress ipAddress))
      {
        LogError("Некорректный IP-адрес устройства коммутации шин.", isDeviceLog: true);
        return false;
      }

      if (ExecutionConfig.GetIsIdleModeEnabled())
      {
        return true;
      }

      // TODO : Получить и обработать ответ
      await _deviceBusCommutation.DeviceProtocol.QueryAsync(cmd.ToString(), timeout: 1000);
      return true;
    }

    static public async Task<bool> ExecuteSelfTestAsync(CancellationToken cancellationToken, Device.DeviceBusCommutation _deviceBusCommutation, SwitchingDeviceTypeConnector testType, int busContact, int action)
    {
      if (!SelfTestManager.ValidateParameters(testType, busContact, action))
      {
        LogError($"Некорректные параметры: Тип проверки - {testType}, Контакт - {busContact}, Действие - {action}.", isDeviceLog: true);
        return false;
      }

      if (ExecutionConfig.GetIsIdleModeEnabled())
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

      await _deviceBusCommutation.DeviceProtocol.QueryAsync(cmd.ToString(), timeout: 1000);
      return true;
    }
  }
}

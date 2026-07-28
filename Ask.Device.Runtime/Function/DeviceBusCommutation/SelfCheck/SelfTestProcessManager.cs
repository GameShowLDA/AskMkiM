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
    /// Запускает самоконтроль выбранных цепей устройства коммутации шин.
    /// </summary>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <param name="messageService">Сервис взаимодействия с пользователем.</param>
    /// <param name="selectedType">Тип выполняемой проверки.</param>
    /// <param name="device">Проверяемое устройство коммутации шин.</param>
    /// <param name="meter">Мультиметр для проверки целостности цепей.</param>
    static public async Task StartSelfCheck(CancellationToken cancellationToken, IUserInteractionService messageService, System.Enum selectedType, ISwitchingDevice device = null, IMultimeter meter = null)
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
          // await SelfTestRunner.RunSelfCheckAdcAsync(cancellationToken, messageService, device, meter);
          // await SelfTestRunner.RunSelfCheckAdcReversedAsync(cancellationToken, messageService, device, meter);
          // await SelfTestRunner.RunSelfCheckPintAsync(cancellationToken, messageService, device, meter);
          // await SelfTestRunner.RunSelfCheckShuntAsync(cancellationToken, messageService, device, meter);
          await SelfTestRunner.RunSelfCheckBreakdownTesterAsync(cancellationToken, messageService, device, meter);
          break;

        case SwitchingDeviceTypeConnector.BlockingRelay:
          await SelfTestRunner.RunSelfCheckBlockingRelayAsync(cancellationToken, messageService, device, meter);
          break;

        case SwitchingDeviceTypeConnector.Multimeter:
          await SelfTestRunner.RunSelfCheckMultimeterAsync(cancellationToken, messageService, device, meter);
          break;

        // case SwitchingDeviceTypeConnector.ADC:
        //  await SelfTestRunner.RunSelfCheckAdcAsync(cancellationToken, messageService, device, meter);
        //  break;

        // case SwitchingDeviceTypeConnector.ADCReversed:
        //  await SelfTestRunner.RunSelfCheckAdcReversedAsync(cancellationToken, messageService, device, meter);
        //  break;

        // case SwitchingDeviceTypeConnector.PINT:
        //  await SelfTestRunner.RunSelfCheckPintAsync(cancellationToken, messageService, device, meter);
        //  break;

        // case SwitchingDeviceTypeConnector.Shunt:
        //  await SelfTestRunner.RunSelfCheckShuntAsync(cancellationToken, messageService, device, meter);
        //  break;

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
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <param name="testType">Тип цепи для проверки.</param>
    /// <param name="messageService">Сервис взаимодействия с пользователем.</param>
    /// <param name="device">Проверяемое устройство коммутации шин.</param>
    /// <param name="meter">Мультиметр для проверки целостности цепей.</param>
    /// <returns>
    /// <see langword="true"/>, если все цепи прошли проверку.
    /// В противном случае — <see langword="false"/>.
    /// </returns>
    /// <exception cref="OperationCanceledException">
    /// Выбрасывается, если запрошена отмена через <paramref name="cancellationToken"/>.
    /// </exception>
    internal static async Task<bool> SelfCheckCircuitAsync(CancellationToken cancellationToken, SwitchingDeviceTypeConnector testType, IUserInteractionService messageService, ISwitchingDevice device = null, IMultimeter meter = null)
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
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <param name="messageService">Сервис взаимодействия с пользователем.</param>
    /// <param name="selfTestChecker">Средство самоконтроля устройства коммутации шин.</param>
    /// <param name="meter">Мультиметр для проверки целостности цепи.</param>
    /// <param name="testType">Тип проверяемой цепи.</param>
    /// <param name="circuitName">Название проверяемой цепи.</param>
    /// <param name="busContact">Контакт шины.</param>
    /// <returns>
    /// <see langword="true"/>, если цепь и её реле прошли проверку.
    /// В противном случае — <see langword="false"/>.
    /// </returns>
    private static async Task<bool> PerformCircuitTestAsync(CancellationToken cancellationToken, IUserInteractionService messageService, ISelfTestCheckerDeviceBusCommutation selfTestChecker, IMultimeter meter, SwitchingDeviceTypeConnector testType, string circuitName, int busContact)
    {
      await messageService.ShowMessageAsync(new ShowMessageModel($"Запуск теста {circuitName}"), true);

      await messageService.ShowMessageAsync(new ShowMessageModel($"Проверка целостности цепи {circuitName}...") { IndentLevel = 1 });
      if (!await UserActionHelper.GetRunWithUserRepeatAsync(
        () => SelfTestRetryHelper.TryCloseCircuitWithRetryAsync(
          cancellationToken,
          messageService,
          selfTestChecker,
          testType,
          busContact,
          circuitName),
        messageService,
        deviceTask: true))
      {
        return false;
      }

      bool continuityResult = false;

      if (meter.ContinuityManager != null)
      {
        await UserActionHelper.RunWithUserRepeatAsync(async () =>
        {
          continuityResult = await meter.ContinuityManager.CheckContinuityAsync(
            true,
            messageService);
          await messageService.ShowMessageAsync(new ShowMessageModel($"{circuitName}", message: "Проверка подключение", type: continuityResult ? ShowMessageModel.MessageType.Success : ShowMessageModel.MessageType.Error) { IndentLevel = 2 }, skipPause: !continuityResult);

          if (continuityResult)
          {
            bool relayResult = await PerformRelayCheck(
              cancellationToken,
              messageService,
              selfTestChecker,
              testType,
              circuitName,
              busContact,
              meter);

            if (ExecutionConfig.GetIsIdleModeEnabled())
            {
              continuityResult = relayResult;
            }
          }

          return continuityResult;

        }, messageService);
      }
      else
      {
        await messageService.ShowMessageAsync(new ShowMessageModel($"Прибор не поддерживает самоконтроль для {circuitName}. Пропуск теста."));
      }

      if (!await UserActionHelper.GetRunWithUserRepeatAsync(
        () => SelfTestRetryHelper.ExecuteHardwareOperationAsync(
          () => selfTestChecker.ExecuteSelfTestAsync(cancellationToken, testType, busContact, 2),
          messageService,
          $"Размыкание цепи {circuitName}"),
        ExecutionConfig.GetIsIdleModeEnabled() ? messageService : null,
        deviceTask: true))
      {
        if (!ExecutionConfig.GetIsIdleModeEnabled())
        {
          await messageService.ShowMessageAsync(
            new ShowMessageModel(
              $"Размыкание цепи {circuitName}",
              type: ShowMessageModel.MessageType.Error));
        }

        return false;
      }

      await messageService.ShowMessageAsync(new ShowMessageModel($"\"{circuitName}\" отключен.", type: ShowMessageModel.MessageType.Success) { IndentLevel = 1 });
      return continuityResult;
    }

    /// <summary>
    /// Проверяет главные реле в цепи самоконтроля для указанного типа проверки.
    /// </summary>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <param name="messageService">Сервис взаимодействия с пользователем.</param>
    /// <param name="selfTestChecker">Средство самоконтроля устройства коммутации шин.</param>
    /// <param name="testType">Тип проверяемой цепи.</param>
    /// <param name="circuitName">Название проверяемой цепи.</param>
    /// <param name="busContact">Контакт шины.</param>
    /// <param name="meter">Мультиметр для проверки состояния реле.</param>
    /// <returns>
    /// <see langword="true"/>, если все реле прошли проверку.
    /// В противном случае — <see langword="false"/>.
    /// </returns>
    /// <exception cref="OperationCanceledException">
    /// Выбрасывается, если запрошена отмена через <paramref name="cancellationToken"/>.
    /// </exception>
    private static async Task<bool> PerformRelayCheck(CancellationToken cancellationToken, IUserInteractionService messageService, ISelfTestCheckerDeviceBusCommutation selfTestChecker, SwitchingDeviceTypeConnector testType, string circuitName, int busContact, IMultimeter meter)
    {
      int relayCount = await GetRelayCountAsync(
        messageService,
        selfTestChecker,
        testType,
        circuitName,
        busContact);

      if (relayCount < 0)
      {
        return false;
      }

      LogInformation($"Обнаружено {relayCount} реле в цепи {circuitName}.", isDeviceLog: true);
      for (int relay = 1; relay <= relayCount; relay++)
      {
        if (!await PerformSingleRelayCheckAsync(
          cancellationToken,
          messageService,
          selfTestChecker,
          testType,
          circuitName,
          busContact,
          meter,
          relay))
        {
          return false;
        }
      }

      return true;
    }

    /// <summary>
    /// Получает количество реле в указанной цепи с поддержкой повторного запроса в холостом режиме.
    /// </summary>
    /// <param name="messageService">Сервис взаимодействия с пользователем.</param>
    /// <param name="selfTestChecker">Средство самоконтроля устройства коммутации шин.</param>
    /// <param name="testType">Тип проверяемой цепи.</param>
    /// <param name="circuitName">Название проверяемой цепи.</param>
    /// <param name="busContact">Контакт шины.</param>
    /// <returns>Количество реле или отрицательное значение, если получить его не удалось.</returns>
    private static async Task<int> GetRelayCountAsync(
      IUserInteractionService messageService,
      ISelfTestCheckerDeviceBusCommutation selfTestChecker,
      SwitchingDeviceTypeConnector testType,
      string circuitName,
      int busContact)
    {
      if (!ExecutionConfig.GetIsIdleModeEnabled())
      {
        return await selfTestChecker.GetRelayCountAsync(testType, busContact);
      }

      int relayCount = await UserActionHelper.GetRunWithUserRepeatAsync(
        async () =>
        {
          int result = await selfTestChecker.GetRelayCountAsync(testType, busContact);
          if (result < 0)
          {
            await ShowRelayCountErrorAsync(messageService, circuitName, skipPause: true);
          }

          return result;
        },
        result => result >= 0,
        messageService,
        deviceTask: true);

      if (relayCount < 0)
      {
        await ShowRelayCountErrorAsync(messageService, circuitName);
      }

      return relayCount;
    }

    /// <summary>
    /// Выводит сообщение об ошибке получения количества реле.
    /// </summary>
    /// <param name="messageService">Сервис взаимодействия с пользователем.</param>
    /// <param name="circuitName">Название проверяемой цепи.</param>
    /// <param name="skipPause">
    /// <see langword="true"/>, чтобы не приостанавливать выполнение после вывода сообщения.
    /// </param>
    /// <returns>Задача, представляющая асинхронный вывод сообщения.</returns>
    private static Task ShowRelayCountErrorAsync(
      IUserInteractionService messageService,
      string circuitName,
      bool skipPause = false)
    {
      return messageService.ShowMessageAsync(
        new ShowMessageModel(
          "Ошибка",
          message: $"Невозможно получить количество реле для {circuitName}.",
          type: ShowMessageModel.MessageType.Error),
        skipPause);
    }

    /// <summary>
    /// Проверяет одно реле: изменяет его состояние, контролирует целостность цепи и возвращает исходное состояние.
    /// </summary>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <param name="messageService">Сервис взаимодействия с пользователем.</param>
    /// <param name="selfTestChecker">Средство самоконтроля устройства коммутации шин.</param>
    /// <param name="testType">Тип проверяемой цепи.</param>
    /// <param name="circuitName">Название проверяемой цепи.</param>
    /// <param name="busContact">Контакт шины.</param>
    /// <param name="meter">Мультиметр для проверки целостности цепи.</param>
    /// <param name="relay">Номер проверяемого реле.</param>
    /// <returns>
    /// <see langword="true"/>, если проверка реле выполнена успешно.
    /// В противном случае — <see langword="false"/>.
    /// </returns>
    /// <exception cref="OperationCanceledException">
    /// Выбрасывается, если запрошена отмена через <paramref name="cancellationToken"/>.
    /// </exception>
    private static async Task<bool> PerformSingleRelayCheckAsync(
      CancellationToken cancellationToken,
      IUserInteractionService messageService,
      ISelfTestCheckerDeviceBusCommutation selfTestChecker,
      SwitchingDeviceTypeConnector testType,
      string circuitName,
      int busContact,
      IMultimeter meter,
      int relay)
    {
      cancellationToken.ThrowIfCancellationRequested();
      await messageService.ShowMessageAsync(
        new ShowMessageModel($"Проверка реле {relay} в цепи {circuitName}") { IndentLevel = 1 });

      if (!await SetRelayStateAsync(
        cancellationToken,
        messageService,
        selfTestChecker,
        testType,
        busContact,
        relay,
        action: 2,
        operationMessage: $"Включение реле {relay} в цепи {circuitName}"))
      {
        if (!ExecutionConfig.GetIsIdleModeEnabled())
        {
          await messageService.ShowMessageAsync(
            new ShowMessageModel(
              $"Включении реле {relay} в цепи {circuitName}",
              type: ShowMessageModel.MessageType.Error));
        }

        return false;
      }

      LogInformation($"Реле {relay} выключено, проверяем целостность цепи...", isDeviceLog: true);

      if (!await UserActionHelper.GetRunWithUserRepeatAsync(
        () => SelfTestRetryHelper.CheckRelayStateAsync(cancellationToken, messageService, meter, relay),
        messageService))
      {
        return false;
      }

      if (await SetRelayStateAsync(
        cancellationToken,
        messageService,
        selfTestChecker,
        testType,
        busContact,
        relay,
        action: 1,
        operationMessage: $"Выключение реле {relay} в цепи {circuitName}"))
      {
        return true;
      }

      LogError($"Ошибка при выключении реле {relay} в цепи {circuitName}.", isDeviceLog: true);
      return false;
    }

    /// <summary>
    /// Устанавливает состояние реле с поддержкой повторного выполнения при аппаратной ошибке.
    /// </summary>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <param name="messageService">Сервис взаимодействия с пользователем.</param>
    /// <param name="selfTestChecker">Средство самоконтроля устройства коммутации шин.</param>
    /// <param name="testType">Тип проверяемой цепи.</param>
    /// <param name="busContact">Контакт шины.</param>
    /// <param name="relay">Номер реле.</param>
    /// <param name="action">Код устанавливаемого состояния реле.</param>
    /// <param name="operationMessage">Название операции для вывода результата.</param>
    /// <returns>
    /// <see langword="true"/>, если состояние реле установлено успешно.
    /// В противном случае — <see langword="false"/>.
    /// </returns>
    private static Task<bool> SetRelayStateAsync(
      CancellationToken cancellationToken,
      IUserInteractionService messageService,
      ISelfTestCheckerDeviceBusCommutation selfTestChecker,
      SwitchingDeviceTypeConnector testType,
      int busContact,
      int relay,
      int action,
      string operationMessage)
    {
      return UserActionHelper.GetRunWithUserRepeatAsync(
        () => SelfTestRetryHelper.ExecuteHardwareOperationAsync(
          () => selfTestChecker.ControlRelayAsync(
            cancellationToken,
            testType,
            relay,
            busContact,
            action),
          messageService,
          operationMessage),
        messageService,
        deviceTask: true);
    }

    /// <summary>
    /// Отправляет устройству команду управления реле цепи самоконтроля.
    /// </summary>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <param name="_deviceBusCommutation">Устройство коммутации шин.</param>
    /// <param name="testType">Тип управляемой цепи.</param>
    /// <param name="relayNumber">Номер реле.</param>
    /// <param name="busContact">Контакт шины.</param>
    /// <param name="action">Код устанавливаемого состояния реле.</param>
    /// <returns>
    /// <see langword="true"/>, если команда отправлена или успешно выполнена в холостом режиме.
    /// В противном случае — <see langword="false"/>.
    /// </returns>
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
        return !IdleHardwareErrorSimulator.ShouldSimulateHardwareError();
      }

      // TODO : Получить и обработать ответ
      await _deviceBusCommutation.DeviceProtocol.QueryAsync(cmd.ToString(), timeout: 1000);
      return true;
    }

    /// <summary>
    /// Отправляет устройству команду замыкания или размыкания цепи самоконтроля.
    /// </summary>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <param name="_deviceBusCommutation">Устройство коммутации шин.</param>
    /// <param name="testType">Тип проверяемой цепи.</param>
    /// <param name="busContact">Контакт шины.</param>
    /// <param name="action">Код действия над цепью.</param>
    /// <returns>
    /// <see langword="true"/>, если параметры корректны и команда отправлена либо успешно выполнена
    /// в холостом режиме. В противном случае — <see langword="false"/>.
    /// </returns>
    static public async Task<bool> ExecuteSelfTestAsync(CancellationToken cancellationToken, Device.DeviceBusCommutation _deviceBusCommutation, SwitchingDeviceTypeConnector testType, int busContact, int action)
    {
      if (!SelfTestManager.ValidateParameters(testType, busContact, action))
      {
        LogError($"Некорректные параметры: Тип проверки - {testType}, Контакт - {busContact}, Действие - {action}.", isDeviceLog: true);
        return false;
      }

      if (ExecutionConfig.GetIsIdleModeEnabled())
      {
        return !IdleHardwareErrorSimulator.ShouldSimulateHardwareError();
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

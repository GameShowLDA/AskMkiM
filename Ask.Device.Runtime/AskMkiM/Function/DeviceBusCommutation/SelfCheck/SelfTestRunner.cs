using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.SwitchingDevice;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;

namespace Ask.Device.Runtime.AskMkiM.Function.DeviceBusCommutation.SelfCheck
{
  /// <summary>
  /// Содержит методы запуска самотестирования различных цепей устройства коммутации шин:
  /// блокировочного реле, мультиметра, АЦП, ПИНТ, шунта, пробойной установки и других.
  /// </summary>
  internal static class SelfTestRunner
  {
    /// <summary>
    /// Выполняет самопроверку цепи блокирующего реле.
    /// </summary>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <param name="messageService">Сервис взаимодействия с пользователем.</param>
    /// <param name="getNextTestNumber">Функция получения следующего порядкового номера теста.</param>
    /// <param name="device">Проверяемое устройство коммутации шин.</param>
    /// <param name="meter">Мультиметр для проверки целостности цепей.</param>
    static internal async Task RunSelfCheckBlockingRelayAsync(CancellationToken cancellationToken, IUserInteractionService messageService, Func<int> getNextTestNumber, ISwitchingDevice device = null, IMultimeter meter = null)
    {
      await SelfTestProcessManager.SelfCheckCircuitAsync(cancellationToken, SwitchingDeviceTypeConnector.BlockingRelay, messageService, getNextTestNumber, device, meter);
    }

    /// <summary>
    /// Выполняет самопроверку цепи мультиметра.
    /// </summary>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <param name="messageService">Сервис взаимодействия с пользователем.</param>
    /// <param name="getNextTestNumber">Функция получения следующего порядкового номера теста.</param>
    /// <param name="device">Проверяемое устройство коммутации шин.</param>
    /// <param name="meter">Мультиметр для проверки целостности цепей.</param>
    static internal async Task RunSelfCheckMultimeterAsync(CancellationToken cancellationToken, IUserInteractionService messageService, Func<int> getNextTestNumber, ISwitchingDevice device = null, IMultimeter meter = null)
    {
      await SelfTestProcessManager.SelfCheckCircuitAsync(cancellationToken, SwitchingDeviceTypeConnector.Multimeter, messageService, getNextTestNumber, device, meter);
    }

    /// <summary>
    /// Выполняет самопроверку цепи АЦП.
    /// </summary>
    static internal async Task RunSelfCheckAdcAsync(CancellationToken cancellationToken, IUserInteractionService messageService, ISwitchingDevice device = null, IMultimeter meter = null)
    {
      throw new NotSupportedException("Самоконтроль цепи АЦП временно отключён.");
      // TODO : Как надо будет, раскоментировать данный метод проверки самоконтроля УКШ в плане АЦП.
      // await SelfTestProcessManager.SelfCheckCircuitAsync(cancellationToken, SwitchingDeviceTypeConnector.ADC, messageService, device, meter);
    }

    /// <summary>
    /// Выполняет самопроверку цепи АЦП в инверсной конфигурации.
    /// </summary>
    static internal async Task RunSelfCheckAdcReversedAsync(CancellationToken cancellationToken, IUserInteractionService messageService, ISwitchingDevice device = null, IMultimeter meter = null)
    {
      throw new NotSupportedException("Самоконтроль цепи АЦП с переполюсовкой временно отключён.");
      // TODO : Как надо будет, раскоментировать данный метод проверки самоконтроля УКШ в плане АЦП Переполюсовка. 
      // await SelfTestProcessManager.SelfCheckCircuitAsync(cancellationToken, SwitchingDeviceTypeConnector.ADCReversed, messageService, device, meter);
    }

    /// <summary>
    /// Выполняет самопроверку цепи программируемого источника тока и напряжения (ПИНТ).
    /// </summary>
    static internal async Task RunSelfCheckPintAsync(CancellationToken cancellationToken, IUserInteractionService messageService, ISwitchingDevice device = null, IMultimeter meter = null)
    {
      throw new NotSupportedException("Самоконтроль цепи ПИНТ временно отключён.");

      // TODO : Как надо будет, раскоментировать данный метод проверки самоконтроля УКШ в плане ПИНТ. 
      // await SelfTestProcessManager.SelfCheckCircuitAsync(cancellationToken, SwitchingDeviceTypeConnector.PINT, messageService, device, meter);
    }

    /// <summary>
    /// Выполняет самопроверку цепи с шунтом.
    /// </summary>
    static internal async Task RunSelfCheckShuntAsync(CancellationToken cancellationToken, IUserInteractionService messageService, ISwitchingDevice device = null, IMultimeter meter = null)
    {
      throw new NotSupportedException("Самоконтроль цепи ШУНТ временно отключён.");
      // TODO : Как надо будет, раскоментировать данный метод проверки самоконтроля УКШ в плане ШУНТ. 
      // await SelfTestProcessManager.SelfCheckCircuitAsync(cancellationToken, SwitchingDeviceTypeConnector.Shunt, messageService, device, meter);
    }

    /// <summary>
    /// Выполняет самопроверку цепи пробойной установки (ПКИ).
    /// </summary>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <param name="messageService">Сервис взаимодействия с пользователем.</param>
    /// <param name="getNextTestNumber">Функция получения следующего порядкового номера теста.</param>
    /// <param name="device">Проверяемое устройство коммутации шин.</param>
    /// <param name="meter">Мультиметр для проверки целостности цепей.</param>
    static internal async Task RunSelfCheckBreakdownTesterAsync(CancellationToken cancellationToken, IUserInteractionService messageService, Func<int> getNextTestNumber, ISwitchingDevice device = null, IMultimeter meter = null)
    {
      await SelfTestProcessManager.SelfCheckCircuitAsync(cancellationToken, SwitchingDeviceTypeConnector.BreakdownTester, messageService, getNextTestNumber, device, meter);
    }

  }
}

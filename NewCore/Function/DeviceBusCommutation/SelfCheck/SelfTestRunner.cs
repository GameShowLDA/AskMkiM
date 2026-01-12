using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.SwitchingDevice;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;

namespace NewCore.Function.DeviceBusCommutation.SelfCheck
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
    static internal async Task RunSelfCheckBlockingRelayAsync(CancellationToken cancellationToken, IUserInteractionService messageService, ISwitchingDevice device = null, IFastMeter meter = null)
    {
      await SelfTestProcessManager.SelfCheckCircuitAsync(cancellationToken, SwitchingDeviceTypeConnector.BlockingRelay, messageService, device, meter);
    }

    /// <summary>
    /// Выполняет самопроверку цепи мультиметра.
    /// </summary>
    static internal async Task RunSelfCheckMultimeterAsync(CancellationToken cancellationToken, IUserInteractionService messageService, ISwitchingDevice device = null, IFastMeter meter = null)
    {
      await SelfTestProcessManager.SelfCheckCircuitAsync(cancellationToken, SwitchingDeviceTypeConnector.Multimeter, messageService, device, meter);
    }

    /// <summary>
    /// Выполняет самопроверку цепи АЦП.
    /// </summary>
    static internal async Task RunSelfCheckAdcAsync(CancellationToken cancellationToken, IUserInteractionService messageService, ISwitchingDevice device = null, IFastMeter meter = null)
    {
      await SelfTestProcessManager.SelfCheckCircuitAsync(cancellationToken, SwitchingDeviceTypeConnector.ADC, messageService, device, meter);
    }

    /// <summary>
    /// Выполняет самопроверку цепи АЦП в инверсной конфигурации.
    /// </summary>
    static internal async Task RunSelfCheckAdcReversedAsync(CancellationToken cancellationToken, IUserInteractionService messageService, ISwitchingDevice device = null, IFastMeter meter = null)
    {
      await SelfTestProcessManager.SelfCheckCircuitAsync(cancellationToken, SwitchingDeviceTypeConnector.ADCReversed, messageService, device, meter);
    }

    /// <summary>
    /// Выполняет самопроверку цепи программируемого источника тока и напряжения (ПИНТ).
    /// </summary>
    static internal async Task RunSelfCheckPintAsync(CancellationToken cancellationToken, IUserInteractionService messageService, ISwitchingDevice device = null, IFastMeter meter = null)
    {
      await SelfTestProcessManager.SelfCheckCircuitAsync(cancellationToken, SwitchingDeviceTypeConnector.PINT, messageService, device, meter);
    }

    /// <summary>
    /// Выполняет самопроверку цепи с шунтом.
    /// </summary>
    static internal async Task RunSelfCheckShuntAsync(CancellationToken cancellationToken, IUserInteractionService messageService, ISwitchingDevice device = null, IFastMeter meter = null)
    {
      await SelfTestProcessManager.SelfCheckCircuitAsync(cancellationToken, SwitchingDeviceTypeConnector.Shunt, messageService, device, meter);
    }

    /// <summary>
    /// Выполняет самопроверку цепи пробойной установки (ПКИ).
    /// </summary>
    static internal async Task RunSelfCheckBreakdownTesterAsync(CancellationToken cancellationToken, IUserInteractionService messageService, ISwitchingDevice device = null, IFastMeter meter = null)
    {
      await SelfTestProcessManager.SelfCheckCircuitAsync(cancellationToken, SwitchingDeviceTypeConnector.BreakdownTester, messageService, device, meter);
    }

  }
}

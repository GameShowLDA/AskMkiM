using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AppConfiguration.Interface;
using NewCore.Base.Function.DBC;
using NewCore.Base.Interface.Main;

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
    static internal async Task RunSelfCheckBlockingRelayAsync(CancellationToken cancellationToken, IUserMessageService messageService, ISwitchingDevice device = null, IFastMeter meter = null)
    {
      await SelfTestProcessManager.SelfCheckCircuitAsync(cancellationToken, TypeConnector.BlockingRelay, messageService, device, meter);
    }

    /// <summary>
    /// Выполняет самопроверку цепи мультиметра.
    /// </summary>
    static internal async Task RunSelfCheckMultimeterAsync(CancellationToken cancellationToken, IUserMessageService messageService, ISwitchingDevice device = null, IFastMeter meter = null)
    {
      await SelfTestProcessManager.SelfCheckCircuitAsync(cancellationToken, TypeConnector.Multimeter, messageService, device, meter);
    }

    /// <summary>
    /// Выполняет самопроверку цепи АЦП.
    /// </summary>
    static internal async Task RunSelfCheckAdcAsync(CancellationToken cancellationToken, IUserMessageService messageService, ISwitchingDevice device = null, IFastMeter meter = null)
    {
      await SelfTestProcessManager.SelfCheckCircuitAsync(cancellationToken, TypeConnector.ADC, messageService, device, meter);
    }

    /// <summary>
    /// Выполняет самопроверку цепи АЦП в инверсной конфигурации.
    /// </summary>
    static internal async Task RunSelfCheckAdcReversedAsync(CancellationToken cancellationToken, IUserMessageService messageService, ISwitchingDevice device = null, IFastMeter meter = null)
    {
      await SelfTestProcessManager.SelfCheckCircuitAsync(cancellationToken, TypeConnector.ADCReversed, messageService, device, meter);
    }

    /// <summary>
    /// Выполняет самопроверку цепи программируемого источника тока и напряжения (ПИНТ).
    /// </summary>
    static internal async Task RunSelfCheckPintAsync(CancellationToken cancellationToken, IUserMessageService messageService, ISwitchingDevice device = null, IFastMeter meter = null)
    {
      await SelfTestProcessManager.SelfCheckCircuitAsync(cancellationToken, TypeConnector.PINT, messageService, device, meter);
    }

    /// <summary>
    /// Выполняет самопроверку цепи с шунтом.
    /// </summary>
    static internal async Task RunSelfCheckShuntAsync(CancellationToken cancellationToken, IUserMessageService messageService, ISwitchingDevice device = null, IFastMeter meter = null)
    {
      await SelfTestProcessManager.SelfCheckCircuitAsync(cancellationToken, TypeConnector.Shunt, messageService, device, meter);
    }

    /// <summary>
    /// Выполняет самопроверку цепи пробойной установки (ПКИ).
    /// </summary>
    static internal async Task RunSelfCheckBreakdownTesterAsync(CancellationToken cancellationToken, IUserMessageService messageService, ISwitchingDevice device = null, IFastMeter meter = null)
    {
      await SelfTestProcessManager.SelfCheckCircuitAsync(cancellationToken, TypeConnector.BreakdownTester, messageService, device, meter);
    }

  }
}

using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter.Capabilities;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.SwitchingDevice;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Core.Shared.Metadata.Static.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ask.Device.Runtime.Function.Multimeter.SelfCheck
{
  public class SelfTestManager : ISelfTestCheckerMultimeter
  {
    private static readonly double IdealVoltage = 0;
    private static readonly double IdealResistance = 0;
    private static readonly double IdealCapacity = 0;

    public Type GetTestTypeEnum()
    {
      return typeof(MultimeterTypeConnector);
    }
    public async Task StartSelfCheck(CancellationToken cancellationToken, Enum selectedType, IUserInteractionService? userMessageService = null, ISwitchingDevice device = null, IFastMeter meter = null)
    {
      await userMessageService.ShowMessageAsync(ExecutorMessageBuilder.BuildMultimeterSetupMessage());

      await device.ConnectableManager.InitializeAsync();
      await meter.ConnectableManager.InitializeAsync();

      await device.RelayManager.EnableRelay(userMessageService);

      switch (selectedType)
      {
        case MultimeterTypeConnector.Voltage:
          await StartVoltageMeasurementTest(cancellationToken, device, meter, userMessageService);
          break;

        case MultimeterTypeConnector.Resistance:
          await StartResistanceMeasurementTest(cancellationToken, device, meter, userMessageService);
          break;

        case MultimeterTypeConnector.Capacity:
          await StartCapacitanceMeasurementTest(cancellationToken, device, meter, userMessageService);
          break;

        case MultimeterTypeConnector.FullCheck:
          await StartVoltageMeasurementTest(cancellationToken, device, meter, userMessageService);
          await StartResistanceMeasurementTest(cancellationToken, device, meter, userMessageService);
          await StartCapacitanceMeasurementTest(cancellationToken, device, meter, userMessageService);
          break;
      }

      await device.RelayManager.DisableRelay(userMessageService);
    }

    private async Task StartVoltageMeasurementTest(CancellationToken cancellationToken, ISwitchingDevice device, IFastMeter meter, IUserInteractionService? userMessageService = null)
    {
      cancellationToken.ThrowIfCancellationRequested();
      await device.ConnectorManager.ConnectMultimeter(SwitchingBusNew.AB1, userMessageService);

      await meter.DcVoltageManager.SetDCVoltageModeAsync(userMessageService);
      cancellationToken.ThrowIfCancellationRequested();

      cancellationToken.ThrowIfCancellationRequested();

      double result = await meter.DcVoltageManager.MeasureDCVoltageAsync(IdealVoltage);
      cancellationToken.ThrowIfCancellationRequested();

      await SelfTestHelper.IsCorrectRangeAsync(IdealVoltage, result, "напряжения", userMessageService);

      await meter.AcVoltageManager.SetACVoltageModeAsync(userMessageService);
      cancellationToken.ThrowIfCancellationRequested();

      cancellationToken.ThrowIfCancellationRequested();

      result = await meter.AcVoltageManager.MeasureACVoltageAsync(IdealVoltage);
      cancellationToken.ThrowIfCancellationRequested();

      await SelfTestHelper.IsCorrectRangeAsync(IdealResistance, result, "напряжения", userMessageService);

      await device.ConnectorManager.DisconnectMultimeter(SwitchingBusNew.AB1, userMessageService);
    }

    private async Task StartResistanceMeasurementTest(CancellationToken cancellationToken, ISwitchingDevice device, IFastMeter meter, IUserInteractionService? userMessageService = null)
    {
      await device.ConnectorManager.ConnectMultimeter(SwitchingBusNew.AB2, userMessageService);

      await meter.ResistanceManager.SetResistanceModeAsync(userMessageService);
      double result = await meter.ResistanceManager.MeasureResistanceAsync(rangeFrom: IdealResistance, rangeTo: IdealResistance);
      await SelfTestHelper.IsCorrectRangeAsync(IdealResistance, result, "сопротивления", userMessageService);

      await device.ConnectorManager.DisconnectMultimeter(SwitchingBusNew.AB2, userMessageService);
      cancellationToken.ThrowIfCancellationRequested();
    }

    private async Task StartCapacitanceMeasurementTest(CancellationToken cancellationToken, ISwitchingDevice device, IFastMeter meter, IUserInteractionService? userMessageService = null)
    {
      cancellationToken.ThrowIfCancellationRequested();
      await device.ConnectorManager.ConnectMultimeter(SwitchingBusNew.AB4, userMessageService);

      await meter.CapacitanceManager.SetCapacitanceModeAsync(userMessageService);
      cancellationToken.ThrowIfCancellationRequested();
      cancellationToken.ThrowIfCancellationRequested();
      double result = await meter.CapacitanceManager.MeasureCapacitanceAsync(IdealCapacity);

      cancellationToken.ThrowIfCancellationRequested();

      await SelfTestHelper.IsCorrectRangeAsync(IdealCapacity, result, "емкости", userMessageService);

      await device.ConnectorManager.DisconnectMultimeter(SwitchingBusNew.AB4, userMessageService);
    }
  }
}
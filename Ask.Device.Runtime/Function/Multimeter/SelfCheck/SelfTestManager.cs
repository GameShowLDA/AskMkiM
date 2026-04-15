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

    private double _voltageDcRangeFrom = -0.02;
    private double _voltageDcRangeTo = 0.02;

    private double _voltageAcRangeFrom = 0.97;
    private double _voltageAcRangeTo = 1.03;

    private double _resistanceRangeFrom = -0.02;
    private double _resistanceRangeTo = 0.02;

    private double _capacityRangeFrom = -0.02;
    private double _capacityRangeTo = 0.02;
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

      double result = await meter.DcVoltageManager.MeasureDCVoltageAsync(rangeFrom: _voltageDcRangeFrom, rangeTo: _voltageDcRangeTo, userMessageService: userMessageService);
      cancellationToken.ThrowIfCancellationRequested();

      await SelfTestHelper.IsCorrectRangeAsync(_voltageDcRangeFrom, _voltageDcRangeTo, result, "напряжения", userMessageService);

      await meter.AcVoltageManager.SetACVoltageModeAsync(userMessageService);
      cancellationToken.ThrowIfCancellationRequested();

      cancellationToken.ThrowIfCancellationRequested();

      result = await meter.AcVoltageManager.MeasureACVoltageAsync(rangeFrom: _voltageAcRangeFrom, rangeTo: _voltageAcRangeTo, userMessageService: userMessageService);
      cancellationToken.ThrowIfCancellationRequested();

      await SelfTestHelper.IsCorrectRangeAsync(_voltageAcRangeFrom, _voltageAcRangeTo, result, "напряжения", userMessageService);

      await device.ConnectorManager.DisconnectMultimeter(SwitchingBusNew.AB1, userMessageService);
    }

    private async Task StartResistanceMeasurementTest(CancellationToken cancellationToken, ISwitchingDevice device, IFastMeter meter, IUserInteractionService? userMessageService = null)
    {
      await device.ConnectorManager.ConnectMultimeter(SwitchingBusNew.AB2, userMessageService);

      await meter.ResistanceManager.SetResistanceModeAsync(userMessageService);

      double result = await meter.ResistanceManager.MeasureResistanceAsync(rangeFrom: _resistanceRangeFrom, rangeTo: _resistanceRangeTo, userMessageService: userMessageService);
      await SelfTestHelper.IsCorrectRangeAsync(_resistanceRangeFrom, _resistanceRangeTo, result, "сопротивления", userMessageService);

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
      double result = await meter.CapacitanceManager.MeasureCapacitanceAsync(rangeFrom: _capacityRangeFrom, rangeTo: _capacityRangeTo, userMessageService: userMessageService);

      cancellationToken.ThrowIfCancellationRequested();

      await SelfTestHelper.IsCorrectRangeAsync(_capacityRangeFrom, _capacityRangeTo, result, "емкости", userMessageService);

      await device.ConnectorManager.DisconnectMultimeter(SwitchingBusNew.AB4, userMessageService);
    }
  }
}
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
    public Type GetTestTypeEnum()
    {
      return typeof(MultimeterTypeConnector);
    }

    public async Task StartSelfCheck(CancellationToken cancellationToken, Enum selectedType, IUserInteractionService? userMessageService = null, ISwitchingDevice device = null, IFastMeter meter = null)
    {
      await userMessageService.ShowMessageAsync(ExecutorMessageBuilder.BuildMultimeterSetupMessage());

      await device.ConnectableManager.InitializeAsync();
      await meter.ConnectableManager.InitializeAsync();

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
    }

    private async Task StartVoltageMeasurementTest(CancellationToken cancellationToken, ISwitchingDevice device, IFastMeter meter, IUserInteractionService? userMessageService = null)
    {
      cancellationToken.ThrowIfCancellationRequested();
      await device.ConnectorManager.ConnectMultimeter(SwitchingBusNew.AB1, userMessageService);

      await meter.DcVoltageManager.SetDCVoltageModeAsync(userMessageService);
      cancellationToken.ThrowIfCancellationRequested();
      await device.RelayManager.EnableRelay(userMessageService);
      cancellationToken.ThrowIfCancellationRequested();
      double result = await meter.DcVoltageManager.MeasureDCVoltageAsync(userMessageService:  userMessageService);
      cancellationToken.ThrowIfCancellationRequested();
      await device.RelayManager.DisableRelay(userMessageService);
      await SelfTestHelper.IsCorrectRangeAsync(-0.02, 0.02, result, "напряжения", userMessageService);

      await meter.AcVoltageManager.SetACVoltageModeAsync(userMessageService);
      cancellationToken.ThrowIfCancellationRequested();
      await device.RelayManager.EnableRelay(userMessageService);
      cancellationToken.ThrowIfCancellationRequested();
      result = await meter.AcVoltageManager.MeasureACVoltageAsync(userMessageService: userMessageService);
      cancellationToken.ThrowIfCancellationRequested();
      await device.RelayManager.DisableRelay(userMessageService);
      await SelfTestHelper.IsCorrectRangeAsync(0.97, 1.03, result, "напряжения", userMessageService);

      await device.ConnectorManager.DisconnectMultimeter(SwitchingBusNew.AB1, userMessageService);
    }

    private async Task StartResistanceMeasurementTest(CancellationToken cancellationToken, ISwitchingDevice device, IFastMeter meter, IUserInteractionService? userMessageService = null)
    {
      cancellationToken.ThrowIfCancellationRequested();
      await device.ConnectorManager.ConnectMultimeter(SwitchingBusNew.AB2, userMessageService);

      await meter.ResistanceManager.SetResistanceModeAsync(userMessageService);
      cancellationToken.ThrowIfCancellationRequested();
      await device.RelayManager.EnableRelay(userMessageService);
      cancellationToken.ThrowIfCancellationRequested();
      double result = await meter.ResistanceManager.MeasureResistanceAsync(userMessageService: userMessageService);
      cancellationToken.ThrowIfCancellationRequested();
      await device.RelayManager.DisableRelay(userMessageService);
      await SelfTestHelper.IsCorrectRangeAsync(-0.02, 0.02, result, "сопротивления", userMessageService);

      await device.ConnectorManager.DisconnectMultimeter(SwitchingBusNew.AB2, userMessageService);
    }

    private async Task StartCapacitanceMeasurementTest(CancellationToken cancellationToken, ISwitchingDevice device, IFastMeter meter, IUserInteractionService? userMessageService = null)
    {
      cancellationToken.ThrowIfCancellationRequested();
      await device.ConnectorManager.ConnectMultimeter(SwitchingBusNew.AB4, userMessageService);

      await meter.CapacitanceManager.SetCapacitanceModeAsync(userMessageService);
      cancellationToken.ThrowIfCancellationRequested();
      await device.RelayManager.EnableRelay(userMessageService);
      cancellationToken.ThrowIfCancellationRequested();
      double result = await meter.CapacitanceManager.MeasureCapacitanceAsync(userMessageService: userMessageService);
      cancellationToken.ThrowIfCancellationRequested();
      await device.RelayManager.DisableRelay(userMessageService);
      await SelfTestHelper.IsCorrectRangeAsync(-0.02, 0.02, result, "емкости", userMessageService);

      await device.ConnectorManager.DisconnectMultimeter(SwitchingBusNew.AB4, userMessageService);
    }
  }
}
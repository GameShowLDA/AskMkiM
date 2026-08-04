using Ask.Core.Shared.DTO.Devices.Measurements;
using Ask.Core.Shared.DTO.Executor;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter.Capabilities;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.SwitchingDevice;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Core.Shared.Metadata.Static.Messages;

namespace Ask.Device.Runtime.Function.Base.Multimeter.SelfCheck
{
  public class SelfTestManager : ISelfTestCheckerMultimeter
  {
    private static readonly double IdealVoltage = 0;
    private static readonly double IdealResistance = 0.25;
    private static readonly double IdealCapacity = 0.7;

    private static double VoltageRange(double voltage = 0) => (0.01 * voltage) + 0.02;
    private static double ResistanceRange(double resistance = 0) => 0.05;
    private static double CapacityRange(double resistance = 0) => 0.3;

    public Type GetTestTypeEnum()
    {
      return typeof(MultimeterTypeConnector);
    }
    public async Task StartSelfCheck(CancellationToken cancellationToken, Enum selectedType, ActionSettings settings, IUserInteractionService? userMessageService = null, ISwitchingDevice device = null, IMultimeter meter = null)
    {
      await userMessageService.ShowMessageAsync(ExecutorMessageBuilder.BuildMultimeterSetupMessage());

      var deviceTitle = ExecutorMessageBuilder.BuildDeviceHealthCheckTitle(meter);


      settings.DeviceResults.Add(new DeviceExecutionResult
      {
        DeviceName = $"{deviceTitle.Header} \"{deviceTitle.Message}\""
      });

      await device.ConnectableManager.InitializeAsync();
      await meter.ConnectableManager.InitializeAsync();

      await device.RelayManager.EnableRelay(userMessageService);

      switch (selectedType)
      {
        case MultimeterTypeConnector.Voltage:
          await StartVoltageMeasurementTest(cancellationToken, device, meter, settings, userMessageService);
          break;

        case MultimeterTypeConnector.Resistance:
          await StartResistanceMeasurementTest(cancellationToken, device, meter, settings, userMessageService);
          break;

        case MultimeterTypeConnector.Capacity:
          await StartCapacitanceMeasurementTest(cancellationToken, device, meter, settings, userMessageService);
          break;

        case MultimeterTypeConnector.FullCheck:
          await StartVoltageMeasurementTest(cancellationToken, device, meter, settings, userMessageService);
          await StartResistanceMeasurementTest(cancellationToken, device, meter, settings, userMessageService);
          await StartCapacitanceMeasurementTest(cancellationToken, device, meter, settings, userMessageService);
          break;
      }

      await device.RelayManager.DisableRelay(userMessageService);
    }

    private async Task StartVoltageMeasurementTest(CancellationToken cancellationToken, ISwitchingDevice device, IMultimeter meter, ActionSettings settings, IUserInteractionService? userMessageService = null)
    {
      cancellationToken.ThrowIfCancellationRequested();
      await device.ConnectorManager.ConnectMultimeter(SwitchingBusNew.AB1, userMessageService);

      cancellationToken.ThrowIfCancellationRequested();
      await meter.DcVoltageManager.SetDCVoltageModeAsync(userMessageService);

      cancellationToken.ThrowIfCancellationRequested();
      MeasurementRange measurementRangeDc = new MeasurementRange(IdealVoltage, IdealVoltage, IdealVoltage);
      double result = await meter.DcVoltageManager.MeasureDCVoltageAsync(measurementRangeDc);

      cancellationToken.ThrowIfCancellationRequested();
      //await SelfTestHelper.IsCorrectRangeAsync(IdealVoltage, result, "напряжения", userMessageService);
      bool result_status = SelfTestHelper.InRange(IdealVoltage, result, VoltageRange());
      await SelfTestHelper.IsCorrectRangeAsync(result_status, result, "напряжения DCW", settings, "В", userMessageService);

      cancellationToken.ThrowIfCancellationRequested();
      await meter.AcVoltageManager.SetACVoltageModeAsync(userMessageService);

      cancellationToken.ThrowIfCancellationRequested();
      MeasurementRange measurementRangeAc = new MeasurementRange(IdealVoltage, IdealVoltage, IdealVoltage);
      result = await meter.AcVoltageManager.MeasureACVoltageAsync(measurementRangeAc);

      cancellationToken.ThrowIfCancellationRequested();
      //await SelfTestHelper.IsCorrectRangeAsync(IdealResistance, result, "напряжения", userMessageService);
      result_status = SelfTestHelper.InRange(IdealVoltage, result, VoltageRange(result));
      await SelfTestHelper.IsCorrectRangeAsync(result_status, result, "напряжения ACW", settings, "В", userMessageService);

      cancellationToken.ThrowIfCancellationRequested();
      await device.ConnectorManager.DisconnectMultimeter(SwitchingBusNew.AB1, userMessageService);
    }

    private async Task StartResistanceMeasurementTest(CancellationToken cancellationToken, ISwitchingDevice device, IMultimeter meter, ActionSettings settings, IUserInteractionService? userMessageService = null)
    {
      cancellationToken.ThrowIfCancellationRequested();
      await device.ConnectorManager.ConnectMultimeter(SwitchingBusNew.AB2, userMessageService);

      cancellationToken.ThrowIfCancellationRequested();
      await meter.ResistanceManager.SetResistanceModeAsync(userMessageService);

      cancellationToken.ThrowIfCancellationRequested();
      MeasurementRange measurementRangeRes = new MeasurementRange(IdealResistance, IdealResistance, IdealResistance);
      double result = await meter.ResistanceManager.MeasureResistanceAsync(measurementRangeRes);

      cancellationToken.ThrowIfCancellationRequested();
      //await SelfTestHelper.IsCorrectRangeAsync(IdealResistance, result, "сопротивления", userMessageService);
      bool result_status = SelfTestHelper.InRange(IdealResistance, result, ResistanceRange());
      await SelfTestHelper.IsCorrectRangeAsync(result_status, result, "сопротивления", settings, "Ом", userMessageService);

      cancellationToken.ThrowIfCancellationRequested();
      await device.ConnectorManager.DisconnectMultimeter(SwitchingBusNew.AB2, userMessageService);
    }

    private async Task StartCapacitanceMeasurementTest(CancellationToken cancellationToken, ISwitchingDevice device, IMultimeter meter, ActionSettings settings, IUserInteractionService? userMessageService = null)
    {
      cancellationToken.ThrowIfCancellationRequested();
      await device.ConnectorManager.ConnectMultimeter(SwitchingBusNew.AB1, userMessageService);

      cancellationToken.ThrowIfCancellationRequested();
      await device.RelayManager.DisableRelay(userMessageService);

      cancellationToken.ThrowIfCancellationRequested();
      await meter.CapacitanceManager.SetCapacitanceModeAsync(userMessageService);

      cancellationToken.ThrowIfCancellationRequested();

      MeasurementRange measurementRangeCap = new MeasurementRange(IdealCapacity, IdealCapacity / 2, IdealCapacity * 2);
      double result = await meter.CapacitanceManager.MeasureCapacitanceAsync(measurementRangeCap);

      cancellationToken.ThrowIfCancellationRequested();
      //await SelfTestHelper.IsCorrectRangeAsync(IdealCapacity, result, "емкости", userMessageService);
      bool result_status = SelfTestHelper.InRange(IdealCapacity, result, CapacityRange());
      await SelfTestHelper.IsCorrectRangeAsync(result_status, result, "емкости", settings, "нФ", userMessageService);

      await device.ConnectorManager.DisconnectMultimeter(SwitchingBusNew.AB1, userMessageService);
    }
  }
}

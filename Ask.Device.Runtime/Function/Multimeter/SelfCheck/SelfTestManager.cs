using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter.Capabilities;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.SwitchingDevice;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Core.Shared.Metadata.Static.Messages;
using Ask.Device.Runtime.Function.Keysight3466new;

namespace Ask.Device.Runtime.Function.Multimeter.SelfCheck
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

      cancellationToken.ThrowIfCancellationRequested();
      await meter.DcVoltageManager.SetDCVoltageModeAsync(userMessageService);

      await VoltageMeasurement(cancellationToken, value => meter.DcVoltageManager.MeasureDCVoltageAsync(value), 0, userMessageService);
      await VoltageMeasurement(cancellationToken, value => meter.DcVoltageManager.MeasureDCVoltageAsync(value), 10, userMessageService);
      await VoltageMeasurement(cancellationToken, value => meter.DcVoltageManager.MeasureDCVoltageAsync(value), 100, userMessageService);

      cancellationToken.ThrowIfCancellationRequested();
      await meter.AcVoltageManager.SetACVoltageModeAsync(userMessageService);

      await VoltageMeasurement(cancellationToken, value => meter.AcVoltageManager.MeasureACVoltageAsync(value), 0, userMessageService);
      await VoltageMeasurement(cancellationToken, value => meter.AcVoltageManager.MeasureACVoltageAsync(value), 10, userMessageService);
      await VoltageMeasurement(cancellationToken, value => meter.AcVoltageManager.MeasureACVoltageAsync(value), 100, userMessageService);

      cancellationToken.ThrowIfCancellationRequested();
      await device.ConnectorManager.DisconnectMultimeter(SwitchingBusNew.AB1, userMessageService);
    }

    private async Task VoltageMeasurement(CancellationToken cancellationToken, Func<double, Task<double>> measureMethod, double idealValue, IUserInteractionService? userMessageService = null)
    {
      cancellationToken.ThrowIfCancellationRequested();
      double result = await measureMethod(idealValue);

      cancellationToken.ThrowIfCancellationRequested();
      bool result_status = SelfTestHelper.InRange(IdealVoltage, result, VoltageRange());
      await SelfTestHelper.IsCorrectRangeAsync(result_status, result, "напряжения", "В", userMessageService);
    }

    private async Task StartResistanceMeasurementTest(CancellationToken cancellationToken, ISwitchingDevice device, IFastMeter meter, IUserInteractionService? userMessageService = null)
    {
      cancellationToken.ThrowIfCancellationRequested();
      await device.ConnectorManager.ConnectMultimeter(SwitchingBusNew.AB4, userMessageService);

      cancellationToken.ThrowIfCancellationRequested();
      await meter.ResistanceManager.SetResistanceModeAsync(userMessageService);

      await ResistanceMeasurement(cancellationToken, () => meter.ResistanceManager.MeasureResistanceAsync(), 100, userMessageService);
      await ResistanceMeasurement(cancellationToken, () => meter.ResistanceManager.MeasureResistanceAsync(), 1_000, userMessageService);
      await ResistanceMeasurement(cancellationToken, () => meter.ResistanceManager.MeasureResistanceAsync(), 10_000, userMessageService);
      await ResistanceMeasurement(cancellationToken, () => meter.ResistanceManager.MeasureResistanceAsync(), 100_000, userMessageService);
      await ResistanceMeasurement(cancellationToken, () => meter.ResistanceManager.MeasureResistanceAsync(), 1_000_000, userMessageService);
      await ResistanceMeasurement(cancellationToken, () => meter.ResistanceManager.MeasureResistanceAsync(), 10_000_000, userMessageService);

      cancellationToken.ThrowIfCancellationRequested();
      await device.ConnectorManager.DisconnectMultimeter(SwitchingBusNew.AB4, userMessageService);
    }

    private async Task ResistanceMeasurement(CancellationToken cancellationToken, Func<Task<double>> measureMethod, double idealValue, IUserInteractionService? userMessageService = null)
    {
      cancellationToken.ThrowIfCancellationRequested();
      double result = await measureMethod();

      cancellationToken.ThrowIfCancellationRequested();
      bool result_status = SelfTestHelper.InRange(IdealResistance, result, ResistanceRange());
      await SelfTestHelper.IsCorrectRangeAsync(result_status, result, "сопротивления", "Ом", userMessageService);
    }

    private async Task StartVoltageMeasurementTestOLD(CancellationToken cancellationToken, ISwitchingDevice device, IFastMeter meter, IUserInteractionService? userMessageService = null)
    {
      //VoltageMeasurement(cancellationToken, value => meter.DcVoltageManager.MeasureDCVoltageAsync(value));

      cancellationToken.ThrowIfCancellationRequested();
      await device.ConnectorManager.ConnectMultimeter(SwitchingBusNew.AB1, userMessageService);

      cancellationToken.ThrowIfCancellationRequested();
      await meter.DcVoltageManager.SetDCVoltageModeAsync(userMessageService);

      cancellationToken.ThrowIfCancellationRequested();
      double result = await meter.DcVoltageManager.MeasureDCVoltageAsync(IdealVoltage);
      //VoltageMeasurement(cancellationToken, value => meter.DcVoltageManager.MeasureDCVoltageAsync(value));

      cancellationToken.ThrowIfCancellationRequested();
      //await SelfTestHelper.IsCorrectRangeAsync(IdealVoltage, result, "напряжения", userMessageService);
      bool result_status = SelfTestHelper.InRange(IdealVoltage, result, VoltageRange());
      await SelfTestHelper.IsCorrectRangeAsync(result_status, result, "напряжения", "В", userMessageService);

      cancellationToken.ThrowIfCancellationRequested();
      await meter.AcVoltageManager.SetACVoltageModeAsync(userMessageService);

      cancellationToken.ThrowIfCancellationRequested();
      result = await meter.AcVoltageManager.MeasureACVoltageAsync(IdealVoltage);

      cancellationToken.ThrowIfCancellationRequested();
      //await SelfTestHelper.IsCorrectRangeAsync(IdealResistance, result, "напряжения", userMessageService);
      result_status = SelfTestHelper.InRange(IdealVoltage, result, VoltageRange(result));
      await SelfTestHelper.IsCorrectRangeAsync(result_status, result, "напряжения", "В", userMessageService);

      cancellationToken.ThrowIfCancellationRequested();
      await device.ConnectorManager.DisconnectMultimeter(SwitchingBusNew.AB1, userMessageService);
    }

    private async Task StartResistanceMeasurementTestOLD(CancellationToken cancellationToken, ISwitchingDevice device, IFastMeter meter, IUserInteractionService? userMessageService = null)
    {
      cancellationToken.ThrowIfCancellationRequested();
      await device.ConnectorManager.ConnectMultimeter(SwitchingBusNew.AB2, userMessageService);

      cancellationToken.ThrowIfCancellationRequested();
      await meter.ResistanceManager.SetResistanceModeAsync(userMessageService);

      cancellationToken.ThrowIfCancellationRequested();
      double result = await meter.ResistanceManager.MeasureResistanceAsync();

      cancellationToken.ThrowIfCancellationRequested();
      //await SelfTestHelper.IsCorrectRangeAsync(IdealResistance, result, "сопротивления", userMessageService);
      bool result_status = SelfTestHelper.InRange(IdealResistance, result, ResistanceRange());
      await SelfTestHelper.IsCorrectRangeAsync(result_status, result, "сопротивления", "Ом", userMessageService);

      cancellationToken.ThrowIfCancellationRequested();
      await device.ConnectorManager.DisconnectMultimeter(SwitchingBusNew.AB2, userMessageService);
    }

    private async Task StartCapacitanceMeasurementTest(CancellationToken cancellationToken, ISwitchingDevice device, IFastMeter meter, IUserInteractionService? userMessageService = null)
    {
      cancellationToken.ThrowIfCancellationRequested();
      await device.ConnectorManager.ConnectMultimeter(SwitchingBusNew.AB1, userMessageService);

      cancellationToken.ThrowIfCancellationRequested();
      await device.RelayManager.DisableRelay(userMessageService);

      cancellationToken.ThrowIfCancellationRequested();
      await meter.CapacitanceManager.SetCapacitanceModeAsync(userMessageService);

      cancellationToken.ThrowIfCancellationRequested();
      double result = await meter.CapacitanceManager.MeasureCapacitanceAsync(IdealCapacity);

      cancellationToken.ThrowIfCancellationRequested();
      //await SelfTestHelper.IsCorrectRangeAsync(IdealCapacity, result, "емкости", userMessageService);
      bool result_status = SelfTestHelper.InRange(IdealCapacity, result, CapacityRange());
      await SelfTestHelper.IsCorrectRangeAsync(result_status, result, "емкости", "нФ", userMessageService);

      await device.ConnectorManager.DisconnectMultimeter(SwitchingBusNew.AB1, userMessageService);
    }
  }
}

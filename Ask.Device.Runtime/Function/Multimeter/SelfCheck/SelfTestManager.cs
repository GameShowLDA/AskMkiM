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

    private static double VoltageTolerance(double voltage = 0) => (0.1 * voltage) + 0.02;
    private static double ResistanceTolerance(double resistance = 0, double fallibility = 1) => (fallibility / 100) * resistance; //(0.01 * resistance) + 0.1;
    private static double CapacityTolerance(double capacity = 0) => (0.01 * capacity) + 1;

    public Type GetTestTypeEnum()
    {
      return typeof(MultimeterTypeConnector);
    }
    public async Task StartSelfCheck(CancellationToken cancellationToken, Enum selectedType, IUserInteractionService? userMessageService = null, ISwitchingDevice device = null, IFastMeter meter = null)
    {
      await userMessageService.ShowMessageAsync(ExecutorMessageBuilder.BuildMultimeterSetupMessage());

      await device.ConnectableManager.InitializeAsync();
      await meter.ConnectableManager.InitializeAsync();

      await device.ConnectorManager.DisconnectAllBuses(userMessageService);

      switch (selectedType)
      {
        case MultimeterTypeConnector.Voltage:
          await StartVoltageMeasurementTestNEW(cancellationToken, device, meter, userMessageService);
          break;

        case MultimeterTypeConnector.Resistance:
          await StartResistanceMeasurementTestNEW(cancellationToken, device, meter, userMessageService);
          break;

        case MultimeterTypeConnector.Capacity:
          await StartCapacitanceMeasurementTestNEW(cancellationToken, device, meter, userMessageService);
          break;

        case MultimeterTypeConnector.FullCheck:
          await StartVoltageMeasurementTestNEW(cancellationToken, device, meter, userMessageService);
          await StartResistanceMeasurementTestNEW(cancellationToken, device, meter, userMessageService);
          await StartCapacitanceMeasurementTestNEW(cancellationToken, device, meter, userMessageService);
          break;
      }
      

      await device.ConnectorManager.DisconnectAllBuses(userMessageService);
    }

    private async Task StartVoltageMeasurementTestNEW(CancellationToken cancellationToken, ISwitchingDevice device, IFastMeter meter, IUserInteractionService? userMessageService = null)
    {
      cancellationToken.ThrowIfCancellationRequested();
      await device.ConnectorManager.ConnectMultimeter(SwitchingBusNew.AB1, userMessageService);

      Func<Task<double>> measureDcVoltage = () => meter.DcVoltageManager.MeasureDCVoltageAsync(userMessageService: userMessageService);
      Func<Task<double>> measureAcVoltage = () => meter.AcVoltageManager.MeasureACVoltageAsync(userMessageService: userMessageService);

      cancellationToken.ThrowIfCancellationRequested();
      await meter.DcVoltageManager.SetDCVoltageModeAsync(userMessageService);

      await device.RelayManager.EnableRelay(userMessageService);

      await VoltageMeasurement(cancellationToken, VoltageRange.mV_100, meter.DcVoltageManager.SetVoltageRangeAsync, measureDcVoltage, userMessageService);
      await VoltageMeasurement(cancellationToken, VoltageRange.V_1, meter.DcVoltageManager.SetVoltageRangeAsync, measureDcVoltage, userMessageService);
      await VoltageMeasurement(cancellationToken, VoltageRange.V_10, meter.DcVoltageManager.SetVoltageRangeAsync, measureDcVoltage, userMessageService);
      await VoltageMeasurement(cancellationToken, VoltageRange.V_100, meter.DcVoltageManager.SetVoltageRangeAsync, measureDcVoltage, userMessageService);
      await VoltageMeasurement(cancellationToken, VoltageRange.V_1000, meter.DcVoltageManager.SetVoltageRangeAsync, measureDcVoltage, userMessageService);

      cancellationToken.ThrowIfCancellationRequested();
      await meter.DcVoltageManager.SetVoltageRangeAsync(VoltageRange.Auto, userMessageService);

      cancellationToken.ThrowIfCancellationRequested();
      await meter.AcVoltageManager.SetACVoltageModeAsync(userMessageService);

      await VoltageMeasurement(cancellationToken, VoltageRange.mV_100, meter.AcVoltageManager.SetVoltageRangeAsync, measureAcVoltage, userMessageService);
      await VoltageMeasurement(cancellationToken, VoltageRange.V_1, meter.AcVoltageManager.SetVoltageRangeAsync, measureAcVoltage, userMessageService);
      await VoltageMeasurement(cancellationToken, VoltageRange.V_10, meter.AcVoltageManager.SetVoltageRangeAsync, measureAcVoltage, userMessageService);
      await VoltageMeasurement(cancellationToken, VoltageRange.V_100, meter.AcVoltageManager.SetVoltageRangeAsync, measureAcVoltage, userMessageService);
      await VoltageMeasurement(cancellationToken, VoltageRange.V_750, meter.AcVoltageManager.SetVoltageRangeAsync, measureAcVoltage, userMessageService);

      await device.RelayManager.DisableRelay(userMessageService);

      cancellationToken.ThrowIfCancellationRequested();
      await meter.AcVoltageManager.SetVoltageRangeAsync(VoltageRange.Auto, userMessageService);

      cancellationToken.ThrowIfCancellationRequested();
      await device.ConnectorManager.DisconnectMultimeter(SwitchingBusNew.AB1, userMessageService);
    }

    private async Task VoltageMeasurement(CancellationToken cancellationToken, VoltageRange range, Func<VoltageRange, IUserInteractionService?, Task<bool>> setVoltageRange, Func<Task<double>> measureVoltage, IUserInteractionService? userMessageService = null)
    {
      cancellationToken.ThrowIfCancellationRequested();
      await setVoltageRange(range, userMessageService);

      cancellationToken.ThrowIfCancellationRequested();
      double result = await measureVoltage();

      cancellationToken.ThrowIfCancellationRequested();
      bool resultStatus = SelfTestHelper.InRange(IdealVoltage, result, VoltageTolerance());
      await SelfTestHelper.IsCorrectRangeAsync(resultStatus, result, "напряжения", "В", userMessageService);
    }

    private async Task StartResistanceMeasurementTestNEW(CancellationToken cancellationToken, ISwitchingDevice device, IFastMeter meter, IUserInteractionService? userMessageService = null)
    {
      cancellationToken.ThrowIfCancellationRequested();
      await device.ConnectorManager.ConnectMultimeter(SwitchingBusNew.AB4, userMessageService);

      cancellationToken.ThrowIfCancellationRequested();
      await device.RelayManager.ConnectRCRelay(userMessageService);

      cancellationToken.ThrowIfCancellationRequested();
      await meter.ResistanceManager.SetResistanceModeAsync(userMessageService);

      await ResistanceMeasurement(cancellationToken, 1, 2, device, meter, 50, userMessageService);
      await ResistanceMeasurement(cancellationToken, 2, 100, device, meter, 1, userMessageService);
      await ResistanceMeasurement(cancellationToken, 3, 1_000, device, meter, 1, userMessageService);
      await ResistanceMeasurement(cancellationToken, 4, 10_000, device, meter, 1, userMessageService);
      await ResistanceMeasurement(cancellationToken, 5, 100_000, device, meter, 1, userMessageService);
      await ResistanceMeasurement(cancellationToken, 6, 1_000_000, device, meter, 1, userMessageService);
      await ResistanceMeasurement(cancellationToken, 7, 10_000_000, device, meter, 6, userMessageService);
      await ResistanceMeasurement(cancellationToken, 8, 85_000_000, device, meter, 1, userMessageService);

      cancellationToken.ThrowIfCancellationRequested();
      await device.RelayManager.DisconnectRCRelay(userMessageService);

      cancellationToken.ThrowIfCancellationRequested();
      await device.ConnectorManager.DisconnectMultimeter(SwitchingBusNew.AB4, userMessageService);
    }

    // fallibility - погрешность (в процентах)
    private async Task ResistanceMeasurement(CancellationToken cancellationToken, int numberResistor, int idealResult, ISwitchingDevice device, IFastMeter meter, int fallibility, IUserInteractionService? userMessageService = null)
    {
      cancellationToken.ThrowIfCancellationRequested();
      await device.RelayManager.ConnectResistor(numberResistor, userMessageService);

      cancellationToken.ThrowIfCancellationRequested();
      double result = await meter.ResistanceManager.MeasureResistanceAsync();

      cancellationToken.ThrowIfCancellationRequested();
      bool result_status = SelfTestHelper.InRange(idealResult, result, ResistanceTolerance(idealResult, fallibility));
      await SelfTestHelper.IsCorrectRangeAsync(result_status, result, "сопротивления", "Ом", userMessageService);

      cancellationToken.ThrowIfCancellationRequested();
      await device.RelayManager.DisconnectResistor(numberResistor, userMessageService);
    }

    private async Task StartCapacitanceMeasurementTestNEW(CancellationToken cancellationToken, ISwitchingDevice device, IFastMeter meter, IUserInteractionService? userMessageService = null)
    {
      cancellationToken.ThrowIfCancellationRequested();
      await device.ConnectorManager.ConnectMultimeter(SwitchingBusNew.AB4, userMessageService);

      cancellationToken.ThrowIfCancellationRequested();
      await device.RelayManager.ConnectRCRelay(userMessageService);

      cancellationToken.ThrowIfCancellationRequested();
      await meter.CapacitanceManager.SetCapacitanceModeAsync(userMessageService);

      await CapacitanceMeasurement(cancellationToken, 1, 3.3, device, meter, userMessageService);
      await CapacitanceMeasurement(cancellationToken, 2, 10, device, meter, userMessageService);
      await CapacitanceMeasurement(cancellationToken, 3, 100, device, meter, userMessageService);
      await CapacitanceMeasurement(cancellationToken, 4, 1_000, device, meter, userMessageService);
      await CapacitanceMeasurement(cancellationToken, 5, 6_800, device, meter, userMessageService);
      await CapacitanceMeasurement(cancellationToken, 6, 100_000, device, meter, userMessageService);

      cancellationToken.ThrowIfCancellationRequested();
      await device.RelayManager.DisconnectRCRelay(userMessageService);

      cancellationToken.ThrowIfCancellationRequested();
      await device.ConnectorManager.DisconnectMultimeter(SwitchingBusNew.AB4, userMessageService);
    }

    private async Task CapacitanceMeasurement(CancellationToken cancellationToken, int numberCapacitor, double idealResult, ISwitchingDevice device, IFastMeter meter, IUserInteractionService? userMessageService = null)
    {
      cancellationToken.ThrowIfCancellationRequested();
      await device.RelayManager.ConnectCapacitor(numberCapacitor, userMessageService);

      cancellationToken.ThrowIfCancellationRequested();
      double result = await meter.CapacitanceManager.MeasureCapacitanceAsync();

      cancellationToken.ThrowIfCancellationRequested();
      bool result_status = SelfTestHelper.InRange(idealResult, result, CapacityTolerance(idealResult));
      await SelfTestHelper.IsCorrectRangeAsync(result_status, result, "емкости", "нФ", userMessageService);

      cancellationToken.ThrowIfCancellationRequested();
      await device.RelayManager.DisconnectCapacitor(numberCapacitor, userMessageService);
    }
  }
}

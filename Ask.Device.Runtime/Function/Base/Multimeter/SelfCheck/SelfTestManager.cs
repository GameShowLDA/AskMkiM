using System;
using Ask.Core.Shared.DTO.Protocol;
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

    private static double VoltageTolerance(double voltage = 0) => (0.1 * voltage) + 0.02;
    private static double ResistanceTolerance(double resistance = 0, double fallibility = 1) => (fallibility / 100) * resistance; //(0.01 * resistance) + 0.1;
    private static double CapacityTolerance(double capacity = 0) => (0.05 * capacity) + 1;

    public Type GetTestTypeEnum()
    {
      return typeof(MultimeterTypeConnector);
    }
    public async Task StartSelfCheck(CancellationToken cancellationToken, Enum selectedType, IUserInteractionService? userMessageService = null, ISwitchingDevice device = null, IMultimeter meter = null)
    {
      ArgumentNullException.ThrowIfNull(userMessageService);
      ArgumentNullException.ThrowIfNull(device);
      ArgumentNullException.ThrowIfNull(meter);

      cancellationToken.ThrowIfCancellationRequested();
      await userMessageService.ShowMessageAsync(ExecutorMessageBuilder.BuildMultimeterSetupMessage());

      cancellationToken.ThrowIfCancellationRequested();
      await device.ConnectableManager.InitializeAsync(userMessageService);

      cancellationToken.ThrowIfCancellationRequested();
      await meter.ConnectableManager.InitializeAsync(userMessageService);

      try
      {
        cancellationToken.ThrowIfCancellationRequested();
        await device.ConnectorManager.DisconnectAllBuses(userMessageService);

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
      finally
      {
        await device.ConnectorManager.DisconnectAllBuses(userMessageService);
      }
    }

    private async Task StartVoltageMeasurementTest(CancellationToken cancellationToken, ISwitchingDevice device, IMultimeter meter, IUserInteractionService? userMessageService = null)
    {
      cancellationToken.ThrowIfCancellationRequested();
      await device.ConnectorManager.ConnectMultimeter(SwitchingBusNew.AB1, userMessageService);

      var relayEnabled = false;

      try
      {
        cancellationToken.ThrowIfCancellationRequested();
        await meter.DcVoltageManager.SetDCVoltageModeAsync(userMessageService);

        await device.RelayManager.EnableRelay(userMessageService);
        relayEnabled = true;

        await VoltageMeasurement(cancellationToken, 0.1, meter.DcVoltageManager.SetDCVoltageRangeAsync, meter.DcVoltageManager.MeasureDCVoltageAsync, userMessageService);
        await VoltageMeasurement(cancellationToken, 1, meter.DcVoltageManager.SetDCVoltageRangeAsync, meter.DcVoltageManager.MeasureDCVoltageAsync, userMessageService);
        await VoltageMeasurement(cancellationToken, 10, meter.DcVoltageManager.SetDCVoltageRangeAsync, meter.DcVoltageManager.MeasureDCVoltageAsync, userMessageService);
        await VoltageMeasurement(cancellationToken, 100, meter.DcVoltageManager.SetDCVoltageRangeAsync, meter.DcVoltageManager.MeasureDCVoltageAsync, userMessageService);
        await VoltageMeasurement(cancellationToken, 1000, meter.DcVoltageManager.SetDCVoltageRangeAsync, meter.DcVoltageManager.MeasureDCVoltageAsync, userMessageService);

        cancellationToken.ThrowIfCancellationRequested();
        await meter.DcVoltageManager.SetDCVoltageRangeAsync(0, userMessageService);

        cancellationToken.ThrowIfCancellationRequested();
        await meter.AcVoltageManager.SetACVoltageModeAsync(userMessageService);

        await VoltageMeasurement(cancellationToken, 0.1, meter.AcVoltageManager.SetACVoltageRangeAsync, meter.AcVoltageManager.MeasureACVoltageAsync, userMessageService);
        await VoltageMeasurement(cancellationToken, 1, meter.AcVoltageManager.SetACVoltageRangeAsync, meter.AcVoltageManager.MeasureACVoltageAsync, userMessageService);
        await VoltageMeasurement(cancellationToken, 10, meter.AcVoltageManager.SetACVoltageRangeAsync, meter.AcVoltageManager.MeasureACVoltageAsync, userMessageService);
        await VoltageMeasurement(cancellationToken, 100, meter.AcVoltageManager.SetACVoltageRangeAsync, meter.AcVoltageManager.MeasureACVoltageAsync, userMessageService);
        await VoltageMeasurement(cancellationToken, 750, meter.AcVoltageManager.SetACVoltageRangeAsync, meter.AcVoltageManager.MeasureACVoltageAsync, userMessageService);

        cancellationToken.ThrowIfCancellationRequested();
        await meter.AcVoltageManager.SetACVoltageRangeAsync(0, userMessageService);
      }
      finally
      {
        if (relayEnabled)
        {
          await device.RelayManager.DisableRelay(userMessageService);
        }

        await device.ConnectorManager.DisconnectMultimeter(SwitchingBusNew.AB1, userMessageService);
      }
    }

    private async Task VoltageMeasurement(CancellationToken cancellationToken, double range, Func<double, IUserInteractionService?, Task<bool>> setVoltageRange, Func<double, double, double, IUserInteractionService?, Task<double>> measureVoltage, IUserInteractionService? userMessageService = null)
    {
      cancellationToken.ThrowIfCancellationRequested();
      await setVoltageRange(range, userMessageService);

      cancellationToken.ThrowIfCancellationRequested();
      double result = await measureVoltage(0, -0.2, 0.2, userMessageService);

      cancellationToken.ThrowIfCancellationRequested();
      bool resultStatus = SelfTestHelper.InRange(IdealVoltage, result, VoltageTolerance());
      await SelfTestHelper.IsCorrectRangeAsync(resultStatus, result, "напряжения", "В", 0, 2, userMessageService);
      cancellationToken.ThrowIfCancellationRequested();
    }

    private async Task StartResistanceMeasurementTest(CancellationToken cancellationToken, ISwitchingDevice device, IMultimeter meter, IUserInteractionService? userMessageService = null)
    {
      cancellationToken.ThrowIfCancellationRequested();
      await device.ConnectorManager.ConnectMultimeter(SwitchingBusNew.AB4, userMessageService);

      var rcRelayConnected = false;

      try
      {
        cancellationToken.ThrowIfCancellationRequested();
        await device.RelayManager.ConnectRCRelay(userMessageService);
        rcRelayConnected = true;

        cancellationToken.ThrowIfCancellationRequested();
        await meter.ResistanceManager.SetResistanceModeAsync(userMessageService);

        await ResistanceMeasurement(cancellationToken, 1, 2, device, meter, 50, userMessageService);
        await ResistanceMeasurement(cancellationToken, 2, 100, device, meter, 1, userMessageService);
        await ResistanceMeasurement(cancellationToken, 3, 1_050, device, meter, 1, userMessageService);
        await ResistanceMeasurement(cancellationToken, 4, 10_000, device, meter, 1, userMessageService);
        await ResistanceMeasurement(cancellationToken, 5, 100_000, device, meter, 1, userMessageService);
        await ResistanceMeasurement(cancellationToken, 6, 1_000_000, device, meter, 1, userMessageService);
        await ResistanceMeasurement(cancellationToken, 7, 10_000_000, device, meter, 6, userMessageService);
        await ResistanceMeasurement(cancellationToken, 8, 86_000_000, device, meter, 1, userMessageService);
      }
      finally
      {
        if (rcRelayConnected)
        {
          await device.RelayManager.DisconnectRCRelay(userMessageService);
        }

        await device.ConnectorManager.DisconnectMultimeter(SwitchingBusNew.AB4, userMessageService);
      }
    }

    // fallibility - погрешность (в процентах)
    private async Task ResistanceMeasurement(CancellationToken cancellationToken, int numberResistor, int idealResult, ISwitchingDevice device, IMultimeter meter, int fallibility, IUserInteractionService? userMessageService = null)
    {
      cancellationToken.ThrowIfCancellationRequested();
      var resistorConnected = await device.RelayManager.ConnectResistor(numberResistor, userMessageService);

      try
      {
        double range = ResistanceTolerance(idealResult, fallibility);

        cancellationToken.ThrowIfCancellationRequested();
        double result = await meter.ResistanceManager.MeasureResistanceAsync(idealResult, idealResult - range, idealResult + range, userMessageService);

        cancellationToken.ThrowIfCancellationRequested();
        bool result_status = SelfTestHelper.InRange(idealResult, result, range);
        await SelfTestHelper.IsCorrectRangeAsync(result_status, result, "сопротивления", "Ом", idealResult, fallibility, userMessageService);

        cancellationToken.ThrowIfCancellationRequested();
      }
      finally
      {
        if (resistorConnected)
        {
          await device.RelayManager.DisconnectResistor(numberResistor, userMessageService);
        }
      }
    }

    private async Task StartCapacitanceMeasurementTest(CancellationToken cancellationToken, ISwitchingDevice device, IMultimeter meter, IUserInteractionService? userMessageService = null)
    {
      cancellationToken.ThrowIfCancellationRequested();
      await device.ConnectorManager.ConnectMultimeter(SwitchingBusNew.AB4, userMessageService);

      var rcRelayConnected = false;

      try
      {
        cancellationToken.ThrowIfCancellationRequested();
        await device.RelayManager.ConnectRCRelay(userMessageService);
        rcRelayConnected = true;

        await CapacitanceMeasurement(cancellationToken, 1, 3.3, device, meter, userMessageService: userMessageService);
        await CapacitanceMeasurement(cancellationToken, 2, 10, device, meter, userMessageService: userMessageService);
        await CapacitanceMeasurement(cancellationToken, 3, 130, device, meter, userMessageService: userMessageService);
        await CapacitanceMeasurement(cancellationToken, 4, 1_000, device, meter, userMessageService: userMessageService);
        //Неисправен
        //await CapacitanceMeasurement(cancellationToken, 5, 6_800, device, meter, userMessageService: userMessageService);
        await CapacitanceMeasurement(cancellationToken, 6, 86_000, device, meter, userMessageService: userMessageService);
      }
      finally
      {
        if (rcRelayConnected)
        {
          await device.RelayManager.DisconnectRCRelay(userMessageService);
        }

        await device.ConnectorManager.DisconnectMultimeter(SwitchingBusNew.AB4, userMessageService);
      }
    }

    // resultReactiveResistance - должен ли конденсатор пройти проверку реактивного сопротивления или должен её провалить
    private async Task CapacitanceMeasurement(CancellationToken cancellationToken, int numberCapacitor, double idealResult, ISwitchingDevice device, IMultimeter meter, IUserInteractionService? userMessageService = null)
    {
      cancellationToken.ThrowIfCancellationRequested();
      var capacitorConnected = await device.RelayManager.ConnectCapacitor(numberCapacitor, userMessageService);

      try
      {
        cancellationToken.ThrowIfCancellationRequested();
        await meter.ResistanceManager.SetResistanceModeAsync(userMessageService);

        cancellationToken.ThrowIfCancellationRequested();
        double result = await meter.ResistanceManager.MeasureResistanceAsync();
        var activeResistanceCorrect = result > 50;
        var resultType = activeResistanceCorrect
          ? ShowMessageModel.MessageType.Success
          : ShowMessageModel.MessageType.Error;
        var status = activeResistanceCorrect ? "НОРМА" : "БРАК";
        var meaning = MeasurementValueFormatter.IsOverloadValue(result) ? "Overload" : $"{result} Ом";

        await userMessageService.ShowMessageAsync(
           new ShowMessageModel(
             header: $"Тест активного сопротивления (>50 Ом)",
             message: $"{meaning} [{status}]",
             type: resultType));

        cancellationToken.ThrowIfCancellationRequested();
        if (!activeResistanceCorrect)
        {
          return;
        }

        await meter.CapacitanceManager.SetCapacitanceModeAsync(userMessageService);

        List<double> measuremend = new List<double>();
        double range = CapacityTolerance(idealResult);

        for (int i = 0; i < 6; i++)
        {
          cancellationToken.ThrowIfCancellationRequested();
          result = await meter.CapacitanceManager.MeasureCapacitanceAsync(idealResult, idealResult - range, idealResult + range, userMessageService: userMessageService);
          if (result > 0)
          {
            measuremend.Add(result);
          }
          else
          {
            i--;
          }
        }
        result = measuremend.Average();

        cancellationToken.ThrowIfCancellationRequested();
        bool result_status = SelfTestHelper.InRange(idealResult, result, range);
        await SelfTestHelper.IsCorrectRangeAsync(result_status, result, "емкости", "нФ", idealResult, 5, userMessageService);

        cancellationToken.ThrowIfCancellationRequested();
      }
      finally
      {
        if (capacitorConnected)
        {
          await device.RelayManager.DisconnectCapacitor(numberCapacitor, userMessageService);
        }
      }
    }
  }
}

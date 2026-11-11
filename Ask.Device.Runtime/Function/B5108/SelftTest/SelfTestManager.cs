using Ask.Core.Services.Errors.Device;
using Ask.Core.Services.UI;
using Ask.Core.Shared.DTO.Devices.Measurements;
using Ask.Core.Shared.DTO.Executor;
using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.PowerSupply;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.PowerSupply.Capabilities;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Device.ResponseProcessor.PowerSupply;
using System.ComponentModel;
using static Ask.LogLib.LoggerUtility;

namespace Ask.Device.Runtime.Function.B5108.SelftTest;

internal sealed class SelfTestManager : ISelfTestCheckerPowerSupply
{
  private enum TypeConnector
  {
    [Description("Проверка напряжения")]
    Voltage = 1,
  }

  /// <inheritdoc />
  public Type GetTestTypeEnum() => typeof(TypeConnector);

  /// <inheritdoc />
  public async Task StartSelfCheck(CancellationToken cancellationToken, Enum? selectedType, ActionSettings settings,
    IUserInteractionService userMessageService, IPowerSupply powerSupply, IMultimeter meter)
  {
    if (selectedType is not TypeConnector.Voltage)
      throw new ArgumentException("Выберите проверку напряжения Б5-108.", nameof(selectedType));

    cancellationToken.ThrowIfCancellationRequested();
    var deviceResult = new DeviceExecutionResult(powerSupply.Name, powerSupply.NumberChassis, powerSupply.Number);
    var test = new TestExecutionResult { TestName = "Проверка выходного напряжения" };
    settings.DeviceResults.Add(deviceResult);
    deviceResult.Tests.Add(test);

    await PowerSupplySelfTestMessages.PublishTitleAsync(powerSupply, userMessageService);
    await PowerSupplySelfTestMessages.PublishInformationAsync("Инициализация устройств", userMessageService);
    await InitializeDeviceAsync(powerSupply, userMessageService);
    cancellationToken.ThrowIfCancellationRequested();
    await InitializeDeviceAsync(meter, userMessageService);

    await PowerSupplySelfTestMessages.PublishInformationAsync(test.TestName, userMessageService);
    if (!await meter.DcVoltageManager.SetDCVoltageModeAsync(userMessageService))
      throw new DeviceException($"Не удалось установить режим измерения постоянного напряжения на {meter.Name}.");

    cancellationToken.ThrowIfCancellationRequested();
    Exception? testFailure = null;
    bool outputDisabled = false;
    try
    {
      await powerSupply.OutputManager.SetOutputStateAsync(false, cancellationToken, userMessageService);
      outputDisabled = true;
      await powerSupply.VoltageManager.SetVoltageAsync(0, cancellationToken, userMessageService);
      await powerSupply.CurrentManager.SetCurrentAsync(0.10m, cancellationToken, userMessageService);
      outputDisabled = false;
      await powerSupply.OutputManager.SetOutputStateAsync(true, cancellationToken, userMessageService);

      for (decimal voltage = 0.1m; voltage < 1m; voltage += 0.1m)
        await CheckVoltagePointAsync(voltage, test, powerSupply, meter, userMessageService, cancellationToken);

      for (decimal voltage = 1m; voltage < 10m; voltage++)
        await CheckVoltagePointAsync(voltage, test, powerSupply, meter, userMessageService, cancellationToken);

      for (decimal voltage = 10m; voltage <= powerSupply.VoltageManager.MaxVoltage; voltage += 10m)
        await CheckVoltagePointAsync(voltage, test, powerSupply, meter, userMessageService, cancellationToken);

      await powerSupply.OutputManager.SetOutputStateAsync(false, cancellationToken, userMessageService);
      outputDisabled = true;
      await powerSupply.VoltageManager.SetVoltageAsync(0, cancellationToken, userMessageService);
      await powerSupply.CurrentManager.SetCurrentAsync(0.10m, cancellationToken, userMessageService);

    }
    catch (Exception ex)
    {
      testFailure = ex;
      throw;
    }
    finally
    {
      using var finalization = EquipmentExecutionContext.EnterMandatoryFinalization();
      try
      {
        if (!outputDisabled)
          await powerSupply.OutputManager.SetOutputStateAsync(false);
      }
      catch (Exception ex) when (testFailure != null)
      {
        LogException(ex, customMessage: $"Не удалось выключить выход {powerSupply.Name} после сбоя самоконтроля.");
      }
    }
  }

  private static async Task CheckVoltagePointAsync(decimal voltage, TestExecutionResult test, IPowerSupply powerSupply,
    IMultimeter meter, IUserInteractionService userMessageService, CancellationToken cancellationToken)
  {
    cancellationToken.ThrowIfCancellationRequested();
    decimal tolerance = 0.01m + voltage * 0.01m;
    var expected = new MeasurementRange((double)voltage, (double)(voltage - tolerance), (double)(voltage + tolerance));

    await PowerSupplySelfTestMessages.PublishVoltagePointAsync(expected.TargetValue, userMessageService);
    await powerSupply.VoltageManager.SetVoltageAsync(voltage, cancellationToken, userMessageService);
    double measured = await meter.DcVoltageManager.MeasureDCVoltageAsync(expected, userMessageService);
    cancellationToken.ThrowIfCancellationRequested();

    bool passed = measured >= expected.LowerBound && measured <= expected.UpperBound;
    string? failure = null;
    if (!passed)
    {
      failure = PowerSupplySelfTestMessages.DescribeVoltageFailure(expected, measured);
      test.Errors.Add(new TestError { Message = failure });
    }

    await PowerSupplySelfTestMessages.PublishVoltageResultAsync(expected, measured, passed, failure, userMessageService);
  }

  private static async Task InitializeDeviceAsync(IDevice device, IUserInteractionService userMessageService)
  {
    var (connected, answer) = await device.ConnectableManager.InitializeAsync(userMessageService);
    if (!connected)
      throw new DeviceException($"Не удалось инициализировать {device.Name}: {answer}");
  }
}

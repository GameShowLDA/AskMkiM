using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.BreakdownTester;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.BreakdownTester.Capabilities;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.SwitchingDevice;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums.Commands;
using Ask.Core.Shared.Metadata.Static;
using Ask.Core.Shared.Metadata.Static.Messages;
using System.ComponentModel;

namespace Ask.Device.Runtime.Function.GPT.SelfCheck
{
  public class SelfTestManager : ISelfTestCheckerBreakdownTester
  {
    /// <summary>
    /// Тип проверки цепи самоконтроля.
    /// </summary>
    public enum TypeConnector
    {
      /// <summary>
      /// Полная проверка всех цепей устройства самоконтроля.
      /// Используется для последовательного запуска всех поддерживаемых тестов.
      /// </summary>
      [Description("Полная проверка устройства")]
      FullCheck = 0,

      [Description("Проверка переменного напряжения")]
      ACW = 1,

      [Description("Проверка постоянного напряжения")]
      DCW = 2,

      [Description("Проверка сопротивления изоляции")]
      IR = 3,
    }

    public async Task StartSelfCheck(CancellationToken cancellationToken, System.Enum selectedType, IUserInteractionService? userMessageService = null, IBreakdownTester breakdownTester = null, ISwitchingDevice device = null, IFastMeter meter = null)
    {
      await userMessageService.ShowMessageAsync(ExecutorMessageBuilder.BuildDeviceHealthCheckTitle(breakdownTester));
      await InitDevices(userMessageService, device, meter, breakdownTester);

      await device.ConnectorManager.ConnectBreakdownTester(userMessageService);
      await device.ConnectorManager.EnableDivider(userMessageService);

      switch (selectedType)
      {
        case TypeConnector.IR:
          await PerformIrCheckAsync(cancellationToken, breakdownTester, device, meter, userMessageService);
          break;

        case TypeConnector.ACW:
          await PerformAcwCheckAsync(cancellationToken, breakdownTester, device, meter, userMessageService);
          break;

        case TypeConnector.DCW:
          await PerformDcwCheckAsync(cancellationToken, breakdownTester, device, meter, userMessageService);
          break;

        case TypeConnector.FullCheck:
          await PerformIrCheckAsync(cancellationToken, breakdownTester, device, meter, userMessageService);
          await Task.Delay(500);

          await PerformDcwCheckAsync(cancellationToken, breakdownTester, device, meter, userMessageService);
          await Task.Delay(500);

          await PerformAcwCheckAsync(cancellationToken, breakdownTester, device, meter, userMessageService);
          await Task.Delay(500);
          break;
      }

      await device.ConnectorManager.DisconnectBreakdownTester(userMessageService);
      await device.ConnectorManager.DisableDivider(userMessageService);
    }


    /// <summary>
    /// Выполняет самопроверку режима IR (сопротивление изоляции).
    /// </summary>
    private async Task PerformIrCheckAsync(
      CancellationToken cancellationToken,
      IBreakdownTester breakdownTester,
      ISwitchingDevice device,
      IFastMeter meter,
      IUserInteractionService? userMessageService = null)
    {
      try
      {
        string name = breakdownTester.Name;
        int numberChassis = breakdownTester.NumberChassis;
        int number = breakdownTester.Number;
        cancellationToken.ThrowIfCancellationRequested();

        await userMessageService.AppendEmptyLineAsync();
        await userMessageService.ShowMessageAsync(new ShowMessageModel("Проверка измерения соопротивления изоляции"));
        await userMessageService.ShowMessageAsync(new ShowMessageModel("Настройка оборудования"));

        await breakdownTester.IrManger.Mode.SetModeAsync(userMessageService);
        await breakdownTester.IrManger.Time.SetTestTimeAsync(1, userMessageService);
        await breakdownTester.IrManger.Time.SetRampTimeAsync(0.1, userMessageService);

        await meter.DcVoltageManager.SetDCVoltageModeAsync(userMessageService);

        List<int> voltage = new List<int>() { 100, 500, 1000 };
        int param = 10;

        foreach (var item in voltage)
        {
          cancellationToken.ThrowIfCancellationRequested();
          await userMessageService.AppendEmptyLineAsync();
          await userMessageService.ShowMessageAsync(new ShowMessageModel($"Проверка при напряжении {item}В") { IndentLevel = 1 });
          await breakdownTester.IrManger.Voltage.SetVoltageAsync(item, userMessageService);

          (var lowerBound, var upperBound, var delta) = MeasurementErrorDefaults.CalculateToleranceRange(MeasurementTypeCommand.SI, param);
          var result = (await breakdownTester.IrManger.Measure.MeasureAsync(param, lowerBound, upperBound, userMessageService: userMessageService)).value;

          var err = result - param;
          await userMessageService.ShowMessageAsync(new ShowMessageModel("Результат измерения сопротивления изоляции", message: MeasurementValueFormatter.FormatWithUnit(result, "МОм"), type: result >= lowerBound && result <= upperBound ? ShowMessageModel.MessageType.Success : ShowMessageModel.MessageType.Error) { IndentLevel = 1 }, skipPause: true);
          await userMessageService.ShowMessageAsync(new ShowMessageModel($"Погрешность измерения ({lowerBound} - {upperBound} МОм)", message: MeasurementValueFormatter.FormatWithUnit(err, "МОм"), type: result >= lowerBound && result <= upperBound ? ShowMessageModel.MessageType.Success : ShowMessageModel.MessageType.Error) { IndentLevel = 2 }, skipPause: true);

        }
      }
      catch (Exception)
      {
      }
    }

    /// <summary>
    /// Выполняет самопроверку режима ACW (переменное напряжение).
    /// </summary>
    private async Task PerformAcwCheckAsync(
      CancellationToken cancellationToken,
      IBreakdownTester breakdownTester,
      ISwitchingDevice device,
      IFastMeter meter,
      IUserInteractionService? userMessageService = null)
    {
      try
      {
        string name = breakdownTester.Name;
        int numberChassis = breakdownTester.NumberChassis;
        int number = breakdownTester.Number;
        cancellationToken.ThrowIfCancellationRequested();

        await userMessageService.AppendEmptyLineAsync();
        await userMessageService.ShowMessageAsync(new ShowMessageModel("Проверка напряжения ACW с ППУ"));
        await userMessageService.ShowMessageAsync(new ShowMessageModel("Настройка оборудования"));

        await breakdownTester.AcwManger.Mode.SetModeAsync(userMessageService);
        await breakdownTester.AcwManger.Time.SetTestTimeAsync(5, userMessageService);
        await breakdownTester.AcwManger.Time.SetRampTimeAsync(0.1, userMessageService);

        await meter.AcVoltageManager.SetACVoltageModeAsync(userMessageService);

        List<int> voltage = new List<int>() { 100, 200, 400, 500, 600, 700 };

        foreach (var item in voltage)
        {
          cancellationToken.ThrowIfCancellationRequested();
          await userMessageService.AppendEmptyLineAsync();
          await userMessageService.ShowMessageAsync(new ShowMessageModel($"Проверка при напряжении {item}В") { IndentLevel = 1 });
          await breakdownTester.AcwManger.Voltage.SetVoltageAsync(item, userMessageService);

          var bound = item / 100 * 5;
          (var lowerBound, var upperBound) = (item - bound, item + bound);

          await breakdownTester.AcwManger.Measure.ApplyVoltageAsync();

          await Task.Delay(1000);

          var result = await meter.AcVoltageManager.MeasureACVoltageAsync(item, lowerBound, upperBound);
          result *= 10;
          result += item / 100 * meter.AcwPpuDividerCoefficientPercent;

          await breakdownTester.AcwManger.Measure.StopMeasure();

          var err = result - item;
          await userMessageService.ShowMessageAsync(new ShowMessageModel("Результат ACW", message: MeasurementValueFormatter.FormatWithUnit(result, "В"), type: result >= lowerBound && result <= upperBound ? ShowMessageModel.MessageType.Success : ShowMessageModel.MessageType.Error) { IndentLevel = 1 }, skipPause: true);
          await userMessageService.ShowMessageAsync(new ShowMessageModel($"Погрешность измерения ({lowerBound} - {upperBound} В)", message: MeasurementValueFormatter.FormatWithUnit(err, "В"), type: result >= lowerBound && result <= upperBound ? ShowMessageModel.MessageType.Success : ShowMessageModel.MessageType.Error) { IndentLevel = 2 }, skipPause: true);

        }
      }
      catch (Exception)
      {
      }
    }

    /// <summary>
    /// Выполняет самопроверку режима DCW (постоянное напряжение).
    /// </summary>
    private async Task PerformDcwCheckAsync(
      CancellationToken cancellationToken,
      IBreakdownTester breakdownTester,
      ISwitchingDevice device,
      IFastMeter meter,
      IUserInteractionService? userMessageService = null)
    {
      try
      {
        string name = breakdownTester.Name;
        int numberChassis = breakdownTester.NumberChassis;
        int number = breakdownTester.Number;
        cancellationToken.ThrowIfCancellationRequested();

        await userMessageService.AppendEmptyLineAsync();
        await userMessageService.ShowMessageAsync(new ShowMessageModel("Проверка напряжения DCW с ППУ"));
        await userMessageService.ShowMessageAsync(new ShowMessageModel("Настройка оборудования"));

        await breakdownTester.DcwManger.Mode.SetModeAsync(userMessageService);
        await breakdownTester.DcwManger.Time.SetTestTimeAsync(5, userMessageService);
        await breakdownTester.DcwManger.Time.SetRampTimeAsync(0.1, userMessageService);

        await meter.DcVoltageManager.SetDCVoltageModeAsync(userMessageService);

        List<int> voltage = new List<int>() { 100, 200, 400, 500, 600, 700 };

        foreach (var item in voltage)
        {
          cancellationToken.ThrowIfCancellationRequested();
          await userMessageService.AppendEmptyLineAsync();
          await userMessageService.ShowMessageAsync(new ShowMessageModel($"Проверка при напряжении {item}В") { IndentLevel = 1 });
          await breakdownTester.DcwManger.Voltage.SetVoltageAsync(item, userMessageService);

          var bound = item / 100 * 5;
          (var lowerBound, var upperBound) = (item - bound, item + bound);

          await breakdownTester.DcwManger.Measure.ApplyVoltageAsync();

          await Task.Delay(1000);

          var result = await meter.DcVoltageManager.MeasureDCVoltageAsync(item, lowerBound, upperBound);
          result *= 10;
          result += item / 100 * meter.DcwPpuDividerCoefficientPercent;
          await breakdownTester.DcwManger.Measure.StopMeasure();

          var err = result - item;
          await userMessageService.ShowMessageAsync(new ShowMessageModel("Результат DCW", message: MeasurementValueFormatter.FormatWithUnit(result, "В"), type: result >= lowerBound && result <= upperBound ? ShowMessageModel.MessageType.Success : ShowMessageModel.MessageType.Error) { IndentLevel = 1 }, skipPause: true);
          await userMessageService.ShowMessageAsync(new ShowMessageModel($"Погрешность измерения ({lowerBound} - {upperBound} В)", message: MeasurementValueFormatter.FormatWithUnit(err, "В"), type: result >= lowerBound && result <= upperBound ? ShowMessageModel.MessageType.Success : ShowMessageModel.MessageType.Error) { IndentLevel = 2 }, skipPause: true);

        }
      }
      catch (Exception)
      {
      }
    }

    public Type GetTestTypeEnum()
    {
      return typeof(TypeConnector);
    }

    private async Task InitDevices(IUserInteractionService userMessageService, ISwitchingDevice switchingDevice, IFastMeter meter, IBreakdownTester breakdownTester)
    {
      string name = breakdownTester.Name;
      int numberChassis = breakdownTester.NumberChassis;
      int number = breakdownTester.Number;

      await userMessageService.ShowMessageAsync(new ShowMessageModel("Инициализация устройств"));
      await breakdownTester.ConnectableManager.InitializeAsync(userMessageService);
      await meter.ConnectableManager.InitializeAsync(userMessageService);
      await switchingDevice.ConnectableManager.InitializeAsync(userMessageService);
    }
  }
}

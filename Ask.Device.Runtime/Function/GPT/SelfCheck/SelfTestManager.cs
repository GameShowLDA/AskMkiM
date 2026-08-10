using Ask.Core.Shared.Metadata.Enums.FileEnums;
using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Shared.DTO.Devices.Measurements;
using Ask.Core.Shared.DTO.Executor;
using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.BreakdownTester;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.BreakdownTester.Capabilities;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.SwitchingDevice;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums.Commands;
using Ask.Core.Shared.Metadata.Enums.UnitEnums;
using Ask.Core.Shared.Metadata.Static;
using Ask.Core.Shared.Metadata.Static.Messages;
using System.ComponentModel;
using static Ask.Core.Shared.DTO.Protocol.ShowMessageModel;
using static Ask.Device.Runtime.Function.GPT.SelfCheck.SelfTestManager;
using EquipmentMessages = Ask.Device.ResponseProcessor.BreakdownTester.ResponseProcessing.BreakdownTesterMessages;
using MeasurementMessages = Ask.Device.ResponseProcessor.BreakdownTester.ResponseProcessing.BreakdownTesterMessages;
using SelfTestMessages = Ask.Device.ResponseProcessor.BreakdownTester.ResponseProcessing.BreakdownTesterMessages;

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

    /// <inheritdoc />
    public async Task StartSelfCheck(CancellationToken cancellationToken, System.Enum selectedType, ActionSettings settings, IUserInteractionService? userMessageService = null, IBreakdownTester breakdownTester = null, ISwitchingDevice device = null, IMultimeter meter = null)
    {
      settings.DeviceResults.Add(new DeviceExecutionResult(breakdownTester.Name, breakdownTester.NumberChassis, breakdownTester.Number));

      await EquipmentMessages.PublishDeviceHealthCheckTitleAsync(breakdownTester, userMessageService);
      await InitDevices(userMessageService, device, meter, breakdownTester);

      await device.ConnectorManager.ConnectBreakdownTester(userMessageService);
      await device.ConnectorManager.EnableDivider(userMessageService);

      switch (selectedType)
      {
        case TypeConnector.IR:
          await PerformIrCheckAsync(cancellationToken, breakdownTester, device, meter, settings, userMessageService);
          break;

        case TypeConnector.ACW:
          await PerformAcwCheckAsync(cancellationToken, breakdownTester, device, meter, settings, userMessageService);
          break;

        case TypeConnector.DCW:
          await PerformDcwCheckAsync(cancellationToken, breakdownTester, device, meter, settings, userMessageService);
          break;

        case TypeConnector.FullCheck:
          await PerformIrCheckAsync(cancellationToken, breakdownTester, device, meter, settings, userMessageService, 1.ToString());
          await Task.Delay(500);

          await PerformDcwCheckAsync(cancellationToken, breakdownTester, device, meter, settings, userMessageService, 2.ToString());
          await Task.Delay(500);

          await PerformAcwCheckAsync(cancellationToken, breakdownTester, device, meter, settings, userMessageService, 3.ToString());
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
      IMultimeter meter,
      ActionSettings settings,
      IUserInteractionService? userMessageService = null,
      string testNumber = null)
    {
      try
      {
        string name = breakdownTester.Name;
        int numberChassis = breakdownTester.NumberChassis;
        int number = breakdownTester.Number;
        cancellationToken.ThrowIfCancellationRequested();

        await userMessageService.AppendEmptyLineAsync();
        var testName = "Тест измерения сопротивления изоляции";
        settings.DeviceResults.LastOrDefault()?.Tests.Add(new TestExecutionResult { TestName = testName, });
        if (!string.IsNullOrEmpty(testNumber))
        {
          testName = $"{testNumber}. {testName}";
        }
        await SelfTestMessages.PublishInformationAsync("Проверка измерения соопротивления изоляции", userMessageService);
        await SelfTestMessages.PublishInformationAsync("Настройка оборудования", userMessageService);

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
          await SelfTestMessages.PublishInformationAsync(
            $"Проверка при напряжении {item}В",
            userMessageService,
            indentLevel: 1);
          await breakdownTester.IrManger.Voltage.SetVoltageAsync(item, userMessageService);

          (var lowerBound, var upperBound, var delta) = MeasurementErrorDefaults.CalculateToleranceRange(MeasurementTypeCommand.SI, param);

          MeasurementRange measurementRange = new MeasurementRange(param, lowerBound, upperBound);
          var result = (await breakdownTester.IrManger.Measure.MeasureAsync(ElectricalTestFunction.InsulationResistance, measurementRange)).value;

          var err = result - param;
          bool isSuccessful = result >= lowerBound && result <= upperBound;
          var formattedResult = MeasurementValueFormatter.FormatWithUnit(result, "МОм");
          string? executionErrorMessage = !isSuccessful
            ? $"СИ. Проверка при напряжении {item}В " +
              $"({lowerBound} - {upperBound} МОм) : {formattedResult}"
            : null;
          if (executionErrorMessage != null)
          {
            settings.DeviceResults[0].Tests.LastOrDefault()?.Errors.Add(new TestError
            {
              Message = executionErrorMessage,
            });
          }

          await MeasurementMessages.PublishMeasurementResultAsync(CheckType.SelfTest,
            ResistanceUnit.MegaOhm,
            new MeasurementRange(result, lowerBound, upperBound),
            isSuccessful,
            $"Проверка при напряжении {item}В",
            executionErrorMessage,
            outputService: userMessageService);

          await MeasurementMessages.PublishMeasurementErrorAsync(CheckType.SelfTest,
            ResistanceUnit.MegaOhm,
            new MeasurementRange(err, lowerBound, upperBound),
            isSuccessful,
            userMessageService,
            showAllowedRange: true,
            executionErrorMessage: string.Empty);

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
      IMultimeter meter,
      ActionSettings settings,
      IUserInteractionService? userMessageService = null,
      string testNumber = null)
    {
      try
      {
        string name = breakdownTester.Name;
        int numberChassis = breakdownTester.NumberChassis;
        int number = breakdownTester.Number;
        cancellationToken.ThrowIfCancellationRequested();

        await userMessageService.AppendEmptyLineAsync();
        var testName = "Тест измерения напряжения ACW";
        settings.DeviceResults.LastOrDefault()?.Tests.Add(new TestExecutionResult { TestName = testName, });
        if (!string.IsNullOrEmpty(testNumber))
        {
          testName = $"{testNumber}. {testName}";
        }
        await SelfTestMessages.PublishInformationAsync(testName, userMessageService);
        await SelfTestMessages.PublishInformationAsync("Настройка оборудования", userMessageService);

        await breakdownTester.AcwManger.Mode.SetModeAsync(userMessageService);
        await breakdownTester.AcwManger.Time.SetTestTimeAsync(5, userMessageService);
        await breakdownTester.AcwManger.Time.SetRampTimeAsync(0.1, userMessageService);

        await meter.AcVoltageManager.SetACVoltageModeAsync(userMessageService);

        List<int> voltage = new List<int>() { 100, 200, 400, 500, 600, 700 };

        foreach (var item in voltage)
        {
          cancellationToken.ThrowIfCancellationRequested();
          await userMessageService.AppendEmptyLineAsync();
          await SelfTestMessages.PublishInformationAsync(
            $"Проверка при напряжении {item}В",
            userMessageService,
            indentLevel: 1);
          await breakdownTester.AcwManger.Voltage.SetVoltageAsync(item, userMessageService);

          var bound = item / 100 * 5;
          (var lowerBound, var upperBound) = (item - bound, item + bound);

          await breakdownTester.AcwManger.Measure.ApplyVoltageAsync();

          await Task.Delay(1000);

          MeasurementRange measurementRangeAc = new MeasurementRange(item, lowerBound, upperBound);
          var result = await meter.AcVoltageManager.MeasureACVoltageAsync(measurementRangeAc);
          if (!ExecutionConfig.GetIsIdleModeEnabled())
          {
            result *= 10;
            result += item / 100 * meter.AcwPpuDividerCoefficientPercent;
          }

          await breakdownTester.AcwManger.Measure.StopMeasure();

          var err = result - item;
          bool isSuccessful = result >= lowerBound && result <= upperBound;
          var formattedResult = MeasurementValueFormatter.FormatWithUnit(result, "В");
          string? executionErrorMessage = !isSuccessful
            ? $"ПИ ACW. Проверка при напряжении {item}В " +
              $"({lowerBound} - {upperBound} В) : {formattedResult}"
            : null;
          await SelfTestMessages.PublishResultAsync(
            "Результат ACW",
            isSuccessful,
            userMessageService,
            message: formattedResult,
            indentLevel: 1,
            executionErrorMessage);

          if (executionErrorMessage != null)
          {
            settings.DeviceResults[0].Tests.LastOrDefault()?.Errors.Add(new TestError
            {
              Message = executionErrorMessage,
            });
          }

          await MeasurementMessages.PublishMeasurementErrorAsync(CheckType.SelfTest,
            VoltageUnit.Volt,
            new MeasurementRange(err, lowerBound, upperBound),
            isSuccessful,
            userMessageService,
            showAllowedRange: true,
            executionErrorMessage: string.Empty);

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
      IMultimeter meter,
      ActionSettings settings,
      IUserInteractionService? userMessageService = null,
      string testNumber = null)
    {
      try
      {
        string name = breakdownTester.Name;
        int numberChassis = breakdownTester.NumberChassis;
        int number = breakdownTester.Number;
        cancellationToken.ThrowIfCancellationRequested();

        await userMessageService.AppendEmptyLineAsync();
        var testName = "Тест измерения напряжения DCW";
        settings.DeviceResults.LastOrDefault()?.Tests.Add(new TestExecutionResult { TestName = testName, });
        if (!string.IsNullOrEmpty(testNumber))
        {
          testName = $"{testNumber}. {testName}";
        }
        await SelfTestMessages.PublishInformationAsync(testName, userMessageService);
        await SelfTestMessages.PublishInformationAsync("Настройка оборудования", userMessageService);

        await breakdownTester.DcwManger.Mode.SetModeAsync(userMessageService);
        await breakdownTester.DcwManger.Time.SetTestTimeAsync(5, userMessageService);
        await breakdownTester.DcwManger.Time.SetRampTimeAsync(0.1, userMessageService);

        await meter.DcVoltageManager.SetDCVoltageModeAsync(userMessageService);

        List<int> voltage = new List<int>() { 100, 200, 400, 500, 600, 700 };

        foreach (var item in voltage)
        {
          cancellationToken.ThrowIfCancellationRequested();
          await userMessageService.AppendEmptyLineAsync();
          await SelfTestMessages.PublishInformationAsync(
            $"Проверка при напряжении {item}В",
            userMessageService,
            indentLevel: 1);
          await breakdownTester.DcwManger.Voltage.SetVoltageAsync(item, userMessageService);

          var bound = item / 100 * 5;
          (var lowerBound, var upperBound) = (item - bound, item + bound);

          await breakdownTester.DcwManger.Measure.ApplyVoltageAsync();

          await Task.Delay(1000);

          MeasurementRange measurementRange = new MeasurementRange(item, lowerBound, upperBound);
          var result = await meter.DcVoltageManager.MeasureDCVoltageAsync(measurementRange);
          if (!ExecutionConfig.GetIsIdleModeEnabled())
          {
            result *= 10;
            result += item / 100 * meter.DcwPpuDividerCoefficientPercent;
          }
          await breakdownTester.DcwManger.Measure.StopMeasure();

          var err = result - item;
          bool isSuccessful = result >= lowerBound && result <= upperBound;
          var formattedResult = MeasurementValueFormatter.FormatWithUnit(result, "В");
          string? executionErrorMessage = !isSuccessful
            ? $"ПИ DCW. Проверка при напряжении {item}В " +
              $"({lowerBound} - {upperBound} В) : {formattedResult}"
            : null;
          if (executionErrorMessage != null)
          {
            settings.DeviceResults[0].Tests.LastOrDefault()?.Errors.Add(new TestError
            {
              Message = executionErrorMessage,
            });
          }
          await SelfTestMessages.PublishResultAsync(
            "Результат DCW",
            isSuccessful,
            userMessageService,
            message: formattedResult,
            indentLevel: 1,
            executionErrorMessage);


          await MeasurementMessages.PublishMeasurementErrorAsync(CheckType.SelfTest,
            VoltageUnit.Volt,
            new MeasurementRange(err, lowerBound, upperBound),
            isSuccessful,
            userMessageService,
            showAllowedRange: true,
            executionErrorMessage: string.Empty);

        }
      }
      catch (Exception)
      {
      }
    }

    /// <inheritdoc />
    public Type GetTestTypeEnum()
    {
      return typeof(TypeConnector);
    }

    /// <summary>
    /// Выполняет инициализацию пробойной установки, мультиметра
    /// и коммутационного устройства.
    /// </summary>
    /// <param name="userMessageService">Сервис взаимодействия с пользователем.</param>
    /// <param name="switchingDevice">Коммутационное устройство.</param>
    /// <param name="meter">Мультиметр.</param>
    /// <param name="breakdownTester">Пробойная установка.</param>
    /// <returns>Асинхронная задача инициализации устройств.</returns>
    private async Task InitDevices(IUserInteractionService userMessageService, ISwitchingDevice switchingDevice, IMultimeter meter, IBreakdownTester breakdownTester)
    {
      string name = breakdownTester.Name;
      int numberChassis = breakdownTester.NumberChassis;
      int number = breakdownTester.Number;

      await SelfTestMessages.PublishInformationAsync("Инициализация устройств", userMessageService);
      await breakdownTester.ConnectableManager.InitializeAsync(userMessageService);
      await meter.ConnectableManager.InitializeAsync(userMessageService);
      await switchingDevice.ConnectableManager.InitializeAsync(userMessageService);
    }
  }
}

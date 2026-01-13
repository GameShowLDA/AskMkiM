using Ask.Core.Services.Errors.Device.Adapters;
using Ask.Core.Services.Errors.Device.Breakdown;
using Ask.Core.Services.Errors.Device.Multimeter;
using Ask.Core.Services.UI;
using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.BreakdownTester;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.BreakdownTester.Capabilities;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.SwitchingDevice;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using System.ComponentModel;

namespace NewCore.Function.GPT.SelfCheck
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

      /// <summary>
      /// Аналого-цифровой преобразователь (АЦП), используется для измерения входного сигнала.
      /// Подключение: разъем XS3.
      /// </summary>
      [Description("Проверка переменного напряжения")]
      ACW = 1,

      /// <summary>
      /// Аналого-цифровой преобразователь (АЦП) с переполюсовкой,
      /// предназначен для измерения сигнала с измененной полярностью.
      /// Подключение: разъем XS3.
      /// </summary>
      [Description("Проверка постоянного напряжения")]
      DCW = 2,
    }

    public async Task StartSelfCheck(CancellationToken cancellationToken, System.Enum selectedType, IUserInteractionService? userMessageService = null, IBreakdownTester breakdownTester = null, ISwitchingDevice device = null, IFastMeter meter = null)
    {
      switch (selectedType)
      {
        case TypeConnector.ACW:
          await InitDevices(userMessageService, device, meter, breakdownTester);
          await PerformAcwCheckAsync(cancellationToken, breakdownTester, device, meter, userMessageService);
          break;

        case TypeConnector.DCW:
          await InitDevices(userMessageService, device, meter, breakdownTester);
          await PerformDcwCheckAsync(cancellationToken, breakdownTester, device, meter, userMessageService);
          break;

        case TypeConnector.FullCheck:
          await InitDevices(userMessageService, device, meter, breakdownTester);
          await PerformAcwCheckAsync(cancellationToken, breakdownTester, device, meter, userMessageService);
          await Task.Delay(1000);
          await PerformDcwCheckAsync(cancellationToken, breakdownTester, device, meter, userMessageService);
          break;
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

        await userMessageService.ShowMessageAsync(new ShowMessageModel("Настройка для проверки пременного напряжения"));
        await breakdownTester.AcwManger.Mode.SetModeAsync(userMessageService);
        await breakdownTester.AcwManger.Time.SetTestTimeAsync(1, userMessageService);
        await breakdownTester.AcwManger.CurrentLimits.SetHighCurrentLimitAsync(10, userMessageService);
        await breakdownTester.AcwManger.Time.SetRampTimeAsync(0.1, userMessageService);
        await meter.AcVoltageManager.SetACVoltageModeAsync(userMessageService);

        await device.ConnectorManager.ConnectBreakdownTesterAndMultimeter(userMessageService);

        for (int i = 100; i <= 500; i += 100)
        {
          await userMessageService.ShowMessageAsync(new ShowMessageModel($"Проверка напряжения {i}В") { IndentLevel = 1 });
          await Task.Delay(50);

          await breakdownTester.AcwManger.Voltage.SetVoltageAsync(i, userMessageService);

          await breakdownTester.AcwManger.Measure.ApplyVoltageAsync(userMessageService);
          await Task.Delay(150);

          var resultMeter = await meter.AcVoltageManager.MeasureACVoltageAsync(i, userMessageService);

          await breakdownTester.AcwManger.Measure.StopMeasure();
        }
        await device.ConnectorManager.DisconnectBreakdownTesterAndMultimeter(userMessageService);
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
      string name = breakdownTester.Name;
      int numberChassis = breakdownTester.NumberChassis;
      int number = breakdownTester.Number;

      await userMessageService.ShowMessageAsync(new ShowMessageModel("Настройка для проверки пременного напряжения"));
      if (!await UserActionHelper.GetRunWithUserRepeatAsync(async () => (await breakdownTester.DcwManger.Mode.SetModeAsync(userMessageService)).Success, userMessageService))
      {
        throw IrExceptionFactory.SetModeFailed(name, numberChassis, number);
      }

      await breakdownTester.DcwManger.Time.SetTestTimeAsync(1, userMessageService);
      await breakdownTester.DcwManger.CurrentLimits.SetHighCurrentLimitAsync(10, userMessageService);
      await breakdownTester.DcwManger.Time.SetRampTimeAsync(0.1, userMessageService);

      await meter.DcVoltageManager.SetDCVoltageModeAsync(userMessageService);

      await device.ConnectorManager.ConnectBreakdownTesterAndMultimeter(userMessageService);

      for (int i = 100; i <= 500; i += 100)
      {
        await userMessageService.ShowMessageAsync(new ShowMessageModel($"Проверка напряжения {i}В") { IndentLevel = 1 });
        await Task.Delay(50);
        await breakdownTester.DcwManger.Voltage.SetVoltageAsync(i, userMessageService);
        await breakdownTester.DcwManger.Measure.ApplyVoltageAsync(userMessageService);
        await Task.Delay(200);

        var resultMeter = await meter.DcVoltageManager.MeasureDCVoltageAsync(i, userMessageService);

        await breakdownTester.DcwManger.Measure.StopMeasure();
      }
      await device.ConnectorManager.DisconnectBreakdownTesterAndMultimeter(userMessageService);
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
      if (!await UserActionHelper.GetRunWithUserRepeatAsync(async () => (await breakdownTester.ConnectableManager.InitializeAsync(userMessageService)).Connect, userMessageService))
      {
        throw ConnectionExceptionAdapter.ConnectFailed(name, numberChassis, number);
      }
      if (!await UserActionHelper.GetRunWithUserRepeatAsync(async () => (await meter.ConnectableManager.InitializeAsync(userMessageService)).Connect, userMessageService))
      {
        throw ConnectionExceptionAdapter.ConnectFailed(name, numberChassis, number);
      }
      if (!await UserActionHelper.GetRunWithUserRepeatAsync(async () => (await switchingDevice.ConnectableManager.InitializeAsync(userMessageService)).Connect, userMessageService))
      {
        throw ConnectionExceptionAdapter.ConnectFailed(name, numberChassis, number);
      }
    }
  }
}

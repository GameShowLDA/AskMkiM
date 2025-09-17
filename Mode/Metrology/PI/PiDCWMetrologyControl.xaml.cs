using System.Windows;
using System.Windows.Controls;
using AppConfiguration.Error.Device.Breakdown;
using AppConfiguration.Interface;
using Mode.Base;
using Mode.Metrology.MeasurementSystem;
using NewCore.Base.Interface.Main;
using UI.Controls.ProtocolNew;
using Utilities;
using Utilities.Help;
using Utilities.Interface;
using Utilities.Models;
using static NewCore.Enum.MetrologyEnum;

namespace Mode.Metrology.PI
{
  /// <summary>
  /// Логика взаимодействия для PiMetrologyControl.xaml.
  /// </summary>
  public partial class PiDCWMetrologyControl : UserControl, IExecution
  {
    MetrologicalModeRole metrologicalModeRole => MetrologicalModeRole.PI;

    PiMeasurement testMeasurement = new PiMeasurement();

    (bool Success, string Message, DataModel DataModel) Data;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="PiDCWMetrologyControl"/>.
    /// </summary>
    public PiDCWMetrologyControl()
    {
      InitializeComponent();
      InitializeSettings();

      // Регистрируем обработчик движения мыши
      MouseMove += (s, e) =>
      {
        // Обновляем последний элемент под курсором
        HelpProvider.SetHelpKey(this, "UtilityModePI");
      };
    }

    /// <summary>
    /// Инициализирует все необходимые настройки для компонента.
    /// Очищает предыдущий контент и добавляет новые элементы управления.
    /// </summary>
    public void InitializeSettings()
    {
      ProtocolUI.SetSettings(
        this,
        StartDelegate: ExecuteMeasurementProcess,
        true,
        StopDelegate: async (CancellationToken token) =>
        {
          await testMeasurement.FinalizeMeasurement(ProtocolUI);
        });
    }

    /// <summary>
    /// Выполнение контроля.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    private async Task ExecuteMeasurementProcess(CancellationToken cancellationToken)
    {
      Data = UIValidationHelper.TryValidateAndParseInputWithEquipment(ProtocolUI, timeCheck: true, voltageCheck: true, timeRampCheck: true);
      if (!Data.Success)
      {
        await ProtocolUI.ShowMessageAsync(new ShowMessageModel("Ошибка", ShowMessageModel.ErrorMessage.TitleColor, Data.Message), SkipStepModeCheck: true);
        return;
      }

      var first = Data.DataModel.FirstPoint;
      var second = Data.DataModel.SecondPoint;
      var param = Data.DataModel.Param;

      await NewCore.Communication.DeviceCommandSender.ResetAllSystem();

      var connect = await testMeasurement.ConnectToEquipment(first, second, metrologicalModeRole, ProtocolUI);
      if (!connect.Connect)
      {
        await ProtocolUI.ShowMessageAsync(new ShowMessageModel("Ошибка", ShowMessageModel.ErrorMessage.TitleColor, connect.Message), SkipStepModeCheck: true);
        return;
      }

      await testMeasurement.SetupCommutation(ProtocolUI, first, second, metrologicalModeRole);
      await testMeasurement.ConfigureMeter(ProtocolUI, metrologicalModeRole, Data.DataModel);
      await UserActionHelper.RunWithUserRepeatAsync(async () => await testMeasurement.PerformMeasurement(metrologicalModeRole, param, ProtocolUI), ProtocolUI, true);
    }

    public ITextAdapter GetControl()
    {
      return ProtocolUI;
    }

    private class PiMeasurement : BaseMeasurement
    {
      public PiMeasurement() : base() { }

      /// <inheritdoc />
      public override async Task ConfigureMeter(IUserMessageService messageService, MetrologicalModeRole metrologicalModeRole, DataModel dataModel = null)
      {
        var breakDown = Devices.TryGetValue(MetrologicalModeRole.PI, out var meter) ? meter.OfType<IBreakdownTester>().FirstOrDefault() : null;
        string name = breakDown.Name;
        int chassis = breakDown.NumberChassis;
        int numer = breakDown.Number;

        if (!await UserActionHelper.GetRunWithUserRepeatAsync(async () => (await breakDown.ConnectableManager.InitializeAsync(messageService)).Connect, messageService))
          throw new Exception($"Нет подключения к {breakDown.Name}({breakDown.NumberChassis}.{breakDown.Number})");

        if (!await UserActionHelper.GetRunWithUserRepeatAsync(async () => (await breakDown.DcwManger.SetModeAsync()).Success, messageService))
          throw DcwExceptionFactory.SetModeFailed(name, chassis, numer);

        if (!await UserActionHelper.GetRunWithUserRepeatAsync(async () => (await breakDown.DcwManger.SetTestTimeAsync(dataModel.Time)).Success, messageService))
          throw DcwExceptionFactory.SetTestTimeFailed(name, chassis, numer);

        if (!await UserActionHelper.GetRunWithUserRepeatAsync(async () => (await breakDown.DcwManger.SetRampTimeAsync(dataModel.RampTime)).Success, messageService))
          throw DcwExceptionFactory.SetRampTimeFailed(name, chassis, numer);

        if (!await UserActionHelper.GetRunWithUserRepeatAsync(async () => (await breakDown.DcwManger.SetLowCurrentLimitAsync(0)).Success, messageService))
          throw DcwExceptionFactory.SetLowLimitFailed(name, chassis, numer);

        if (!await UserActionHelper.GetRunWithUserRepeatAsync(async () => (await breakDown.DcwManger.SetHighCurrentLimitAsync(10)).Success, messageService))
          throw DcwExceptionFactory.SetHighLimitFailed(name, chassis, numer);

        if (!await UserActionHelper.GetRunWithUserRepeatAsync(async () => (await breakDown.DcwManger.SetVoltageAsync(dataModel.Param)).Success, messageService))
          throw DcwExceptionFactory.SetVoltageFailed(name, chassis, numer);
      }

      /// <inheritdoc />
      public override async Task<bool> PerformMeasurement(MetrologicalModeRole metrologicalModeRole, double param, ProtocolUI protocolUI)
      {
        var meterDevice = Devices.TryGetValue(MetrologicalModeRole.PI, out var meter) ? meter.OfType<IBreakdownTester>().FirstOrDefault() : null;
        await protocolUI.ShowMessageAsync(new ShowMessageModel(header: "Выполнение измерения сопротивления изоляции", headerColor: ShowMessageModel.SuccessMessage.TitleColor));

        // TODO : позже прописать погрешность в настрйоках
        double firstNorm = param - (param / 100.0 * 5);
        double lastNorm = param + (param / 100.0 * 5);

        // double firstNorm = param - ((param / 100.0 * GetPercentageError(TypeCommand.CI)) + GetNumericError(TypeCommand.CI));
        // double lastNorm = param + (param / 100.0 * GetPercentageError(TypeCommand.CI)) + GetNumericError(TypeCommand.CI);

        await meterDevice.DcwManger.MeasureCurrentAsync();

        string result = await Application.Current.Dispatcher.InvokeAsync(() =>
        {
          VoltageValue chassisManagerWindow = new VoltageValue();
          protocolUI.Effect = new System.Windows.Media.Effects.BlurEffect();

          bool? dialogResult = chassisManagerWindow.ShowDialog();
          protocolUI.Effect = null;

          if (dialogResult == true)
          {
            return chassisManagerWindow.VoltageResult;
          }
          else
          {
            return string.Empty;
          }
        });

        await protocolUI.ShowMessageAsync(new ShowMessageModel($"\tДиапазон допускаемых значений", null, $"{firstNorm:F2}-{lastNorm:F2}", ShowMessageModel.SuccessMessage.TitleColor));
        if (!string.IsNullOrEmpty(result) && double.TryParse(result, out var value))
        {
          double pog = value - param;

          var answer = (value >= firstNorm && value <= lastNorm) ? false : true; ;

          ShowMessageModel showMessageModel = new ShowMessageModel($"\tРезультат измерения напряжения", null, $"{result:F2} [{(!answer ? ShowMessageModel.ErrorMessage.Title : ShowMessageModel.ErrorMessage.Item1)}]");
          showMessageModel.MessageColor = (value >= firstNorm && value <= lastNorm) ? ShowMessageModel.SuccessMessage.TitleColor : ShowMessageModel.ErrorMessage.TitleColor;
          showMessageModel.ExecutionError = (value >= firstNorm && value <= lastNorm) ? false : true;
          showMessageModel.CanBeDeleted = showMessageModel.ExecutionError;
          await protocolUI.ShowMessageAsync(showMessageModel);
          await protocolUI.ShowMessageAsync(new ShowMessageModel("\tПогрешность измерения", message: $"{pog}В [{(!answer ? ShowMessageModel.ErrorMessage.Title : ShowMessageModel.ErrorMessage.Item1)}]", messageColor: showMessageModel.MessageColor));
        }
        else
        {
          await protocolUI.ShowMessageAsync(new ShowMessageModel("Ошибка", ShowMessageModel.ErrorMessage.TitleColor, "Некорректно введённое эталонное значение напряжения."));
        }

        return true;
      }

      public override async Task FinalizeMeasurement(IUserMessageService messageService)
      {
        await base.FinalizeMeasurement(messageService);
        var breakDown = Devices.TryGetValue(MetrologicalModeRole.PI, out var meter) ? meter.OfType<IBreakdownTester>().FirstOrDefault() : null;
      }
    }
  }
}

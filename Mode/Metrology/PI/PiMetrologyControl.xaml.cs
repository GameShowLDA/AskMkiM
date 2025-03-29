using System.Windows;
using System.Windows.Controls;
using Mode.Base;
using Mode.Metrology.MeasurementSystem;
using Mode.Settings.DeviceConfig.ChassisManager;
using NewCore.Base.Interface.Main;
using NewCore.Device;
using UI.Controls.Protocol;
using Utilities.Models;
using static AppConfiguration.MeasurementError.MeasurementErrorConfig;
using static AppConfiguration.MeasurementError.MeasurementErrorModel;
using static NewCore.Enum.MetrologyEnum;
using static Utilities.LoggerUtility;

namespace Mode.Metrology.PI
{
  /// <summary>
  /// Логика взаимодействия для PiMetrologyControl.xaml.
  /// </summary>
  public partial class PiMetrologyControl : UserControl
  {
    MetrologicalModeRole metrologicalModeRole => MetrologicalModeRole.PI;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="PiMetrologyControl"/>.
    /// </summary>
    public PiMetrologyControl()
    {
      InitializeComponent();
      InitializeSettingsAsync().ConfigureAwait(true);
    }

    /// <summary>
    /// Инициализирует все необходимые настройки для компонента.
    /// Очищает предыдущий контент и добавляет новые элементы управления.
    /// </summary>
    public async Task InitializeSettingsAsync()
    {
      try
      {
        ProtocolUI.SetSettings(this, StartDelegate: ExecuteMeasurementProcess, true, null);
      }
      catch (Exception ex)
      {
        var methodName = System.Reflection.MethodBase.GetCurrentMethod().Name;
        LogError($"Ошибка загрузки элемента метрологии СИ в методе {methodName}: {ex.Message}");
      }
    }

    /// <summary>
    /// Выполнение контроля.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    private async Task ExecuteMeasurementProcess(CancellationToken cancellationToken)
    {
      var (ok, msg, dataModel) = UIValidationHelper.TryValidateAndParseInputWithEquipment(ProtocolUI, timeCheck: true, timeRampCheck:true);
      if (!ok)
      {
        await ProtocolUI.ShowMessageAsync(new ShowMessageModel("Ошибка", ShowMessageModel.ErrorMessage.Item2, msg));
        return;
      }

      var first = dataModel.FirstPoint;
      var second = dataModel.SecondPoint;
      var param = dataModel.Param;

      await NewCore.Communication.DeviceCommandSender.ResetAllSystem();

      PiMeasurement testMeasurement = new PiMeasurement();
      var connect = await testMeasurement.ConnectToEquipment(first, second, metrologicalModeRole, ProtocolUI);
      if (!connect.Connect)
      {
        await ProtocolUI.ShowMessageAsync(new ShowMessageModel("Ошибка", ShowMessageModel.ErrorMessage.Item2, connect.Message));
        return;
      }

      await testMeasurement.SetupCommutation(first, second, metrologicalModeRole);
      await testMeasurement.ConfigureMeter(metrologicalModeRole, dataModel);
      await testMeasurement.PerformMeasurement(metrologicalModeRole, param, ProtocolUI);
      await testMeasurement.FinalizeMeasurement();
    }

    private class PiMeasurement : BaseMeasurement
    {
      public PiMeasurement() : base() { }

      /// <inheritdoc />
      public override async Task ConfigureMeter(MetrologicalModeRole metrologicalModeRole, DataModel dataModel = null)
      {
        var breakDown = Devices.TryGetValue(MetrologicalModeRole.PI, out var meter) ? meter.OfType<IBreakdownTester>().FirstOrDefault() : null;
        await breakDown.ConnectableManager.ConnectAsync();
        await breakDown.DcwManger.SetModeAsync();
        await breakDown.DcwManger.SetVoltageAsync(dataModel.Param);
        await breakDown.DcwManger.SetTestTimeAsync(dataModel.Time);
        await breakDown.DcwManger.SetRampTimeAsync(dataModel.RampTime);
        await breakDown.DcwManger.SetLowCurrentLimitAsync(0);
        await breakDown.DcwManger.SetHighCurrentLimitAsync(10);
      }

      /// <inheritdoc />
      public override async Task PerformMeasurement(MetrologicalModeRole metrologicalModeRole, double param, ProtocolUI protocolUI)
      {
        var meterDevice = Devices.TryGetValue(MetrologicalModeRole.PI, out var meter) ? meter.OfType<IBreakdownTester>().FirstOrDefault() : null;
        await protocolUI.ShowMessageAsync(new ShowMessageModel(header: "Выполнение измерения сопротивления изоляции", headerColor: ShowMessageModel.SuccessMessage.Item2));

        // TODO : позже прописать погрешность в настрйоках
        double firstNorm = param - (param / 100.0 * 5);
        double lastNorm = param + (param / 100.0 * 5 );
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

        await protocolUI.ShowMessageAsync(new ShowMessageModel($"\tДиапазон допускаемых значений", null, $"{firstNorm:F2}-{lastNorm:F2}", ShowMessageModel.SuccessMessage.Item2));
        if (!string.IsNullOrEmpty(result) && double.TryParse(result, out var value))
        {
          double pog = value - param;

          var answer = (value >= firstNorm && value <= lastNorm) ? false : true; ;

          ShowMessageModel showMessageModel = new ShowMessageModel($"\tРезультат измерения напряжения", null, $"{result:F2} [{(!answer ? ShowMessageModel.SuccessMessage.Item1 : ShowMessageModel.ErrorMessage.Item1)}]");
          showMessageModel.MessageColor = (value >= firstNorm && value <= lastNorm) ? ShowMessageModel.SuccessMessage.Item2 : ShowMessageModel.ErrorMessage.Item2;
          showMessageModel.ExecutionError = (value >= firstNorm && value <= lastNorm) ? false : true;
          showMessageModel.CanBeDeleted = showMessageModel.ExecutionError;
          await protocolUI.ShowMessageAsync(showMessageModel);
          await protocolUI.ShowMessageAsync(new ShowMessageModel("\tПогрешность измерения", message: $"{pog}В [{(!answer ? ShowMessageModel.SuccessMessage.Item1 : ShowMessageModel.ErrorMessage.Item1)}]", messageColor: showMessageModel.MessageColor));
        }
        else
        {
          await protocolUI.ShowMessageAsync(new ShowMessageModel("Ошибка", ShowMessageModel.ErrorMessage.Item2, "Некорректно введённое эталонное значение напряжения."));
        }
      }

      public override async Task FinalizeMeasurement()
      {
        await base.FinalizeMeasurement();
        var breakDown = Devices.TryGetValue(MetrologicalModeRole.PI, out var meter) ? meter.OfType<IBreakdownTester>().FirstOrDefault() : null;
        await breakDown.ConnectableManager.DisconnectAsync();
      }
    }
  }
}

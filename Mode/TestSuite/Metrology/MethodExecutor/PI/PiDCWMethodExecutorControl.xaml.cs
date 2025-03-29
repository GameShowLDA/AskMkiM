using System.Windows.Controls;
using Mode.Base;
using NewCore.Base.Interface.Main;
using UI.Controls.Protocol;
using Utilities.Models;
using static Utilities.LoggerUtility;

namespace Mode.TestSuite.Metrology.MethodExecutor.PI
{
  /// <summary>
  /// Логика взаимодействия для PiDCWMethodExecutorControl.xaml.
  /// </summary>
  public partial class PiDCWMethodExecutorControl : UserControl
  {
    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="PiDCWMethodExecutorControl"/>.
    /// </summary>
    public PiDCWMethodExecutorControl()
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
      var (ok, msg, dataModel) = UIValidationHelper.TryValidateAndParseInputWithEquipment(ProtocolUI, timeCheck: true, timeRampCheck: true, voltageCheck: true, busCheck: true);
      if (!ok)
      {
        await ProtocolUI.ShowMessageAsync(new ShowMessageModel("Ошибка", ShowMessageModel.ErrorMessage.Item2, msg));
        return;
      }

      var first = dataModel.FirstPoint;
      var second = dataModel.SecondPoint;
      var param = dataModel.Param;

      PiDCWMethodExecutorMeasurement testMeasurement = new PiDCWMethodExecutorMeasurement();
      try
      {
        var connect = await testMeasurement.ConnectToEquipment(first, second, ProtocolUI);
        if (!connect.Connect)
        {
          await ProtocolUI.ShowMessageAsync(new ShowMessageModel("Ошибка", ShowMessageModel.ErrorMessage.Item2, connect.Message));
          return;
        }

        await testMeasurement.SetupCommutation(ProtocolUI, first, second, dataModel.ActiveBus);
        await testMeasurement.ConfigureMeter(dataModel);
        await testMeasurement.RunParallelModuleTasksAsync(ProtocolUI, dataModel);
      }
      finally
      {
        await testMeasurement.FinalizeAsync();
      }
    }

    private class PiDCWMethodExecutorMeasurement : BaseMethodExecutor
    {
      public PiDCWMethodExecutorMeasurement() : base() { }

      /// <inheritdoc />
      public override async Task ConfigureMeter(DataModel dataModel = null)
      {
        var breakDown = Devices.OfType<IBreakdownTester>().FirstOrDefault();
        await breakDown.ConnectableManager.ConnectAsync();
        await breakDown.DcwManger.SetModeAsync();
        await breakDown.DcwManger.SetVoltageAsync(dataModel.Voltage);
        await breakDown.DcwManger.SetTestTimeAsync(dataModel.Time);
        await breakDown.DcwManger.SetRampTimeAsync(dataModel.RampTime);
        await breakDown.DcwManger.SetHighCurrentLimitAsync(dataModel.Param);
      }

      /// <inheritdoc />
      public override async Task PerformMeasurement(ProtocolUI protocolUI, DataModel dataModel)
      {
        var breakDown = Devices.OfType<IBreakdownTester>().FirstOrDefault();

        await protocolUI.ShowMessageAsync(new ShowMessageModel("\tИспытания прочности изоляции(ACW)"));

        var answer = await breakDown.DcwManger.MeasureCurrentAsync();
        var pause = false;
        var successMessage = ShowMessageModel.SuccessMessage.Item1;
        var colorMessage = ShowMessageModel.SuccessMessage.Item2;
        if (answer > dataModel.Param)
        {
          successMessage = ShowMessageModel.ErrorMessage.Item1;
          colorMessage = ShowMessageModel.ErrorMessage.Item2;
          if (await AppManager.Config.ExecutionConfig.GetIsStopOnErrorEnabled())
          {
            pause = true;
          }
        }

        await protocolUI.ShowMessageAsync(new ShowMessageModel($"\t\tРезультат измерения разряда {HighestBitCount}({GetBitString()})", message: $"{answer.ToString()} мА [{successMessage}]", messageColor: colorMessage));
        if (pause)
        {
          await protocolUI.PauseAsync();
        }
      }

      public override async Task FinalizeAsync()
      {
        await base.FinalizeAsync();
        var breakDown = Devices.OfType<IBreakdownTester>().FirstOrDefault();
        await breakDown.ConnectableManager.DisconnectAsync();
      }
    }
  }
}

using System.Windows.Controls;
using AppConfiguration.Execution;
using Mode.Base;
using NewCore.Base.Interface.Main;
using UI.Controls.ProtocolNew;
using Utilities.Models;

namespace Mode.TestSuite.Metrology.MethodExecutor.CI
{
  /// <summary>
  /// Логика взаимодействия для CiMethodExecutor.xaml.
  /// </summary>
  public partial class CiMethodExecutor : UserControl
  {
    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="CiMethodExecutor"/>.
    /// </summary>
    public CiMethodExecutor()
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
      ProtocolUI.SetSettings(this, StartDelegate: ExecuteMeasurementProcess, true, null);
    }

    /// <summary>
    /// Выполнение контроля.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    private async Task ExecuteMeasurementProcess(CancellationToken cancellationToken)
    {
      var (ok, msg, dataModel) = UIValidationHelper.TryValidateAndParseInputWithEquipment(ProtocolUI, timeCheck: true, voltageCheck: true, busCheck: true);
      if (!ok)
      {
        await ProtocolUI.ShowMessageAsync(new ShowMessageModel("Ошибка", ShowMessageModel.ErrorMessage.TitleColor, msg));
        return;
      }

      var first = dataModel.FirstPoint;
      var second = dataModel.SecondPoint;
      var param = dataModel.Param;

      TestMeasurement testMeasurement = new TestMeasurement();
      try
      {
        var connect = await testMeasurement.ConnectToEquipment(first, second, ProtocolUI);
        if (!connect.Connect)
        {
          await ProtocolUI.ShowMessageAsync(new ShowMessageModel("Ошибка", ShowMessageModel.ErrorMessage.TitleColor, connect.Message));
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

    private class TestMeasurement : BaseMethodExecutor
    {
      public TestMeasurement() : base() { }

      /// <inheritdoc />
      public override async Task ConfigureMeter(DataModel dataModel = null)
      {
        var breakDown = Devices.OfType<IBreakdownTester>().FirstOrDefault();
        await breakDown.ConnectableManager.ConnectAsync();
        await breakDown.IrManger.SetModeAsync();
        await breakDown.IrManger.SetVoltageAsync(dataModel.Voltage);
        await breakDown.IrManger.SetTestTimeAsync(dataModel.Time);
      }

      /// <inheritdoc />
      public override async Task PerformMeasurement(ProtocolUI protocolUI, DataModel dataModel)
      {
        var breakDown = Devices.OfType<IBreakdownTester>().FirstOrDefault();
        await protocolUI.ShowMessageAsync(new ShowMessageModel("\tИзмерение сопротивления изоляции"));

        var answer = await breakDown.IrManger.MeasureResistanceAsync(dataModel.Param, dataModel.Param, 60000);
        var pause = false;
        var successMessage = ShowMessageModel.ErrorMessage.Title;
        var colorMessage = ShowMessageModel.SuccessMessage.TitleColor;
        if (answer < (dataModel.Param * 1000))
        {
          successMessage = ShowMessageModel.ErrorMessage.Item1;
          colorMessage = ShowMessageModel.ErrorMessage.TitleColor;
          if (await ExecutionConfig.GetIsStopOnErrorEnabled())
          {
            pause = true;
          }
        }

        await protocolUI.ShowMessageAsync(new ShowMessageModel($"\t\tРезультат измерения разряда {HighestBitCount}({GetBitString()})", message: $"{answer.ToString()} МОм [{successMessage}]", messageColor: colorMessage));
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

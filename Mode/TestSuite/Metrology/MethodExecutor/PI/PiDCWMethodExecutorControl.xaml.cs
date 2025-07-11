using System.Windows.Controls;
using AppConfiguration.Execution;
using Mode.Base;
using NewCore.Base.Interface.Main;
using UI.Controls.ProtocolNew;
using Utilities;
using Utilities.Models;

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
      ProtocolUI.SetSettings(this, StartDelegate: ExecuteMeasurementProcess, true, null);
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
        await ProtocolUI.ShowMessageAsync(new ShowMessageModel("Ошибка", message: msg, type: ShowMessageModel.MessageType.Error), SkipStepModeCheck: true);
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
          await ProtocolUI.ShowMessageAsync(new ShowMessageModel("Ошибка", message: connect.Message, type: ShowMessageModel.MessageType.Error), SkipStepModeCheck: true);
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

        if (!(await breakDown.DcwManger.SetVoltageAsync(dataModel.Voltage)).Item1)
        {
          throw new Exception("Не удалость выставить напряжение на ППУ.");
        }

        await breakDown.DcwManger.SetTestTimeAsync(dataModel.Time);
        await breakDown.DcwManger.SetRampTimeAsync(dataModel.RampTime);
        await breakDown.DcwManger.SetHighCurrentLimitAsync(dataModel.Param);
      }

      /// <inheritdoc />
      public override async Task PerformMeasurement(ProtocolUI protocolUI, DataModel dataModel)
      {
        var breakDown = Devices.OfType<IBreakdownTester>().FirstOrDefault();

        await protocolUI.ShowMessageAsync(new ShowMessageModel("\tИспытания прочности изоляции(DCW)"));
        await UserActionHelper.RunWithUserRepeatAsync(async () =>
        {
          var answer = await breakDown.DcwManger.MeasureCurrentAsync();
          var type = ShowMessageModel.MessageType.Success;

          if (answer >= dataModel.Param)
          {
            type = ShowMessageModel.MessageType.Error;
          }

          // await protocolUI.ShowMessageAsync(new ShowMessageModel($"\t\tРезультат измерения разряда ({GetBitString()})", message: $"{answer.ToString()} мА", type: type));
          return type == ShowMessageModel.MessageType.Success ? true : false;

        }, protocolUI);
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

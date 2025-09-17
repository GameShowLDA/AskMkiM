using System.Windows.Controls;
using AppConfiguration.Error.Device;
using AppConfiguration.Error.Device.Breakdown;
using AppConfiguration.Execution;
using Mode.Base;
using NewCore.Base.Interface.Main;
using UI.Controls.ProtocolNew;
using Utilities;
using Utilities.Help;
using Utilities.Interface;
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

      // Регистрируем обработчик движения мыши
      MouseMove += (s, e) =>
      {
        // Обновляем последний элемент под курсором
        HelpProvider.SetHelpKey(this, "TestSIGroupMethod");
      };
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
        await ProtocolUI.ShowMessageAsync(new ShowMessageModel("Ошибка", message: msg, type: ShowMessageModel.MessageType.Error), SkipStepModeCheck: true);
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
          await ProtocolUI.ShowMessageAsync(new ShowMessageModel("Ошибка", message: connect.Message, type: ShowMessageModel.MessageType.Error), SkipStepModeCheck: true);
          return;
        }

        await testMeasurement.SetupCommutation(ProtocolUI, first, second, dataModel.ActiveBus);
        await testMeasurement.ConfigureMeter(ProtocolUI, dataModel);
        await testMeasurement.RunParallelModuleTasksAsync(ProtocolUI, dataModel);
      }
      finally
      {
        await testMeasurement.FinalizeAsync(ProtocolUI);
      }
    }

    private class TestMeasurement : BaseMethodExecutor
    {
      public TestMeasurement() : base() { }

      /// <inheritdoc />
      public override async Task ConfigureMeter(IUserMessageService messageService, DataModel dataModel = null)
      {
        var breakDown = Devices.OfType<IBreakdownTester>().FirstOrDefault();
        var name = breakDown.Name;
        var chassis = breakDown.NumberChassis;
        var number = breakDown.Number;

        if (!await UserActionHelper.GetRunWithUserRepeatAsync(async () => (await breakDown.ConnectableManager.InitializeAsync(messageService)).Connect, messageService))
          throw ConnectionExceptionFactory.ConnectFailed(name, chassis, number);

        if (!await UserActionHelper.GetRunWithUserRepeatAsync(async () => (await breakDown.IrManger.SetModeAsync()).Success, messageService))
          throw IrExceptionFactory.SetModeFailed(name, chassis, number);

        if (!await UserActionHelper.GetRunWithUserRepeatAsync(async () => (await breakDown.IrManger.SetVoltageAsync(dataModel.Voltage)).Success, messageService))
          throw IrExceptionFactory.SetVoltageFailed(name, chassis, number);

        if (!await UserActionHelper.GetRunWithUserRepeatAsync(async () => (await breakDown.IrManger.SetTestTimeAsync(dataModel.Time)).Success, messageService))
          throw IrExceptionFactory.SetTestTimeFailed(name, chassis, number);
      }

      /// <inheritdoc />
      public override async Task PerformMeasurement(ProtocolUI protocolUI, DataModel dataModel)
      {
        await UserActionHelper.RunWithUserRepeatAsync(async () =>
        {
          protocolUI.GetCancellationToken().ThrowIfCancellationRequested();

          var breakDown = Devices.OfType<IBreakdownTester>().FirstOrDefault();
          await protocolUI.ShowMessageAsync(new ShowMessageModel("\tИзмерение сопротивления изоляции"));

          var answer = await breakDown.IrManger.MeasureResistanceAsync(dataModel.Param, dataModel.Param, 60000);
          var pause = false;
          var type = ShowMessageModel.MessageType.Success;
          if (answer < dataModel.Param)
          {
            type = ShowMessageModel.MessageType.Error;
          }

          await protocolUI.ShowMessageAsync(new ShowMessageModel($"\t\tРезультат измерения разряда {HighestBitCount}({GetBitString()})", message: $"{answer.ToString()} МОм", type: type), skipPause: true);
          return type == ShowMessageModel.MessageType.Success ? true : false;
        }, protocolUI);
      }

      public override async Task FinalizeAsync(IUserMessageService messageService)
      {
        await base.FinalizeAsync(messageService);
      }
    }
  }
}

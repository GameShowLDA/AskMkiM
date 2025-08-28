using System.Windows.Controls;
using AppConfiguration.Error.Device;
using AppConfiguration.Error.Device.Breakdown;
using AppConfiguration.Execution;
using Mode.Base;
using NewCore.Base.Interface.Main;
using UI.Controls.ProtocolNew;
using Utilities;
using Utilities.Interface;
using Utilities.Models;

namespace Mode.TestSuite.Metrology.MethodExecutor.PI
{
  /// <summary>
  /// Логика взаимодействия для PiMethodExecutorControl.xaml.
  /// </summary>
  public partial class PiACWMethodExecutorControl : UserControl
  {
    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="PiACWMethodExecutorControl"/>.
    /// </summary>
    public PiACWMethodExecutorControl()
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

      PiACWMethodExecutorMeasurement testMeasurement = new PiACWMethodExecutorMeasurement();
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

    private class PiACWMethodExecutorMeasurement : BaseMethodExecutor
    {
      public PiACWMethodExecutorMeasurement() : base() { }

      /// <inheritdoc />
      public override async Task ConfigureMeter(IUserMessageService messageService, DataModel dataModel = null)
      {
        var breakDown = Devices.OfType<IBreakdownTester>().FirstOrDefault();
        var name = breakDown.Name;
        var chassis = breakDown.NumberChassis;
        var number = breakDown.Number;

        if (!await UserActionHelper.GetRunWithUserRepeatAsync(async () => (await breakDown.ConnectableManager.ConnectAsync(messageService)).Connect, messageService))
          throw ConnectionExceptionFactory.ConnectFailed(name, chassis, number);

        if (!await UserActionHelper.GetRunWithUserRepeatAsync(async () => (await breakDown.AcwManger.SetVoltageAsync(dataModel.Voltage)).Success, messageService))
          throw AcwExceptionFactory.SetVoltageFailed(name, chassis, number);

        if (!await UserActionHelper.GetRunWithUserRepeatAsync(async () => (await breakDown.AcwManger.SetTestTimeAsync(dataModel.Time)).Success, messageService))
          throw AcwExceptionFactory.SetTestTimeFailed(name, chassis, number);

        if (!await UserActionHelper.GetRunWithUserRepeatAsync(async () => (await breakDown.AcwManger.SetRampTimeAsync(dataModel.RampTime)).Success, messageService))
          throw AcwExceptionFactory.SetRampTimeFailed(name, chassis, number);

        if (!await UserActionHelper.GetRunWithUserRepeatAsync(async () => (await breakDown.AcwManger.SetHighCurrentLimitAsync(dataModel.Param)).Success, messageService))
          throw AcwExceptionFactory.SetHighLimitFailed(name, chassis, number);

        if (!await UserActionHelper.GetRunWithUserRepeatAsync(async () => (await breakDown.AcwManger.SetFrequencyAsync(50)).Success, messageService))
          throw AcwExceptionFactory.SetFrequencyFailed(name, chassis, number);
      }

      /// <inheritdoc />
      public override async Task PerformMeasurement(ProtocolUI protocolUI, DataModel dataModel)
      {
        var breakDown = Devices.OfType<IBreakdownTester>().FirstOrDefault();

        await protocolUI.ShowMessageAsync(new ShowMessageModel("\tИспытания прочности изоляции(ACW)"));
        await UserActionHelper.RunWithUserRepeatAsync(async () =>
        {
          protocolUI.GetCancellationToken().ThrowIfCancellationRequested();
          var answer = await breakDown.AcwManger.MeasureCurrentAsync();
          var type = ShowMessageModel.MessageType.Success;
          if (answer > dataModel.Param)
          {
            type = ShowMessageModel.MessageType.Error;
          }

          // await protocolUI.ShowMessageAsync(new ShowMessageModel($"\t\tРезультат измерения разряда {HighestBitCount}({GetBitString()})", message: $"{answer.ToString()} мА", type: type));
          return type == ShowMessageModel.MessageType.Success ? true : false;

        }, protocolUI);
      }

      public override async Task FinalizeAsync(IUserMessageService messageService)
      {
        await base.FinalizeAsync(messageService);
        var breakDown = Devices.OfType<IBreakdownTester>().FirstOrDefault();
        await breakDown.ConnectableManager.DisconnectAsync(messageService);
      }
    }
  }
}

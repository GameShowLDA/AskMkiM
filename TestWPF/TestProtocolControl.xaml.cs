using System.Windows.Controls;
using Mode.Base;
using Mode.TestSuite.Metrology.MethodExecutor;
using NewCore.Base.Interface.Main;
using UI.Controls.ProtocolNew;
using Utilities.Interface;
using Utilities.Models;

namespace TestWPF
{
  /// <summary>
  /// Логика взаимодействия для TestProtocolControl.xaml
  /// </summary>
  public partial class TestProtocolControl : UserControl
  {
    public TestProtocolControl()
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
        await ProtocolUI.ShowMessageAsync(new ShowMessageModel("Ошибка", message: msg, type: ShowMessageModel.MessageType.Error));
        return;
      }

      var first = dataModel.FirstPoint;
      var second = dataModel.SecondPoint;
      var param = dataModel.Param;

      // ManagerChassis managerChassis = new ManagerChassis();
      // managerChassis.ConnectionDetails = "192.168.1.0";
      // await managerChassis.PowerManager.StartPowerAsync();
      // await Task.Delay(5000);
      // await NewCore.Communication.DeviceCommandSender.ResetAllSystem();

      TestMeasurement testMeasurement = new TestMeasurement();
      var connect = await testMeasurement.ConnectToEquipment(first, second, ProtocolUI);
      if (!connect.Connect)
      {
        await ProtocolUI.ShowMessageAsync(new ShowMessageModel("Ошибка", message: connect.Message, type: ShowMessageModel.MessageType.Error));
        return;
      }

      await testMeasurement.SetupCommutation(ProtocolUI, first, second, dataModel.ActiveBus);
      // await testMeasurement.ConfigureMeter(dataModel);
      // await testMeasurement.RunAllStepsAsync(ProtocolUI, dataModel);
      await testMeasurement.RunParallelModuleTasksAsync(ProtocolUI, dataModel);
      await testMeasurement.FinalizeAsync(ProtocolUI);
    }
  }
  public class TestMeasurement : BaseMethodExecutor
  {
    public TestMeasurement() : base() { }

    /// <inheritdoc />
    public override async Task ConfigureMeter(IUserMessageService messageService, DataModel dataModel = null)
    {
      var breakDown = Devices.OfType<IBreakdownTester>().FirstOrDefault();
      await breakDown.ConnectableManager.ConnectAsync(messageService);
      await breakDown.IrManger.SetModeAsync();
      await breakDown.IrManger.SetVoltageAsync(dataModel.Voltage);
      await breakDown.IrManger.SetTestTimeAsync(dataModel.Time);
    }

    /// <inheritdoc />
    public override async Task PerformMeasurement(ProtocolUI protocolUI, DataModel dataModel)
    {
      //var breakDown = Devices.OfType<IBreakdownTester>().FirstOrDefault();
      await protocolUI.ShowMessageAsync(new ShowMessageModel("\tИзмерение сопротивления изоляции"));

      // var answer = await breakDown.IrManger.MeasureResistanceAsync();
      var answer = 0;
      var type = ShowMessageModel.MessageType.Success;

      if (answer < (dataModel.Param * 1000))
      {
        type = ShowMessageModel.MessageType.Error;
      }

      await protocolUI.ShowMessageAsync(new ShowMessageModel($"\t\tРезультат измерения разряда {HighestBitCount}({GetBitString()})", message: $"{answer.ToString()} МОм", type: type));
    }

    public override async Task FinalizeAsync(IUserMessageService messageService)
    {
      await base.FinalizeAsync(messageService);
      var breakDown = Devices.OfType<IBreakdownTester>().FirstOrDefault();
      await breakDown.ConnectableManager.DisconnectAsync(messageService);
    }
  }
}

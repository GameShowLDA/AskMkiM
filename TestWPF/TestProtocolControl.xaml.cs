using System.Windows.Controls;
using Mode.Base;
using Mode.Metrology.MeasurementSystem;
using Mode.Models;
using Mode.TestSuite.Metrology.MethodExecutor;
using Mode.TestSuite.Metrology.NodeMethod;
using NewCore.Base.Interface.Main;
using NewCore.Device;
using Newtonsoft.Json.Linq;
using UI.Controls.Protocol;
using Utilities.Models;
using YamlDotNet.Core.Tokens;
using static AppConfiguration.MeasurementError.MeasurementErrorConfig;
using static AppConfiguration.MeasurementError.MeasurementErrorModel;
using static NewCore.Enum.MetrologyEnum;
using static Utilities.LoggerUtility;

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
        await ProtocolUI.ShowMessageAsync(new ShowMessageModel("Ошибка", ShowMessageModel.ErrorMessage.TitleColor, msg));
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
        await ProtocolUI.ShowMessageAsync(new ShowMessageModel("Ошибка", ShowMessageModel.ErrorMessage.TitleColor, connect.Message));
        return;
      }

      await testMeasurement.SetupCommutation(ProtocolUI, first, second, dataModel.ActiveBus);
      // await testMeasurement.ConfigureMeter(dataModel);
      // await testMeasurement.RunAllStepsAsync(ProtocolUI, dataModel);
      await testMeasurement.RunParallelModuleTasksAsync(ProtocolUI, dataModel);
      await testMeasurement.FinalizeAsync();
    }
  }
  public class TestMeasurement : BaseMethodExecutor
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
      //var breakDown = Devices.OfType<IBreakdownTester>().FirstOrDefault();
      await protocolUI.ShowMessageAsync(new ShowMessageModel("\tИзмерение сопротивления изоляции"));

      // var answer = await breakDown.IrManger.MeasureResistanceAsync();
      var answer = 0;
      var successMessage = ShowMessageModel.ErrorMessage.Title;
      var colorMessage = ShowMessageModel.SuccessMessage.TitleColor;
      if (answer < (dataModel.Param * 1000))
      {
        successMessage = ShowMessageModel.ErrorMessage.Item1;
        colorMessage = ShowMessageModel.ErrorMessage.TitleColor;
      }

      await protocolUI.ShowMessageAsync(new ShowMessageModel($"\t\tРезультат измерения разряда {HighestBitCount}({GetBitString()})", message: $"{answer.ToString()} МОм [{successMessage}]", messageColor: colorMessage));
    }

    public override async Task FinalizeAsync()
    {
      await base.FinalizeAsync();
      var breakDown = Devices.OfType<IBreakdownTester>().FirstOrDefault();
      await breakDown.ConnectableManager.DisconnectAsync();
    }
  }
}

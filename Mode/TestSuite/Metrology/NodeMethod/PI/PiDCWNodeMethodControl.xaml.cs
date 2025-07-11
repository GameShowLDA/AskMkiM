using System.Windows.Controls;
using Mode.Base;
using NewCore.Base.Interface.Main;
using UI.Controls.ProtocolNew;
using Utilities;
using Utilities.Models;

namespace Mode.TestSuite.Metrology.NodeMethod.PI
{
  /// <summary>
  /// Логика взаимодействия для PiNodeMethodControl.xaml.
  /// </summary>
  public partial class PiDCWNodeMethodControl : UserControl
  {
    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="PiDCWNodeMethodControl"/>.
    /// </summary>
    public PiDCWNodeMethodControl()
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
        await ProtocolUI.ShowMessageAsync(new ShowMessageModel("Ошибка", message: msg, type: ShowMessageModel.MessageType.Error));
        return;
      }

      var first = dataModel.FirstPoint;
      var second = dataModel.SecondPoint;
      var param = dataModel.Param;
      await NewCore.Communication.DeviceCommandSender.ResetAllSystem();

      PiNodeMethod testMeasurement = new PiNodeMethod();
      var connect = await testMeasurement.ConnectToEquipment(first, second, ProtocolUI);
      if (!connect.Connect)
      {
        await ProtocolUI.ShowMessageAsync(new ShowMessageModel("Ошибка", message: connect.Message, type: ShowMessageModel.MessageType.Error));
        return;
      }

      await testMeasurement.SetupCommutation(ProtocolUI, first, second, dataModel.ActiveBus);
      await testMeasurement.ConfigureMeter(dataModel);
      await testMeasurement.PerformMeasurement(ProtocolUI, dataModel);
      await testMeasurement.FinalizeAsync();
    }

    private class PiNodeMethod : BaseNodeTest
    {
      public PiNodeMethod() : base() { }

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
        var token = protocolUI.GetCancellationToken();

        while (true)
        {
          token.ThrowIfCancellationRequested();

          protocolUI.GetCancellationToken();

          var connectResult = await GetNextPoint(protocolUI);
          if (connectResult.Step)
          {
            await protocolUI.ShowMessageAsync(new ShowMessageModel("\tИспытания прочности изоляции(DCW)"));

            await UserActionHelper.RunWithUserRepeatAsync(async () =>
            {
              token.ThrowIfCancellationRequested();

              var answer = await breakDown.DcwManger.MeasureCurrentAsync();
              var type = ShowMessageModel.MessageType.Success;

              if (answer >= dataModel.Param)
              {
                type = ShowMessageModel.MessageType.Error;
              }

              // await protocolUI.ShowMessageAsync(new ShowMessageModel("\tРезультат измерения", message: $"{answer.ToString()} мА", type: type), skipPause: true);

              return type == ShowMessageModel.MessageType.Success ? true : false;
            }, protocolUI);
          }
          else
          {
            break;
          }
        }
      }

      public override async Task FinalizeAsync()
      {
        await base.FinalizeAsync();
        var breakDown = Devices.OfType<IBreakdownTester>().FirstOrDefault();
        await breakDown.ConnectableManager.DisconnectAsync();
        ResetPoints();
      }
    }
  }
}

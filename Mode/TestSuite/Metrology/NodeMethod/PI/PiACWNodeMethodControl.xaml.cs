using System.Windows.Controls;
using Mode.Base;
using NewCore.Base.Interface.Main;
using UI.Controls.ProtocolNew;
using Utilities.Models;
using static NewCore.Enum.MetrologyEnum;

namespace Mode.TestSuite.Metrology.NodeMethod.PI
{
  /// <summary>
  /// Логика взаимодействия для PiACWNodeMethodControl.xaml.
  /// </summary>
  public partial class PiACWNodeMethodControl : UserControl
  {
    PiNodeMethod testMeasurement = new PiNodeMethod();

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="PiACWNodeMethodControl"/>.
    /// </summary>
    public PiACWNodeMethodControl()
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
      ProtocolUI.SetSettings(
        this,
        StartDelegate: ExecuteMeasurementProcess,
        true,
        StopDelegate: async (CancellationToken token) =>
        {
          await testMeasurement.FinalizeAsync();
        });
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
        await ProtocolUI.ShowMessageAsync(new ShowMessageModel("Ошибка", ShowMessageModel.ErrorMessage.TitleColor, msg));
        return;
      }

      var first = dataModel.FirstPoint;
      var second = dataModel.SecondPoint;
      var param = dataModel.Param;
      await NewCore.Communication.DeviceCommandSender.ResetAllSystem();


      var connect = await testMeasurement.ConnectToEquipment(first, second, ProtocolUI);
      if (!connect.Connect)
      {
        await ProtocolUI.ShowMessageAsync(new ShowMessageModel("Ошибка", ShowMessageModel.ErrorMessage.TitleColor, connect.Message));
        return;
      }

      await testMeasurement.SetupCommutation(ProtocolUI, first, second, dataModel.ActiveBus);
      await testMeasurement.ConfigureMeter(dataModel);
      await testMeasurement.PerformMeasurement(ProtocolUI, dataModel);
    }

    private class PiNodeMethod : BaseNodeTest
    {
      public PiNodeMethod() : base() { }

      /// <inheritdoc />
      public override async Task ConfigureMeter(DataModel dataModel = null)
      {
        var breakDown = Devices.OfType<IBreakdownTester>().FirstOrDefault();
        await breakDown.ConnectableManager.ConnectAsync();
        await breakDown.AcwManger.SetModeAsync();
        await breakDown.AcwManger.SetTestTimeAsync(dataModel.Time);
        await breakDown.AcwManger.SetRampTimeAsync(dataModel.RampTime);
        await breakDown.AcwManger.SetHighCurrentLimitAsync(dataModel.Param);
        await breakDown.AcwManger.SetFrequencyAsync(50);
        await breakDown.AcwManger.SetVoltageAsync(dataModel.Voltage);
      }

      /// <inheritdoc />
      public override async Task PerformMeasurement(ProtocolUI protocolUI, DataModel dataModel)
      {
        var breakDown = Devices.OfType<IBreakdownTester>().FirstOrDefault();
        var token = protocolUI.GetCancellationToken();

        while (true)
        {
          token.ThrowIfCancellationRequested();

          var connectResult = await GetNextPoint(protocolUI);
          if (connectResult.Step)
          {
            await protocolUI.ShowMessageAsync(new ShowMessageModel("\tИспытания прочности изоляции(ACW)"));

            var answer = await breakDown.AcwManger.MeasureCurrentAsync();
            var successMessage = ShowMessageModel.ErrorMessage.Title;
            var colorMessage = ShowMessageModel.SuccessMessage.TitleColor;

            bool error = false;
            if (answer >= dataModel.Param)
            {
              successMessage = ShowMessageModel.ErrorMessage.Item1;
              colorMessage = ShowMessageModel.ErrorMessage.TitleColor;
              error = true;
            }

            await protocolUI.ShowMessageAsync(new ShowMessageModel("\tРезультат измерения", message: $"{answer.ToString()} мА [{successMessage}]", messageColor: colorMessage));
            if (error)
            {
              await protocolUI.PauseAsync();
              error = false;
            }
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

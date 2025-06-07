using System.Windows.Controls;
using Mode.Base;
using NewCore.Base.Interface.Main;
using UI.Controls.ProtocolNew;
using Utilities.Models;

namespace Mode.TestSuite.Metrology.NodeMethod.CI
{
  /// <summary>
  /// Логика взаимодействия для CiNodeMethodControl.xaml.
  /// </summary>
  public partial class CiNodeMethodControl : UserControl
  {
    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="CiNodeMethodControl"/>.
    /// </summary>
    public CiNodeMethodControl()
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
      var (ok, msg, dataModel) = UIValidationHelper.TryValidateAndParseInputWithEquipment(ProtocolUI, timeCheck: true, voltageCheck: true);
      if (!ok)
      {
        await ProtocolUI.ShowMessageAsync(new ShowMessageModel("Ошибка", message: msg, type: ShowMessageModel.MessageType.Error));
        return;
      }

      var first = dataModel.FirstPoint;
      var second = dataModel.SecondPoint;
      var param = dataModel.Param;
      await NewCore.Communication.DeviceCommandSender.ResetAllSystem();

      CiNodeMethod testMeasurement = new CiNodeMethod();
      var connect = await testMeasurement.ConnectToEquipment(first, second, ProtocolUI);
      if (!connect.Connect)
      {
        await ProtocolUI.ShowMessageAsync(new ShowMessageModel("Ошибка", message: connect.Message, type: ShowMessageModel.MessageType.Error));
        return;
      }

      await testMeasurement.SetupCommutation(ProtocolUI, first, second, NewCore.Enum.DeviceEnum.BusPoint.A);
      await testMeasurement.ConfigureMeter(dataModel);
      await testMeasurement.PerformMeasurement(ProtocolUI, dataModel);
      await testMeasurement.FinalizeAsync();
    }

    private class CiNodeMethod : BaseNodeTest
    {
      public CiNodeMethod() : base() { }

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
        var token = protocolUI.GetCancellationToken();

        while (true)
        {
          token.ThrowIfCancellationRequested();

          var connectResult = await GetNextPoint(protocolUI);
          if (connectResult.Step)
          {
            await protocolUI.ShowMessageAsync(new ShowMessageModel($"Подключение точки {connectResult.PointModel.PointNumber} к шине {AssignedBus}", type: ShowMessageModel.MessageType.Success));
            await protocolUI.ShowMessageAsync(new ShowMessageModel("\tИзмерение сопротивления изоляции"));

            var answer = await breakDown.IrManger.MeasureResistanceAsync();
            var type = ShowMessageModel.MessageType.Success;

            if (answer < (dataModel.Param * 1000))
            {
              type = ShowMessageModel.MessageType.Error;
            }

            await protocolUI.ShowMessageAsync(new ShowMessageModel("\tРезультат измерения", message: $"{answer.ToString()} МОм", type: type));
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
      }
    }
  }
}

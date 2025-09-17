using System.Windows.Controls;
using System.Xml.Linq;
using AppConfiguration.Error.Device;
using AppConfiguration.Error.Device.Breakdown;
using Mode.Base;
using NewCore.Base.Interface.Main;
using UI.Controls.ProtocolNew;
using Utilities;
using Utilities.Help;
using Utilities.Interface;
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

      // Регистрируем обработчик движения мыши
      MouseMove += (s, e) =>
      {
        // Обновляем последний элемент под курсором
        HelpProvider.SetHelpKey(this, "TestSINodeMethod");
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
      await testMeasurement.ConfigureMeter(ProtocolUI, dataModel);
      await testMeasurement.PerformMeasurement(ProtocolUI, dataModel);
      await testMeasurement.FinalizeAsync(ProtocolUI);
    }

    private class CiNodeMethod : BaseNodeTest
    {
      public CiNodeMethod() : base() { }

      /// <inheritdoc />
      public override async Task ConfigureMeter(IUserMessageService messageService, DataModel dataModel = null)
      {
        var breakDown = Devices.OfType<IBreakdownTester>().FirstOrDefault();
        string name = breakDown.Name;
        int chassis = breakDown.NumberChassis;
        int numer = breakDown.Number;

        if (!await UserActionHelper.GetRunWithUserRepeatAsync(async () => (await breakDown.ConnectableManager.InitializeAsync(messageService)).Connect, messageService))
          throw ConnectionExceptionFactory.ConnectFailed(name, chassis, numer);

        if (!await UserActionHelper.GetRunWithUserRepeatAsync(async () => (await breakDown.IrManger.SetModeAsync()).Success, messageService))
          throw IrExceptionFactory.SetModeFailed(name, chassis, numer);

        if (!await UserActionHelper.GetRunWithUserRepeatAsync(async () => (await breakDown.IrManger.SetVoltageAsync(dataModel.Voltage)).Success, messageService))
          throw IrExceptionFactory.SetVoltageFailed(name, chassis, numer);

        if (!await UserActionHelper.GetRunWithUserRepeatAsync(async () => (await breakDown.IrManger.SetTestTimeAsync(dataModel.Time)).Success, messageService))
          throw IrExceptionFactory.SetTestTimeFailed(name, chassis, numer);

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
            await protocolUI.ShowMessageAsync(new ShowMessageModel("Измерение сопротивления изоляции"));

            await UserActionHelper.RunWithUserRepeatAsync(async () =>
            {
              token.ThrowIfCancellationRequested();
              var answer = await breakDown.IrManger.MeasureResistanceAsync(dataModel.Param, 1000, 60000);
              var type = ShowMessageModel.MessageType.Success;

              if (answer < dataModel.Param)
              {
                type = ShowMessageModel.MessageType.Error;
              }

              // await protocolUI.ShowMessageAsync(new ShowMessageModel("\tРезультат измерения", message: $"{answer.ToString()} МОм", type: type), skipPause: true);

              return type == ShowMessageModel.MessageType.Success ? true : false;

            }, protocolUI);
          }
          else
          {
            break;
          }
        }
      }

      public override async Task FinalizeAsync(IUserMessageService messageService)
      {
        await base.FinalizeAsync(messageService);
      }
    }
  }
}

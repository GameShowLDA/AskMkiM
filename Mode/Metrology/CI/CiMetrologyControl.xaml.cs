using System.Windows.Controls;
using Mode.Base;
using Mode.Metrology.MeasurementSystem;
using NewCore.Base.Interface.Main;
using NewCore.Device;
using UI.Controls.Protocol;
using Utilities.Models;
using static AppConfiguration.MeasurementError.MeasurementErrorConfig;
using static AppConfiguration.MeasurementError.MeasurementErrorModel;
using static NewCore.Enum.MetrologyEnum;
using static Utilities.LoggerUtility;

namespace Mode.Metrology.CI
{
  /// <summary>
  /// Логика взаимодействия для CiMetrologyControl.xaml.
  /// </summary>
  public partial class CiMetrologyControl : UserControl
  {
    MetrologicalModeRole metrologicalModeRole => MetrologicalModeRole.CI;

    CiMeasurement testMeasurement = new CiMeasurement();

    (bool Success, string Message, DataModel DataModel) Data;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="CiMetrologyControl"/>.
    /// </summary>
    public CiMetrologyControl()
    {
      InitializeComponent();
      InitializeSettings();
    }

    /// <summary>
    /// Инициализирует все необходимые настройки для компонента.
    /// Очищает предыдущий контент и добавляет новые элементы управления.
    /// </summary>
    public void InitializeSettings()
    {
      try
      {
        ProtocolUI.SetSettings(
          this, 
          StartDelegate: ExecuteMeasurementProcess, 
          true,
          ReturnDelegate: async (CancellationToken token) => {await testMeasurement.PerformMeasurement(metrologicalModeRole, Data.DataModel.Param, ProtocolUI);
          });
      }
      catch (Exception ex)
      {
        var methodName = System.Reflection.MethodBase.GetCurrentMethod().Name;
        LogError($"Ошибка загрузки элемента метрологии СИ в методе {methodName}: {ex.Message}");
      }
    }

    /// <summary>
    /// Выполнение контроля.
    /// </summary>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns></returns>
    private async Task ExecuteMeasurementProcess(CancellationToken cancellationToken)
    {
      Data = UIValidationHelper.TryValidateAndParseInputWithEquipment(ProtocolUI, timeCheck: true, voltageCheck: true);
      if (!Data.Success)
      {
        await ProtocolUI.ShowMessageAsync(new ShowMessageModel("Ошибка", ShowMessageModel.ErrorMessage.Item2, Data.Message));
        return;
      }

      var first = Data.DataModel.FirstPoint;
      var second = Data.DataModel.SecondPoint;
      var param = Data.DataModel.Param;

      await NewCore.Communication.DeviceCommandSender.ResetAllSystem();

      var connect = await testMeasurement.ConnectToEquipment(first, second, metrologicalModeRole, ProtocolUI);
      if (!connect.Connect)
      {
        await ProtocolUI.ShowMessageAsync(new ShowMessageModel("Ошибка", ShowMessageModel.ErrorMessage.Item2, connect.Message));
        return;
      }

      await testMeasurement.SetupCommutation(ProtocolUI, first, second, metrologicalModeRole);
      await testMeasurement.ConfigureMeter(metrologicalModeRole, Data.DataModel);
      await testMeasurement.PerformMeasurement(metrologicalModeRole, param, ProtocolUI);
      await testMeasurement.FinalizeMeasurement();
    }

    private class CiMeasurement : BaseMeasurement
    {
      public CiMeasurement() : base() { }

      /// <inheritdoc />
      public override async Task ConfigureMeter(MetrologicalModeRole metrologicalModeRole, DataModel dataModel = null)
      {
        var breakDown = Devices.TryGetValue(MetrologicalModeRole.CI, out var meter) ? meter.OfType<IBreakdownTester>().FirstOrDefault() : null;
        await breakDown.ConnectableManager.ConnectAsync();
        await breakDown.IrManger.SetModeAsync();
        await breakDown.IrManger.SetVoltageAsync(dataModel.Voltage);
        await breakDown.IrManger.SetTestTimeAsync(dataModel.Time);
      }

      /// <inheritdoc />
      public override async Task PerformMeasurement(MetrologicalModeRole metrologicalModeRole, double param, ProtocolUI protocolUI)
      {
        var meterDevice = Devices.TryGetValue(MetrologicalModeRole.CI, out var meter) ? meter.OfType<IBreakdownTester>().FirstOrDefault() : null;
        await protocolUI.ShowMessageAsync(new ShowMessageModel(header: "Выполнение измерения сопротивления изоляции", headerColor: ShowMessageModel.SuccessMessage.Item2));

        double firstNorm = param - ((param / 100.0 * GetPercentageError(TypeCommand.CI)) + GetNumericError(TypeCommand.CI));
        double lastNorm = param + (param / 100.0 * GetPercentageError(TypeCommand.CI)) + GetNumericError(TypeCommand.CI);

        var result = await meterDevice.IrManger.MeasureResistanceAsync();

        ShowMessageModel showMessageModel = new ShowMessageModel($"\tРезультат сопротивления ({firstNorm:F2}-{lastNorm:F2})", null, $"{result:F2}");
        showMessageModel.MessageColor = (result >= firstNorm && result <= lastNorm) ? ShowMessageModel.SuccessMessage.Item2 : ShowMessageModel.ErrorMessage.Item2;
        showMessageModel.ExecutionError = (result >= firstNorm && result <= lastNorm) ? false : true;
        showMessageModel.CanBeDeleted = showMessageModel.ExecutionError;
        await protocolUI.ShowMessageAsync(showMessageModel);
      }

      public override async Task FinalizeMeasurement()
      {
        await base.FinalizeMeasurement();
        var breakDown = Devices.TryGetValue(MetrologicalModeRole.CI, out var meter) ? meter.OfType<IBreakdownTester>().FirstOrDefault() : null;
        await breakDown.ConnectableManager.DisconnectAsync();
      }
    }
  }
}

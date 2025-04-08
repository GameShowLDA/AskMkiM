using System.Diagnostics.Metrics;
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

namespace Mode.Metrology.KC
{
  /// <summary>
  /// Логика взаимодействия для KcMetrologyControl.xaml.
  /// </summary>
  public partial class KcMetrologyControl : UserControl
  {
    MetrologicalModeRole metrologicalModeRole => MetrologicalModeRole.KC;

    KcMeasurement testMeasurement = new KcMeasurement();

    (bool Success, string Message, DataModel DataModel) Data;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="KcMetrologyControl"/>.
    /// </summary>
    public KcMetrologyControl()
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
      ProtocolUI.SetSettings(
        this,
        StartDelegate: ExecuteMeasurementProcess,
        true,
        ReturnDelegate: async (CancellationToken token) =>
        {
          await testMeasurement.PerformMeasurement(metrologicalModeRole, Data.DataModel.Param, ProtocolUI);
        });
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

      var connect = await testMeasurement.ConnectToEquipment(first, second, metrologicalModeRole, ProtocolUI);
      if (!connect.Connect)
      {
        await ProtocolUI.ShowMessageAsync(new ShowMessageModel("Ошибка", ShowMessageModel.ErrorMessage.Item2, connect.Message));
        return;
      }

      await testMeasurement.SetupCommutation(ProtocolUI, first, second, metrologicalModeRole);
      await testMeasurement.ConfigureMeter(metrologicalModeRole);
      await testMeasurement.PerformMeasurement(metrologicalModeRole, param, ProtocolUI);
      await testMeasurement.FinalizeMeasurement();
    }

    private class KcMeasurement : BaseMeasurement
    {
      public KcMeasurement() : base() { }

      /// <inheritdoc />
      public override async Task ConfigureMeter(MetrologicalModeRole metrologicalModeRole, DataModel dataModel = null)
      {
        await base.ConfigureMeter(metrologicalModeRole, dataModel);
        var fastMeter = Devices.TryGetValue(metrologicalModeRole, out var meter) ? meter.OfType<IFastMeter>().FirstOrDefault() : null;
        await fastMeter.ResistanceManager.SetResistanceModeAsync();
      }

      /// <inheritdoc />
      public override async Task PerformMeasurement(MetrologicalModeRole metrologicalModeRole, double param, ProtocolUI protocolUI)
      {
        var fastMeter = Devices.TryGetValue(metrologicalModeRole, out var meter) ? meter.OfType<IFastMeter>().FirstOrDefault() : null;
        await protocolUI.ShowMessageAsync(new ShowMessageModel(header: "Выполнение измерения сопротивления", headerColor: ShowMessageModel.SuccessMessage.Item2));

        double firstNorm = param - ((param / 100.0 * GetPercentageError(TypeCommand.KC)) + GetNumericError(TypeCommand.KC));
        double lastNorm = param + (param / 100.0 * GetPercentageError(TypeCommand.KC)) + GetNumericError(TypeCommand.KC);

        await Task.Delay(1000);
        var result = await fastMeter.ResistanceManager.MeasureResistanceAsync(param, firstNorm, lastNorm);
      }
    }
  }
}

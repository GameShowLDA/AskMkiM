using System.Windows.Controls;
using Mode.Base;
using Mode.Metrology.MeasurementSystem;
using NewCore.Base.Interface.Main;
using NewCore.Device;
using UI.Controls.Protocol;
using Utilities.Models;
using static AppConfig.Config.MeasurementErrorConfig;
using static AppConfig.Data.MeasurementError.MeasurementErrorModel;
using static NewCore.Enum.MetrologyEnum;
using static Utilities.LoggerUtility;

namespace Mode.Metrology.IE
{
  /// <summary>
  /// Логика взаимодействия для IeMetrologyControl.xaml.
  /// </summary>
  public partial class IeMetrologyControl : UserControl
  {
    MetrologicalModeRole metrologicalModeRole => MetrologicalModeRole.IE;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="IeMetrologyControl"/>.
    /// </summary>
    public IeMetrologyControl()
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
      try
      {
        ProtocolUI.SetSettings(this, StartDelegate: ExecuteMeasurementProcess, true, null);
      }
      catch (Exception ex)
      {
        var methodName = System.Reflection.MethodBase.GetCurrentMethod().Name;
        LogError($"Ошибка загрузки элемента метрологии КС в методе {methodName}: {ex.Message}");
      }
    }

    /// <summary>
    /// Выполнение контроля.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    private async Task ExecuteMeasurementProcess(CancellationToken cancellationToken)
    {
      var (ok, msg, first, second, param) = await UIValidationHelper.TryValidateAndParseInputWithEquipmentAsync<IeMeasurement>(ProtocolUI);
      if (!ok)
      {
        await ProtocolUI.ShowMessageAsync(new ShowMessageModel("Ошибка", ShowMessageModel.ErrorMessage.Item2, msg));
        return;
      }

      IeMeasurement testMeasurement = new IeMeasurement();
      var connect = await testMeasurement.ConnectToEquipment(first, second, metrologicalModeRole, ProtocolUI);
      if (!connect.Connect)
      {
        await ProtocolUI.ShowMessageAsync(new ShowMessageModel("Ошибка", ShowMessageModel.ErrorMessage.Item2, connect.Message));
        return;
      }

      await testMeasurement.SetupCommutation(first, second, metrologicalModeRole);
      await testMeasurement.ConfigureMeter(metrologicalModeRole);
      await testMeasurement.PerformMeasurement(metrologicalModeRole, param, ProtocolUI);
      await testMeasurement.FinalizeMeasurement();
    }

    private class IeMeasurement : BaseMeasurement
    {
      public IeMeasurement() : base() { }

      /// <inheritdoc />
      public override async Task ConfigureMeter(MetrologicalModeRole metrologicalModeRole)
      {
        var fastMeter = Devices.TryGetValue(metrologicalModeRole, out var meter) ? meter.OfType<IFastMeter>().FirstOrDefault() : null;
        await fastMeter.CapacitanceManager.SetCapacitanceModeAsync();
      }

      /// <inheritdoc />
      public override async Task PerformMeasurement(MetrologicalModeRole metrologicalModeRole, double param, ProtocolUI protocolUI)
      {
        var fastMeter = Devices.TryGetValue(metrologicalModeRole, out var meter) ? meter.OfType<IFastMeter>().FirstOrDefault() : null;
        await protocolUI.ShowMessageAsync(new ShowMessageModel(header: "Выполнение измерения ёмкости", headerColor: ShowMessageModel.SuccessMessage.Item2));

        double firstNorm = param - ((param / 100.0 * GetPercentageError(TypeCommand.IE)) + GetNumericError(TypeCommand.IE));
        double lastNorm = param + (param / 100.0 * GetPercentageError(TypeCommand.IE)) + GetNumericError(TypeCommand.IE);

        var result = await fastMeter.CapacitanceManager.MeasureCapacitanceAsync();

        ShowMessageModel showMessageModel = new ShowMessageModel($"\tРезультат ёмкости ({firstNorm:F2}-{lastNorm:F2})", null, $"{result:F2}");
        showMessageModel.MessageColor = (result >= firstNorm && result <= lastNorm) ? ShowMessageModel.SuccessMessage.Item2 : ShowMessageModel.ErrorMessage.Item2;
        showMessageModel.ExecutionError = (result >= firstNorm && result <= lastNorm) ? false : true;
        showMessageModel.CanBeDeleted = showMessageModel.ExecutionError;

        await protocolUI.ShowMessageAsync(showMessageModel);
      }
    }
  }
}

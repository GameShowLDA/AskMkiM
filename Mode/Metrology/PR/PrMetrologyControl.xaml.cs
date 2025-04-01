using System.Windows.Controls;
using Mode.Base;
using Mode.Metrology.MeasurementSystem;
using Mode.Models;
using NewCore.Base.Interface.Main;
using NewCore.Device;
using UI.Controls.Protocol;
using Utilities.Models;
using static AppConfiguration.MeasurementError.MeasurementErrorConfig;
using static AppConfiguration.MeasurementError.MeasurementErrorModel;
using static NewCore.Enum.DeviceEnum;
using static NewCore.Enum.MetrologyEnum;
using static Utilities.LoggerUtility;

namespace Mode.Metrology.PR
{
  /// <summary>
  /// Логика взаимодействия для PrMetrologyControl.xaml
  /// </summary>
  public partial class PrMetrologyControl : UserControl
  {
    MetrologicalModeRole metrologicalModeRole => MetrologicalModeRole.PR;

    PrMeasurement testMeasurement = new PrMeasurement();

    (bool Success, string Message, DataModel DataModel) Data;
    public PrMetrologyControl()
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
          ReturnDelegate: async (CancellationToken token) =>
          {
            await testMeasurement.PerformMeasurement(metrologicalModeRole, Data.DataModel.Param, ProtocolUI);
          }, 
          StopDelegate: async (CancellationToken token) =>
          {
            await testMeasurement.FinalizeMeasurement();
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
      await testMeasurement.MintSettings(Data.DataModel);
      await testMeasurement.PerformMeasurement(metrologicalModeRole, param, ProtocolUI);
    }

    private class PrMeasurement : BaseMeasurement
    {
      public PrMeasurement() : base() { }

      /// <summary>
      /// Устанавливает настройки на модуль источника напряжения и тока.
      /// </summary>
      /// <param name="dataModel">Модель введенных данных.</param>
      /// <returns></returns>
      public async Task MintSettings(DataModel dataModel)
      {
        var mint = Devices.TryGetValue(MetrologicalModeRole.PR, out var meter) ? meter.OfType<IPowerSourceModule>().FirstOrDefault() : null;
        (var current, VoltageSources voltage) = SelectOptimalCurrentAndVoltage(dataModel.Param);

        int integerPart = current.IntegerCurrent;
        int decimalPart = current.decimalCurrent;

        await mint.VoltageManager.SetSourceVoltageAsync(voltage);
        await mint.CurrentManager.SetCurrentLevelAsync(integerPart, decimalPart);
      }

      /// <inheritdoc />
      public override async Task ConfigureMeter(MetrologicalModeRole metrologicalModeRole, DataModel dataModel = null)
      {
        var fastMeter = Devices.TryGetValue(metrologicalModeRole, out var meter) ? meter.OfType<IFastMeter>().FirstOrDefault() : null;
        await fastMeter.DcVoltageManager.SetDCVoltageModeAsync();
      }

      /// <inheritdoc />
      public override async Task PerformMeasurement(MetrologicalModeRole metrologicalModeRole, double param, ProtocolUI protocolUI)
      {
        var meterDevice = Devices.TryGetValue(metrologicalModeRole, out var meter) ? meter.OfType<IFastMeter>().FirstOrDefault() : null;
        await protocolUI.ShowMessageAsync(new ShowMessageModel(header: "Выполнение проверки релейной", headerColor: ShowMessageModel.SuccessMessage.Item2));

        (var current, VoltageSources voltageSource) = SelectOptimalCurrentAndVoltage(param);
        double currentGenerial = (current.decimalCurrent / 1000.0) + current.IntegerCurrent;

        double firstNorm = param - ((param / 100.0 * GetPercentageError(TypeCommand.PR)) + GetNumericError(TypeCommand.PR));
        double lastNorm = param + (param / 100.0 * GetPercentageError(TypeCommand.PR)) + GetNumericError(TypeCommand.PR);

        await Task.Delay(1000);
        var voltage = await meterDevice.DcVoltageManager.MeasureDCVoltageAsync(param * (currentGenerial / 1000));
        await protocolUI.ShowMessageAsync(new ShowMessageModel($"\tРезультат измерения напряжения", message: $"{voltage} В.", messageColor: ShowMessageModel.SuccessMessage.Item2));
        var result = voltage / (currentGenerial / 1000);

        ShowMessageModel showMessageModel = new ShowMessageModel($"\tРезультат сопротивления ({firstNorm:F2}-{lastNorm:F2})", null, $"{result:F2}");
        showMessageModel.MessageColor = (result >= firstNorm && result <= lastNorm) ? ShowMessageModel.SuccessMessage.Item2 : ShowMessageModel.ErrorMessage.Item2;
        showMessageModel.ExecutionError = (result >= firstNorm && result <= lastNorm) ? false : true;
        showMessageModel.CanBeDeleted = showMessageModel.ExecutionError;
        await protocolUI.ShowMessageAsync(showMessageModel);
      }

      /// <summary>
      /// Выбирает оптимальные параметры тока и источника напряжения
      /// в зависимости от значения сопротивления.
      /// </summary>
      /// <param name="resistance">Измеренное сопротивление в Омах.</param>
      /// <returns>
      /// Кортеж, содержащий:
      /// - рекомендуемый ток в миллиамперах;
      /// - источник напряжения из перечисления VoltageSources.
      /// </returns>
      private ((int IntegerCurrent, int decimalCurrent), VoltageSources Voltage) SelectOptimalCurrentAndVoltage(double resistance)
      {
        if (resistance >= 0 && resistance <= 100)
        {
          return ((20, 0), VoltageSources.Supply5V);
        }
        else if (resistance > 100 && resistance <= 1000)
        {
          return ((9, 0), VoltageSources.Supply12V);
        }
        else if (resistance > 1000 && resistance <= 10000)
        {
          return ((0, 900), VoltageSources.Supply12V);
        }
        else if (resistance > 10000 && resistance <= 100000)
        {
          return ((0, 90), VoltageSources.Supply12V);
        }
        else
        {
          return ((0, 0), VoltageSources.Supply12V);
        }
      }
    }
  }
}

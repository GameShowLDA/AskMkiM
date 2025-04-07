using System.Text.Json;
using System.Windows.Controls;
using AppConfiguration.Execution;
using Mode.Base;
using Mode.Metrology.MeasurementSystem;
using Mode.Models;
using NewCore.Base.DeviceResponses;
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
        var data = SelectOptimalCurrentAndVoltage(dataModel.Param, mint);

        int integerPart = data.IntegerCurrent;
        int decimalPart = data.DecimalCurrent;

        await mint.VoltageManager.SetSourceVoltageAsync(data.Voltage);
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
        protocolUI.GetCancellationToken().ThrowIfCancellationRequested();
        var mint = Devices.TryGetValue(MetrologicalModeRole.PR, out var power) ? power.OfType<IPowerSourceModule>().FirstOrDefault() : null;
        var meterDevice = Devices.TryGetValue(metrologicalModeRole, out var meter) ? meter.OfType<IFastMeter>().FirstOrDefault() : null;

        await protocolUI.ShowMessageAsync(new ShowMessageModel(header: "Выполнение проверки релейной", headerColor: ShowMessageModel.SuccessMessage.Item2));

        var data = SelectOptimalCurrentAndVoltage(param, mint);
        double currentGenerial = (data.DecimalCurrent / 1000.0) + data.IntegerCurrent;

        double firstNorm = param - ((param / 100.0 * GetPercentageError(TypeCommand.PR)) + GetNumericError(TypeCommand.PR));
        double lastNorm = param + (param / 100.0 * GetPercentageError(TypeCommand.PR)) + GetNumericError(TypeCommand.PR);

        await Task.Delay(1000);
        var voltage = await meterDevice.DcVoltageManager.MeasureDCVoltageAsync(param * (currentGenerial / 1000));
        double fakeCurrent = GetInterpolatedCurrent(param, mint);
        var result = voltage / (fakeCurrent / 1000.0);

        ShowMessageModel showMessageModel = new ShowMessageModel($"\tРезультат измерения сопротивления ({firstNorm:F2}-{lastNorm:F2})", null, $"{result:F2}");
        showMessageModel.MessageColor = (result >= firstNorm && result <= lastNorm) ? ShowMessageModel.SuccessMessage.Item2 : ShowMessageModel.ErrorMessage.Item2;
        showMessageModel.ExecutionError = (result >= firstNorm && result <= lastNorm) ? false : true;
        showMessageModel.CanBeDeleted = showMessageModel.ExecutionError;
        await protocolUI.ShowMessageAsync(showMessageModel);
      }

      /// <summary>
      /// Возвращает диапазон параметров (коэффициенты, ток, напряжение), соответствующий указанному сопротивлению.
      /// </summary>
      /// <param name="resistance">Измеренное сопротивление (в Омах).</param>
      /// <param name="powerSourceModule">Модуль источника питания с диапазонами калибровки.</param>
      /// <returns>
      /// Объект <see cref="ResistanceCalibrationRange"/>, соответствующий диапазону сопротивления.
      /// Если диапазон не найден, возвращается пустой объект со значениями по умолчанию.
      /// </returns>
      private ResistanceCalibrationRange SelectOptimalCurrentAndVoltage(double resistance, IPowerSourceModule powerSourceModule)
      {
        var json = powerSourceModule.ResistanceCalibrationJson;

        var list = string.IsNullOrWhiteSpace(json)
          ? new List<ResistanceCalibrationRange>()
          : JsonSerializer.Deserialize<List<ResistanceCalibrationRange>>(json) ?? new List<ResistanceCalibrationRange>();

        var matched = list.FirstOrDefault(r =>
          resistance >= r.ResistanceMin && resistance <= r.ResistanceMax);

        return matched ?? new ResistanceCalibrationRange
        {
          ResistanceMin = 0,
          ResistanceMax = 0,
          IntegerCurrent = 0,
          DecimalCurrent = 0,
          DecimalCurrentFake = 0,
          IntegerCurrentFake = 0,
          Voltage = VoltageSources.Supply12V
        };
      }

      public static double GetInterpolatedCurrent(double resistance, IPowerSourceModule module)
      {
        if (string.IsNullOrWhiteSpace(module.ResistanceCalibrationJson))
          throw new InvalidOperationException("Calibration JSON пуст или отсутствует.");

        // Десериализация
        var ranges = JsonSerializer.Deserialize<List<ResistanceCalibrationRange>>(module.ResistanceCalibrationJson);

        if (ranges == null || !ranges.Any())
          throw new InvalidOperationException("Не удалось десериализовать калибровочные диапазоны.");

        var range = ranges.FirstOrDefault(r =>
            resistance >= r.ResistanceMin && resistance <= r.ResistanceMax);

        if (range == null)
        {
          throw new ArgumentOutOfRangeException(nameof(resistance),
              $"Сопротивление {resistance} Ом не входит ни в один из диапазонов.");
        }

        // Процент положения внутри диапазона
        double percent = (resistance - range.ResistanceMin) /
                         (range.ResistanceMax - range.ResistanceMin);

        // Преобразование токов в double (миллиамперы)
        double real = range.IntegerCurrent + range.DecimalCurrent / 1000.0;
        double fake = range.IntegerCurrentFake + range.DecimalCurrentFake / 1000.0;

        // Интерполяция
        return real + (fake - real) * percent;
      }

    }
  }
}

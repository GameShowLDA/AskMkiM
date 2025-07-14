using System.Windows;
using System.Windows.Controls;
using AppConfiguration.Interface;
using Mode.Base;
using Mode.Metrology.MeasurementSystem;
using Mode.Metrology.PI;
using NewCore.Base.Interface.Main;
using UI.Controls.ProtocolNew;
using Utilities;
using Utilities.Interface;
using Utilities.Models;
using static NewCore.Enum.MetrologyEnum;
namespace Mode.Metrology.KN
{
  /// <summary>
  /// Логика взаимодействия для KnDCWMetrologyControl.xaml
  /// </summary>
  public partial class KnDCWMetrologyControl : UserControl, IExecution
  {
    MetrologicalModeRole metrologicalModeRole => MetrologicalModeRole.KN;

    KnMeasurement testMeasurement = new KnMeasurement();

    (bool Success, string Message, DataModel DataModel) Data;

    public KnDCWMetrologyControl()
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
        StopDelegate: async (CancellationToken token) =>
        {
          await testMeasurement.FinalizeMeasurement(ProtocolUI);
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
        await ProtocolUI.ShowMessageAsync(new ShowMessageModel("Ошибка", message: Data.Message, type: ShowMessageModel.MessageType.Error), SkipStepModeCheck: true);
        return;
      }

      var first = Data.DataModel.FirstPoint;
      var second = Data.DataModel.SecondPoint;
      var param = Data.DataModel.Param;

      var connect = await testMeasurement.ConnectToEquipment(first, second, metrologicalModeRole, ProtocolUI);
      if (!connect.Connect)
      {
        await ProtocolUI.ShowMessageAsync(new ShowMessageModel("Ошибка", message: connect.Message, type: ShowMessageModel.MessageType.Error), SkipStepModeCheck: true);
        return;
      }

      await testMeasurement.SetupCommutation(ProtocolUI, first, second, metrologicalModeRole);
      await testMeasurement.ConfigureMeter(ProtocolUI, metrologicalModeRole);
      await UserActionHelper.RunWithUserRepeatAsync(async () => await testMeasurement.PerformMeasurement(metrologicalModeRole, param, ProtocolUI), ProtocolUI, true);
    }

    public ITextAdapter GetControl()
    {
      return ProtocolUI;
    }

    private class KnMeasurement : BaseMeasurement
    {
      public KnMeasurement() : base() { }

      /// <inheritdoc />
      public override async Task ConfigureMeter(IUserMessageService messageService, MetrologicalModeRole metrologicalModeRole, DataModel dataModel = null)
      {
        await base.ConfigureMeter(messageService, metrologicalModeRole, dataModel);
        var fastMeter = Devices.TryGetValue(metrologicalModeRole, out var meter) ? meter.OfType<IFastMeter>().FirstOrDefault() : null;


        if (!await UserActionHelper.GetRunWithUserRepeatAsync(() => fastMeter.DcVoltageManager.SetDCVoltageModeAsync(), messageService))
        {
          throw new Exception($"Ошибка установка режима измерения постоянного напряжения {fastMeter.Name}({fastMeter.NumberChassis}.{fastMeter.Number})");
        }
      }

      /// <inheritdoc />
      public override async Task<bool> PerformMeasurement(MetrologicalModeRole metrologicalModeRole, double param, ProtocolUI protocolUI)
      {
        protocolUI.GetCancellationToken().ThrowIfCancellationRequested();

        var fastMeter = Devices.TryGetValue(metrologicalModeRole, out var meter) ? meter.OfType<IFastMeter>().FirstOrDefault() : null;
        await protocolUI.ShowMessageAsync(new ShowMessageModel(header: "Выполнение измерения напряжения(DCW)"));

        double firstNorm = param - ((param / 100.0 * 1) + 0.01);
        double lastNorm = param + ((param / 100.0 * 1) + 0.01);

        await fastMeter.DcVoltageManager.MeasureDCVoltageAsync();

        string result = await Application.Current.Dispatcher.InvokeAsync(() =>
        {
          VoltageValue chassisManagerWindow = new VoltageValue();
          protocolUI.Effect = new System.Windows.Media.Effects.BlurEffect();

          bool? dialogResult = chassisManagerWindow.ShowDialog();
          protocolUI.Effect = null;

          if (dialogResult == true)
          {
            return chassisManagerWindow.VoltageResult;
          }
          else
          {
            return string.Empty;
          }
        });

        await protocolUI.ShowMessageAsync(new ShowMessageModel($"\tДиапазон допускаемых значений", null, $"{firstNorm:F2}-{lastNorm:F2}"), skipPause: true);
        if (!string.IsNullOrEmpty(result) && double.TryParse(result, out var value))
        {
          double pog = value - param;

          var answer = (value >= firstNorm && value <= lastNorm) ? false : true; ;

          ShowMessageModel showMessageModel = new ShowMessageModel($"\tРезультат измерения напряжения", null, $"{result:F2}");
          showMessageModel.Status = (!answer ? ShowMessageModel.MessageType.Success : ShowMessageModel.MessageType.Error);
          showMessageModel.ExecutionError = (value >= firstNorm && value <= lastNorm) ? false : true;
          showMessageModel.CanBeDeleted = showMessageModel.ExecutionError;
          await protocolUI.ShowMessageAsync(showMessageModel, skipPause: true);
          await protocolUI.ShowMessageAsync(new ShowMessageModel("\tПогрешность измерения", message: $"{pog}В", type: showMessageModel.Status), skipPause: true);
        }
        else
        {
          await protocolUI.ShowMessageAsync(new ShowMessageModel("Ошибка", message: "Некорректно введённое эталонное значение напряжения.", type: ShowMessageModel.MessageType.Error), skipPause: true);
        }

        return true;
      }
    }
  }
}

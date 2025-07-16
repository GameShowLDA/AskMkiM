using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using AppConfiguration.Enums;
using AppConfiguration.Error.Device.Multimeter;
using AppConfiguration.Interface;
using AppConfiguration.MeasurementError;
using Mode.Base;
using Mode.Metrology.MeasurementSystem;
using NewCore.Base.Device;
using NewCore.Base.DeviceResponses;
using NewCore.Base.Interface.Main;
using UI.Controls.ProtocolNew;
using Utilities;
using Utilities.Interface;
using Utilities.Models;
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
    MetrologicalModeRole metrologicalModeRole => MetrologicalModeRole.KC;

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
          StopDelegate: async (CancellationToken token) =>
          {
            await testMeasurement.FinalizeMeasurement(ProtocolUI);
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
        await ProtocolUI.ShowMessageAsync(new ShowMessageModel("Ошибка", message: Data.Message, type: ShowMessageModel.MessageType.Error), SkipStepModeCheck: true);
        return;
      }

      var first = Data.DataModel.FirstPoint;
      var second = Data.DataModel.SecondPoint;
      var param = Data.DataModel.Param;

      await NewCore.Communication.DeviceCommandSender.ResetAllSystem();

      var connect = await testMeasurement.ConnectToEquipment(first, second, metrologicalModeRole, ProtocolUI);
      if (!connect.Connect)
      {
        await ProtocolUI.ShowMessageAsync(new ShowMessageModel("Ошибка", message: connect.Message, type: ShowMessageModel.MessageType.Error), SkipStepModeCheck: true);
        return;
      }

      await testMeasurement.SetupCommutation(ProtocolUI, first, second, metrologicalModeRole);
      await testMeasurement.ConfigureMeter(ProtocolUI, metrologicalModeRole, Data.DataModel);

      await UserActionHelper.RunWithUserRepeatAsync(async () => await testMeasurement.PerformMeasurement(metrologicalModeRole, param, ProtocolUI), ProtocolUI, true);
    }

    public ITextAdapter GetControl()
    {
      return ProtocolUI;
    }

    private class PrMeasurement : BaseMeasurement
    {
      public PrMeasurement() : base() { }

      /// <inheritdoc />
      public override async Task ConfigureMeter(IUserMessageService messageService, MetrologicalModeRole metrologicalModeRole, DataModel dataModel = null)
      {
        await base.ConfigureMeter(messageService, metrologicalModeRole, dataModel);
        var fastMeter = Devices.TryGetValue(metrologicalModeRole, out var meter) ? meter.OfType<IFastMeter>().FirstOrDefault() : null;

        if (!await UserActionHelper.GetRunWithUserRepeatAsync(() => fastMeter.ContinuityManager.SetContinuityModeAsync(), messageService))
          throw ContinuityExceptionFactory.SetModeFailed(fastMeter.Name, fastMeter.NumberChassis, fastMeter.Number);
      }

      /// <inheritdoc />
      public override async Task<bool> PerformMeasurement(MetrologicalModeRole metrologicalModeRole, double param, ProtocolUI protocolUI)
      {
        var fastMeter = Devices.TryGetValue(metrologicalModeRole, out var meter) ? meter.OfType<IFastMeter>().FirstOrDefault() : null;

        for (int i = 0; i < 10; i++)
        {
          await protocolUI.ShowMessageAsync(new ShowMessageModel(header: "Выполнение измерения сопротивления"), IsBlockStart: true);
          var (firstNorm, lastNorm) = ErrorProviderLocator.Provider.GetRange(TypeCommand.PR, param);

          var result = await fastMeter.ContinuityManager.CheckContinuityAsync(param);

          await protocolUI.ShowMessageAsync(new ShowMessageModel("Результат измерения сопротивления", message: $"{result} Ом", type: (result >= firstNorm && result <= lastNorm ? ShowMessageModel.MessageType.Success : ShowMessageModel.MessageType.Error)) { IndentLevel = 1 }, skipPause: true);
          await protocolUI.ShowMessageAsync(new ShowMessageModel("Диапазон допускаемых значений", message: $"от {firstNorm} до {lastNorm} Ом") { IndentLevel = 2 }, skipPause: true);
          await protocolUI.ShowMessageAsync(new ShowMessageModel("Погрешность измерения", message: $"{(Math.Abs(result - param))} Ом", type: (result >= firstNorm && result <= lastNorm ? ShowMessageModel.MessageType.Success : ShowMessageModel.MessageType.Error)) { IndentLevel = 2 }, skipPause: true);
        }
        return true;
      }


    }
  }
}

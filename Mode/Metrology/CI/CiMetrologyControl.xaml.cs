using System.Windows.Controls;
using AppConfiguration.Enums;
using AppConfiguration.Interface;
using AppConfiguration.MeasurementError;
using Mode.Base;
using Mode.Metrology.MeasurementSystem;
using NewCore.Base.Interface.Main;
using UI.Controls.ProtocolNew;
using Utilities;
using Utilities.Models;
using static NewCore.Enum.MetrologyEnum;

namespace Mode.Metrology.CI
{
  /// <summary>
  /// Логика взаимодействия для CiMetrologyControl.xaml.
  /// </summary>
  public partial class CiMetrologyControl : UserControl, IExecution
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
      ProtocolUI.SetSettings(
        this,
        StartDelegate: ExecuteMeasurementProcess,
        true);
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
        throw new Exception();
      }

      var first = Data.DataModel.FirstPoint;
      var second = Data.DataModel.SecondPoint;
      var param = Data.DataModel.Param;

      await NewCore.Communication.DeviceCommandSender.ResetAllSystem();

      var connect = await testMeasurement.ConnectToEquipment(first, second, metrologicalModeRole, ProtocolUI);
      if (!connect.Connect)
      {
        await ProtocolUI.ShowMessageAsync(new ShowMessageModel("Ошибка", message: connect.Message, type: ShowMessageModel.MessageType.Error), SkipStepModeCheck: true);
        throw new Exception();
      }

      await testMeasurement.SetupCommutation(ProtocolUI, first, second, metrologicalModeRole);
      await testMeasurement.ConfigureMeter(metrologicalModeRole, Data.DataModel);

      await UserActionHelper.RunWithUserRepeatAsync(async () => await testMeasurement.PerformMeasurement(metrologicalModeRole, param, ProtocolUI), ProtocolUI, true);
      await testMeasurement.FinalizeMeasurement();
    }

    public ITextAdapter GetControl()
    {
      return ProtocolUI;
    }

    private class CiMeasurement : BaseMeasurement
    {
      public CiMeasurement() : base() { }

      /// <inheritdoc />
      public override async Task ConfigureMeter(MetrologicalModeRole metrologicalModeRole, DataModel dataModel = null)
      {
        await base.ConfigureMeter(metrologicalModeRole, dataModel);
        var breakDown = Devices.TryGetValue(MetrologicalModeRole.CI, out var meter) ? meter.OfType<IBreakdownTester>().FirstOrDefault() : null;

        if (!(await breakDown.ConnectableManager.ConnectAsync()).Connect)
        {
          throw new Exception($"Нет подключения к {breakDown.Name}({breakDown.NumberChassis}.{breakDown.Number})");
        }

        if (!(await breakDown.IrManger.SetModeAsync()).Success)
        {
          throw new Exception($"Ошибка установка режима IR {breakDown.Name}({breakDown.NumberChassis}.{breakDown.Number})");
        }

        if (!(await breakDown.IrManger.SetVoltageAsync(dataModel.Voltage)).Success)
        {
          throw new Exception($"Ошибка установка напряжения {breakDown.Name}({breakDown.NumberChassis}.{breakDown.Number})");
        }

        if (!(await breakDown.IrManger.SetTestTimeAsync(dataModel.Time)).Success)
        {
          throw new Exception($"Ошибка установка времени теста {breakDown.Name}({breakDown.NumberChassis}.{breakDown.Number})");
        }
      }

      /// <inheritdoc />
      public override async Task<bool> PerformMeasurement(MetrologicalModeRole metrologicalModeRole, double param, ProtocolUI protocolUI)
      {
        var meterDevice = Devices.TryGetValue(MetrologicalModeRole.CI, out var meter) ? meter.OfType<IBreakdownTester>().FirstOrDefault() : null;
        await protocolUI.ShowMessageAsync(new ShowMessageModel(header: "Выполнение измерения сопротивления изоляции"));
        var (firstNorm, lastNorm) = ErrorProviderLocator.Provider.GetRange(TypeCommand.CI, param);

        var result = await meterDevice.IrManger.MeasureResistanceAsync(param, firstNorm, lastNorm);

        await protocolUI.ShowMessageAsync(new ShowMessageModel("Результат измерения сопротивления изоляции", message: $"{result} Ом", type: (result >= firstNorm && result <= lastNorm ? ShowMessageModel.MessageType.Success : ShowMessageModel.MessageType.Error)) { IndentLevel = 1 }, skipPause: true);
        await protocolUI.ShowMessageAsync(new ShowMessageModel("Диапазон допускаемых значений", message: $"от {firstNorm} до {lastNorm} Ом") { IndentLevel = 2 }, skipPause: true);
        await protocolUI.ShowMessageAsync(new ShowMessageModel("Погрешность измерения", message: $"{(Math.Abs(result - param))} Ом", type: (result >= firstNorm && result <= lastNorm ? ShowMessageModel.MessageType.Success : ShowMessageModel.MessageType.Error)) { IndentLevel = 2 }, skipPause: true);

        return true;
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

using AppConfiguration.Interface;
﻿using System.Windows.Controls;
using AppConfiguration.Enums;
using AppConfiguration.MeasurementError;
using Mode.Base;
using Mode.Metrology.MeasurementSystem;
using NewCore.Base.Interface.Main;
using System.Windows.Controls;
using UI.Controls.ProtocolNew;
using Utilities.Models;
using static NewCore.Enum.MetrologyEnum;

namespace Mode.Metrology.IE
{
  /// <summary>
  /// Логика взаимодействия для IeMetrologyControl.xaml.
  /// </summary>
  public partial class IeMetrologyControl : UserControl, IExecution
  {
    MetrologicalModeRole metrologicalModeRole => MetrologicalModeRole.IE;

    IeMeasurement testMeasurement = new IeMeasurement();

    (bool Success, string Message, DataModel DataModel) Data;

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

      ProtocolUI.SetSettings(
        this,
        StartDelegate: ExecuteMeasurementProcess,
        true,
        ReturnDelegate: async (CancellationToken token) =>
        {
          return await testMeasurement.PerformMeasurement(metrologicalModeRole, Data.DataModel.Param, ProtocolUI);
        });
    }

    /// <summary>
    /// Выполнение контроля.
    /// </summary>
    /// <param name="cancellationToken"></param>
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
      await testMeasurement.ConfigureMeter(metrologicalModeRole);
      await testMeasurement.PerformMeasurement(metrologicalModeRole, param, ProtocolUI);
      await testMeasurement.FinalizeMeasurement();
    }

    public ITextAdapter GetControl()
    {
      return ProtocolUI;
    }

    private class IeMeasurement : BaseMeasurement
    {
      public IeMeasurement() : base() { }

      /// <inheritdoc />
      public override async Task ConfigureMeter(MetrologicalModeRole metrologicalModeRole, DataModel dataModel = null)
      {
        await base.ConfigureMeter(metrologicalModeRole, dataModel);
        var fastMeter = Devices.TryGetValue(metrologicalModeRole, out var meter) ? meter.OfType<IFastMeter>().FirstOrDefault() : null;
        await fastMeter.CapacitanceManager.SetCapacitanceModeAsync();
      }

      /// <inheritdoc />
      public override async Task<bool> PerformMeasurement(MetrologicalModeRole metrologicalModeRole, double param, ProtocolUI protocolUI)
      {
        var fastMeter = Devices.TryGetValue(metrologicalModeRole, out var meter) ? meter.OfType<IFastMeter>().FirstOrDefault() : null;
        await protocolUI.ShowMessageAsync(new ShowMessageModel(header: "Выполнение измерения ёмкости"));
        var (firstNorm, lastNorm) = ErrorProviderLocator.Provider.GetRange(TypeCommand.IE, param);

        var result = await fastMeter.CapacitanceManager.MeasureCapacitanceAsync();

        await protocolUI.ShowMessageAsync(new ShowMessageModel("Результат измерения ёмкости", message: $"{result} нФ", type: (result >= firstNorm && result <= lastNorm ? ShowMessageModel.MessageType.Success : ShowMessageModel.MessageType.Error)) { IndentLevel = 1 });
        await protocolUI.ShowMessageAsync(new ShowMessageModel("Диапазон допускаемых значений", message: $"от {firstNorm} до {lastNorm} нФ") { IndentLevel = 2 });
        await protocolUI.ShowMessageAsync(new ShowMessageModel("Погрешность измерения", message: $"{(Math.Abs(result - param))} нФ", type: (result >= firstNorm && result <= lastNorm ? ShowMessageModel.MessageType.Success : ShowMessageModel.MessageType.Error)) { IndentLevel = 2 });
        return true;
      }
    }
  }
}

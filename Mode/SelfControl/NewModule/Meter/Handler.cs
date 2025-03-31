using System.Globalization;
using System.Windows;
using System.Windows.Media;
using DataBaseConfiguration.Services;
using NewCore.Base.Device;
using NewCore.Base.Interface.Main;
using UI.Controls.Protocol;
using Utilities.Models;
using static AppConfiguration.Execution.ExecutionConfig;
using static Utilities.DelegateManager;
using static Utilities.LoggerUtility;
using static Utilities.Models.ShowMessageModel;

namespace Mode.SelfControl.NewModule.Meter
{
  /// <summary>
  /// Класс Handler выполняет самоконтроль мультиметра, осуществляет проверку сопротивления и ёмкости.
  /// </summary>
  internal class Handler
  {
    ProtocolUI ProtocolSelfCheckControl;
    private readonly Tuple<string, Color> goodText = SuccessMessage;
    private readonly Tuple<string, Color> errorText = ErrorMessage;
    private IFastMeter meter;
    private ISwitchingDevice deviceBusCommutation;

    /// <summary>
    /// Словарь номиналов резисторов.
    /// </summary>
    readonly Dictionary<int, double> resistanceValue = new Dictionary<int, double>()
    {
      { 1 , 1.7 },
      { 2 , 100 },
      { 3 , 1000 },
      { 4 , 10000 },
      { 5 , 100000 },
      { 6 , 1000000 },
      { 7 , 10400000 },
      { 8 , 87000000 },
    };

    /// <summary>
    /// Словарь номиналов конденсаторов.
    /// </summary>
    readonly Dictionary<int, double> capacitanceValue = new Dictionary<int, double>()
    {
      { 1 , 0.001 },
      { 2 , 1.07 },
      { 3 , 6.2 },
      { 4 , 0.01 },
      { 5 , 100 },
      { 6 , 0.13 },
    };

    bool returnMeasure = false;

    /// <summary>
    /// Конструктор Handler, принимающий ProtocolSelfCheckControl и модель устройства.
    /// </summary>
    /// <param name="protocolSelfCheck">Объект для управления протоколом самоконтроля.</param>
    /// <param name="deviceModel">Модель устройства для создания объекта коммутации шин.</param>
    internal Handler(ProtocolUI protocolSelfCheck, ISwitchingDevice deviceModel)
    {
      ProtocolSelfCheckControl = protocolSelfCheck;
      deviceBusCommutation = deviceModel;
      var chassisNumber = deviceBusCommutation.NumberChassis;
      meter = new FastMeterServices().GetDevicesByNumberChassis(chassisNumber).FirstOrDefault();
    }

    /// <summary>
    /// Возвращает делегат старта самоконтроля.
    /// </summary>
    internal StartDelegate GetStartDelegate()
    {
      StartDelegate startDelegate = RunSelfCheck;
      return startDelegate;
    }

    /// <summary>
    /// Возвращает делегат остановки самоконтроля.
    /// </summary>
    internal StopDelegate GetStopDelegate()
    {
      StopDelegate stopDelegate = StopAsync;
      return stopDelegate;
    }

    /// <summary>
    /// Осуществляет завершение самоконтроля: финализирует протокол и выводит итоговое сообщение.
    /// </summary>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    private async Task StopAsync(CancellationToken cancellationToken)
    {
      LogInformation("Запущен метод завершения самоконтроля");
      await ProtocolSelfCheckControl.FinalizeAsync();
      await ProtocolSelfCheckControl.ShowMessageAsync(
        new ShowMessageModel("\tСамоконтроль", null, $"[{goodText.Item1}]", goodText.Item2));
      LogInformation("Завершён метод завершения самоконтроля");
    }

    /// <summary>
    /// Выполняет самоконтроль мультиметра. Подключается к устройствам, устанавливает соединение с УКШ,
    /// подключает цепи, выполняет проверки сопротивления и ёмкости.
    /// </summary>
    private async Task RunSelfCheck(CancellationToken token)
    {
      //if (!await ProtocolSelfCheckControl.AttemptDeviceConnection(
      //      new List<IDevice> { meter, deviceBusCommutation },
      //      ProtocolSelfCheckControl.ShowMessageAsync))
      //{
      //  return;
      //}

      await ProtocolSelfCheckControl.ShowMessageAsync(
        new ShowMessageModel("\r\nСамоконтроль мультиметра", goodText.Item2));

      if (!await GetIsIdleModeEnabled())
      {
        await deviceBusCommutation.ConnectorManager.ConnectBreakdownTester();
      }

      await CheckResistance(token);
      await CheckCapacitance(token);

      if (!await GetIsIdleModeEnabled())
      {
        await deviceBusCommutation.ConnectorManager.DisconnectBreakdownTester();
      }
    }

    /// <summary>
    /// Производит проверку сопротивления для резисторов.
    /// Измеряет сопротивление, сравнивает с допустимыми пределами и выводит результат.
    /// </summary>
    private async Task CheckResistance(CancellationToken token)
    {
      ProtocolSelfCheckControl.GetCancellationToken().ThrowIfCancellationRequested();
      await ProtocolSelfCheckControl.ShowMessageAsync(
        new ShowMessageModel("\tКонтроль сопротивления", goodText.Item2));

      await meter.ResistanceManager.MeasureResistanceAsync();

      await Task.Delay(1, token);
      for (int i = 1; i <= 8; i++)
      {
        ProtocolSelfCheckControl.GetCancellationToken().ThrowIfCancellationRequested();
        if (!await GetIsIdleModeEnabled())
        {
          await deviceBusCommutation.ResistorManager.ConnectResistor(i.ToString(CultureInfo.InvariantCulture));
        }

        resistanceValue.TryGetValue(i, out double meaning);
        double result = await GetIsErrorSimulationEnabled() ? 0 : meaning;
        double first = meaning - (0.01 * meaning + 5);
        double last = meaning + (0.01 * meaning + 5);

        if (!await GetIsIdleModeEnabled())
        {
          result = await meter.ResistanceManager.MeasureResistanceAsync();
          await deviceBusCommutation.ResistorManager.DisconnectResistor(i.ToString(CultureInfo.InvariantCulture));
        }

        if (result >= first && result <= last)
        {
          await ProtocolSelfCheckControl.ShowMessageAsync(
            new ShowMessageModel($"\t\tРезистор №{i}: ({first} - {last} Ом)", null, $"{result} Ом", goodText.Item2));
        }
        else
        {
          await ProtocolSelfCheckControl.ShowMessageAsync(
            new ShowMessageModel($"\t\tРезистор №{i}: ({first} - {last} Ом)", null, $"{result} Ом", errorText.Item2));
          if (await GetIsStopOnErrorEnabled())
          {
            await ProtocolSelfCheckControl.PauseAsync();
            if (returnMeasure)
            {
              i--;
              returnMeasure = false;
              ProtocolSelfCheckControl.ReturnMeasureResistanceButtonVisibility = Visibility.Collapsed;
            }
          }
        }

        await Task.Delay(1, token);
      }
    }

    /// <summary>
    /// Производит проверку ёмкости для конденсаторов.
    /// Измеряет ёмкость, сравнивает с допустимыми пределами и выводит результат.
    /// </summary>
    private async Task CheckCapacitance(CancellationToken token)
    {
      ProtocolSelfCheckControl.GetCancellationToken().ThrowIfCancellationRequested();
      await ProtocolSelfCheckControl.ShowMessageAsync(
        new ShowMessageModel("\tКонтроль ёмкости", goodText.Item2));
      for (int i = 1; i <= 6; i++)
      {
        ProtocolSelfCheckControl.GetCancellationToken().ThrowIfCancellationRequested();
        if (!await GetIsIdleModeEnabled())
        {
          await deviceBusCommutation.CapacitorManager.ConnectCapacitor(i.ToString(CultureInfo.InvariantCulture));
        }

        capacitanceValue.TryGetValue(i, out double meaning);
        double result = meaning;
        double first = meaning - (0.05 * meaning + 0.0005);
        double last = meaning + (0.05 * meaning + 0.0005);

        if (!await GetIsIdleModeEnabled())
        {
          result = await meter.CapacitanceManager.MeasureCapacitanceAsync();
          await deviceBusCommutation.CapacitorManager.DisconnectCapacitor(i.ToString(CultureInfo.InvariantCulture));
        }

        if (result >= first && result <= last)
        {
          await ProtocolSelfCheckControl.ShowMessageAsync(
            new ShowMessageModel($"\t\tКонденсатор №{i}: ({first} - {last} мкФ)", null, $"{result} мкФ", goodText.Item2));
        }
        else
        {
          await ProtocolSelfCheckControl.ShowMessageAsync(
            new ShowMessageModel($"\t\tКонденсатор №{i}: ({first} - {last} мкФ)", null, $"{result} мкФ", errorText.Item2));
        }

        await Task.Delay(200, token);
      }
    }
  }
}

using System.Globalization;
using System.Net;
using System.Windows;
using System.Windows.Media;
using Core.Abstract;
using Core.ConfigCollector;
using UI.Controls.Protocol;
using Utilities.Models;
using static AppConfig.Config.ExecutionConfig;
using static Utilities.DelegateManager;
using static Utilities.LoggerUtility;
using static Utilities.Models.ShowMessageModel;

namespace Mode.SelfControl.Module.Meter
{
  internal class Handler
  {
    ProtocolUI ProtocolSelfCheckControl;
    private readonly Tuple<string, Color> goodText = SuccessMessage;
    private readonly Tuple<string, Color> errorText = ErrorMessage;
    private MeterBase meter;
    private Core.DeviceBusCommutation.Model deviceBusCommutation;
    /// <summary>
    /// Номинал резисторов.
    /// </summary>
    readonly Dictionary<int, double> resistanceValue = new Dictionary<int, double>()
    {
      { 1 , 1.7},
      { 2 , 100},
      { 3 , 1000},
      { 4 , 10000},
      { 5 , 100000},
      { 6 , 1000000},
      { 7 , 10400000},
      { 8 , 87000000},
    };

    /// <summary>
    /// Номинал конденсаторов.
    /// </summary>
    readonly Dictionary<int, double> capacitanceValue = new Dictionary<int, double>()
    {
      { 1 , 0.001},
      { 2 , 1.07},
      { 3 , 6.2},
      { 4 , 0.01},
      { 5 , 100},
      { 6 , 0.13},
    };

    bool returnMeasure = false;

    internal Handler(ProtocolUI protocolSelfCheck, object deviceModel)
    {
      ProtocolSelfCheckControl = protocolSelfCheck;

      // TODO : Переопределить мультиметр
      // meter = Core.KeysightLibrary.Model.CreateFromObject(deviceModel);
    }

    internal StartDelegate GetStartDelegate()
    {
      StartDelegate startDelegate = RunSelfCheck;
      return startDelegate;
    }

    internal StopDelegate GetStopDelegate()
    {
      StopDelegate stopDelegate = StopAsync;
      return stopDelegate;
    }

    /// <summary>
    /// Останавливает самоконтроль, отключая необходимые компоненты и отображая соответствующие сообщения.
    /// </summary>
    private async Task StopAsync(CancellationToken cancellationToken)
    {
      LogInformation($"Запущен метод завершения самоконтроля");
      await ProtocolSelfCheckControl.FinalizeAsync();
      await ProtocolSelfCheckControl.ShowMessageAsync(new ShowMessageModel("\tСамоконтроль", null, $"[{goodText.Item1}]", goodText.Item2));
      LogInformation($"Завершён метод завершения самоконтроля");
    }

    /// <summary>
    /// Асинхронный метод для выполнения самоконтроля.
    /// </summary>
    private async Task RunSelfCheck(CancellationToken token)
    {
      deviceBusCommutation = ConfigCollector.GetDeviceBusCommutation();
      if (!await ProtocolSelfCheckControl.AttemptDeviceConnection(new List<Core.Model.DeviceModel> { meter, deviceBusCommutation }, ProtocolSelfCheckControl.ShowMessageAsync))
      {
        return;
      }

      // TODO : Переопределить мультиметр
      // meter = await Core.KeysightLibrary.Model.CreateAsync();
      await ProtocolSelfCheckControl.ShowMessageAsync(new ShowMessageModel("\r\nСамоконтроль мультиметра", goodText.Item2));

      if (!await GetIsIdleModeEnabled())
      {
        await Core.DeviceBusCommutation.Functions.ConnectXs9ToXs4(deviceBusCommutation.IPAddress);
      }
      await CheckResistance(token);
      await CheckCapacitance(token);

      if (!await GetIsIdleModeEnabled())
      {
        await Core.DeviceBusCommutation.Functions.DisconnectXs9ToXs4(deviceBusCommutation.IPAddress);
      }
    }

    /// <summary>
    /// Контроль сопротивления.
    /// </summary>
    /// <param name="token"></param>
    /// <returns></returns>
    private async Task CheckResistance(CancellationToken token)
    {
      ProtocolSelfCheckControl.GetCancellationToken().ThrowIfCancellationRequested();
      await ProtocolSelfCheckControl.ShowMessageAsync(new ShowMessageModel("\tКонтроль сопротивления", goodText.Item2));
      meter.MeasureResistance();
      await Task.Delay(1, token);
      for (int i = 1; i <= 8; i++)
      {
        ProtocolSelfCheckControl.GetCancellationToken().ThrowIfCancellationRequested();
        if (!await GetIsIdleModeEnabled())
        {
          await Core.DeviceBusCommutation.Functions.ConnectResistor(IPAddress.Parse("192.168.0.20"), i.ToString(CultureInfo.InvariantCulture));
        }

        resistanceValue.TryGetValue(i, out double meaning);
        double result = await GetIsErrorSimulationEnabled() ? 0 : meaning;
        double first = meaning - (0.01 * meaning + 5);
        double last = meaning + (0.01 * meaning + 5);

        if (!await GetIsIdleModeEnabled())
        {
          result = meter.MeasureResistance();
          await Core.DeviceBusCommutation.Functions.DisconnectResistor(IPAddress.Parse("192.168.0.20"), i.ToString(CultureInfo.InvariantCulture));
        }
        if (result >= first && result <= last)
        {
          await ProtocolSelfCheckControl.ShowMessageAsync(new ShowMessageModel($"\t\tРезистор №{i}: ({first} - {last} Ом)", null, $"{result} Ом", goodText.Item2));
        }
        else
        {
          await ProtocolSelfCheckControl.ShowMessageAsync(new ShowMessageModel($"\t\tРезистор №{i}:({first} - {last}) Ом", null, $"{result} Ом", errorText.Item2));
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
    /// Контроль ёмкости.
    /// </summary>
    /// <param name="token"></param>
    /// <returns></returns>
    private async Task CheckCapacitance(CancellationToken token)
    {
      ProtocolSelfCheckControl.GetCancellationToken().ThrowIfCancellationRequested();
      await ProtocolSelfCheckControl.ShowMessageAsync(new ShowMessageModel("\tКонтроль ёмкости", goodText.Item2));
      for (int i = 1; i <= 6; i++)
      {
        ProtocolSelfCheckControl.GetCancellationToken().ThrowIfCancellationRequested();
        if (!await GetIsIdleModeEnabled())
        {
          await Core.DeviceBusCommutation.Functions.ConnectCapacitor(deviceBusCommutation.IPAddress, i.ToString(CultureInfo.InvariantCulture));
        }

        capacitanceValue.TryGetValue(i, out double meaning);
        double result = meaning;
        double first = meaning - (0.05 * meaning + 0.0005);
        double last = meaning + (0.05 * meaning + 0.0005);

        if (!await GetIsIdleModeEnabled())
        {
          result = meter.MeasureCapacitance();
          await Core.DeviceBusCommutation.Functions.DisconnectCapacitor(IPAddress.Parse("192.168.0.20"), i.ToString(CultureInfo.InvariantCulture));
        }

        if (result >= first && result <= last)
        {
          await ProtocolSelfCheckControl.ShowMessageAsync(new ShowMessageModel($"\t\tКонденсатор №{i}: ({first} - {last} мкФ)", null, $"{result} мкФ", goodText.Item2));
        }
        else
        {
          await ProtocolSelfCheckControl.ShowMessageAsync(new ShowMessageModel($"\t\tКонденсатор №{i}: ({first} - {last} мкФ)", null, $"{result} мкФ", errorText.Item2));
        }

        await Task.Delay(200, token);
      }
    }
  }
}

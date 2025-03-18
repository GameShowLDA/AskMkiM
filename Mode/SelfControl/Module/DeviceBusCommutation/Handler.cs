using System.Globalization;
using System.Net;
using System.Windows;
using System.Windows.Media;
using Core.Abstract;
using Core.Model;
using UI.Controls.Protocol;
using Utilities.Models;
using static AppConfig.Config.ExecutionConfig;
using static Core.Enum.DeviceEnum;
using static Utilities.DelegateManager;
using static Utilities.LoggerUtility;
using static Utilities.Models.ShowMessageModel;

namespace Mode.SelfControl.Module.DeviceBusCommutation
{
  internal class Handler
  {
    ProtocolUI ProtocolSelfCheckControl;
    private readonly Tuple<string, Color> goodText = SuccessMessage;
    private readonly Tuple<string, Color> errorText = ErrorMessage;
    private Core.DeviceBusCommutation.Model deviceBusCommutation;
    List<int> errorRelays;
    MeterBase meter;
    internal Handler(ProtocolUI protocolSelfCheck, object deviceModel)
    {
      ProtocolSelfCheckControl = protocolSelfCheck;
      deviceBusCommutation = Core.DeviceBusCommutation.Model.CreateFromObject(deviceModel);

      // TODO : Переопределить мультиметр
      // meter = new Core.KeysightLibrary.Model();
    }

    #region StartDelegate
    internal StartDelegate GetStartDelegate()
    {
      StartDelegate startDelegate = RunSelfCheck;
      return startDelegate;
    }

    /// <summary>
    /// Асинхронный метод для выполнения самоконтроля.
    /// </summary>
    private async Task RunSelfCheck(CancellationToken token)
    {
      if (deviceBusCommutation == null)
      {
        await ProtocolSelfCheckControl.ShowMessageAsync(new ShowMessageModel("Модель УКШ не найдена!", goodText.Item2));
        return;
      }

      await ProtocolSelfCheckControl.ShowMessageAsync(new ShowMessageModel("Запуск проверки оборудования", goodText.Item2));
      ProtocolSelfCheckControl.GetCancellationToken().ThrowIfCancellationRequested();

      if (!await ProtocolSelfCheckControl.AttemptDeviceConnection(new List<DeviceModel>() { deviceBusCommutation, meter }, ProtocolSelfCheckControl.ShowMessageAsync))
      {
        return;
      }

      await Core.DeviceBusCommutation.Functions.ResetAsync(deviceBusCommutation.IPAddress);
      await ProtocolSelfCheckControl.ShowMessageAsync(new ShowMessageModel("Самоконтроль УКШ".ToUpper(CultureInfo.CurrentCulture), goodText.Item2));

      errorRelays = new List<int>();

      foreach (RelayCheck checkType in Enum.GetValues(typeof(RelayCheck)))
      {
        ProtocolSelfCheckControl.GetCancellationToken().ThrowIfCancellationRequested();

        Tuple<int, List<List<int>>> selfTestRelays = GetSelfTestDeviceBusCommutation(checkType);
        Console.WriteLine(GetInfoBlock(checkType));

        await ProtocolSelfCheckControl.ShowMessageAsync(new ShowMessageModel($"{GetInfoBlock(checkType).ToUpper(CultureInfo.CurrentCulture)}", goodText.Item2));
        if (!await CheckRelaysWithMultimeterAsync(token, selfTestRelays.Item1, (int)checkType, selfTestRelays.Item2))
        {
          await ProtocolSelfCheckControl.RemoveLineContainingTextAsync(GetInfoBlock(checkType).ToUpper(CultureInfo.CurrentCulture));
        }
      }

      if (errorRelays.Count > 0)
      {
        errorRelays.Sort();

        await ProtocolSelfCheckControl.ShowMessageAsync(new ShowMessageModel("\tСписок неотработанных реле:", errorText.Item2));
        foreach (var item in errorRelays)
        {
          await ProtocolSelfCheckControl.ShowMessageAsync(new ShowMessageModel($"\t\tРеле ", null, item.ToString(CultureInfo.CurrentCulture), errorText.Item2));
        }
      }

      ProtocolSelfCheckControl.PauseButtonVisibility = Visibility.Collapsed;
      await ProtocolSelfCheckControl.ShowMessageAsync(new ShowMessageModel("\tСамоконтроль", goodText.Item2, $"[{goodText.Item1}]", goodText.Item2));
    }

    /// <summary>
    /// Асинхронно проверяет реле с использованием мультиметра для заданного числа шин и блока.
    /// </summary>
    /// <param name="countBuses">Количество цепей (шины), которые необходимо проверить.</param>
    /// <param name="numberBlock">Номер блока для проверки реле.</param>
    /// <param name="relays">Список списков идентификаторов реле для каждой цепи.</param>
    private async Task<bool> CheckRelaysWithMultimeterAsync(CancellationToken token, int countBuses, int numberBlock, List<List<int>> relays)
    {
      var time = 10;
      bool error = false;

      for (int i = 1; i <= countBuses; i++)
      {
        ProtocolSelfCheckControl.GetCancellationToken().ThrowIfCancellationRequested();
        await Application.Current.Dispatcher.Invoke(() => ProtocolSelfCheckControl.CheckStepModeAsync());

        string relaysStr;
        try
        {
          relaysStr = $"Номера реле: \"{string.Join(", ", relays[i - 1])}\"";
        }
        catch (Exception ex)
        {
          relaysStr = ex.ToString();
        }

        LogInformation($"Начата проверка блока {numberBlock} - номер цепи {i} - ({relaysStr})");


        if (!await GetIsIdleModeEnabled())
        {
          foreach (var relay in relays[i - 1])
          {
            Core.DeviceBusCommutation.Functions.ConnectRelayIdleMode(relay);
          }
          await Core.DeviceBusCommutation.Functions.ConnectChainCircuit(IPAddress.Parse("192.168.0.20"), numberBlock, i);
        }

        await Task.Delay(time);
        double result = await GetIsIdleModeEnabled() ? await GetIsErrorSimulationEnabled() ? 9.9E+37 : 0 : meter.MeasureContinuity();
        bool success = result != 9.9E+37;
        string statusMessage = await GetStatusMessage(success, await GetIsErrorSimulationEnabled());

        ShowMessageModel showMessageModel = new ShowMessageModel($"\tЦепь {i}-({relaysStr})", null, statusMessage, success ? goodText.Item2 : errorText.Item2);
        showMessageModel.CanBeDeleted = false;
        await ProtocolSelfCheckControl.ShowMessageAsync(showMessageModel);

        if (success)
        {
          bool relayError = false;

          if (!await CheckRelays(relays[i - 1], token, time) && !error)
          {
            relayError = true;
          }

          if (!relayError)
          {
            await ProtocolSelfCheckControl.RemoveLineContainingTextAsync(showMessageModel.ToString());
          }
        }
        else if (!error)
        {
          error = true;
        }


        LogInformation($"Закончена проверка блока {numberBlock} - номер цепи {i} - ({relaysStr})");

        if (!await GetIsIdleModeEnabled())
        {
          foreach (var relay in relays[i - 1])
          {
            Core.DeviceBusCommutation.Functions.DisconnectRelayIdleMode(relay);
          }
          await Core.DeviceBusCommutation.Functions.DisconnectChainCircuit(IPAddress.Parse("192.168.0.20"), numberBlock, i);
        }

        if (!success && await GetIsStopOnErrorEnabled())
        {
          ProtocolSelfCheckControl.ReturnMeasureResistanceButtonVisibility = Visibility.Visible;
          await ProtocolSelfCheckControl.PauseAsync();
          //if (returnMeasure)
          //{
          //  i--;
          //  returnMeasure = false;
          //  ProtocolSelfCheckControl.ReturnMeasureResistanceButtonVisibility = Visibility.Collapsed;
          //}
        }

        await Task.Delay(time);
      }
      return error;
    }

    /// <summary>
    /// Проверка реле на залипание в собранной цепочке.
    /// </summary>
    /// <param name="relays">Список реле.</param>
    /// <param name="token">Токен отмены.</param>
    /// <param name="time">Время задержки в миллисекундах</param>
    /// <returns></returns>
    private async Task<bool> CheckRelays(List<int> relays, CancellationToken token, int time)
    {
      bool error = false;
      for (int i = 0; i < relays.Count; i++)
      {
        int relay = relays[i];

        ProtocolSelfCheckControl.GetCancellationToken().ThrowIfCancellationRequested();
        await Application.Current.Dispatcher.Invoke(() => ProtocolSelfCheckControl.CheckStepModeAsync());

        bool success = await DisconnectRelayAndCheckStatus(relay, time);
        if (!error && !success)
        {
          error = true;
        }

        if (!success && await GetIsStopOnErrorEnabled())
        {
          if (await HandleRelayError(i))
          {
            i--;
          }
        }

        await ConnectRelay(relay);
      }

      return error;
    }
    /// <summary>
    /// Отключает реле и проверяет его состояние.
    /// </summary>
    /// <param name="relay">Номер реле.</param>
    /// <param name="time">Время задержки в миллисекундах.</param>
    /// <returns>True, если реле отключено успешно, иначе False.</returns>
    private async Task<bool> DisconnectRelayAndCheckStatus(int relay, int time)
    {
      await ProtocolSelfCheckControl.ShowMessageAsync(new ShowMessageModel($"\t\tОтключение реле", null, relay.ToString(CultureInfo.CurrentCulture), goodText.Item2) { CanBeDeleted = true });
      bool success = false;

      if (!await GetIsIdleModeEnabled())
      {
        await Core.DeviceBusCommutation.Functions.DisconnectRelay(deviceBusCommutation.IPAddress, relay);
        await Task.Delay(time);
        if (meter.MeasureContinuity() == 9.9E+37)
        {
          success = true;
        }
        else
        {
          if (!errorRelays.Contains(relay))
          {
            errorRelays.Add(relay);
          }
        }
      }

      string statusMessage = await GetStatusMessage(success, await GetIsErrorSimulationEnabled());
      await ProtocolSelfCheckControl.ShowMessageAsync(new ShowMessageModel($"\t\t\tРеле {relay}", null, statusMessage, !success ? goodText.Item2 : errorText.Item2) { CanBeDeleted = !success });

      return success;
    }

    /// <summary>
    /// Обрабатывает ошибку реле.
    /// </summary>
    /// <param name="currentIndex">Текущий индекс реле.</param>
    /// <returns>True, если нужно повторить проверку, иначе False.</returns>
    private async Task<bool> HandleRelayError(int currentIndex)
    {
      ProtocolSelfCheckControl.ReturnMeasureResistanceButtonVisibility = Visibility.Visible;
      await ProtocolSelfCheckControl.PauseAsync();
      //if (returnMeasure)
      //{
      //  returnMeasure = false;
      //  ProtocolSelfCheckControl.ReturnMeasureResistanceButtonVisibility = Visibility.Collapsed;
      //  return true;
      //}
      return false;
    }

    /// <summary>
    /// Подключает реле.
    /// </summary>
    /// <param name="relay">Номер реле.</param>
    /// <returns></returns>
    private async Task ConnectRelay(int relay)
    {
      await ProtocolSelfCheckControl.ShowMessageAsync(new ShowMessageModel($"\t\tПодключение реле", null, relay.ToString(CultureInfo.CurrentCulture), goodText.Item2) { CanBeDeleted = true });
      if (!await GetIsIdleModeEnabled())
      {
        await Core.DeviceBusCommutation.Functions.ConnectRelay(deviceBusCommutation.IPAddress, relay);
      }
    }

    /// <summary>
    /// Генерирует статусное сообщение на основе успешности проверки и режима ошибок.
    /// </summary>
    /// <param name="success">Флаг успешного прохождения проверки.</param>
    /// <param name="isErrorSimulationMode">Флаг режима имитации ошибок.</param>
    /// <returns>Статусное сообщение.</returns>
    private async Task<string> GetStatusMessage(bool success, bool isErrorSimulationMode)
    {
      string statusMessage = await GetIsIdleModeEnabled()
          ? isErrorSimulationMode
              ? $"[{errorText.Item1}]"
              : $"[{goodText.Item1}]"
          : !success
              ? $"[{errorText.Item1}]"
              : $"[{goodText.Item1}]";

      return statusMessage;
    }
    #endregion

    #region StopDelegate
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
    #endregion
  }
}

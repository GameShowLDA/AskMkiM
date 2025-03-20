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
  /// <summary>
  /// Класс Handler реализует логику самоконтроля для устройств коммутации шин. 
  /// Он подключается к устройствам, выполняет сброс системы, проверяет реле с использованием мультиметра,
  /// отображает статусные сообщения и обрабатывает ошибки, связанные с реле.
  /// </summary>
  internal class Handler
  {
    ProtocolUI ProtocolSelfCheckControl;
    private readonly Tuple<string, Color> goodText = SuccessMessage;
    private readonly Tuple<string, Color> errorText = ErrorMessage;
    private Core.DeviceBusCommutation.Model deviceBusCommutation;
    List<int> errorRelays;
    MeterBase meter;

    /// <summary>
    /// Инициализирует объект Handler, используя объект ProtocolSelfCheckControl и модель устройства.
    /// </summary>
    /// <param name="protocolSelfCheck">Объект для управления протоколом самоконтроля.</param>
    /// <param name="deviceModel">Модель устройства для создания объекта коммутации шин.</param>
    internal Handler(ProtocolUI protocolSelfCheck, object deviceModel)
    {
      ProtocolSelfCheckControl = protocolSelfCheck;
      deviceBusCommutation = Core.DeviceBusCommutation.Model.CreateFromObject(deviceModel);

      // TODO: Переопределить мультиметр
      // meter = new Core.KeysightLibrary.Model();
    }

    #region StartDelegate

    /// <summary>
    /// Возвращает делегат, ссылающийся на метод RunSelfCheck, для запуска процесса самоконтроля.
    /// </summary>
    /// <returns>Делегат StartDelegate.</returns>
    internal StartDelegate GetStartDelegate()
    {
      StartDelegate startDelegate = RunSelfCheck;
      return startDelegate;
    }

    /// <summary>
    /// Выполняет самоконтроль. Метод проверяет наличие модели устройства, подключается к устройствам,
    /// сбрасывает систему, выполняет проверку реле для каждого блока, отображает результаты проверки и скрывает кнопку паузы.
    /// </summary>
    /// <param name="token">Токен отмены операции.</param>
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
      await ProtocolSelfCheckControl.ShowMessageAsync(new ShowMessageModel("САМООБНАРУЖЕНИЕ УКШ".ToUpper(CultureInfo.CurrentCulture), goodText.Item2));

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
    /// Проверяет реле с использованием мультиметра для заданного количества шин и блока.
    /// Для каждой цепи выполняется подключение, проверка состояния, отключение и отображение статусного сообщения.
    /// </summary>
    /// <param name="token">Токен отмены.</param>
    /// <param name="countBuses">Количество цепей для проверки.</param>
    /// <param name="numberBlock">Номер блока проверки реле.</param>
    /// <param name="relays">Список списков номеров реле для каждой цепи.</param>
    /// <returns>True, если обнаружены ошибки; иначе False.</returns>
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
        }

        await Task.Delay(time);
      }

      return error;
    }

    /// <summary>
    /// Проверяет реле в собранной цепочке, отключая и проверяя каждое реле, а затем подключая его обратно.
    /// Если реле не проходит проверку, оно добавляется в список ошибок.
    /// </summary>
    /// <param name="relays">Список номеров реле для проверки.</param>
    /// <param name="token">Токен отмены.</param>
    /// <param name="time">Время задержки в миллисекундах для проверки.</param>
    /// <returns>True, если обнаружены ошибки; иначе False.</returns>
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
    /// Отключает указанное реле и проверяет его состояние с помощью мультиметра.
    /// Если измеренное значение равно 9.9E+37, реле считается отключенным успешно; иначе, реле добавляется в список ошибок.
    /// </summary>
    /// <param name="relay">Номер реле для проверки.</param>
    /// <param name="time">Время задержки в миллисекундах для проведения измерения.</param>
    /// <returns>True, если реле отключено успешно; иначе False.</returns>
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
    /// Обрабатывает ошибку, возникшую при проверке реле. Отображает кнопку возврата и приостанавливает процесс.
    /// </summary>
    /// <param name="currentIndex">Индекс реле, вызвавшего ошибку.</param>
    /// <returns>True, если необходимо повторить проверку данного реле; иначе False.</returns>
    private async Task<bool> HandleRelayError(int currentIndex)
    {
      ProtocolSelfCheckControl.ReturnMeasureResistanceButtonVisibility = Visibility.Visible;
      await ProtocolSelfCheckControl.PauseAsync();
      return false;
    }

    /// <summary>
    /// Подключает указанное реле и отображает сообщение о подключении.
    /// </summary>
    /// <param name="relay">Номер реле для подключения.</param>
    private async Task ConnectRelay(int relay)
    {
      await ProtocolSelfCheckControl.ShowMessageAsync(new ShowMessageModel($"\t\tПодключение реле", null, relay.ToString(CultureInfo.CurrentCulture), goodText.Item2) { CanBeDeleted = true });
      if (!await GetIsIdleModeEnabled())
      {
        await Core.DeviceBusCommutation.Functions.ConnectRelay(deviceBusCommutation.IPAddress, relay);
      }
    }

    /// <summary>
    /// Генерирует статусное сообщение для реле в зависимости от результата проверки и режима имитации ошибок.
    /// </summary>
    /// <param name="success">Указывает, успешно ли прошло отключение реле.</param>
    /// <param name="isErrorSimulationMode">Указывает, включен ли режим имитации ошибок.</param>
    /// <returns>Статусное сообщение в виде строки.</returns>
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
    /// <summary>
    /// Возвращает делегат остановки самоконтроля, ссылающийся на метод StopAsync.
    /// </summary>
    /// <returns>Делегат StopDelegate.</returns>
    internal StopDelegate GetStopDelegate()
    {
      StopDelegate stopDelegate = StopAsync;
      return stopDelegate;
    }

    /// <summary>
    /// Завершает процесс самоконтроля, выполняет финализацию протокола и отображает итоговое сообщение.
    /// </summary>
    /// <param name="cancellationToken">Токен отмены операции.</param>
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

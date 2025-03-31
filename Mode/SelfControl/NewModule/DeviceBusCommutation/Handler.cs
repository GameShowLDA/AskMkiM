using System.Windows.Media;
using DataBaseConfiguration.Services;
using NewCore.Base.Device;
using NewCore.Base.Interface.Main;
using UI.Controls.Protocol;
using Utilities.Models;
using static Utilities.DelegateManager;
using static Utilities.LoggerUtility;
using static Utilities.Models.ShowMessageModel;

namespace Mode.SelfControl.NewModule.DeviceBusCommutation
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

    private ISwitchingDevice deviceBusCommutation;
    IFastMeter meter;

    List<int> errorRelays;

    /// <summary>
    /// Инициализирует объект Handler, используя объект ProtocolSelfCheckControl и модель устройства.
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

      //if (!await ProtocolSelfCheckControl.AttemptDeviceConnection(new List<IDevice>() { deviceBusCommutation, meter }, ProtocolSelfCheckControl.ShowMessageAsync))
      //{
      //  return;
      //}

      await deviceBusCommutation.ConnectableManager.ResetAsync();
      errorRelays = new List<int>();

      // TODO : Реализовать самоконтроль
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

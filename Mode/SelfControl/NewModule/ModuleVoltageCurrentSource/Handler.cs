using System.Globalization;
using System.Windows;
using System.Windows.Media;
using DataBaseConfiguration.Services.Device;
using NewCore.Base.Interface.Main;
using UI.Controls.ProtocolNew;
using Utilities.Models;
using static AppConfiguration.Execution.ExecutionConfig;
using static NewCore.Enum.DeviceEnum;
using static Utilities.DelegateManager;
using static Utilities.LoggerUtility;
using static Utilities.Models.ShowMessageModel;

namespace Mode.SelfControl.NewModule.ModuleVoltageCurrentSource
{
  /// <summary>
  /// Класс Handler осуществляет самоконтроль для модуля источника напряжения и тока. 
  /// Он инициализирует устройство, проверяет формирование дискретных уровней напряжения, коммутацию МИНТ, 
  /// выполняет измерения напряжения, управляет подключением и отключением шин, и сбрасывает настройки источника.
  /// </summary>
  internal class Handler
  {
    ProtocolUI ProtocolSelfCheckControl;
    private IPowerSourceModule moduleVoltageCurrentSource;
    private IFastMeter meter;
    private ISwitchingDevice switchingDevice;

    /// <summary>
    /// Инициализирует новый экземпляр Handler, принимающий объект управления протоколом и модель устройства.
    /// </summary>
    /// <param name="protocolSelfCheck">Объект ProtocolSelfCheckControl для отображения сообщений и управления процессом самоконтроля.</param>
    /// <param name="deviceModel">Модель устройства, используемая для создания модели модуля источника напряжения и тока.</param>
    internal Handler(ProtocolUI protocolSelfCheck, IPowerSourceModule deviceModel)
    {
      ProtocolSelfCheckControl = protocolSelfCheck;
      moduleVoltageCurrentSource = deviceModel;
      var chassisNumber = moduleVoltageCurrentSource.NumberChassis;
      meter = new FastMeterServices().GetDevicesByNumberChassis(chassisNumber).FirstOrDefault();
      switchingDevice = new SwitchingDeviceServices().GetDevicesByNumberChassis(chassisNumber).FirstOrDefault();
    }

    /// <summary>
    /// Возвращает делегат для запуска процесса самоконтроля.
    /// </summary>
    /// <returns>Делегат StartDelegate, ссылающийся на метод RunSelfCheck.</returns>
    internal StartDelegate GetStartDelegate()
    {
      StartDelegate startDelegate = RunSelfCheck;
      return startDelegate;
    }

    /// <summary>
    /// Возвращает делегат для остановки процесса самоконтроля.
    /// </summary>
    /// <returns>Делегат StopDelegate, ссылающийся на метод StopAsync.</returns>
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
      await ProtocolSelfCheckControl.ShowMessageAsync(new ShowMessageModel("\tСамоконтроль", null, type: MessageType.Success));
      LogInformation($"Завершён метод завершения самоконтроля");
    }

    /// <summary>
    /// Асинхронный метод для настроек самоконтроля.
    /// </summary>
    private async Task RunSelfCheck(CancellationToken token)
    {
      if (meter == null)
      {
        await ProtocolSelfCheckControl.ShowMessageAsync(new ShowMessageModel("Ошибка", null, "Измеритель быстрый не задан!"));
        return;
      }

      //if (!await GetIsIdleModeEnabled())
      //{
      //  var chassisNumber = moduleVoltageCurrentSource.NumberChassis;
      //  var managerShassy = new ChassisManagerServices().GetById(chassisNumber);

      //  if (!await ProtocolSelfCheckControl.AttemptDeviceConnection(new List<IDevice>()
      //  {
      //    managerShassy,
      //    moduleVoltageCurrentSource,
      //  }, ProtocolSelfCheckControl.ShowMessageAsync))
      //  {
      //    return;
      //  }
      //}

      await ProtocolSelfCheckControl.ShowMessageAsync(new ShowMessageModel("\r\nСамоконтроль МИНТ"));

      await GenerateDiscreteVoltageCheck(token);
      ProtocolSelfCheckControl.PauseButtonVisibility = Visibility.Collapsed;
    }

    /// <summary>
    /// Проверка формирования дискрет напряжения.
    /// </summary>
    /// <param name="token">Токен для отмены операции.</param>
    private async Task GenerateDiscreteVoltageCheck(CancellationToken token)
    {
      LogInformation("Начало проверки формирования дискрет напряжения");
      await ProtocolSelfCheckControl.ShowMessageAsync(new ShowMessageModel("\tПроверка формирования дискрет напряжения"));

      if (!await GetIsIdleModeEnabled())
      {
        await InitializeVoltageCurrentSourceAsync();
      }

      await CheckVoltageLevelsAsync(0.1, 0.9, 0.1, 20, token);
      await CheckVoltageLevelsAsync(1, 9, 1, 20, token);
      await CheckVoltageLevelsAsync(10, 40, 10, 20, token);

      if (!await GetIsIdleModeEnabled())
      {
        await ResetVoltageCurrentSourceAsync();
      }

      LogInformation("Завершение проверки формирования дискрет напряжения");
    }

    /// <summary>
    /// Инициализирует источник напряжения и тока.
    /// </summary>
    private async Task InitializeVoltageCurrentSourceAsync()
    {
      LogInformation("Инициализация источника напряжения и тока");
      await switchingDevice.ConnectorManager.ConnectMultimeter(SwitchingBusNew.AB1);
      await moduleVoltageCurrentSource.BusManager.ConnectBusToPositiveAsync(SwitchingBus.A2);
      await moduleVoltageCurrentSource.BusManager.ConnectBusToPositiveAsync(SwitchingBus.B2);

      await meter.DcVoltageManager.MeasureDCVoltageAsync();
      await Task.Delay(40);
    }

    /// <summary>
    /// Проверяет уровни напряжения по заданному диапазону и шагу.
    /// </summary>
    /// <param name="startVoltage">Начальное значение напряжения.</param>
    /// <param name="endVoltage">Конечное значение напряжения.</param>
    /// <param name="step">Шаг напряжения.</param>
    /// <param name="delay">Задержка между измерениями.</param>
    /// <param name="token">Токен для отмены операции.</param>
    private async Task CheckVoltageLevelsAsync(double startVoltage, double endVoltage, double step, int delay, CancellationToken token)
    {
      LogInformation($"Проверка уровней напряжения от {startVoltage} до {endVoltage} с шагом {step}");
      for (double voltage = startVoltage; voltage <= endVoltage; voltage += step)
      {
        ProtocolSelfCheckControl.GetCancellationToken().ThrowIfCancellationRequested();
        double roundedVoltage = Math.Round(voltage, 1);
        await SetVoltageAndShowMessage(roundedVoltage);
        await SetVoltageIfNotIdle(roundedVoltage);
        await MeasureAndCompareVoltage(roundedVoltage, delay, token);
      }
    }

    /// <summary>
    /// Устанавливает напряжение и отображает сообщение.
    /// </summary>
    /// <param name="voltage">Устанавливаемое напряжение.</param>
    private async Task SetVoltageAndShowMessage(double voltage)
    {
      int a = (int)voltage;
      int b = (int)((voltage - a) * 10);
      LogInformation($"Установка напряжения {a}.{b} В");
      await ProtocolSelfCheckControl.ShowMessageAsync(new ShowMessageModel($"\t\tУстанавливаем напряжение {a}.{b} В"));
    }

    /// <summary>
    /// Устанавливает напряжение, если не в режиме ожидания.
    /// </summary>
    /// <param name="voltage">Устанавливаемое напряжение.</param>
    private async Task SetVoltageIfNotIdle(double voltage)
    {
      if (!await GetIsIdleModeEnabled())
      {
        int a = (int)voltage;
        int b = (int)((voltage - a) * 10);
        LogInformation($"Установка уровня напряжения {a}.{b} В");
        await moduleVoltageCurrentSource.VoltageManager.SetVoltageLevelAsync(a, b);
      }
    }

    /// <summary>
    /// Измеряет и сравнивает напряжение.
    /// </summary>
    /// <param name="voltage">Ожидаемое напряжение.</param>
    /// <param name="delay">Задержка перед измерением.</param>
    /// <param name="token">Токен отмены.</param>
    private async Task MeasureAndCompareVoltage(double voltage, int delay, CancellationToken token)
    {
      double tolerance = 0.0001;
      double firstNorm = voltage - (0.01 * voltage + 0.1);
      double lastNorm = voltage + (0.01 * voltage + 0.1);

      await Task.Delay(40, token).ConfigureAwait(true);
      double result = await GetMeasurementResult(voltage, delay, token);

      bool error = !(result >= firstNorm - tolerance && result <= lastNorm + tolerance);
      await ShowMeasurementResult(firstNorm, lastNorm, result, error);

      await Task.Delay(1, token);
    }

    /// <summary>
    /// Получает результат измерения.
    /// </summary>
    /// <param name="voltage">Ожидаемое напряжение.</param>
    /// <param name="delay">Задержка перед измерением.</param>
    /// <param name="token">Токен отмены.</param>
    /// <returns>Результат измерения.</returns>
    private async Task<double> GetMeasurementResult(double voltage, int delay, CancellationToken token)
    {
      if (!await GetIsIdleModeEnabled())
      {
        await Task.Delay(delay, token);
        double result = await meter.DcVoltageManager.MeasureDCVoltageAsync();
        LogInformation($"Измеренное напряжение: {result} В");
        return result;
      }
      else
      {
        double result = !await GetIsErrorSimulationEnabled() ? voltage : voltage + (0.01 * voltage + 0.1) + 1;
        LogInformation($"Симулированное напряжение: {result} В");
        return result;
      }
    }

    /// <summary>
    /// Отображает результат измерения.
    /// </summary>
    /// <param name="firstNorm">Нижняя граница нормы.</param>
    /// <param name="lastNorm">Верхняя граница нормы.</param>
    /// <param name="result">Результат измерения.</param>
    /// <param name="error">Флаг ошибки.</param>
    private async Task ShowMeasurementResult(double firstNorm, double lastNorm, double result, bool error)
    {
      var messageType = !error ? MessageType.Success: MessageType.Error;
      var statusText = !error ? "В норме" : "Вне нормы";
      LogInformation($"Результат измерения: {result} В ({firstNorm} - {lastNorm}). Статус: {statusText}");
      await ProtocolSelfCheckControl.ShowMessageAsync(new ShowMessageModel($"\t\t\tРезультат измерений ({Math.Round(firstNorm, 2)} - {Math.Round(lastNorm, 2)})", null, Math.Round(result, 2).ToString(CultureInfo.CurrentCulture) + "\r\n", type: messageType));
    }

    /// <summary>
    /// Сбрасывает настройки источника напряжения и тока.
    /// </summary>
    /// <returns>Задача, представляющая асинхронную операцию.</returns>
    private async Task ResetVoltageCurrentSourceAsync()
    {
      await moduleVoltageCurrentSource.CurrentManager.SetCurrentLevelAsync(0, 0);
      await moduleVoltageCurrentSource.BusManager.DisconnectBusToPositiveAsync(SwitchingBus.A1);
      await moduleVoltageCurrentSource.BusManager.DisconnectBusToNegativeAsync(SwitchingBus.B1);
      await switchingDevice.ConnectorManager.ConnectMultimeter(SwitchingBusNew.AB1);
    }
  }
}

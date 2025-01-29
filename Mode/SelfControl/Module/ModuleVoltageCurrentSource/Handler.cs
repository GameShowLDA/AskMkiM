using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Core.Abstract;
using Core.Model;
using static Core.DeviceBusCommutation.Enums;
using static Core.ModuleVoltageCurrentSource.Enums;
using static Utilities.DelegateManager;
using System.Windows;
using Utilities.Models;
using static AppConfig.Config.ExecutionConfig;
using static AppConfig.Config.ProtocolConfig;
using static AppConfig.Config.LoopConfig;
using static AppConfig.Config.SystemStateManager;
using static AppConfig.EventAggregator;
using static AppConfig.SettingsFileReader;
using static Utilities.Models.ShowMessageModel;
using static Utilities.LoggerUtility;
using static AppConfig.Config.MeasurementErrorConfig;
using UI.Controls.Protocol;
using System.Windows.Media;
using Core.ConfigCollector;

namespace Mode.SelfControl.Module.ModuleVoltageCurrentSource
{
  internal class Handler
  {
    ProtocolUI ProtocolSelfCheckControl;
    private readonly Tuple<string, Color> goodText = SuccessMessage;
    private readonly Tuple<string, Color> errorText = ErrorMessage;
    private Core.ModuleVoltageCurrentSource.Model moduleVoltageCurrentSource;
    private MeterBase meter;

    internal Handler(ProtocolUI protocolSelfCheck, object deviceModel)
    {
      ProtocolSelfCheckControl = protocolSelfCheck;
      moduleVoltageCurrentSource = Core.ModuleVoltageCurrentSource.Model.CreateFromObject(deviceModel);
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
    /// Асинхронный метод для настроек самоконтроля.
    /// </summary>
    private async Task RunSelfCheck(CancellationToken token)
    {
      meter = ConfigCollector.GetFastMeter();
      if (meter == null)
      {
        await ProtocolSelfCheckControl.ShowMessageAsync(new ShowMessageModel("Ошибка", null, "Измеритель быстрый не задан!"));
        return;
      }

      if (!await GetIsIdleModeEnabled())
      {
        var managerShassy = ConfigCollector.GetManagerShassy();

        if (!await ProtocolSelfCheckControl.AttemptDeviceConnection((new List<DeviceModel>()
        {
          managerShassy,
          moduleVoltageCurrentSource,

        }), ProtocolSelfCheckControl.ShowMessageAsync))
        {
          return;
        }

      }

      await ProtocolSelfCheckControl.ShowMessageAsync(new ShowMessageModel("\r\nСамоконтроль МИНТ", goodText.Item2));

      await GenerateDiscreteVoltageCheck(token);
      await CheckMintSwitching(token);
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
      await Core.DeviceBusCommutation.Functions.ConnectBusAsync(IPAddress.Parse("192.168.0.20"), MeterConnector.XS4, BusDeviceBusCommutation.AB1, true, false);
      await Core.ModuleVoltageCurrentSource.Functions.ConnectBusToPositiveAsync(moduleVoltageCurrentSource.IPAddress, BusModuleVoltageCurrentSource.A1);
      await Core.ModuleVoltageCurrentSource.Functions.ConnectBusToNegativeAsync(moduleVoltageCurrentSource.IPAddress, BusModuleVoltageCurrentSource.B1);
      meter.MeasureVoltageDC();
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
      await ProtocolSelfCheckControl.ShowMessageAsync(new ShowMessageModel($"\t\tУстанавливаем напряжение {a}.{b} В", goodText.Item2));
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
        await Core.ModuleVoltageCurrentSource.Functions.SetVoltageLevelAsync(moduleVoltageCurrentSource.IPAddress, a, b);
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

      if (error && await GetIsStopOnErrorEnabled())
      {
        LogWarning("Обнаружена ошибка при измерении напряжения. Пауза в выполнении.");
        await ProtocolSelfCheckControl.PauseAsync();
      }

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
        double result = meter.MeasureVoltageDC();
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
      Color messageType = !error ? goodText.Item2 : errorText.Item2;
      var statusText = !error ? "В норме" : "Вне нормы";
      LogInformation($"Результат измерения: {result} В ({firstNorm} - {lastNorm}). Статус: {statusText}");
      await ProtocolSelfCheckControl.ShowMessageAsync(new ShowMessageModel($"\t\t\tРезультат измерений ({Math.Round(firstNorm, 2)} - {Math.Round(lastNorm, 2)})", null, Math.Round(result, 2).ToString(CultureInfo.CurrentCulture) + "\r\n", messageType));
    }

    /// <summary>
    /// Проверяет коммутацию МИНТ.
    /// </summary>
    /// <param name="token">Токен отмены операции.</param>
    /// <returns>Задача, представляющая асинхронную операцию.</returns>
    private async Task CheckMintSwitching(CancellationToken token)
    {
      ProtocolSelfCheckControl.GetCancellationToken().ThrowIfCancellationRequested();

      LogInformation("Начало проверки коммутации МИНТ");
      await ProtocolSelfCheckControl.ShowMessageAsync(new ShowMessageModel("\tПроверка коммутации МИНТ", goodText.Item2));

      await InitializeMintSwitching(token);
      await CheckBusesA(token);
      await ResetNegativeBuses();
      await CheckBusesB(token);
      LogInformation("Завершение проверки коммутации МИНТ");
    }

    /// <summary>
    /// Инициализирует коммутацию МИНТ.
    /// </summary>
    /// <param name="token">Токен отмены операции.</param>
    private async Task InitializeMintSwitching(CancellationToken token)
    {
      if (!await GetIsIdleModeEnabled())
      {
        LogInformation("Инициализация коммутации МИНТ");
        await Core.ModuleVoltageCurrentSource.Functions.SetVoltageLevelAsync(moduleVoltageCurrentSource.IPAddress, 5, 0);
        await ConnectAllNegativeBuses();
        await Task.Delay(1000, token);
      }
    }

    /// <summary>
    /// Подключает все отрицательные шины.
    /// </summary>
    private async Task ConnectAllNegativeBuses()
    {
      LogInformation("Подключение всех отрицательных шин");
      foreach (var item in Enum.GetValues(typeof(BusModuleVoltageCurrentSource)))
      {
        if (item.ToString().StartsWith("B", StringComparison.CurrentCultureIgnoreCase))
        {
          await Core.ModuleVoltageCurrentSource.Functions.ConnectBusToNegativeAsync(moduleVoltageCurrentSource.IPAddress, (BusModuleVoltageCurrentSource)item);
        }
      }
    }

    /// <summary>
    /// Проверяет шины A.
    /// </summary>
    /// <param name="token">Токен отмены операции.</param>
    private async Task CheckBusesA(CancellationToken token)
    {
      LogInformation("Начало проверки шин A");
      var filteredBuses = GetFilteredBuses("A");
      foreach (BusModuleVoltageCurrentSource bus in filteredBuses)
      {
        await CheckMintSwitching_BusA(bus, token);
      }
    }

    /// <summary>
    /// Сбрасывает отрицательные шины.
    /// </summary>
    private async Task ResetNegativeBuses()
    {
      LogInformation("Сброс отрицательных шин");
      if (!await GetIsIdleModeEnabled())
      {
        await DisconnectAllNegativeBuses();
        await Core.DeviceBusCommutation.Functions.DisconnectBusAsync(IPAddress.Parse("192.168.0.20"), MeterConnector.XS4, BusDeviceBusCommutation.B1, true, false);
      }

      await ProtocolSelfCheckControl.ShowMessageAsync(new ShowMessageModel("\r\n", null));
    }

    /// <summary>
    /// Отключает все отрицательные шины.
    /// </summary>
    private async Task DisconnectAllNegativeBuses()
    {
      LogInformation("Отключение всех отрицательных шин");
      var busModules = GetFilteredBuses("B");
      foreach (var item in busModules)
      {
        await Core.ModuleVoltageCurrentSource.Functions.DisconnectBusToNegativeAsync(moduleVoltageCurrentSource.IPAddress, item);
      }
    }

    /// <summary>
    /// Проверяет шины B.
    /// </summary>
    /// <param name="token">Токен отмены операции.</param>
    private async Task CheckBusesB(CancellationToken token)
    {
      LogInformation("Начало проверки шин B");
      if (!await GetIsIdleModeEnabled())
      {
        await ConnectAllPositiveBuses();
        await Task.Delay(1000, token);
      }

      var filteredBuses = GetFilteredBuses("B");
      foreach (BusModuleVoltageCurrentSource bus in filteredBuses)
      {
        await CheckMintSwitching_BusB(bus, token);
      }
    }

    /// <summary>
    /// Подключает все положительные шины.
    /// </summary>
    private async Task ConnectAllPositiveBuses()
    {
      LogInformation("Подключение всех положительных шин");
      var busModules = GetFilteredBuses("A");
      foreach (var item in busModules)
      {
        await Core.ModuleVoltageCurrentSource.Functions.ConnectBusToPositiveAsync(moduleVoltageCurrentSource.IPAddress, item);
      }
    }

    /// <summary>
    /// Получает отфильтрованные шины по префиксу.
    /// </summary>
    /// <param name="prefix">Префикс для фильтрации шин.</param>
    /// <returns>Отфильтрованный список шин.</returns>
    private IEnumerable<BusModuleVoltageCurrentSource> GetFilteredBuses(string prefix)
    {
      LogInformation($"Получение отфильтрованных шин с префиксом {prefix}");
      return Enum.GetValues(typeof(BusModuleVoltageCurrentSource))
                 .Cast<BusModuleVoltageCurrentSource>()
                 .Where(bus => bus.ToString().StartsWith(prefix, StringComparison.Ordinal) && !bus.ToString().StartsWith("AB", StringComparison.Ordinal));
    }

    /// <summary>
    /// Проверяет коммутацию МИНТ для шины A.
    /// </summary>
    /// <param name="busA">Шина A для проверки.</param>
    /// <param name="token">Токен отмены операции.</param>
    /// <returns>Задача, представляющая асинхронную операцию.</returns>
    private async Task CheckMintSwitching_BusA(BusModuleVoltageCurrentSource busA, CancellationToken token)
    {
      ProtocolSelfCheckControl.GetCancellationToken().ThrowIfCancellationRequested();

      if (TryGetBusDeviceBusCommutation(busA.ToString(), out BusDeviceBusCommutation foundBus))
      {
        LogInformation($"Проверка шины A: {busA}");
        await ShowBusCheckMessage(busA);
        LogInformation($"Шина найдена: {foundBus}");

        await ConnectAndMeasureBusA(busA, foundBus, token);
        await CheckOtherBusesA(foundBus, token);
        await DisconnectBusA(busA);
      }
      else
      {
        LogWarning($"Шина A не найдена: {busA}");
        await ShowBusNotFoundMessage();
      }
    }

    /// <summary>
    /// Отображает сообщение о проверке шины.
    /// </summary>
    /// <param name="bus">Проверяемая шина.</param>
    private async Task ShowBusCheckMessage(BusModuleVoltageCurrentSource bus)
    {
      LogInformation($"Отображение сообщения о проверке шины {bus}");
      await ProtocolSelfCheckControl.ShowMessageAsync(new ShowMessageModel($"\t\tПроверка шины {bus}", goodText.Item2));
    }

    /// <summary>
    /// Подключает шину A и выполняет измерение напряжения.
    /// </summary>
    /// <param name="busA">Шина A для подключения.</param>
    /// <param name="foundBus">Найденная шина устройства коммутации шин.</param>
    /// <param name="token">Токен отмены операции.</param>
    private async Task ConnectAndMeasureBusA(BusModuleVoltageCurrentSource busA, BusDeviceBusCommutation foundBus, CancellationToken token)
    {
      LogInformation($"Подключение и измерение шины A: {busA}");
      if (!await GetIsIdleModeEnabled())
      {
        await Core.ModuleVoltageCurrentSource.Functions.ConnectBusToPositiveAsync(moduleVoltageCurrentSource.IPAddress, busA);
        await Core.DeviceBusCommutation.Functions.ConnectBusAsync(IPAddress.Parse("192.168.0.20"), MeterConnector.XS4, foundBus, true, false);
      }

      await MeasureVoltage(5);

      if (!await GetIsIdleModeEnabled())
      {
        await Core.DeviceBusCommutation.Functions.DisconnectBusAsync(IPAddress.Parse("192.168.0.20"), MeterConnector.XS4, foundBus, true, false);
        await Task.Delay(10, token);
      }
    }

    /// <summary>
    /// Проверяет другие шины A, исключая найденную.
    /// </summary>
    /// <param name="foundBus">Найденная шина, которую следует исключить.</param>
    /// <param name="token">Токен отмены операции.</param>
    private async Task CheckOtherBusesA(BusDeviceBusCommutation foundBus, CancellationToken token)
    {
      LogInformation($"Проверка других шин A, исключая {foundBus}");
      var filteredBuses = GetFilteredBusesA(foundBus);
      LogInformation($"Без шины {foundBus}: ");
      await ConnectFilteredBuses(filteredBuses);
      await MeasureVoltage(0);
      await DisconnectFilteredBuses(filteredBuses);
    }

    /// <summary>
    /// Получает отфильтрованный список шин A, исключая указанную.
    /// </summary>
    /// <param name="excludeBus">Шина для исключения из списка.</param>
    /// <returns>Отфильтрованный список шин.</returns>
    private IEnumerable<BusDeviceBusCommutation> GetFilteredBusesA(BusDeviceBusCommutation excludeBus)
    {
      LogInformation($"Получение отфильтрованного списка шин A, исключая {excludeBus}");
      return Enum.GetValues(typeof(BusDeviceBusCommutation))
                 .Cast<BusDeviceBusCommutation>()
                 .Where(bus => bus.ToString().StartsWith("A", StringComparison.Ordinal) &&
                               !bus.ToString().StartsWith("AB", StringComparison.Ordinal) &&
                               bus != excludeBus);
    }

    /// <summary>
    /// Подключает отфильтрованные шины.
    /// </summary>
    /// <param name="buses">Список шин для подключения.</param>
    private async Task ConnectFilteredBuses(IEnumerable<BusDeviceBusCommutation> buses)
    {
      LogInformation("Подключение отфильтрованных шин");
      foreach (var bus in buses)
      {
        LogInformation(bus.ToString());
        if (!await GetIsIdleModeEnabled())
        {
          await Core.DeviceBusCommutation.Functions.ConnectBusAsync(IPAddress.Parse("192.168.0.20"), MeterConnector.XS4, bus, true, false);
        }
      }
    }

    /// <summary>
    /// Отключает отфильтрованные шины.
    /// </summary>
    /// <param name="buses">Список шин для отключения.</param>
    private async Task DisconnectFilteredBuses(IEnumerable<BusDeviceBusCommutation> buses)
    {
      LogInformation("Отключение отфильтрованных шин");
      foreach (var bus in buses)
      {
        LogInformation(bus.ToString());
        if (!await GetIsIdleModeEnabled())
        {
          await Core.DeviceBusCommutation.Functions.DisconnectBusAsync(IPAddress.Parse("192.168.0.20"), MeterConnector.XS4, bus, true, false);
        }
      }
    }

    /// <summary>
    /// Отключает шину A от положительного полюса.
    /// </summary>
    /// <param name="busA">Шина A для отключения.</param>
    private async Task DisconnectBusA(BusModuleVoltageCurrentSource busA)
    {
      LogInformation($"Отключение шины A: {busA}");
      if (!await GetIsIdleModeEnabled())
      {
        await Core.ModuleVoltageCurrentSource.Functions.DisconnectBusToPositiveAsync(IPAddress.Parse("192.168.1.2"), busA);
      }
    }

    /// <summary>
    /// Отображает сообщение о том, что шина не найдена или некорректна.
    /// </summary>
    private async Task ShowBusNotFoundMessage()
    {
      LogWarning("Шина не найдена или некорректная");
      await ProtocolSelfCheckControl.ShowMessageAsync(new ShowMessageModel("Шина не найдена или некорректная", errorText.Item2));
    }

    /// <summary>
    /// Проверяет коммутацию МИНТ для шины B.
    /// </summary>
    /// <param name="busB">Шина B для проверки.</param>
    /// <param name="token">Токен отмены операции.</param>
    /// <returns>Задача, представляющая асинхронную операцию.</returns>
    private async Task CheckMintSwitching_BusB(BusModuleVoltageCurrentSource busB, CancellationToken token)
    {
      ProtocolSelfCheckControl.GetCancellationToken().ThrowIfCancellationRequested();

      if (TryGetBusDeviceBusCommutation(busB.ToString(), out BusDeviceBusCommutation foundBus))
      {
        LogInformation($"Проверка шины B: {busB}");
        await ProcessFoundBus(busB, foundBus, token);
      }
      else
      {
        LogWarning($"Шина B не найдена: {busB}");
        await ShowBusNotFoundMessage();
      }
    }

    /// <summary>
    /// Обрабатывает найденную шину.
    /// </summary>
    /// <param name="busB">Шина B для проверки.</param>
    /// <param name="foundBus">Найденная шина устройства коммутации шин.</param>
    /// <param name="token">Токен отмены операции.</param>
    private async Task ProcessFoundBus(BusModuleVoltageCurrentSource busB, BusDeviceBusCommutation foundBus, CancellationToken token)
    {
      LogInformation($"Обработка найденной шины B: {busB}, {foundBus}");
      LogInformation($"Шина найдена: {foundBus}");
      await ProtocolSelfCheckControl.ShowMessageAsync(new ShowMessageModel($"\t\tПроверка шины {busB}", goodText.Item2));

      await ConnectAndMeasureBus(busB, foundBus);
      await CheckOtherBuses(foundBus, token);
      await DisconnectBus(busB);
    }

    /// <summary>
    /// Подключает шину и измеряет напряжение.
    /// </summary>
    /// <param name="busB">Шина B для подключения.</param>
    /// <param name="foundBus">Найденная шина устройства коммутации шин.</param>
    private async Task ConnectAndMeasureBus(BusModuleVoltageCurrentSource busB, BusDeviceBusCommutation foundBus)
    {
      LogInformation($"Подключение и измерение шины B: {busB}");
      if (!await GetIsIdleModeEnabled())
      {
        await Core.ModuleVoltageCurrentSource.Functions.ConnectBusToNegativeAsync(IPAddress.Parse("192.168.1.2"), busB);
        await Core.DeviceBusCommutation.Functions.ConnectBusAsync(IPAddress.Parse("192.168.0.20"), MeterConnector.XS4, foundBus, true, false);
      }

      await MeasureVoltage(5);

      if (!await GetIsIdleModeEnabled())
      {
        await Core.DeviceBusCommutation.Functions.DisconnectBusAsync(IPAddress.Parse("192.168.0.20"), MeterConnector.XS4, foundBus, true, false);
        await Task.Delay(10);
      }
    }

    /// <summary>
    /// Проверяет другие шины, кроме найденной.
    /// </summary>
    /// <param name="foundBus">Найденная шина устройства коммутации шин.</param>
    /// <param name="token">Токен отмены операции.</param>
    private async Task CheckOtherBuses(BusDeviceBusCommutation foundBus, CancellationToken token)
    {
      LogInformation($"Проверка других шин, кроме {foundBus}");
      var filteredBuses = GetFilteredBuses(foundBus);
      LogInformation($"Без шины {foundBus}: ");
      await ConnectFilteredBuses(filteredBuses);
      await MeasureVoltage(0);
      await DisconnectFilteredBuses(filteredBuses);
    }

    /// <summary>
    /// Получает отфильтрованный список шин.
    /// </summary>
    /// <param name="foundBus">Найденная шина устройства коммутации шин.</param>
    /// <returns>Отфильтрованный список шин.</returns>
    private IEnumerable<BusDeviceBusCommutation> GetFilteredBuses(BusDeviceBusCommutation foundBus)
    {
      LogInformation($"Получение отфильтрованного списка шин, исключая {foundBus}");
      return Enum.GetValues(typeof(BusDeviceBusCommutation))
                 .Cast<BusDeviceBusCommutation>()
                 .Where(bus => bus.ToString().StartsWith("B", StringComparison.Ordinal) &&
                               !bus.ToString().StartsWith("AB", StringComparison.Ordinal) &&
                               bus != foundBus);
    }

    /// <summary>
    /// Отключает шину B.
    /// </summary>
    /// <param name="busB">Шина B для отключения.</param>
    private async Task DisconnectBus(BusModuleVoltageCurrentSource busB)
    {
      LogInformation($"Отключение шины B: {busB}");
      if (!await GetIsIdleModeEnabled())
      {
        await Core.ModuleVoltageCurrentSource.Functions.DisconnectBusToNegativeAsync(IPAddress.Parse("192.168.1.2"), busB);
      }
    }

    /// <summary>
    /// Преобразует строковое представление шины в значение перечисления BusDeviceBusCommutation.
    /// </summary>
    /// <param name="busName">Строковое представление шины.</param>
    /// <param name="bus">Значение перечисления BusDeviceBusCommutation.</param>
    /// <returns>true, если преобразование успешно; иначе false.</returns>
    static bool TryGetBusDeviceBusCommutation(string busName, out BusDeviceBusCommutation bus)
    {
      return Enum.TryParse(busName, out bus) && Enum.IsDefined(typeof(BusDeviceBusCommutation), bus);
    }

    /// <summary>
    /// Измерение напряжения.
    /// </summary>
    /// <param name="voltage">Ожидаемое напряжение.</param>
    /// <returns>Задача, представляющая асинхронную операцию.</returns>
    async Task MeasureVoltage(double voltage)
    {
      double firstNorm = voltage != 0 ? voltage - 0.15 : -0.5;
      double lastNorm = voltage != 0 ? voltage + 0.15 : 0.5;
      double dcVoltage = !await GetIsIdleModeEnabled() ? meter.MeasureVoltageDC() : voltage;

      // Вывод измеренного значения
      if (dcVoltage >= firstNorm && dcVoltage <= lastNorm)
      {
        await ProtocolSelfCheckControl.ShowMessageAsync(new ShowMessageModel($"\t\t\tИзмеренное напряжение постоянного тока ({firstNorm} - {lastNorm})", null, dcVoltage.ToString(CultureInfo.CurrentCulture), goodText.Item2));
        Console.ForegroundColor = ConsoleColor.Green;
      }
      else
      {
        await ProtocolSelfCheckControl.ShowMessageAsync(new ShowMessageModel($"\t\t\tИзмеренное напряжение постоянного тока ({firstNorm} - {lastNorm})", null, dcVoltage.ToString(CultureInfo.CurrentCulture), errorText.Item2));
        Console.ForegroundColor = ConsoleColor.Red;
      }
      Console.Write(dcVoltage);
      Console.ForegroundColor = ConsoleColor.White;
      Console.WriteLine($"({firstNorm} - {lastNorm})");
    }

    /// <summary>
    /// Сбрасывает настройки источника напряжения и тока.
    /// </summary>
    /// <returns>Задача, представляющая асинхронную операцию.</returns>
    private async Task ResetVoltageCurrentSourceAsync()
    {
      await Core.ModuleVoltageCurrentSource.Functions.SetCurrentLevelAsync(moduleVoltageCurrentSource.IPAddress, 0, 0);
      await Core.ModuleVoltageCurrentSource.Functions.DisconnectBusToPositiveAsync(moduleVoltageCurrentSource.IPAddress, BusModuleVoltageCurrentSource.A1);
      await Core.ModuleVoltageCurrentSource.Functions.DisconnectBusToNegativeAsync(moduleVoltageCurrentSource.IPAddress, BusModuleVoltageCurrentSource.B1);
      await Core.DeviceBusCommutation.Functions.ConnectBusAsync(IPAddress.Parse("192.168.0.20"), MeterConnector.XS4, BusDeviceBusCommutation.AB1, true, false);
    }

  }
}

using System.Globalization;
using System.Net;
using System.Windows;
using System.Windows.Media.Effects;
using Core.Abstract;
using Core.Enum;
using Core.Model;
using static AppConfig.Config.ExecutionConfig;
using static AppConfig.Config.ProtocolConfig;
using static AppConfig.Config.LoopConfig;
using static AppConfig.Config.SystemStateManager;
using static AppConfig.EventAggregator;
using static AppConfig.SettingsFileReader;
using static Utilities.Models.ShowMessageModel;
using static Utilities.LoggerUtility;
using static AppConfig.Config.MeasurementErrorConfig;
using Core.ConfigCollector;
using Mode.Base.SearchDevices;
using Core.Communication;

namespace Mode.Settings.ConfigSettings
{
  public partial class ConfigSettingsControl
  {
    private static readonly SemaphoreSlim _connectionSemaphore = new SemaphoreSlim(1, 1);

    /// <summary>
    /// Добавляет новое устройство в список.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="model"></param>
    private void SaveDevice<T>(T model)
    {
      var type = DeviceModel.TryGetDeviceTypeFromObject(model);
      if (type == null)
      {
        return;
      }
      switch (type)
      {
        case DeviceEnum.Type.DeviceBusCommutation:
          var modelDBC = Core.DeviceBusCommutation.Model.TryGetDeviceTypeFromObject(model);
          SetDeviceBusCommutation(modelDBC);
          break;

        case DeviceEnum.Type.ModuleRelayControl:
          var modelMKR = Core.ModuleRelayControl.Model.TryGetDeviceTypeFromObject(model);
          AddMkr(modelMKR);
          break;

        case DeviceEnum.Type.ManagerShassy:
          var modelManagerShassy = Core.ManagerShassy.Model.TryGetDeviceTypeFromObject(model);
          SetManagerShassy(model as Core.ManagerShassy.Model);
          break;

        case DeviceEnum.Type.ModuleVoltageCurrentSource:
          var modelVoltage = Core.ModuleVoltageCurrentSource.Model.TryGetDeviceTypeFromObject(model);
          SetMint(modelVoltage);
          break;

        case DeviceEnum.Type.AccurateMeter:
          break;

        case DeviceEnum.Type.FastMeter:
          SetFastMeter(model as MeterBase);
          break;
      }
    }

    /// <summary>
    /// Устанавливает УКШ.
    /// </summary>
    /// <param name="model">Модель УКШ.</param>
    private void SetDeviceBusCommutation(object value)
    {
      var model = Core.DeviceBusCommutation.Model.CreateFromObject(value);
      if (model == null)
      {
        LogError("Объект УКШ на распознан!");
        return;
      }

      deviceBusCommutationContent.Children.Clear();
      deviceBusCommutationModel = model;
      deviceBusCommutationModel.IPAddress = IPAddress.Parse("192.168.0.20");
      ConfigCollector.SetDeviceBusCommunication(model);
      SetNewTreeViewItem(deviceBusCommutationContent, model);
    }

    /// <summary>
    /// Устанавливает Менеджер шасси.
    /// </summary>
    /// <param name="model">Модель Менеджера шасси.</param>
    private void SetManagerShassy(object value)
    {
      var model = Core.ManagerShassy.Model.CreateFromObject(value);
      if (model == null)
      {
        LogError("Объект Менеджера шасси на распознан!");
        return;
      }

      managerShassyContent.Children.Clear();
      managerShassyModel = model;
      managerShassyModel.IPAddress = IPAddress.Parse($"192.168.{managerShassyModel.Number}.0");
      ConfigCollector.SetManagerShassy(model);
      SetNewTreeViewItem(managerShassyContent, model);
    }

    /// <summary>
    /// Устанавливает модуль источника напряжения и тока.
    /// </summary>
    /// <param name="model">Модель модуля источника напряжения и тока.</param>
    private void SetMint(object value)
    {
      var model = Core.ModuleVoltageCurrentSource.Model.CreateFromObject(value);
      if (model == null)
      {
        LogError("Объект МИНТа на распознан!");
        return;
      }

      moduleVoltageCurrentSourceContent.Children.Clear();
      moduleVoltageCurrentSource = model;
      moduleVoltageCurrentSource.IPAddress = IPAddress.Parse($"192.168.1.{moduleVoltageCurrentSource.Number}");
      ConfigCollector.SetMint(model);
      SetNewTreeViewItem(moduleVoltageCurrentSourceContent, model);
    }

    /// <summary>
    /// Устанавливает Быстрый измеритель.
    /// </summary>
    /// <param name="model">Модель Быстрого измерителя.</param>
    private void SetFastMeter(MeterBase model)
    {
      if (model == null)
      {
        LogError("Объект быстрого мультиметра не распознан!");
        return;
      }

      fastMeterContent.Children.Clear();
      fastMeterModel = model;
      ConfigCollector.SetFastMeter(model);
      SetNewTreeViewItem(fastMeterContent, model);
    }


    /// <summary>
    /// Устанавливает Быстрый измеритель.
    /// </summary>
    /// <param name="model">Модель Быстрого измерителя.</param>
    private void SetBreakdown(BreakdownBase model)
    {
      if (model == null)
      {
        LogError("Объект пробойки не распознан!");
        return;
      }

      breakdownContent.Children.Clear();
      breakdownModel = model;
      ConfigCollector.SetBreakdown(model);
      SetNewTreeViewItem(breakdownContent, model);
    }

    /// <summary>
    /// Устанавливает Точный измеритель.
    /// </summary>
    /// <param name="model">Модель Точного измерителя.</param>
    private void SetAccurateMeter(MeterBase model)
    {
      if (model == null)
      {
        LogError("Объект точного мультиметра на распознан!");
        return;
      }

      accurateMeterContent.Children.Clear();
      accurateMeterModel = model;
      ConfigCollector.SetAccurateMeter(model);
      SetNewTreeViewItem(accurateMeterContent, model);
    }

    /// <summary>
    /// Добавляет МКР в список.
    /// </summary>
    /// <param name="model">Модель МКР.</param>
    /// <returns>Возвращает true, если МКР успешно добавлен, иначе false.</returns>
    private bool AddMkr(object model)
    {
      var moduleRelay = Core.ModuleRelayControl.Model.CreateFromObject(model);
      if (moduleRelay == null)
      {
        return false;
      }

      foreach (Core.ModuleRelayControl.Model item in mkrModels)
      {
        if (item.Number == moduleRelay.Number)
        {
          MessageBox.Show($"Модуль с номером {item.Number} уже существует!");
          return false;
        }

      }
      mkrModels.Add(moduleRelay);
      ConfigCollector.AddMkrModels(moduleRelay);
      SetNewTreeViewItem(moduleRelayContent, moduleRelay as DeviceModel);
      return true;
    }

    /// <summary>
    /// Сохраняет все модули МКР.
    /// </summary>
    /// <param name="mkrModels">Список моделей МКР.</param>
    private void SaveAllMkrModels(List<Core.ModuleRelayControl.Model> mkrModels)
    {
      foreach (Core.ModuleRelayControl.Model mkrModel in mkrModels)
      {
        if (mkrModel != null && mkrModel.ModuleActive)
        {
          mkrModel.IPAddress = IPAddress.Parse($"192.168.1.{mkrModel.Number}");
          SetNewTreeViewItem(moduleRelayContent, mkrModel);
        }
        else
        {
          mkrModels.Remove(mkrModel);
        }
      }
    }

    /// <summary>
    /// Запускает автоматический поиск устройств.
    /// </summary>
    private async void ParseDevices()
    {
      var mainWindow = Application.Current.MainWindow;
      var searchDevices = ShowSearchDevicesWindow(mainWindow);

      try
      {
        await SearchAllDevicesAsync(searchDevices);
      }
      finally
      {
        CloseSearchDevicesWindow(searchDevices, mainWindow);
        UpdateConfiguration();
      }
    }

    /// <summary>
    /// Отображает окно поиска устройств и применяет эффект размытия к главному окну.
    /// </summary>
    /// <param name="mainWindow">Главное окно приложения.</param>
    /// <returns>Экземпляр окна поиска устройств.</returns>
    private SearchDevices ShowSearchDevicesWindow(Window mainWindow)
    {
      ApplyBlurEffect(mainWindow);
      var searchDevices = new SearchDevices();
      searchDevices.Show();
      return searchDevices;
    }

    /// <summary>
    /// Применяет эффект размытия к указанному окну.
    /// </summary>
    /// <param name="window">Окно, к которому применяется эффект.</param>
    private void ApplyBlurEffect(Window window)
    {
      window.Effect = new BlurEffect { Radius = 10 };
      window.Opacity = 0.8;
    }

    /// <summary>
    /// Выполняет поиск всех устройств в системе.
    /// </summary>
    /// <param name="searchDevices">Окно поиска устройств для обновления статуса.</param>
    private async Task SearchAllDevicesAsync(SearchDevices searchDevices)
    {
      await SearchManagerShassyAsync(searchDevices);
      var tasks = new List<Task>
      {
          SearchDeviceBusCommutationAsync(searchDevices),
          SearchModulesAsync(searchDevices),
          SearchGptDeviceAsync(searchDevices)
      };

      await Task.WhenAll(tasks);
    }

    /// <summary>
    /// Выполняет поиск устройства УКШ.
    /// </summary>
    /// <param name="searchDevices">Окно поиска устройств для обновления статуса.</param>
    private async Task SearchDeviceBusCommutationAsync(SearchDevices searchDevices)
    {
      await UpdateSearchDescription(searchDevices, "Поиск УКШ...");
      await SetupDeviceBusCommutationAsync();
    }

    /// <summary>
    /// Выполняет поиск менеджера шасси и включает питание.
    /// </summary>
    /// <param name="searchDevices">Окно поиска устройств для обновления статуса.</param>
    private async Task SearchManagerShassyAsync(SearchDevices searchDevices)
    {
      await UpdateSearchDescription(searchDevices, "Поиск менеджера шасси...");
      LogInformation("Начало поиска менеджера шасси");

      const int maxAttempts = 3;
      for (int attempt = 1; attempt <= maxAttempts; attempt++)
      {
        LogInformation($"Попытка подключения к менеджеру шасси: {attempt}");
        if (await SetupAskMkiDeviceAsync())
        {
          await UpdateSearchDescription(searchDevices, "Включение питания шасси...");
          LogInformation("Менеджер шасси найден. Включение питания.");

          var askIp = ConfigCollector.GetManagerShassyIp();
          LogInformation($"IP адрес менеджера шасси: {askIp}");

          await Core.ManagerShassy.Function.StartPowerAsync(managerShassyModel.IPAddress);

          for (int i = 3; i > 0; i--)
          {
            await UpdateSearchDescription(searchDevices, $"Ожидание загрузки блоков: {i} сек...");
            await Task.Delay(1000);
          }

          LogInformation("Загрузка блоков завершена");
          return;
        }
        else
        {
          LogWarning($"Попытка {attempt} не удалась. Повторная попытка через 2 секунды.");
          await Task.Delay(2000);
        }
      }

      LogError("Не удалось найти менеджер шасси после нескольких попыток");
      await UpdateSearchDescription(searchDevices, "Ошибка: Менеджер шасси не найден");
    }


    /// <summary>
    /// Обновляет описание в окне поиска устройств.
    /// </summary>
    /// <param name="searchDevices">Окно поиска устройств.</param>
    /// <param name="description">Новое описание для отображения.</param>
    private async Task UpdateSearchDescription(SearchDevices searchDevices, string description)
    {
      await Application.Current.Dispatcher.InvokeAsync(() => searchDevices.SetDescription(description));
    }

    /// <summary>
    /// Закрывает окно поиска устройств и удаляет эффект размытия с главного окна.
    /// </summary>
    /// <param name="searchDevices">Окно поиска устройств для закрытия.</param>
    /// <param name="mainWindow">Главное окно приложения.</param>
    private void CloseSearchDevicesWindow(SearchDevices searchDevices, Window mainWindow)
    {
      searchDevices.Close();
      RemoveBlurEffect(mainWindow);
    }

    /// <summary>
    /// Удаляет эффект размытия с указанного окна.
    /// </summary>
    /// <param name="window">Окно, с которого удаляется эффект размытия.</param>
    private void RemoveBlurEffect(Window window)
    {
      window.Effect = null;
      window.Opacity = 1;
    }

    /// <summary>
    /// Обновляет конфигурацию устройств после поиска.
    /// </summary>
    private void UpdateConfiguration()
    {
      ClearConfig();
      UpdateDeviceIfActive(managerShassyModel, SetManagerShassy);
      UpdateDeviceIfActive(deviceBusCommutationModel, SetDeviceBusCommutation);
      UpdateDeviceIfActive(fastMeterModel, SaveDevice);
      UpdateDeviceIfActive(accurateMeterModel, SaveDevice);
      UpdateDeviceIfActive(moduleVoltageCurrentSource, SaveDevice);
      UpdateDeviceIfActive(breakdownModel, SetBreakdown);
      SaveAllMkrModels(mkrModels);
    }

    /// <summary>
    /// Обновляет устройство, если оно активно.
    /// </summary>
    /// <typeparam name="T">Тип устройства.</typeparam>
    /// <param name="model">Модель устройства.</param>
    /// <param name="updateAction">Действие для обновления устройства.</param>
    private void UpdateDeviceIfActive<T>(T model, Action<T> updateAction) where T : DeviceModel
    {
      if (model != null && model.ModuleActive)
      {
        updateAction(model);
      }
    }

    /// <summary>
    /// Настройка УКШ и проверка подключения.
    /// </summary>
    private async Task SetupDeviceBusCommutationAsync()
    {
      // TODO : Заглушка по Id.
      deviceBusCommutationModel = new Core.DeviceBusCommutation.Model(DeviceEnum.Type.DeviceBusCommutation, "УКШ", "устройство коммутации шин", IPAddress.Parse("192.168.0.20"), "1", false);
      deviceBusCommutationModel.ModuleActive = await Core.Communication.CommunicationManager.PingAsync(deviceBusCommutationModel.Name, deviceBusCommutationModel.IPAddress);
    }

    /// <summary>
    /// Настройка шасси и проверка подключения.
    /// </summary>
    /// <returns>Возвращает активность АСК-МКИ-М.</returns>
    private async Task<bool> SetupAskMkiDeviceAsync()
    {
      LogInformation("Начало настройки АСК-МКИ-М");

      IPAddress askIp = IPAddress.Parse("192.168.1.0");
      managerShassyModel = new Core.ManagerShassy.Model(
          DeviceEnum.Type.ManagerShassy,
          "АСК-МКИ-М",
          "Менеджер шасси АСК-МКИ-М",
          askIp,
          "1",
          false
      );

      LogInformation($"Попытка подключения к АСК-МКИ-М по адресу: {askIp}");
      bool moduleActive = await CommunicationManager.PingAsync(managerShassyModel.Name, askIp);

      managerShassyModel.ModuleActive = moduleActive;

      LogInformation($"Результат подключения к АСК-МКИ-М: {moduleActive}");

      return moduleActive;
    }

    /// <summary>
    /// Выполняет поиск модулей в системе АСК-МКИ-М.
    /// </summary>
    private async Task SearchModulesAsync(SearchDevices searchDevices)
    {
      await UpdateSearchDescription(searchDevices, "Поиск блоков...");
      await Task.Delay(10);
      mkrModels.Clear();
      moduleVoltageCurrentSource = null;

      for (int i = 1; i <= 14; i++)
      {
        await UpdateSearchDescription(searchDevices, $"Поиск блока в слоте {i}...");
        await SearchModuleAsync(i);
      }
    }

    /// <summary>
    /// Выполняет поиск модуля в указанном слоте.
    /// </summary>
    /// <param name="slotNumber">Номер слота для поиска.</param>
    private async Task SearchModuleAsync(int slotNumber)
    {
      IPAddress ip = IPAddress.Parse($"192.168.1.{slotNumber}");
      if (await CommunicationManager.PingAsync(string.Empty, ip))
      {
        try
        {
          await _connectionSemaphore.WaitAsync();

          LogInformation($"Отправка команды инициализации для слота {slotNumber}");
          Command initialize = new Command(1, 0, 0, 0);
          string answer = await CommunicationManager.SendCommandAsync(ip, initialize, 200);

          LogInformation($"Ответ от слота {slotNumber}: {answer}");

          if (answer.Contains("1.0.1") || answer.Contains("1.0.350"))
          {
            AddMkrModule(ip, slotNumber);
          }
          else if (answer.Contains("2.1"))
          {
            AddMintModule(ip, slotNumber);
          }
        }
        catch (Exception ex)
        {
          LogError($"Ошибка при поиске модуля в слоте {slotNumber}: {ex.Message}");
        }
        finally
        {
          _connectionSemaphore.Release();
        }
      }
      else
      {
        LogInformation($"Модуль в слоте {slotNumber} не отвечает на запрос.");
      }
    }

    /// <summary>
    /// Поиск GPT.
    /// </summary>
    private async Task SearchGptDeviceAsync(SearchDevices searchDevices)
    {
      await UpdateSearchDescription(searchDevices, "Поиск пробойной установки");
      var model = Core.GptLibrary.Model.CreateAsync();
      if (model.ModuleActive)
      {
        LogInformation($"Найдена пробойная установка GPT в {model.Port}. Добавляем в конфигурацию.");
        breakdownModel = model;
      }
    }

    /// <summary>
    /// Добавляет найденный модуль МКР в конфигурацию.
    /// </summary>
    /// <param name="ip">IP-адрес модуля.</param>
    /// <param name="slotNumber">Номер слота модуля.</param>
    private void AddMkrModule(IPAddress ip, int slotNumber)
    {
      LogInformation($"Найден модуль МКР в слоте {slotNumber}. Добавляем в конфигурацию.");
      var deviceModel = new Core.ModuleRelayControl.Model(
          DeviceEnum.Type.ModuleRelayControl,
          "МКР",
          "Добавить описание",
          ip,
          slotNumber.ToString(CultureInfo.CurrentCulture),
          true,
          350,
          DeviceEnum.VoltageType.LowVoltage
      );
      lock (mkrModels)
      {
        mkrModels.Add(deviceModel);
      }
    }

    /// <summary>
    /// Добавляет найденный модуль МИНТ в конфигурацию.
    /// </summary>
    /// <param name="ip">IP-адрес модуля.</param>
    /// <param name="slotNumber">Номер слота модуля.</param>
    private void AddMintModule(IPAddress ip, int slotNumber)
    {
      LogInformation($"Найден модуль МИНТ в слоте {slotNumber}. Добавляем в конфигурацию.");
      moduleVoltageCurrentSource = new Core.ModuleVoltageCurrentSource.Model(
          DeviceEnum.Type.ModuleVoltageCurrentSource,
          "МИНТ",
          "Добавить описание",
          ip,
          slotNumber.ToString(CultureInfo.CurrentCulture),
          true
      );
    }
  }
}

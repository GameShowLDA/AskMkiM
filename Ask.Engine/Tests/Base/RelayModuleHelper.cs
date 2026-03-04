using Ask.Core.Services.Errors.Device;
using Ask.Core.Services.Errors.Device.Adapters;
using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.RelaySwitchModule;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.SwitchingDevice;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums.Commands;
using Ask.Engine.ControlCommandExecutor.BaseStrategies.Data;
using DataBaseConfiguration.Services.Device;

namespace Ask.Engine.Tests.Base
{
  /// <summary>
  /// Вспомогательный класс для работы с модулями МКР.
  /// </summary>
  public static class RelayModuleHelper
  {
    /// <summary>
    /// Получает модули МКР по номеру шасси и диапазону модулей.
    /// </summary>
    /// <param name="chassisNumber">Номер шасси.</param>
    /// <param name="startModule">Номер начального модуля.</param>
    /// <param name="endModule">Номер конечного модуля.</param>
    /// <returns>Список модулей, соответствующих диапазону.</returns>
    public static List<IRelaySwitchModule> GetModulesByRange(int chassisNumber, int startModule, int endModule)
    {
      var relayRepo = new RelaySwitchModuleServices();
      var allModules = relayRepo.GetDevicesByNumberChassis(chassisNumber);
      var filteredModules = allModules
          .Where(m => m.Number >= startModule && m.Number <= endModule)
          .ToList();

      return filteredModules;
    }

    #region Методы для общения

    /// <summary>
    /// Подключает заданный БК к указанной шине.
    /// </summary>
    /// <param name="bus">Шина</param>
    /// <param name="module">Блок коммутации</param>
    public static async Task<bool> BusConnectAsync(SwitchingBus bus, IRelaySwitchModule module, IUserInteractionService _userInteractionService, CancellationToken cancellationToken)
    {
      cancellationToken.ThrowIfCancellationRequested();
      await module.BusManager.ConnectBusAsync(bus, userMessageService: _userInteractionService);

      return true;
    }

    /// <summary>
    /// Отключает заданный БК от указанной шины.
    /// </summary>
    /// <param name="bus">Коммутационная шина</param>
    /// <param name="module">Блок коммутации</param>
    public static async Task<bool> BusDisconnectAsync(SwitchingBus bus, IRelaySwitchModule module, IUserInteractionService _userInteractionService, CancellationToken cancellationToken)
    {
      cancellationToken.ThrowIfCancellationRequested();
      await module.BusManager.DisconnectBusAsync(bus, _userInteractionService);

      return true;
    }

    /// <summary>
    /// Инициализирует БК и отображает сообщение об инициализации.
    /// </summary>
    /// <param name="module">Блок коммутации</param>
    /// <param name="roleName">Название роли блока коммутации</param>
    /// <returns>Возвращает <c>true</c>, если инициализация прошла успешно; иначе — <c>false</c>.</returns>
    public static async Task<bool> InitializeModule(IUserInteractionService messageService, IRelaySwitchModule module, IUserInteractionService _userInteractionService, CancellationToken cancellationToken, string roleName = null)
    {
      cancellationToken.ThrowIfCancellationRequested();
      await module.ConnectableManager.InitializeAsync(messageService);

      return true;
    }

    /// <summary>
    /// Включает измеритель БК.
    /// </summary>
    /// <param name="module">Блок коммутации</param>
    /// <returns>Возвращает <c>true</c>, устройство включилось; иначе — <c>false</c>.</returns>
    public static async Task<bool> MeterEnableAsync(IUserInteractionService messageService, IRelaySwitchModule module, IUserInteractionService _userInteractionService, CancellationToken cancellationToken)
    {
      cancellationToken.ThrowIfCancellationRequested();
      await module.MeterManager.ConnectMeterAsync(_userInteractionService);

      return true;
    }

    /// <summary>
    /// Отключает измеритель БК.
    /// </summary>
    /// <param name="module">Блок коммутации</param>
    /// <returns>Возвращает <c>true</c>, если устройство выключилось; иначе — <c>false</c>.</returns>
    public static async Task<bool> MeterDisableAsync(IUserInteractionService messageService, IUserInteractionService _userInteractionService, IRelaySwitchModule module, CancellationToken cancellationToken)
    {
      cancellationToken.ThrowIfCancellationRequested();
      await module.MeterManager.DisconnectMeterAsync(_userInteractionService);

      return true;
    }

    /// <summary>
    /// Получает ответ измерителя указанного БК и отображает сообщение об измерении.
    /// </summary>
    /// <param name="module">Блок коммутации</param>
    /// <returns>Возвращает <c>true</c>, если есть замыкание; иначе — <c>false</c>.</returns>
    public static async Task<bool> GetMeterAnswer(IRelaySwitchModule module, IUserInteractionService _userInteractionService, CancellationToken cancellationToken)
    {
      try
      {
        cancellationToken.ThrowIfCancellationRequested();
        return await module.MeterManager.GetMeterResponseAsync();
      }
      catch
      {
        return false;
      }
    }

    /// <summary>
    /// Возвращает устройство коммутации шин (УКШ), привязанное к указанному шасси.
    /// Предполагается, что на одно шасси существует ровно одно УКШ.
    /// </summary>
    /// <param name="numberChassis">Номер шасси, в котором требуется найти УКШ.</param>
    /// <returns>Экземпляр <see cref="ISwitchingDevice"/>.</returns>
    /// <exception cref="DeviceException">
    /// Генерируется, если УКШ не найдено в конфигурации.
    /// </exception>
    public static ISwitchingDevice ResolveUksh(int numberChassis)
    {
      var uksh = new SwitchingDeviceServices()
        .GetDevicesByNumberChassis(numberChassis)
        .FirstOrDefault();

      return uksh ?? throw ConnectionExceptionAdapter.NotFoundInConfiguration("Устройство коммутации шин (УКШ)");
    }

    /// <summary>
    /// Возвращает мультиметр (FastMeter), привязанный к указанному шасси.
    /// Используется для точного измерения электрического сопротивления.
    /// </summary>
    /// <param name="numberChassis">Номер шасси, в котором требуется найти мультиметр.</param>
    /// <returns>Экземпляр <see cref="IFastMeter"/>.</returns>
    /// <exception cref="DeviceException">
    /// Генерируется, если мультиметр не найден в конфигурации.
    /// </exception>
    public static IFastMeter ResolveFastMeter(int numberChassis)
    {
      var fastMeter = new FastMeterServices()
        .GetDevicesByNumberChassis(numberChassis)
        .OfType<IFastMeter>()
        .FirstOrDefault();

      return fastMeter ?? throw ConnectionExceptionAdapter.NotFoundInConfiguration("Мультиметр (FastMeter)");
    }

    /// <summary>
    /// Выполняет подключение к устройству, если оно поддерживает интерфейс <see cref="IDevice"/>.
    /// Используется для подключения УКШ, мультиметра и других физических устройств.
    /// </summary>
    /// <param name="device">Устройство для подключения.</param>
    /// <param name="ui">Сервис взаимодействия с пользователем (протокол).</param>
    /// <param name="token">Токен отмены операции.</param>
    /// <exception cref="DeviceException">
    /// Генерируется, если подключение завершилось неуспешно.
    /// </exception>
    public static async Task ConnectIfNeededAsync(
      object device,
      IUserInteractionService ui,
      CancellationToken token)
    {
      token.ThrowIfCancellationRequested();

      if (device is not IDevice connectable)
        return;

      var (connected, message) = await connectable.ConnectableManager.ConnectAsync(ui);
      if (!connected)
      {
        throw ConnectionExceptionAdapter.ConnectByRoleFailed(
          $"{connectable.Name}({connectable.Number}) - {message}");
      }
    }

    /// <summary>
    /// Подключает мультиметр к указанной паре шин через устройство коммутации шин (УКШ).
    /// </summary>
    /// <param name="uksh">Экземпляр УКШ.</param>
    /// <param name="pairBus">Пара шин, к которой требуется подключить мультиметр.</param>
    /// <param name="ui">Сервис взаимодействия с пользователем (протокол).</param>
    /// <param name="token">Токен отмены операции.</param>
    /// <exception cref="DeviceException">
    /// Генерируется, если коммутация мультиметра завершилась ошибкой.
    /// </exception>
    public static async Task ConnectMultimeterToBusAsync(
      ISwitchingDevice uksh,
      SwitchingBusNew pairBus,
      IUserInteractionService ui,
      CancellationToken token)
    {
      token.ThrowIfCancellationRequested();

      await uksh.ConnectorManager.ConnectMultimeter(pairBus, ui);
    }

    /// <summary>
    /// Переводит мультиметр в режим измерения электрического сопротивления.
    /// Должен быть вызван перед выполнением любых измерений.
    /// </summary>
    /// <param name="meter">Экземпляр мультиметра.</param>
    /// <param name="ui">Сервис взаимодействия с пользователем (протокол).</param>
    /// <param name="token">Токен отмены операции.</param>
    /// <exception cref="DeviceException">
    /// Генерируется, если не удалось установить режим измерения сопротивления.
    /// </exception>
    public static async Task EnsureResistanceModeAsync(
      IFastMeter meter,
      IUserInteractionService ui,
      CancellationToken token)
    {
      token.ThrowIfCancellationRequested();

      await meter.ContinuityManager.SetContinuityModeAsync(ui);
    }

    /// <summary>
    /// Выполняет измерение электрического сопротивления с использованием мультиметра.
    /// Возвращает измеренное значение в Омах.
    /// </summary>
    /// <param name="meter">Экземпляр мультиметра.</param>
    /// <param name="ui">Сервис взаимодействия с пользователем (протокол).</param>
    /// <param name="token">Токен отмены операции.</param>
    /// <param name="param">
    /// Ожидаемое значение сопротивления (может быть 0, если эталон отсутствует).
    /// </param>
    /// <param name="lower">Нижняя граница допустимого диапазона.</param>
    /// <param name="upper">Верхняя граница допустимого диапазона.</param>
    /// <returns>Измеренное значение сопротивления (Ом).</returns>
    /// <exception cref="DeviceException">
    /// Генерируется при ошибке выполнения измерения.
    /// </exception>
    public static async Task<double> MeasureResistanceAsync(
      IFastMeter meter,
      IUserInteractionService ui,
      CancellationToken token,
      int pointNumber,
      IRelaySwitchModule _module,
      double param = 0,
      double lower = 0)
    {
      var answer = await meter.ContinuityManager.CheckContinuityAsync(param);
      token.ThrowIfCancellationRequested();
      string point = $"{_module.NumberChassis}.{_module.Number}.{pointNumber}";
      var (_, result) = await MessageManager.ShowMeasurementResultAsync(ui, MeasurementTypeCommand.KC, lower, param, answer, point);
      return result;
    }

    /// <summary>
    /// Выполняет полную подготовку устройства коммутации шин и мультиметра:
    ///  • поиск устройств по номеру шасси;
    ///  • подключение к устройствам;
    ///  • коммутация мультиметра к заданной паре шин;
    ///  • установка режима измерения сопротивления.
    /// </summary>
    /// <param name="numberChassis">Номер шасси.</param>
    /// <param name="pairBus">Пара шин для измерения.</param>
    /// <param name="ui">Сервис взаимодействия с пользователем (протокол).</param>
    /// <param name="token">Токен отмены операции.</param>
    /// <returns>
    /// Кортеж, содержащий инициализированные:
    /// <see cref="ISwitchingDevice"/> и <see cref="IFastMeter"/>.
    /// </returns>
    /// <exception cref="DeviceException">
    /// Генерируется при любой ошибке подготовки оборудования.
    /// </exception>
    public static async Task<(ISwitchingDevice Uksh, IFastMeter Meter)> PrepareUkshAndMeterAsync(
      int numberChassis,
      SwitchingBusNew pairBus,
      IUserInteractionService ui,
      CancellationToken token)
    {
      var uksh = ResolveUksh(numberChassis);
      var meter = ResolveFastMeter(numberChassis);

      await ConnectIfNeededAsync(uksh, ui, token);
      await ConnectIfNeededAsync(meter, ui, token);

      await ConnectMultimeterToBusAsync(uksh, pairBus, ui, token);
      await EnsureResistanceModeAsync(meter, ui, token);

      return (uksh, meter);
    }

    /// <summary>
    /// Отключает мультиметр от указанной пары шин через устройство коммутации шин (УКШ).
    /// Используется при завершении теста или при аварийном выходе.
    /// </summary>
    /// <param name="uksh">Экземпляр устройства коммутации шин.</param>
    /// <param name="pairBus">Пара шин, от которой требуется отключить мультиметр.</param>
    /// <param name="ui">Сервис взаимодействия с пользователем (протокол).</param>
    /// <param name="token">Токен отмены операции.</param>
    /// <exception cref="DeviceException">
    /// Генерируется, если операция отключения завершилась неуспешно.
    /// </exception>
    public static async Task DisconnectMultimeterFromBusAsync(
      ISwitchingDevice uksh,
      SwitchingBusNew pairBus,
      IUserInteractionService ui,
      CancellationToken token)
    {
      await uksh.ConnectorManager.DisconnectMultimeter(pairBus, ui);
    }

    /// <summary>
    /// Выполняет отключение мультиметра от системы и переводит его в безопасное состояние.
    /// Используется при штатном и аварийном завершении измерений.
    /// </summary>
    /// <param name="meter">Экземпляр мультиметра.</param>
    /// <param name="ui">Сервис взаимодействия с пользователем (протокол).</param>
    /// <param name="token">Токен отмены операции.</param>
    public static async Task ShutdownMeterAsync(
      IFastMeter meter,
      IUserInteractionService ui,
      CancellationToken token)
    {
      await meter.ConnectableManager.DisconnectAsync(ui);
    }

    /// <summary>
    /// Выполняет отключение устройства коммутации шин и переводит его в исходное состояние.
    /// </summary>
    /// <param name="uksh">Экземпляр устройства коммутации шин.</param>
    /// <param name="ui">Сервис взаимодействия с пользователем (протокол).</param>
    /// <param name="token">Токен отмены операции.</param>
    public static async Task ShutdownUkshAsync(
      ISwitchingDevice uksh,
      IUserInteractionService ui,
      CancellationToken token)
    {
      await uksh.ConnectorManager.DisconnectAllBuses(ui);
    }

    #endregion
  }
}
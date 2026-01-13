using Ask.Core.Services.Errors.Device.Adapters;
using Ask.Core.Services.Errors.Device.ModuleRelayControl;
using Ask.Core.Services.UI;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.RelaySwitchModule;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
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
    /// <param name="lowVoltage">
    /// Флаг режима низкого вольтажа:
    /// <c>true</c> — использовать низкий уровень напряжения,
    /// <c>false</c> — использовать стандартный (высокий) уровень напряжения.
    /// </param>
    public static async Task<bool> BusConnectAsync(SwitchingBus bus, IRelaySwitchModule module, IUserInteractionService _userInteractionService, CancellationToken cancellationToken)
    {
      cancellationToken.ThrowIfCancellationRequested();

      if (!await UserActionHelper.GetRunWithUserRepeatAsync(() => module.BusManager.ConnectBusAsync(bus, userMessageService: _userInteractionService), _userInteractionService))
        throw BusExceptionFactory.ConnectFailed(bus.ToString(), module.Name, module.NumberChassis, module.Number);

      return true;
    }

    /// <summary>
    /// Отключает заданный БК от указанной шины.
    /// </summary>
    /// <param name="bus">Коммутационная шина</param>
    /// <param name="module">Блок коммутации</param>
    /// <param name="lowVoltage">
    /// Флаг режима низкого вольтажа:
    /// <c>true</c> — использовать низкий уровень напряжения,
    /// <c>false</c> — использовать стандартный (высокий) уровень напряжения.
    /// </param>
    public static async Task<bool> BusDisconnectAsync(SwitchingBus bus, IRelaySwitchModule module, IUserInteractionService _userInteractionService, CancellationToken cancellationToken)
    {
      cancellationToken.ThrowIfCancellationRequested();

      if (!await UserActionHelper.GetRunWithUserRepeatAsync(() => module.BusManager.DisconnectBusAsync(bus), _userInteractionService))
        throw BusExceptionFactory.DisconnectFailed(bus.ToString(), module.Name, module.NumberChassis, module.Number);

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
      var (state, answer) = await UserActionHelper.GetRunWithUserRepeatAsync(() => module.ConnectableManager.InitializeAsync(messageService), _userInteractionService);

      if (!state)
      {
        ConnectionExceptionAdapter.InitializeFailed(module.Name, module.NumberChassis, module.Number);
      }

      return true;
    }

    /// <summary>
    /// Выполняет сброс указанного БК.
    /// </summary>
    /// <param name="module">Блок коммутации</param>
    public static async Task ResetModule(IUserInteractionService messageService, IUserInteractionService _userInteractionService, IRelaySwitchModule module)
    {
      if (!await UserActionHelper.GetRunWithUserRepeatAsync(() => module.ConnectableManager.ResetAsync(messageService), _userInteractionService))
        throw ConnectionExceptionAdapter.ResetFailed(module.Name, module.NumberChassis, module.Number);
    }

    /// <summary>
    /// Подключает точку (реле) заданного БК к указанной шине.
    /// </summary>
    /// <param name="module">Блок коммутации</param>
    /// <param name="bus">Шина</param>
    /// <param name="point">Точка (реле)</param>
    /// <returns>Возвращает <c>true</c>, если точка успешно подключена; иначе — <c>false</c>.</returns>
    public static async Task<bool> PointConnectAsync(IRelaySwitchModule module, BusPoint bus, int point, IUserInteractionService _userInteractionService, CancellationToken cancellationToken)
    {
      cancellationToken.ThrowIfCancellationRequested();
      if (!await UserActionHelper.GetRunWithUserRepeatAsync(() => module.PointManager.ConnectRelayAsync(bus, point, _userInteractionService), _userInteractionService))
        throw RelayExceptionFactory.ConnectPointFailed(point.ToString(), module.Name, module.NumberChassis, module.Number);

      return true;
    }

    /// <summary>
    /// Отключает точку (реле) заданного БК от указанной шины.
    /// </summary>
    /// <param name="module">Блок коммутации</param>
    /// <param name="bus">Шина</param>
    /// <param name="point">Точка (реле)</param>
    /// <returns>Возвращает <c>true</c>, если точка успешно отключена; иначе — <c>false</c>.</returns>
    public static async Task<bool> PointDisconnectAsync(IRelaySwitchModule module, BusPoint bus, int point, IUserInteractionService _userInteractionService, CancellationToken cancellationToken)
    {
      cancellationToken.ThrowIfCancellationRequested();
      if (!await UserActionHelper.GetRunWithUserRepeatAsync(() => module.PointManager.DisconnectRelayAsync(bus, point, _userInteractionService), _userInteractionService))
        throw RelayExceptionFactory.DisconnectPointFailed(point.ToString(), module.Name, module.NumberChassis, module.Number);

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
      if (!await UserActionHelper.GetRunWithUserRepeatAsync(() => module.MeterManager.ConnectMeterAsync(_userInteractionService), _userInteractionService))
        throw MeterExceptionFactory.ConnectFailed(module.Name, module.NumberChassis, module.Number);

      return true;
    }

    /// <summary>
    /// Отключает измеритель БК.
    /// </summary>
    /// <param name="module">Блок коммутации</param>
    /// <returns>Возвращает <c>true</c>, если устройство выключилось; иначе — <c>false</c>.</returns>
    public static async Task<bool> MeterDisableAsync(IUserInteractionService messageService, IUserInteractionService _userInteractionService, IRelaySwitchModule module)
    {
      if (!await UserActionHelper.GetRunWithUserRepeatAsync(() => module.MeterManager.DisconnectMeterAsync(_userInteractionService), _userInteractionService))
        throw MeterExceptionFactory.DisconnectFailed(module.Name, module.NumberChassis, module.Number);

      return true;
    }

    /// <summary>
    /// Получает ответ измерителя указанного БК и отображает сообщение об измерении.
    /// </summary>
    /// <param name="module">Блок коммутации</param>
    /// <returns>Возвращает <c>true</c>, если есть замыкание; иначе — <c>false</c>.</returns>
    public static async Task<bool> GetMeterAnswer(IRelaySwitchModule module, IUserInteractionService _userInteractionService, CancellationToken cancellationToken)
    {
      cancellationToken.ThrowIfCancellationRequested();
      if (!await UserActionHelper.GetRunWithUserRepeatAsync(() => module.MeterManager.GetMeterResponseAsync(_userInteractionService), _userInteractionService))
        throw MeterExceptionFactory.MeterAnswerFailed(module.Name, module.NumberChassis, module.Number);

      return true;
    }

    #endregion
  }
}
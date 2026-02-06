using Ask.Core.Shared.DTO.Devices.RelaySwitchModule;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;

namespace Ask.Core.Shared.Interfaces.DeviceInterfaces.SwitchingDevice.Capabilities
{
  /// <summary>
  /// Интерфейс для управления коммутацией устройств на шинах.
  /// </summary>
  public interface IConnectorDeviceBusCommutation
  {
    /// <summary>
    /// Подключает мультиметр к указанной шине.
    /// </summary>
    /// <param name="bus">Шина, к которой подключается мультиметр.</param>
    /// <returns>Возвращает <c>true</c>, если операция выполнена успешно, иначе <c>false</c>.</returns>
    Task<bool> ConnectMultimeter(SwitchingBusNew bus, IUserInteractionService? userMessageService = null);

    /// <summary>
    /// Отключает мультиметр от указанной шины.
    /// </summary>
    /// <param name="bus">Шина, от которой отключается мультиметр.</param>
    /// <returns>Возвращает <c>true</c>, если операция выполнена успешно, иначе <c>false</c>.</returns>
    Task<bool> DisconnectMultimeter(SwitchingBusNew bus, IUserInteractionService? userMessageService = null);

    /// <summary>
    /// Подключает прецизионный источник напряжения и тока (ПИНТ) к указанной шине.
    /// </summary>
    /// <param name="bus">Шина, к которой подключается ПИНТ.</param>
    /// <returns>Возвращает <c>true</c>, если операция выполнена успешно, иначе <c>false</c>.</returns>
    Task<bool> ConnectPINT(SwitchingBusNew bus, IUserInteractionService? userMessageService = null);

    /// <summary>
    /// Отключает прецизионный источник напряжения и тока (ПИНТ) от указанной шины.
    /// </summary>
    /// <param name="bus">Шина, от которой отключается ПИНТ.</param>
    /// <returns>Возвращает <c>true</c>, если операция выполнена успешно, иначе <c>false</c>.</returns>
    Task<bool> DisconnectPINT(SwitchingBusNew bus, IUserInteractionService? userMessageService = null);

    /// <summary>
    /// Подключает пробойную установку к системе.
    /// </summary>
    /// <returns>Возвращает <c>true</c>, если операция выполнена успешно, иначе <c>false</c>.</returns>
    Task<bool> ConnectBreakdownTester(IUserInteractionService? userMessageService = null);

    /// <summary>
    /// Отключает пробойную установку от системы.
    /// </summary>
    /// <returns>Возвращает <c>true</c>, если операция выполнена успешно, иначе <c>false</c>.</returns>
    Task<bool> DisconnectBreakdownTester(IUserInteractionService? userMessageService = null);

    /// <summary>
    /// Подлючает все шины устрйоства.
    /// </summary>
    Task<bool> ConnectAllBuses(IUserInteractionService? userMessageService = null);

    /// <summary>
    /// Отключает все шины устройства.
    /// </summary>
    Task<bool> DisconnectAllBuses(IUserInteractionService? userMessageService = null);

    /// <summary>
    /// Подлючаект пробойную установку и мультиметр.
    /// </summary>
    /// <param name="userMessageService">Сервис для вывода сообщений.</param>
    Task<bool> ConnectBreakdownTesterAndMultimeter(IUserInteractionService? userMessageService = null);

    /// <summary>
    /// Отключает пробойную установку и мультиметр.
    /// </summary>
    /// <param name="userMessageService">Сервис для вывода сообщений.</param>
    Task<bool> DisconnectBreakdownTesterAndMultimeter(IUserInteractionService? userMessageService = null);

    IReadOnlyList<DeviceConnectionInfo> GetConnectedDevices();
  }
}


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
    /// <param name="userMessageService">Сервис для вывода сообщений.</param>
    /// <returns>
    /// <see langword="true"/>, если операция выполнена успешно; иначе — <see langword="false"/>.
    /// </returns>
    Task<bool> ConnectMultimeter(SwitchingBusNew bus, IUserInteractionService? userMessageService = null);

    /// <summary>
    /// Отключает мультиметр от указанной шины.
    /// </summary>
    /// <param name="bus">Шина, от которой отключается мультиметр.</param>
    /// <param name="userMessageService">Сервис для вывода сообщений.</param>
    /// <returns>
    /// <see langword="true"/>, если операция выполнена успешно; иначе — <see langword="false"/>.
    /// </returns>
    Task<bool> DisconnectMultimeter(SwitchingBusNew bus, IUserInteractionService? userMessageService = null);

    /// <summary>
    /// Подключает прецизионный источник напряжения и тока (ПИНТ) к указанной шине.
    /// </summary>
    /// <param name="bus">Шина, к которой подключается ПИНТ.</param>
    /// <param name="userMessageService">Сервис для вывода сообщений.</param>
    /// <returns>
    /// <see langword="true"/>, если операция выполнена успешно; иначе — <see langword="false"/>.
    /// </returns>
    Task<bool> ConnectPINT(SwitchingBusNew bus, IUserInteractionService? userMessageService = null);

    /// <summary>
    /// Отключает прецизионный источник напряжения и тока (ПИНТ) от указанной шины.
    /// </summary>
    /// <param name="bus">Шина, от которой отключается ПИНТ.</param>
    /// <param name="userMessageService">Сервис для вывода сообщений.</param>
    /// <returns>
    /// <see langword="true"/>, если операция выполнена успешно; иначе — <see langword="false"/>.
    /// </returns>
    Task<bool> DisconnectPINT(SwitchingBusNew bus, IUserInteractionService? userMessageService = null);

    /// <summary>
    /// Подключает пробойную установку к системе.
    /// </summary>
    /// <param name="userMessageService">Сервис для вывода сообщений.</param>
    /// <returns>
    /// <see langword="true"/>, если операция выполнена успешно; иначе — <see langword="false"/>.
    /// </returns>
    Task<bool> ConnectBreakdownTester(IUserInteractionService? userMessageService = null);

    /// <summary>
    /// Отключает пробойную установку от системы.
    /// </summary>
    /// <param name="userMessageService">Сервис для вывода сообщений.</param>
    /// <returns>
    /// <see langword="true"/>, если операция выполнена успешно; иначе — <see langword="false"/>.
    /// </returns>
    Task<bool> DisconnectBreakdownTester(IUserInteractionService? userMessageService = null);

    /// <summary>
    /// Подлючает все шины устрйоства.
    /// </summary>
    /// <param name="userMessageService">Сервис для вывода сообщений.</param>
    /// <returns>
    /// <see langword="true"/>, если операция выполнена успешно; иначе — <see langword="false"/>.
    /// </returns>
    Task<bool> ConnectAllBuses(IUserInteractionService? userMessageService = null);

    /// <summary>
    /// Отключает все шины устройства.
    /// </summary>
    /// <param name="userMessageService">Сервис для вывода сообщений.</param>
    /// <returns>
    /// <see langword="true"/>, если операция выполнена успешно; иначе — <see langword="false"/>.
    /// </returns>
    Task<bool> DisconnectAllBuses(IUserInteractionService? userMessageService = null);

    /// <summary>
    /// Подлючаект пробойную установку и мультиметр.
    /// </summary>
    /// <param name="userMessageService">Сервис для вывода сообщений.</param>
    /// <returns>
    /// <see langword="true"/>, если операция выполнена успешно; иначе — <see langword="false"/>.
    /// </returns>
    Task<bool> ConnectBreakdownTesterAndMultimeter(IUserInteractionService? userMessageService = null);

    /// <summary>
    /// Отключает пробойную установку и мультиметр.
    /// </summary>
    /// <param name="userMessageService">Сервис для вывода сообщений.</param>
    /// <returns>
    /// <see langword="true"/>, если операция выполнена успешно; иначе — <see langword="false"/>.
    /// </returns>
    Task<bool> DisconnectBreakdownTesterAndMultimeter(IUserInteractionService? userMessageService = null);

    /// <summary>
    /// Включает делитель УКШ.
    /// </summary>
    /// <param name="userMessageService">Сервис для вывода сообщений.</param>
    /// <returns>
    /// <see langword="true"/>, если операция выполнена успешно; иначе — <see langword="false"/>.
    /// </returns>
    Task<bool> EnableDivider(IUserInteractionService? userMessageService = null);

    /// <summary>
    /// Отключает делитель УКШ.
    /// </summary>
    /// <param name="userMessageService">Сервис для вывода сообщений.</param>
    /// <returns>
    /// <see langword="true"/>, если операция выполнена успешно; иначе — <see langword="false"/>.
    /// </returns>
    Task<bool> DisableDivider(IUserInteractionService? userMessageService = null);
    IReadOnlyList <DeviceConnectionInfo> GetConnectedDevices();
  }
}

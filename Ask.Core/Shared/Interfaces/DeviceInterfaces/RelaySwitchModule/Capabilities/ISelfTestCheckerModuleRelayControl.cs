using Ask.Core.Shared.Interfaces.DeviceInterfaces.SwitchingDevice;
using Ask.Core.Shared.Interfaces.UiInterfaces;

namespace Ask.Core.Shared.Interfaces.DeviceInterfaces.RelaySwitchModule.Capabilities
{
  /// <summary>
  /// Интерфейс для выполнения проверки модуля коммутации реле.
  /// </summary>
  public interface ISelfTestCheckerModuleRelayControl
  {
    /// <summary>
    /// Запуск самоконтроля модуля коммутации реле.
    /// </summary>
    /// <param name="messageService">Сервис отображения сообщений.</param>
    /// <param name="relaySwitchModule">Модуль коммутации реле.</param>
    /// <param name="device">Устройство коммутации шин.</param>
    /// <param name="meter">Измеритель.</param>
    Task StartSelfCheck(CancellationToken cancellationToken, System.Enum typeConnector, IUserInteractionService? userMessageService = null, ISwitchingDevice device = null);


    /// <summary>
    /// Возвращает тип перечисления, используемый как тип проверки.
    /// </summary>
    Type GetTestTypeEnum();

    /// <summary>
    /// Проверяет пару шин A и B по номеру и выдаёт ответ.
    /// </summary>
    /// <param name="numbet"></param>
    /// <returns></returns>
    Task<(bool, string)> TryGetCheckBusConntcrion(int number, IUserInteractionService? userMessageService = null);
  }
}

using Ask.Core.Shared.DTO.Executor;
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
    Task StartSelfCheck(CancellationToken cancellationToken, System.Enum typeConnector, ActionSettings settings, IUserInteractionService? userMessageService = null, ISwitchingDevice device = null);

    /// <summary>
    /// Выполняет самоконтроль одной точки модуля коммутации реле.
    /// </summary>
    /// <param name="pointNumber">Номер проверяемой точки.</param>
    /// <param name="cancellationToken">Маркер отмены операции.</param>
    /// <param name="userMessageService">Сервис отображения сообщений.</param>
    /// <returns>
    /// <see langword="true"/>, если все этапы самоконтроля точки выполнены успешно.
    /// В противном случае — <see langword="false"/>.
    /// </returns>
    Task<bool> CheckPointAsync(
      int pointNumber,
      CancellationToken cancellationToken = default,
      IUserInteractionService? userMessageService = null);

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

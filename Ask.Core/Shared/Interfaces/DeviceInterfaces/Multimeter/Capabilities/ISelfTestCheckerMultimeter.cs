using Ask.Core.Shared.DTO.Executor;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.SwitchingDevice;
using Ask.Core.Shared.Interfaces.UiInterfaces;

namespace Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter.Capabilities
{
  public interface ISelfTestCheckerMultimeter
  {
    /// <summary>
    /// Запуск самоконтроля мультиметра для выбранного типа проверки.
    /// </summary>
    /// <param name="userMessageService">Сервис отображения сообщений.</param>
    /// <param name="selectedType">Выбранное значение перечисления.</param>
    /// <param name="device">Устройство коммутации шин (необязательно).</param>
    /// <param name="meter">Измеритель (необязательно).</param>
    Task StartSelfCheck(CancellationToken cancellationToken, System.Enum selectedType, ActionSettings settings, IUserInteractionService? userMessageService = null, ISwitchingDevice device = null, IMultimeter meter = null);

    /// <summary>
    /// Возвращает тип перечисления, используемый как тип проверки.
    /// </summary>
    Type GetTestTypeEnum();
  }
}

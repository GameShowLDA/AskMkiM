
using Ask.Core.Shared.DTO.Executor;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.SwitchingDevice;
using Ask.Core.Shared.Interfaces.UiInterfaces;

namespace Ask.Core.Shared.Interfaces.DeviceInterfaces.BreakdownTester.Capabilities
{
  public interface ISelfTestCheckerBreakdownTester
  {
    /// <summary>
    /// Запуск самоконтроля устройства коммутации шин для выбранного типа проверки.
    /// </summary>
    /// <param name="messageService">Сервис отображения сообщений.</param>
    /// <param name="selectedType">Выбранное значение перечисления.</param>
    /// <param name="device">Устройство коммутации шин (необязательно).</param>
    /// <param name="meter">Измеритель (необязательно).</param>
    Task StartSelfCheck(CancellationToken cancellationToken, System.Enum selectedType, ActionSettings settings, IUserInteractionService? userMessageService = null, IBreakdownTester breakdownTester = null, ISwitchingDevice device = null, IMultimeter meter = null);

    /// <summary>
    /// Возвращает тип перечисления, используемый как тип проверки.
    /// </summary>
    Type GetTestTypeEnum();
  }
}

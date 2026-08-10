using Ask.Core.Shared.DTO.Executor;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.SwitchingDevice;
using Ask.Core.Shared.Interfaces.UiInterfaces;

namespace Ask.Core.Shared.Interfaces.DeviceInterfaces.PowerSourceModule.Capabilities
{
  public interface ISelfTestCheckerModuleVoltageCurrentSource
  {
    /// <summary>
    /// Запуск самоконтроля устройства коммутации шин.
    /// </summary>
    /// <param name="messageService"></param>
    /// <returns></returns>
    Task StartSelfCheck(CancellationToken cancellationToken, IUserInteractionService messageService, ActionSettings settings, System.Enum selectedType, ISwitchingDevice device = null, IPowerSourceModule powerDevice = null, IMultimeter meter = null);

    /// <summary>
    /// Возвращает тип перечисления, используемый как тип проверки.
    /// </summary>
    Type GetTestTypeEnum();
  }
}

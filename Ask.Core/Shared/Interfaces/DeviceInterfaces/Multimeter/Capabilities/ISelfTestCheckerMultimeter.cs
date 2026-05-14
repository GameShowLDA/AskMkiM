using Ask.Core.Shared.Interfaces.DeviceInterfaces.BreakdownTester;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.SwitchingDevice;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    Task StartSelfCheck(CancellationToken cancellationToken, System.Enum selectedType, IUserInteractionService? userMessageService = null, ISwitchingDevice device = null, IFastMeter meter = null);

    /// <summary>
    /// Возвращает тип перечисления, используемый как тип проверки.
    /// </summary>
    Type GetTestTypeEnum();
  }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AppConfiguration.Interface;
using NewCore.Base.Interface.Main;
using Utilities.Interface;

namespace NewCore.Base.Interface.Additionally
{
  public interface ISelfTestCheckerModuleVoltageCurrentSource
  {
    /// <summary>
    /// Запуск самоконтроля устройства коммутации шин.
    /// </summary>
    /// <param name="messageService"></param>
    /// <returns></returns>
    Task StartSelfCheck(CancellationToken cancellationToken, IUserMessageService messageService, System.Enum selectedType,  ISwitchingDevice device = null, IPowerSourceModule powerDevice = null, IFastMeter meter = null);

    /// <summary>
    /// Возвращает тип перечисления, используемый как тип проверки.
    /// </summary>
    Type GetTestTypeEnum();
  }
}

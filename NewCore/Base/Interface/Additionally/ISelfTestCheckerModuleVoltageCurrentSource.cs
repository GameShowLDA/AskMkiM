using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AppConfiguration.Interface;
using NewCore.Base.Interface.Main;

namespace NewCore.Base.Interface.Additionally
{
  public interface ISelfTestCheckerModuleVoltageCurrentSource
  {
    /// <summary>
    /// Запуск самоконтроля устройства коммутации шин.
    /// </summary>
    /// <param name="messageService"></param>
    /// <returns></returns>
    Task StartSelfCheck(IUserMessageService messageService, ISwitchingDevice device = null, IPowerSourceModule powerDevice = null, IFastMeter meter = null);
  }
}

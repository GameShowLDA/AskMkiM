using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NewCore.Base.Interface.Main;
using static NewCore.Enum.DeviceEnum;

namespace NewCore.Function.DeviceBusCommutation.SelfCheck
{
  /// <summary>
  /// Содержит вспомогательные методы для инициализации соединений и настройки измерительных приборов
  /// перед запуском процедуры самотестирования.
  /// </summary>
  static internal class SelfTestConnectionHelper
  {
    static internal async Task<bool> SettingsMeter(IFastMeter meter)
    {
      var connect = false;
      connect = (await meter.ConnectableManager.ConnectAsync()).Connect;
      if (!connect)
      {
        return connect;
      }

      await meter.ContinuityManager.SetContinuityModeAsync();

      return connect;
    }
    static internal async Task<bool> CheckConnectionsAsync(ISwitchingDevice device, IFastMeter meter)
    {
      var result1 = await device.ConnectableManager.InitializeAsync();
      var result2 = await meter.ConnectableManager.InitializeAsync();

      if (result1.Connect && result2.Connect)
      {
        SelfTestManager.MeterConnect = true;
        SelfTestManager.DbcConnect = true;
        return true;
      }
      return false;
    }
  }
}

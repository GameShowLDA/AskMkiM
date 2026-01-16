using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.SwitchingDevice;
using Ask.Core.Shared.Interfaces.UiInterfaces;

namespace NewCore.Function.DeviceBusCommutation.SelfCheck
{
  /// <summary>
  /// Содержит вспомогательные методы для инициализации соединений и настройки измерительных приборов
  /// перед запуском процедуры самотестирования.
  /// </summary>
  static internal class SelfTestConnectionHelper
  {
    static internal async Task<bool> SettingsMeter(IFastMeter meter, IUserInteractionService userMessageService)
    {
      var connect = false;
      connect = (await meter.ConnectableManager.ConnectAsync(userMessageService)).Connect;

      if (!connect)
      {
        return connect;
      }

      await meter.ContinuityManager.SetContinuityModeAsync(userMessageService);
      return connect;
    }
    static internal async Task<bool> CheckConnectionsAsync(ISwitchingDevice device, IFastMeter meter, IUserInteractionService userMessageService)
    {
      var result1 = await device.ConnectableManager.InitializeAsync(userMessageService);
      var result2 = await meter.ConnectableManager.InitializeAsync(userMessageService);

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

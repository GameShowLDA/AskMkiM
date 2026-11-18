using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.SwitchingDevice;
using Ask.Core.Shared.Interfaces.UiInterfaces;

namespace Ask.Device.Runtime.Function.DeviceBusCommutation.SelfCheck
{
  /// <summary>
  /// Содержит вспомогательные методы для инициализации соединений и настройки измерительных приборов
  /// перед запуском процедуры самотестирования.
  /// </summary>
  internal static class SelfTestConnectionHelper
  {
    /// <summary>
    /// Настраивает мультиметр для выполнения измерений.
    /// </summary>
    /// <param name="meter">Мультиметр, используемый для выполнения измерений.</param>
    /// <param name="userMessageService">Сервис взаимодействия с пользователем.</param>
    /// <returns>
    /// <see langword="true"/>, если подключение к мультиметру установлено;
    /// иначе — <see langword="false"/>.
    /// </returns>
    internal static async Task<bool> SettingsMeter(IMultimeter meter, IUserInteractionService userMessageService)
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

    /// <summary>
    /// Проверяет и инициализирует подключения к коммутационному устройству и мультиметру.
    /// </summary>
    /// <param name="device">Коммутационное устройство.</param>
    /// <param name="meter">Мультиметр.</param>
    /// <param name="userMessageService">Сервис взаимодействия с пользователем.</param>
    /// <returns>
    /// <see langword="true"/>, если оба устройства успешно подключены;
    /// иначе — <see langword="false"/>.
    /// </returns>
    internal static async Task<bool> CheckConnectionsAsync(ISwitchingDevice device, IMultimeter meter, IUserInteractionService userMessageService)
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

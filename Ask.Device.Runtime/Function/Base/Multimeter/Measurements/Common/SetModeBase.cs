using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.Errors.Device.Multimeter;
using Ask.Core.Services.Extensions;
using Ask.Core.Services.UI;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Device.Emulator;
using Ask.Device.Runtime.Function.Helpers;

namespace Ask.Device.Runtime.Function.Base.Multimeter.Measurements.Common
{
  internal class SetModeBase
  {
    /// <inheritdoc />
    static public async Task<bool> SetModeAsync(IMultimeter device, IMeasurementProfile profile, IUserInteractionService? userMessageService = null)
    {
      var header = EnumExtensions.GetDescription(profile.TypeMode);

      var result = await UserActionHelper.GetRunWithUserRepeatAsync(async () =>
      {
        var succes = await SetModeCoreAsync(device, profile, userMessageService);

        if (!succes || DeviceDisplayConfig.GetConnectionInfoVisibility())
        {
          await DeviceMessageBuilder.ShowConnectionMessageAsync(device, $"Установка режима \"{header}\"", succes, 1, userMessageService);
        }

        return succes;
      }, userMessageService, deviceTask: true);

      if (!result)
      {
        throw DiodeExceptionFactory.SetModeFailed(device.Name, device.NumberChassis, device.Number);
      }

      device.TypeMode = profile.TypeMode;
      return result;
    }

    /// <inheritdoc />
    static private async Task<bool> SetModeCoreAsync(IMultimeter device, IMeasurementProfile profile, IUserInteractionService? userMessageService = null)
    {
      if (device.TypeMode == profile.TypeMode)
      {
        return true;
      }

      if (!ExecutionConfig.GetIsIdleModeEnabled() && !device.ConnectionInfo.IsConnected)
      {
        throw new InvalidOperationException("Прибор не подключен.");
      }

      await DeviceProtocolEmulator.QueryMultimeterAsync(device, profile.SetMode, string.Empty, timeout: profile.Timeout);
      var answer = await DeviceProtocolEmulator.QueryMultimeterAsync(
        device,
        profile.GetMode,
        profile.CheckMode,
        timeout: profile.Timeout);
      if (answer.Contains(profile.CheckMode))
      {
        device.TypeMode = profile.TypeMode;
        return true;
      }

      return false;
    }

  }
}

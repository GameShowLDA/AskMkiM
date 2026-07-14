using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.Errors.Device.Multimeter;
using Ask.Core.Services.Extensions;
using Ask.Core.Services.UI;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Device.Runtime.Base.Helpers;

namespace Ask.Device.Runtime.Base.Multimeter.Measurements.Common
{
  internal class SetModeBase
  {
    /// <inheritdoc />
    static public async Task<bool> SetModeAsync(IMultimeter device, IMeasurementProfile profile, IUserInteractionService? userMessageService = null)
    {
      var header = profile.TypeMode.GetDescription();

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
      if (ExecutionConfig.GetIsIdleModeEnabled())
      {
        return true;
      }

      if (device.TypeMode == profile.TypeMode)
      {
        return true;
      }

      if (!device.ConnectionInfo.IsConnected)
      {
        throw new InvalidOperationException("Прибор не подключен.");
      }

      await device.DeviceProtocol.QueryAsync(profile.SetMode);
      var answer = await device.DeviceProtocol.QueryAsync(profile.GetMode, timeout: profile.Timeout);
      if (answer.Contains(profile.CheckMode))
      {
        device.TypeMode = profile.TypeMode;
        return true;
      }

      return false;
    }

  }
}

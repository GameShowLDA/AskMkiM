using Ask.Protocol.Messages.EntryPoints;
using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.Errors.Device.Multimeter;
using Ask.Core.Services.Extensions;
using Ask.Core.Services.UI;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Device.Emulator;
using Ask.Device.Runtime.Function.Helpers;

namespace Ask.Device.Runtime.Function.Base.Multimeter.Measurements.Common
{
  internal class SetModeBase
  {
    /// <inheritdoc />
    static public async Task<bool> SetModeAsync(IMultimeter device, IMeasurementProfile profile, IUserInteractionService? userMessageService = null)
    {
      var header = GetModeHeader(profile.TypeMode);

      var result = await UserActionHelper.GetRunWithUserRepeatAsync(async () =>
      {
        var succes = await SetModeCoreAsync(device, profile, userMessageService);

        if (!succes || DeviceDisplayConfig.GetConnectionInfoVisibility())
        {
          await DeviceMessages.PublishOperationResultAsync(device, header, succes, 1, userMessageService);
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

      await DeviceProtocolEmulator.QueryMultimeterAsync(device, profile.SetMode, string.Empty);
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

    static private string GetModeHeader(MultimeterTypeMode typeMode)
    {
      return typeMode switch
      {
        MultimeterTypeMode.DcVoltage => "Режим измерения постоянного напряжения",
        MultimeterTypeMode.AcVoltage => "Режим измерения переменного напряжения",
        MultimeterTypeMode.Resistance => "Режим измерения сопротивления",
        MultimeterTypeMode.Capacitance => "Режим измерения ёмкости",
        _ => $"Режим \"{EnumExtensions.GetDescription(typeMode)}\"",
      };
    }

  }
}

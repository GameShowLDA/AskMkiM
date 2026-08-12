using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.Errors.Device.Multimeter;
using Ask.Core.Services.Extensions;
using Ask.Core.Services.UI;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Device.Emulator;
using Ask.Device.ResponseProcessor.Multimeter.ResponseProcessing;
using Ask.Device.Runtime.Function.Helpers;

namespace Ask.Device.Runtime.Function.Base.Multimeter.Measurements.Common
{
  internal class SetModeBase
  {
    /// <inheritdoc />
    static public async Task<bool> SetModeAsync(
      IMultimeter device,
      IMeasurementProfile profile,
      IUserInteractionService? userMessageService = null,
      CancellationToken cancellationToken = default)
    {
      var header = GetModeHeader(profile.TypeMode);

      var result = await UserActionHelper.GetRunWithUserRepeatAsync(async () =>
      {
        cancellationToken.ThrowIfCancellationRequested();
        var succes = await SetModeCoreAsync(device, profile, cancellationToken);

        if (!succes || DeviceDisplayConfig.GetConnectionInfoVisibility())
        {
          await MultimeterMessages.PublishOperationResultAsync(device, header, succes, 1, userMessageService);
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
    static private async Task<bool> SetModeCoreAsync(
      IMultimeter device,
      IMeasurementProfile profile,
      CancellationToken cancellationToken)
    {
      if (device.TypeMode == profile.TypeMode)
      {
        return true;
      }

      if (!ExecutionConfig.GetIsIdleModeEnabled() && !device.ConnectionInfo.IsConnected)
      {
        throw new InvalidOperationException("Прибор не подключен.");
      }

      await DeviceProtocolEmulator.QueryMultimeterAsync(
        device,
        profile.SetMode,
        string.Empty,
        cancellationToken: cancellationToken);
      var answer = await DeviceProtocolEmulator.QueryMultimeterAsync(
        device,
        profile.GetMode,
        profile.CheckMode,
        timeout: profile.Timeout,
        cancellationToken: cancellationToken);
      if (MultimeterResponseProcessor.CheckMode(answer, profile.CheckMode))
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

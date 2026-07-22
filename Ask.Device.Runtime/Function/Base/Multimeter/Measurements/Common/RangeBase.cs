using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.Extensions;
using Ask.Core.Services.UI;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Core.Shared.Metadata.Enums.UnitEnums;
using Ask.Device.Runtime.Function.Helpers;
using System.Collections.Concurrent;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace Ask.Device.Runtime.Function.Base.Multimeter.Measurements.Common
{
  internal static class RangeBase
  {
    private static readonly ConcurrentDictionary<string, double> SelectedRanges = new();

    public static Task<bool> SetRangeAsync(
        IMultimeter device,
        double range,
        IUserInteractionService? userMessageService = null)
    {
      return device.TypeMode switch
      {
        MultimeterTypeMode.AcVoltage => SetACVoltageRangeAsync(device, range, userMessageService),
        MultimeterTypeMode.DcVoltage => SetDCVoltageRangeAsync(device, range, userMessageService),
        MultimeterTypeMode.Capacitance => SetCapacitanceRangeAsync(device, range, userMessageService),
        MultimeterTypeMode.Resistance => SetResistanceRangeAsync(device, range, userMessageService),
        _ => throw new InvalidOperationException($"Невозможно установить диапазон для режима {device.TypeMode}.")
      };
    }

    public static Task<bool> SetRangeForMeasurementAsync(
        IMultimeter device,
        double range,
        IUserInteractionService? userMessageService = null)
    {
      var effectiveRange = range <= 0
        ? GetSelectedRange(device)
        : range;

      return SetRangeAsync(device, effectiveRange, userMessageService);
    }

    private static Task<bool> SetACVoltageRangeAsync(
      IMultimeter device,
      double range,
      IUserInteractionService? userMessageService = null)
    {
      return SetMeasurementRangeAsync(
        device,
        device.ACVCommands,
        range,
        profile => profile.SetRange,
        profile => profile.SetAutoRange,
        profile => profile.GetRangeError,
        profile => profile.SupportedRanges,
        profile => 1d,
        userMessageService);
    }

    private static Task<bool> SetDCVoltageRangeAsync(
      IMultimeter device,
      double range,
      IUserInteractionService? userMessageService = null)
    {
      return SetMeasurementRangeAsync(
        device,
        device.DCVCommands,
        range,
        profile => profile.SetRange,
        profile => profile.SetAutoRange,
        profile => profile.GetRangeError,
        profile => profile.SupportedRanges,
        profile => 1d,
        userMessageService);
    }

    private static Task<bool> SetResistanceRangeAsync(
      IMultimeter device,
      double range,
      IUserInteractionService? userMessageService = null)
    {
      return SetMeasurementRangeAsync(
        device,
        device.ResistanceCommands,
        range,
        profile => profile.SetRange,
        profile => profile.SetAutoRange,
        profile => profile.GetRangeError,
        profile => profile.SupportedRanges,
        profile => 1d,
        userMessageService);
    }

    private static Task<bool> SetCapacitanceRangeAsync(
      IMultimeter device,
      double range,
      IUserInteractionService? userMessageService = null)
    {
      return SetMeasurementRangeAsync(
        device,
        device.CapacitanceCommands,
        range,
        profile => profile.SetRange,
        profile => profile.SetAutoRange,
        profile => profile.GetRangeError,
        profile => profile.SupportedRanges,
        profile => profile.RangeCommandMultiplier,
        userMessageService);
    }

    private static async Task<bool> SetMeasurementRangeAsync<TProfile>(
      IMultimeter device,
      TProfile profile,
      double range,
      Func<TProfile, string> setRangeCommand,
      Func<TProfile, string> setAutoRangeCommand,
      Func<TProfile, string?> getRangeErrorCommand,
      Func<TProfile, double[]> getSupportedRanges,
      Func<TProfile, double>? getRangeCommandMultiplier,
      IUserInteractionService? userMessageService)
      where TProfile : IMeasurementProfile
    {
      var header = EnumExtensions.GetDescription(profile.TypeMode);
      var effectiveRange = range <= 0 ? 0 : ResolveRange(range, getSupportedRanges(profile));
      var rangeText = range <= 0
        ? "Авто"
        : $"{effectiveRange.ToString("G", CultureInfo.InvariantCulture)} {profile.Unit.GetUnit()}";
      var rangeKey = BuildRangeKey(device, profile.TypeMode);

      if (SelectedRanges.TryGetValue(rangeKey, out var selectedRange)
        && selectedRange.Equals(effectiveRange))
      {
        return true;
      }

      var result = await UserActionHelper.GetRunWithUserRepeatAsync(async () =>
      {
        var success = await SetMeasurementRangeCoreAsync(
          device,
          profile,
          effectiveRange,
          setRangeCommand(profile),
          setAutoRangeCommand(profile),
          getRangeErrorCommand(profile),
          Array.Empty<double>(),
          getRangeCommandMultiplier?.Invoke(profile) ?? 1d);

        if (!success || DeviceDisplayConfig.GetConnectionInfoVisibility())
        {
          await DeviceMessageBuilder.ShowConnectionMessageAsync(
            device,
            $"Установка диапазона \"{header}\"",
            rangeText,
            success,
            1,
            userMessageService);
        }

        return success;
      }, userMessageService, deviceTask: true);

      if (!result)
      {
        throw new InvalidOperationException($"Ошибка установки диапазона \"{header}\" для {device.Name}({device.NumberChassis}.{device.Number}).");
      }

      SelectedRanges[rangeKey] = effectiveRange;
      return true;
    }

    private static async Task<bool> SetMeasurementRangeCoreAsync(
      IMultimeter device,
      IMeasurementProfile profile,
      double range,
      string setRangeCommand,
      string setAutoRangeCommand,
      string? getRangeErrorCommand,
      double[] supportedRanges,
      double rangeCommandMultiplier)
    {
      if (ExecutionConfig.GetIsIdleModeEnabled())
      {
        return true;
      }

      if (!device.ConnectionInfo.IsConnected)
      {
        throw new InvalidOperationException("Прибор не подключен.");
      }

      if (device.TypeMode != profile.TypeMode)
      {
        await SetModeBase.SetModeAsync(device, profile);
      }

      var command = range <= 0
        ? setAutoRangeCommand
        : BuildRangeCommand(setRangeCommand, profile, ResolveRange(range, supportedRanges), rangeCommandMultiplier);

      await device.DeviceProtocol.QueryAsync(command);
      await EnsureNoInstrumentErrorAsync(device, getRangeErrorCommand, profile.Timeout);

      return true;
    }

    private static string BuildRangeCommand(string template, IMeasurementProfile profile, double range, double rangeCommandMultiplier)
    {
      var commandRange = range * rangeCommandMultiplier;
      return string.Format(
        CultureInfo.InvariantCulture,
        template,
        commandRange,
        ResolveResolution(profile, commandRange));
    }

    private static double ResolveRange(double requestedRange, double[] supportedRanges)
    {
      var requested = Math.Abs(requestedRange);
      if (supportedRanges.Length == 0)
      {
        return requested;
      }

      foreach (var supportedRange in supportedRanges.OrderBy(value => value))
      {
        if (requested <= supportedRange)
        {
          return supportedRange;
        }
      }

      return supportedRanges.Max();
    }

    private static double ResolveResolution(IMeasurementProfile profile, double range)
    {
      return profile.Unit switch
      {
        VoltageUnit => ResolveVoltageResolution(range),
        ResistanceUnit => ResolveResistanceResolution(range),
        _ => range * 0.000001d
      };
    }

    private static double ResolveVoltageResolution(double range)
    {
      return range switch
      {
        <= 0.1d => 0.0000001d,
        <= 1d => 0.000001d,
        <= 10d => 0.00001d,
        <= 100d => 0.0001d,
        _ => 0.001d
      };
    }

    private static double ResolveResistanceResolution(double range)
    {
      return Math.Max(range * 0.000001d, 0.000001d);
    }

    private static async Task EnsureNoInstrumentErrorAsync(
      IMultimeter device,
      string? getRangeErrorCommand,
      int timeout)
    {
      if (string.IsNullOrWhiteSpace(getRangeErrorCommand))
      {
        return;
      }

      var error = await device.DeviceProtocol.QueryAsync(getRangeErrorCommand, timeout: timeout);
      var normalizedError = error?.TrimStart();
      if (!string.IsNullOrWhiteSpace(normalizedError)
        && !normalizedError.StartsWith("+0", StringComparison.Ordinal)
        && !normalizedError.StartsWith("0", StringComparison.Ordinal))
      {
        throw new InvalidOperationException($"Ошибка установки диапазона: {error}");
      }
    }

    private static double GetSelectedRange(IMultimeter device)
    {
      return SelectedRanges.TryGetValue(BuildRangeKey(device, device.TypeMode), out var range)
        ? range
        : 0;
    }

    private static string BuildRangeKey(IMultimeter device, MultimeterTypeMode typeMode)
    {
      return $"{RuntimeHelpers.GetHashCode(device)}:{typeMode}";
    }
  }
}

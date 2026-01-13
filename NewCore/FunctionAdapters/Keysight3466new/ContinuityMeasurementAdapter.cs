using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.Errors.Device.Multimeter;
using Ask.Core.Services.UI;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter.Capabilities;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using NewCore.Device;
using NewCore.Function.Helpers;
using NewCore.Function.Keysight3466new;

namespace NewCore.FunctionAdapters.Keysight3466new
{
  /// <summary>
  /// Адаптер для выполнения прозвонки с использованием прибора Keysight с выводом сообщений.
  /// </summary>
  internal class ContinuityMeasurementAdapter : IContinuityMeasurement
  {
    private readonly KeysightDevice _device;
    private readonly ContinuityMeasurement _measurement;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="ContinuityMeasurementAdapter"/>.
    /// </summary>
    /// <param name="device">Экземпляр прибора Keysight.</param>
    public ContinuityMeasurementAdapter(KeysightDevice device)
    {
      _device = device ?? throw new ArgumentNullException(nameof(device));
      _measurement = new ContinuityMeasurement(device);
    }

    /// <inheritdoc />
    public async Task<bool> SetContinuityModeAsync(IUserInteractionService? userMessageService = null)
    {
      var result = await UserActionHelper.GetRunWithUserRepeatAsync(async () =>
      {
        var succes = await _measurement.SetContinuityModeAsync();

        if (!succes || await DeviceDisplayConfig.GetConnectionInfoVisibilityAsync())
        {
          await DeviceMessageBuilder.ShowConnectionMessageAsync(_device, "Установка режима прозвонки", string.Empty, succes, 1, userMessageService);
        }

        return succes;
      }, userMessageService, deviceTask: true);

      if (!result)
      {
        throw ContinuityExceptionFactory.SetModeFailed(_device.Name, _device.NumberChassis, _device.Number);
      }

      return result;
    }

    /// <inheritdoc />
    public async Task<bool> CheckContinuityAsync(bool expectedOutcome, IUserInteractionService? userMessageService = null)
    {
      if (await ExecutionConfig.GetIsIdleModeEnabled())
      {
        return true;
      }

      try
      {
        var result = await _measurement.CheckContinuityAsync(expectedOutcome);

        await DeviceMessageBuilder.ShowConnectionMessageAsync(_device, "Результат прозвонки", string.Empty, true, 2, userMessageService);
        return result;
      }
      catch (Exception ex)
      {
        await DeviceMessageBuilder.ShowConnectionMessageAsync(_device, "Ошибка при прозвонке", ex.Message, false, 2, userMessageService);
        return false;
      }
    }

    public async Task<double> CheckContinuityAsync(double expectedOutcome, IUserInteractionService? userMessageService = null)
    {
      if (await ExecutionConfig.GetIsIdleModeEnabled())
      {
        return expectedOutcome;
      }

      try
      {
        double result = await _measurement.CheckContinuityAsync(expectedOutcome);

        await DeviceMessageBuilder.ShowConnectionMessageAsync(_device, "Результат прозвонки", expectedOutcome.ToString(), true, 2, userMessageService);

        return result;
      }
      catch (Exception ex)
      {
        await DeviceMessageBuilder.ShowConnectionMessageAsync(_device, "Ошибка при прозвонке", ex.Message, false, 2, userMessageService);
        return -1;
      }
    }
  }
}

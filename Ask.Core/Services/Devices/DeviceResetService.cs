using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Services.UI;
using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.UiEnums;
using System.Windows.Media;
using static Ask.LogLib.LoggerUtility;

namespace Ask.Core.Services.Devices;

/// <summary>
/// Сбрасывает заданные устройства по их индивидуальным каналам связи.
/// </summary>
public static class DeviceResetService
{
  /// <summary>
  /// Последовательно сбрасывает уникальные устройства.
  /// </summary>
  /// <param name="devices">Устройства, участвующие в текущей проверке.</param>
  /// <param name="messageService">Сервис вывода сообщений и выбора действия при ошибке.</param>
  /// <param name="cancellationToken">Токен отмены операции.</param>
  /// <param name="showTestCompletionHeader">
  /// Признак вывода заголовка завершения теста перед сбросом.
  /// </param>
  public static async Task ResetDevicesAsync(
    IEnumerable<IDevice?> devices,
    IUserInteractionService? messageService = null,
    CancellationToken cancellationToken = default,
    bool showTestCompletionHeader = false)
  {
    ArgumentNullException.ThrowIfNull(devices);
    bool mandatoryFinalization = EquipmentExecutionContext.IsMandatoryFinalization;

    if (showTestCompletionHeader && messageService != null)
    {
      try
      {
        await messageService.AppendEmptyLineAsync();
        await messageService.ShowMessageAsync(
          new ShowMessageModel(
            header: "Завершение теста",
            type: ShowMessageModel.MessageType.Command),
          IsBlockStart: true,
          skipPause: true,
          ignoreOutputValidation: true);
      }
      catch (Exception ex)
      {
        LogException("Ошибка вывода заголовка финального сброса оборудования.", ex);

        if (!mandatoryFinalization)
        {
          throw;
        }
      }
    }

    foreach (var device in GetUniqueDevices(devices))
    {
      bool retry;
      do
      {
        if (!mandatoryFinalization)
        {
          cancellationToken.ThrowIfCancellationRequested();
        }

        retry = false;

        bool reset;
        string? error = null;
        try
        {
          reset = await device.ConnectableManager.ResetAsync();
        }
        catch (Exception ex)
        {
          reset = false;
          error = ex.Message;
          LogException($"Ошибка адресного сброса {GetDeviceLabel(device)}.", ex, isDeviceLog: true);
        }

        try
        {
          await ShowResultAsync(device, reset, error, messageService);
        }
        catch (Exception ex)
        {
          LogException(
            $"Ошибка вывода результата адресного сброса {GetDeviceLabel(device)}.",
            ex,
            isDeviceLog: true);

          if (!mandatoryFinalization)
          {
            throw;
          }
        }

        if (reset || messageService == null || mandatoryFinalization)
        {
          continue;
        }

        var action = await messageService.WaitRetryOrContinueAsync();
        retry = action == UserAction.Retry;
        messageService.ButtonService?.ShowOnlyStopAndFinishButtons();
      }
      while (retry);
    }
  }

  private static IEnumerable<IDevice> GetUniqueDevices(IEnumerable<IDevice?> devices)
  {
    return devices
      .Where(static device => device?.ConnectableManager != null)
      .Cast<IDevice>()
      .DistinctBy(static device => (
        device.DeviceType,
        device is IAttachableDevice attachable ? attachable.NumberChassis : 0,
        device.Number,
        device.ConnectionDetails));
  }

  private static async Task ShowResultAsync(
    IDevice device,
    bool reset,
    string? error,
    IUserInteractionService? messageService)
  {
    string label = GetDeviceLabel(device);
    string logResult = reset
      ? "Сброс выполнен успешно."
      : string.IsNullOrWhiteSpace(error)
        ? "Сброс не выполнен."
        : $"Сброс не выполнен: {error}";

    if (reset)
    {
      LogInformation($"{label}: {logResult}", isDeviceLog: true);
    }
    else
    {
      LogError($"{label}: {logResult}", isDeviceLog: true);
    }

    if (messageService == null)
    {
      return;
    }

    await messageService.ShowMessageAsync(
      new ShowMessageModel(
        header: label,
        message: "Сброс устройства",
        messageColor: reset ? Colors.SeaGreen : Colors.IndianRed,
        type: reset
          ? ShowMessageModel.MessageType.Success
          : ShowMessageModel.MessageType.Error)
      {
        IsDeviceMessage = true,
        IndentLevel = 1,
      },
      skipPause: true);
  }

  private static string GetDeviceLabel(IDevice device)
  {
    return device is IAttachableDevice attachable
      ? $"{device.Name}({attachable.NumberChassis}.{device.Number})"
      : $"{device.Name}({device.Number})";
  }
}

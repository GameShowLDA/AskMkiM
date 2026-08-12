using Ask.Core.Shared.DTO.Executor;
using Ask.Core.Shared.DTO.Protocol;
using System.Globalization;

namespace Ask.UI.Features.ProtocolNew.Protocol;

/// <summary>
/// Формирует обязательный финальный блок протокола выполнения программы контроля.
/// </summary>
internal static class ControlProgramCompletionMessageBuilder
{
  /// <summary>
  /// Формирует сообщение с режимом и фактической продолжительностью выполнения.
  /// </summary>
  /// <param name="settings">Параметры завершённого выполнения.</param>
  /// <returns>Финальный блок протокола.</returns>
  public static ShowMessageModel Build(ActionSettings settings)
  {
    ArgumentNullException.ThrowIfNull(settings);

    double totalSeconds = Math.Round(
      Math.Max(0, settings.ExecutionDuration.TotalSeconds),
      1,
      MidpointRounding.AwayFromZero);
    long minutes = (long)(totalSeconds / 60);
    double seconds = totalSeconds - (minutes * 60);
    string formattedSeconds = seconds.ToString("0.0", CultureInfo.GetCultureInfo("ru-RU"));

    return new ShowMessageModel
    {
      Header = $"ЗАВЕРШЕНИЕ ПРОГРАММЫ ({settings.Mode})",
      Message = $"Время выполнения: {minutes} мин {formattedSeconds} с",
      UseSuccessColorForEntireMessage = true,
      CanBeDeleted = false,
    };
  }
}

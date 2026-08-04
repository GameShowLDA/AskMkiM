using Ask.Core.Shared.DTO.Protocol;
using Ask.Protocol.Messages.Builders;

namespace Ask.Protocol.Messages.EntryPoints;

/// <summary>
/// Предоставляет единые точки формирования сообщений о выполнении процессов.
/// </summary>
public static class ExecutionMessages
{
  /// <summary>
  /// Формирует сообщение о подготовке устройств.
  /// </summary>
  /// <returns>Сообщение о подготовке устройств.</returns>
  public static ShowMessageModel BuildDevicesPreparationMessage()
    => ExecutionMessageBuilder.BuildDevicesPreparationMessage();

  /// <summary>
  /// Формирует сообщение о настройке мультиметра.
  /// </summary>
  /// <returns>Сообщение о настройке мультиметра.</returns>
  public static ShowMessageModel BuildMultimeterSetupMessage()
    => ExecutionMessageBuilder.BuildMultimeterSetupMessage();

  /// <summary>
  /// Формирует сообщение о настройке пробойной установки.
  /// </summary>
  /// <returns>Сообщение о настройке пробойной установки.</returns>
  public static ShowMessageModel BuildBreakdownTesterSetupMessage()
    => ExecutionMessageBuilder.BuildBreakdownTesterSetupMessage();
}

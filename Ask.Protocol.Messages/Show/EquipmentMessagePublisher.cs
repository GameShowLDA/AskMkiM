using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using static Ask.LogLib.LoggerUtility;

namespace Ask.Protocol.Messages.Show;

/// <summary>
/// Записывает сообщения оборудования в журнал и передаёт их в экранный протокол.
/// </summary>
internal static class EquipmentMessagePublisher
{
  /// <summary>
  /// Записывает сообщение в журнал оборудования и передаёт его указанному сервису вывода.
  /// </summary>
  /// <param name="message">Сообщение оборудования.</param>
  /// <param name="outputService">Сервис вывода сообщения в экранный протокол.</param>
  /// <returns>Задача, представляющая операцию публикации сообщения.</returns>
  /// <exception cref="ArgumentNullException">
  /// Выбрасывается, если <paramref name="message"/> равен <see langword="null"/>.
  /// </exception>
  internal static async Task PublishAsync(
    ShowMessageModel message,
    IMessageOutputService? outputService)
  {
    ArgumentNullException.ThrowIfNull(message);

    if (message.Status == ShowMessageModel.MessageType.Error)
    {
      LogError(message.ToString(), isDeviceLog: true);
    }
    else
    {
      LogInformation(message.ToString(), isDeviceLog: true);
    }

    if (outputService != null)
    {
      await outputService.ShowMessageAsync(message, skipPause: true);
    }
  }
}

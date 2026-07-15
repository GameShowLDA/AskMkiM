using Ask.Core.Shared.DTO.Protocol;

namespace Ask.Core.Shared.Interfaces.UiInterfaces
{
  /// <summary>
  /// Предоставляет операции редактора, необходимые для вывода одной записи протокола.
  /// </summary>
  public interface IProtocolEntrySink
  {
    /// <summary>
    /// Добавляет подготовленную запись в редактор протокола.
    /// </summary>
    /// <param name="message">Модель выводимой записи.</param>
    /// <param name="isLastMessage">Признак последней записи текущего блока.</param>
    Task AppendLineAsync(ShowMessageModel message, bool isLastMessage);

    /// <summary>
    /// Удаляет последние строки, заменяемые сокращённым представлением протокола.
    /// </summary>
    Task RemoveLastLinesAsync();
  }
}

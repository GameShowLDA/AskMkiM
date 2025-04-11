using System.Threading.Tasks;
using UI.Components.Invoke.InvokeRichTextBox;
using Utilities.Models;
using static AppConfiguration.Protocol.ProtocolConfig;

namespace UI.Controls.ProtocolController.Message
{
  /// <summary>
  /// Класс для управления сообщениями протокола: отображение, очистка, удаление строк.
  /// </summary>
  public class MessageManager
  {
    private readonly InvokeRichTextBoxUI _protocolTextBox;
    private readonly ActionExecutor _executor;
    private readonly ProtocolController _controller;
    private ShowMessageModel _lastModelMessage;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="MessageManager"/>.
    /// </summary>
    /// <param name="protocolTextBox">Элемент текстового вывода протокола.</param>
    /// <param name="executor">Исполнитель действий.</param>
    /// <param name="controller">Основной контроллер, для вызова ProcessStepModeAsync.</param>
    public MessageManager(InvokeRichTextBoxUI protocolTextBox, ActionExecutor executor, ProtocolController controller)
    {
      _protocolTextBox = protocolTextBox;
      _executor = executor;
      _controller = controller;
    }

    /// <summary>
    /// Выводит информацию в протокол.
    /// </summary>
    /// <param name="showMessageModel">Модель сообщения.</param>
    /// <returns>Возвращает режим по шагам.</returns>
    public async Task<bool> ShowMessageAsync(ShowMessageModel showMessageModel)
    {
      if (!await GetShowDetailedProtocol())
      {
        if (_lastModelMessage != null && _lastModelMessage.CanBeDeleted && !_lastModelMessage.ExecutionError)
        {
          await _protocolTextBox.RemoveLastLinesAsync();
        }

        _lastModelMessage = showMessageModel;
      }

      await _protocolTextBox.AppendLineAsync(showMessageModel);

      if (_executor.IsPaused)
      {
        await _controller.PauseManager.WaitWhilePausedAsync();
      }

      return await _controller.ProcessStepModeAsync(_executor.StepMode);
    }

    /// <summary>
    /// Полностью очищает протокол и сбрасывает последнее сообщение.
    /// </summary>
    /// <returns>Возвращает признак успешного завершения операции.</returns>
    public async Task<bool> ClearAllMessagesAsync()
    {
      await _protocolTextBox.ClearAsync();
      _lastModelMessage = null;

      if (_executor.IsPaused)
      {
        await _controller.PauseManager.WaitWhilePausedAsync();
      }

      return await _controller.ProcessStepModeAsync(_executor.StepMode);
    }

    /// <summary>
    /// Асинхронно удаляет блок, содержащий указанную строку, из RichTextBox.
    /// </summary>
    /// <param name="textToRemove">Строка для поиска и удаления.</param>
    /// <returns>True, если блок был найден и удален; иначе False.</returns>
    public async Task<bool> RemoveLineContainingTextAsync(string textToRemove)
        => await _protocolTextBox.RemoveLineContainingTextAsync(textToRemove);
  }
}

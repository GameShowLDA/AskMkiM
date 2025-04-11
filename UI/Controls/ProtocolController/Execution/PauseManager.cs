using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using UI.Controls.ProtocolController.Message;
using Utilities.Models;
using static Utilities.LoggerUtility;

namespace UI.Controls.ProtocolController.Execution
{
  /// <summary>
  /// Менеджер управления паузой выполнения процесса.
  /// </summary>
  public class PauseManager
  {
    private bool _isPaused;
    private bool _shouldShowPauseMessage;
    private bool _shouldShowResumeMessage;
    private TaskCompletionSource<bool> _pauseCompletionSource;
    private readonly MessageManager _messageManager;
    private readonly ProtocolController _protocolController;

    /// <summary>
    /// Конструктор <see cref="PauseManager"/>.
    /// </summary>
    /// <param name="messageManager">Менеджер сообщений, через который отображаются сообщения паузы.</param>
    public PauseManager(MessageManager messageManager, ProtocolController protocolController)
    {
      _messageManager = messageManager;
      _shouldShowPauseMessage = true;
      _shouldShowResumeMessage = false;
      _protocolController = protocolController;
    }

    /// <summary>
    /// Возвращает признак текущей паузы.
    /// </summary>
    public bool IsPaused => _isPaused;

    /// <summary>
    /// Переводит выполнение в режим паузы.
    /// </summary>
    public async Task PauseAsync()
    {
      if (!_isPaused)
      {
        LogInformation("Срабатывание паузы");
        _isPaused = true;
        _pauseCompletionSource = new TaskCompletionSource<bool>();
        _protocolController.ShowButtonsOnPause();
      }

      await WaitWhilePausedAsync();
    }

    /// <summary>
    /// Возобновляет выполнение после паузы.
    /// </summary>
    public void Resume()
    {
      LogInformation("Срабатывание возобновления после паузы");
      if (_isPaused && _pauseCompletionSource != null && !_pauseCompletionSource.Task.IsCompleted)
      {
        _pauseCompletionSource.SetResult(true);
      }

      _isPaused = false;
    }

    /// <summary>
    /// Ожидает, пока выполнение находится на паузе.
    /// </summary>
    /// <returns>Задача ожидания.</returns>
    public async Task WaitWhilePausedAsync()
    {
      if (_isPaused && _pauseCompletionSource != null && !_pauseCompletionSource.Task.IsCompleted)
      {
        LogInformation("Срабатывание ожидания при самоконтроле");

        if (_shouldShowPauseMessage)
        {
          _shouldShowPauseMessage = false;
          _shouldShowResumeMessage = true;

          await _messageManager.ShowMessageAsync(new ShowMessageModel
          {
            Header = "Выполнение поставлено на паузу!",
            CanBeDeleted = false
          });
        }

        await _pauseCompletionSource.Task;
        _shouldShowPauseMessage = true;
      }
      if (_shouldShowResumeMessage)
      {
        _shouldShowResumeMessage = false;

        await _messageManager.ShowMessageAsync(new ShowMessageModel
        {
          Header = "Выполнение снято с паузы!",
          CanBeDeleted = false
        });
      }
    }
  }
}

using System.Threading.Tasks;
using Utilities.Models;
using static Utilities.LoggerUtility;

namespace UI.Controls.ProtocolController
{
  /// <summary>
  /// Управляет паузой и возобновлением выполнения алгоритма.
  /// </summary>
  public class ProtocolPauseManager
  {
    private bool _isPaused;
    private bool _shouldShowPauseMessage = true;
    private bool _shouldShowResumeMessage = false;

    /// <summary>
    /// Точка ожидания при паузе.
    /// </summary>
    public TaskCompletionSource<bool> PauseCompletionSource { get; private set; }

    /// <summary>
    /// Возвращает true, если выполнение находится на паузе.
    /// </summary>
    public bool IsPaused => _isPaused;

    /// <summary>
    /// Устанавливает состояние паузы.
    /// </summary>
    public async Task PauseAsync(Protocol protocol)
    {
      if (!_isPaused)
      {
        LogInformation("Срабатывание паузы");
        _isPaused = true;
        PauseCompletionSource = new TaskCompletionSource<bool>();
        protocol?.ButtonManager.ShowButtonsOnPause();
      }

      await WaitWhilePausedAsync(protocol);
    }

    /// <summary>
    /// Снимает паузу и продолжает выполнение.
    /// </summary>
    public void Resume()
    {
      if (_isPaused)
      {
        _isPaused = false;
        PauseCompletionSource?.TrySetResult(true);
        LogInformation("Продолжение выполнения");
      }
    }

    /// <summary>
    /// Ожидает завершения паузы.
    /// </summary>
    public async Task WaitWhilePausedAsync(Protocol protocol = null)
    {
      if (_isPaused && PauseCompletionSource is { Task.IsCompleted: false })
      {
        LogInformation("Ожидание выхода из паузы...");

        if (protocol != null && _shouldShowPauseMessage)
        {
          _shouldShowPauseMessage = false;
          _shouldShowResumeMessage = true;

          await protocol.Message.AppendLineAsync(new ShowMessageModel
          {
            Header = "Выполнение поставлено на паузу!",
            CanBeDeleted = false
          });
        }

        await PauseCompletionSource.Task;
        _shouldShowPauseMessage = true;
      }

      if (protocol != null && _shouldShowResumeMessage)
      {
        _shouldShowResumeMessage = false;

        await protocol.Message.AppendLineAsync(new ShowMessageModel
        {
          Header = "Выполнение снято с паузы!",
          CanBeDeleted = false
        });
      }
    }

    /// <summary>
    /// Полностью сбрасывает состояние паузы.
    /// </summary>
    public void Reset()
    {
      _isPaused = false;
      _shouldShowPauseMessage = true;
      _shouldShowResumeMessage = false;
      PauseCompletionSource = null;
    }

    /// <summary>
    /// Гарантированно снимает состояние паузы и продолжает выполнение,
    /// используется при отмене задачи.
    /// </summary>
    public void ReleasePause()
    {
      if (_isPaused || (PauseCompletionSource is { Task.IsCompleted: false }))
      {
        LogInformation("Принудительное снятие паузы (отмена задачи).");
        _isPaused = false;
        PauseCompletionSource?.TrySetResult(true);
      }

      Reset();
    }
  }
}

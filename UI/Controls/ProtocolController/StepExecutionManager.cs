using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace UI.Controls.ProtocolController
{
  public enum StepExecutionMode
  {
    RunAll,
    StepInto,
    StepOver
  }

  public class StepExecutionManager
  {
    private TaskCompletionSource<bool> _stepTcs;
    private bool _insideBlock;
    private bool _continueBlock;

    public StepExecutionMode CurrentMode { get; private set; } = StepExecutionMode.RunAll;

    public ICommand F5Command { get; }
    public ICommand F10Command { get; }
    public ICommand F11Command { get; }

    public StepExecutionManager()
    {
      F5Command = new RelayCommand(() => SetMode(StepExecutionMode.RunAll));
      F10Command = new RelayCommand(() => StepOverBlock());
      F11Command = new RelayCommand(() => StepInto());
    }

    public void SetMode(StepExecutionMode mode)
    {
      CurrentMode = mode;
      ReleaseWaiting(); // Всегда снимаем ожидания при смене режима
    }

    public void StepInto()
    {
      SetMode(StepExecutionMode.StepInto);
      _stepTcs?.TrySetResult(true);
    }

    public void StepOverBlock()
    {
      SetMode(StepExecutionMode.StepOver);
      _continueBlock = true;
      _stepTcs?.TrySetResult(true);
    }

    public void EnterBlock()
    {
      _insideBlock = true;
      _continueBlock = false;
    }

    public async Task ExitBlockAsync()
    {
      _insideBlock = false;
      _continueBlock = false;

      if (CurrentMode == StepExecutionMode.StepOver)
      {
        _stepTcs = new TaskCompletionSource<bool>();
        await _stepTcs.Task;
      }
    }

    public async Task WaitIfStepModeAsync()
    {
      if (CurrentMode == StepExecutionMode.RunAll)
        return;

      if (CurrentMode == StepExecutionMode.StepInto)
      {
        _stepTcs = new TaskCompletionSource<bool>();
        await _stepTcs.Task;
      }
      else if (CurrentMode == StepExecutionMode.StepOver)
      {
        if (_insideBlock && _continueBlock)
          return;

        if (!_insideBlock)
        {
          _stepTcs = new TaskCompletionSource<bool>();
          await _stepTcs.Task;
        }
      }
    }

    /// <summary>
    /// Освобождает ожидание и снимает все активные паузы.
    /// </summary>
    public void ReleaseWaiting()
    {
      _stepTcs?.TrySetResult(true);
    }

    /// <summary>
    /// Сбрасывает внутреннее состояние при отмене задачи.
    /// </summary>
    public void Reset()
    {
      _insideBlock = false;
      _continueBlock = false;
      CurrentMode = StepExecutionMode.RunAll;
      ReleaseWaiting();
    }
  }
}

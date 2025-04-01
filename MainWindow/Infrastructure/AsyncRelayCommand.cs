using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace MainWindowProgram.Infrastructure
{
  /// <summary>
  /// Универсальная асинхронная команда с поддержкой параметров и без них.
  /// </summary>
  public class AsyncRelayCommand : ICommand
  {
    private readonly Func<Task>? _execute;
    private readonly Func<object?, Task>? _executeWithParam;
    private readonly Func<bool>? _canExecute;
    private readonly Predicate<object?>? _canExecuteWithParam;
    private bool _isExecuting;

    public event EventHandler? CanExecuteChanged;

    /// <summary>
    /// Команда без параметров.
    /// </summary>
    public AsyncRelayCommand(Func<Task> execute, Func<bool>? canExecute = null)
    {
      _execute = execute ?? throw new ArgumentNullException(nameof(execute));
      _canExecute = canExecute ?? (() => true);
    }

    /// <summary>
    /// Команда с параметром.
    /// </summary>
    public AsyncRelayCommand(Func<object?, Task> executeWithParam, Predicate<object?>? canExecuteWithParam = null)
    {
      _executeWithParam = executeWithParam ?? throw new ArgumentNullException(nameof(executeWithParam));
      _canExecuteWithParam = canExecuteWithParam;
    }

    public bool CanExecute(object? parameter)
    {
      if (_isExecuting)
        return false;

      if (_execute != null)
        return _canExecute?.Invoke() ?? true;

      if (_canExecuteWithParam != null)
        return _canExecuteWithParam(parameter);

      return true;
    }

    public async void Execute(object? parameter)
    {
      if (!CanExecute(parameter))
        return;

      _isExecuting = true;
      RaiseCanExecuteChanged();

      try
      {
        if (_execute != null)
          await _execute();
        else if (_executeWithParam != null)
          await _executeWithParam(parameter);
      }
      finally
      {
        _isExecuting = false;
        RaiseCanExecuteChanged();
      }
    }

    /// <summary>
    /// Принудительное обновление доступности команды.
    /// </summary>
    public void RaiseCanExecuteChanged() =>
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
  }
}

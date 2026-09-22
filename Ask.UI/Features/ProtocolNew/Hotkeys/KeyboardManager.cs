using Ask.Core.Services.App;
using Ask.Core.Services.EventCore.Adapters;
using Ask.UI.Infrastructure.UI.Overlay.Drawer.Runtime;
using System.Windows;
using System.Windows.Input;

namespace Ask.UI.Features.ProtocolNew.Hotkeys
{
  /// <summary>
  /// Менеджер для глобальной обработки клавиш, связанных с пошаговым режимом выполнения.
  /// Обрабатывает клавиши F5, F10, F11 и специальный F4,
  /// и позволяет асинхронно ожидать следующую команду пользователя.
  /// </summary>
  public static class KeyboardManager
  {
    /// <summary>
    /// Объект для управления задачей ожидания пользовательского действия.
    /// </summary>
    private static TaskCompletionSource<bool>? _tcs;
    private static int _hookUsers;

    /// <summary>
    /// Делегат, вызываемый при нажатии клавиши Enter (запуск).
    /// </summary>
    public static Action? OnStartPressed;

    /// <summary>
    /// Делегат, вызываемый при запуске выполнения клавишами пошагового режима F10 или F11.
    /// </summary>
    public static Action? OnStartPressedByStepMode;

    /// <summary>
    /// Единый обработчик F5 с учётом текущего состояния: запуск, пауза или продолжение.
    /// </summary>
    public static Action? OnRunOrPausePressed;

    /// <summary>
    /// Делегат, вызываемый при нажатии клавиши P (остановка или продолжение).
    /// </summary>
    public static Action? OnPausePressed;

    /// <summary>
    /// Делегат, вызываемый для продолжения приостановленного выполнения.
    /// </summary>
    public static Action? OnContinuePressed;

    /// <summary>
    /// Делегат, вызываемый при нажатии клавиши Escape (завершить).
    /// </summary>
    public static Action? OnExitPressed;

    /// <summary>
    /// Делегат, вызываемый для повтора текущей операции.
    /// </summary>
    public static Action? OnRepeatPressed;

    /// <summary>
    /// Регистрирует глобальный обработчик нажатий клавиш.
    /// Используется для отслеживания F5, F10, F11 и F4 во всех окнах приложения.
    /// </summary>
    public static void RegisterGlobalStepHooks()
    {
      if (_hookUsers++ == 0) InputManager.Current.PreProcessInput += OnGlobalKeyPressed;
    }

    /// <summary>
    /// Отменяет регистрацию глобального обработчика нажатий клавиш.
    /// </summary>
    public static void UnregisterGlobalStepHooks()
    {
      if (_hookUsers > 0 && --_hookUsers == 0) InputManager.Current.PreProcessInput -= OnGlobalKeyPressed;
    }

    /// <summary>
    /// Обработчик глобальных нажатий клавиш.
    /// Отслеживает F10 (Step Over), F11 (Step Into), F5 (Continue)
    /// и F4 для режима, активированного от брейкпоинта.
    /// </summary>
    /// <param name="sender">Источник события.</param>
    /// <param name="e">Аргументы события нажатия клавиши.</param>
    private static void OnGlobalKeyPressed(object sender, PreProcessInputEventArgs e)
    {
      var args = e.StagingItem.Input as KeyEventArgs;
      if (args == null || args.RoutedEvent != Keyboard.PreviewKeyDownEvent || args.Handled
        || args.IsRepeat || Keyboard.Modifiers != ModifierKeys.None) return;

      var key = args.Key == Key.System ? args.SystemKey : args.Key;
      if (DrawerHostService.Instance.ShouldBlockGlobalInput)
      {
        return;
      }

      if (StepControlManager.IsBreakpointStepModeActive && _tcs == null && Keyboard.Modifiers == ModifierKeys.None)
      {
        switch (key)
        {
          case Key.F5:
            ExecutionEventAdapter.ExecutionControlEventAdapter.Raise(Ask.Core.Shared.Metadata.Enums.HotkeysEnums.ExecutionControlButton.Run);
            args.Handled = true;
            return;
          case Key.F10:
            ExecutionEventAdapter.ExecutionControlEventAdapter.Raise(Ask.Core.Shared.Metadata.Enums.HotkeysEnums.ExecutionControlButton.StepOver);
            args.Handled = true;
            return;
          case Key.F11:
            ExecutionEventAdapter.ExecutionControlEventAdapter.Raise(Ask.Core.Shared.Metadata.Enums.HotkeysEnums.ExecutionControlButton.StepInto);
            args.Handled = true;
            return;
        }
      }

      if (key == Key.F4 && Keyboard.Modifiers == ModifierKeys.None && TryHandleBreakpointF4())
      {
        args.Handled = true;
        return;
      }

      if (_tcs == null) return;
      if (!StepControlManager.StepMode) return;

      switch (key)
      {
        case Key.F10:
          StepControlManager.RequestStepOverUntilNextControlCommand();
          _tcs.TrySetResult(true);
          args.Handled = true;
          MessageEventAdapter.RaiseInfoMessage("Нажата клавиша: F10", true);
          Application.Current.Dispatcher.InvokeAsync(() =>
          {
            var win = Application.Current.MainWindow;
            win?.Focus();
            Keyboard.Focus(win);
          });
          break;

        case Key.F11:
          StepControlManager.SetStepIntoMode();
          _tcs.TrySetResult(true);
          args.Handled = true;
          MessageEventAdapter.RaiseInfoMessage("Нажата клавиша: F11", true);
          break;

        case Key.F5:
          StepControlManager.DisableStepMode();
          if (_tcs != null && !_tcs.Task.IsCompleted)
          {
            _tcs.TrySetResult(true);
          }
          // Синхронизируем UI с действием "Продолжить":
          // убираем шаговые кнопки и возвращаем "Пауза / Завершить".
          args.Handled = true;
          MessageEventAdapter.RaiseInfoMessage("Нажата клавиша: F5", true);
          break;

        default:
          break;
      }
    }

    /// <summary>
    /// Обрабатывает F4 только для пошагового режима, включенного из BreakpointHandler.
    /// </summary>
    /// <returns><c>true</c>, если F4 был обработан.</returns>
    internal static bool TryHandleBreakpointF4()
    {
      if (!StepControlManager.IsBreakpointStepModeActive || StepControlManager.BreakpointCommandInfo == null)
      {
        return false;
      }

      var command = StepControlManager.BreakpointCommandInfo;
      var caption = $"{command.CommandNumber} {command.Mnemonic}".Trim();
      var body = string.IsNullOrWhiteSpace(command.CommandBody)
        ? "<пусто>"
        : command.CommandBody;

      ExecutionEventAdapter.RaiseBreakpointF4Pressed(command);
      MessageEventAdapter.RaiseInfoMessage(
        $"Нажата клавиша: F4 на команде {caption}. Тело команды: {body}",
        true);

      return true;
    }

    /// <summary>
    /// Ожидает нажатие одной из клавиш управления пошаговым выполнением.
    /// Может быть отменено с помощью переданного CancellationToken.
    /// </summary>
    /// <param name="cancellationToken">Токен отмены ожидания.</param>
    public static async Task WaitForNextStepKeyAsync(CancellationToken cancellationToken)
    {
      cancellationToken.ThrowIfCancellationRequested();
      var completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
      _tcs = completion;

      using (cancellationToken.Register(() => completion.TrySetCanceled(cancellationToken)))
      {
        try
        {
          await completion.Task;
          cancellationToken.ThrowIfCancellationRequested();
        }
        catch (TaskCanceledException)
        {
          throw new OperationCanceledException("Ожидание пошаговой команды было прервано.", cancellationToken);
        }
        finally
        {
          Interlocked.CompareExchange(ref _tcs, null, completion);
        }
      }
    }

    /// <summary>
    /// Принудительно завершает ожидание следующего шага.
    /// Используется, например, для внешнего управления пошаговым режимом.
    /// </summary>
    public static void TriggerStep() => _tcs?.TrySetResult(true);
  }

}

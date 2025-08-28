using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using static Utilities.LoggerUtility;

namespace UI.Controls.ProtocolNew
{
  /// <summary>
  /// Менеджер для глобальной обработки клавиш, связанных с пошаговым режимом выполнения.
  /// Обрабатывает клавиши F5, F10 и F11, и позволяет асинхронно ожидать следующую команду пользователя.
  /// </summary>
  public static class KeyboardManager
  {
    /// <summary>
    /// Объект для управления задачей ожидания пользовательского действия.
    /// </summary>
    private static TaskCompletionSource<bool>? _tcs;

    /// <summary>
    /// Делегат, вызываемый при нажатии клавиши Enter (запуск).
    /// </summary>
    public static Action? OnStartPressed;

    public static Action? OnStartPressedByStepMode;

    /// <summary>
    /// Делегат, вызываемый при нажатии клавиши P (остановка или продолжение).
    /// </summary>
    public static Action? OnPausePressed;
    public static Action? OnContinuePressed;

    /// <summary>
    /// Делегат, вызываемый при нажатии клавиши Escape (завершить).
    /// </summary>
    public static Action? OnExitPressed;
    public static Action? OnRepeatPressed;


    /// <summary>
    /// Регистрирует глобальный обработчик нажатий клавиш.
    /// Используется для отслеживания F5, F10, F11 во всех окнах приложения.
    /// </summary>
    public static void RegisterGlobalStepHooks()
    {
      InputManager.Current.PreProcessInput += OnGlobalKeyPressed;
    }

    /// <summary>
    /// Отменяет регистрацию глобального обработчика нажатий клавиш.
    /// </summary>
    public static void UnregisterGlobalStepHooks()
    {
      InputManager.Current.PreProcessInput -= OnGlobalKeyPressed;
    }

    /// <summary>
    /// Обработчик глобальных нажатий клавиш.
    /// Отслеживает F10 (Step Over), F11 (Step Into) и F5 (Continue).
    /// </summary>
    /// <param name="sender">Источник события.</param>
    /// <param name="e">Аргументы события нажатия клавиши.</param>
    private static void OnGlobalKeyPressed(object sender, PreProcessInputEventArgs e)
    {
      if (_tcs == null) return;

      var args = e.StagingItem.Input as KeyEventArgs;
      if (args == null || args.RoutedEvent != Keyboard.KeyDownEvent) return;

      var key = args.Key == Key.System ? args.SystemKey : args.Key;
      
      LogInformation($"[KEYBOARD] Detected key: {key}");

      switch (key)
      {
        case Key.F10:
          StepControlManager.IsStepInto = false;
          _tcs.TrySetResult(true);
          args.Handled = true;
          AppConfiguration.Base.EventAggregator.RaiseInfoMessage("Нажата клавиша: F10", true);
          Application.Current.Dispatcher.InvokeAsync(() =>
          {
            var win = Application.Current.MainWindow;
            win?.Focus();
            Keyboard.Focus(win);
          });
          break;

        case Key.F11:
          StepControlManager.IsStepInto = true;
          _tcs.TrySetResult(true);
          AppConfiguration.Base.EventAggregator.RaiseInfoMessage("Нажата клавиша: F11", true);
          break;

        case Key.F5:
          StepControlManager.DisableStepMode();
          LogInformation("[KEYBOARD] Step mode DISABLED via F5");
          if (_tcs != null && !_tcs.Task.IsCompleted)
          {
            _tcs.TrySetResult(true);
          }
          args.Handled = true;
          AppConfiguration.Base.EventAggregator.RaiseInfoMessage("Нажата клавиша: F5", true);
          break;

        default:
          break;
      }
    }

    /// <summary>
    /// Ожидает нажатие одной из клавиш управления пошаговым выполнением.
    /// Может быть отменено с помощью переданного CancellationToken.
    /// </summary>
    /// <param name="cancellationToken">Токен отмены ожидания.</param>
    public static async Task WaitForNextStepKeyAsync(CancellationToken cancellationToken)
    {
      // Выводим постоянное сообщение об ожидании
      AppConfiguration.Base.EventAggregator.RaiseWarningMessage("Ожидание нажатия F5, F10 или F11...");

      _tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

      using (cancellationToken.Register(() => _tcs.TrySetCanceled(cancellationToken)))
      {
        try
        {
          await _tcs.Task;
        }
        finally
        {
          _tcs = null;
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

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
  public static class KeyboardManager
  {
    private static TaskCompletionSource<bool> _tcs;

    public static void RegisterGlobalStepHooks()
    {
      InputManager.Current.PreProcessInput += OnGlobalKeyPressed;
    }

    public static void UnregisterGlobalStepHooks()
    {
      InputManager.Current.PreProcessInput -= OnGlobalKeyPressed;
    }

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
          break;

        case Key.F5:
          StepControlManager.DisableStepMode();
          LogInformation("[KEYBOARD] Step mode DISABLED via F5");
          if (_tcs != null && !_tcs.Task.IsCompleted)
          {
            _tcs.TrySetResult(true);
          }
          args.Handled = true;
          break;

        default:
          break;
      }
    }

    public static async Task WaitForNextStepKeyAsync()
    {
      _tcs = new TaskCompletionSource<bool>();
      await _tcs.Task;
      _tcs = null;
    }
    public static void TriggerStep() => _tcs?.TrySetResult(true);
  }

}

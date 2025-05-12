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
    public static async Task WaitForNextStepKeyAsync()
    {
      try
      {
        _tcs = new TaskCompletionSource<bool>();

        KeyEventHandler handler = null;
        handler = (s, e) =>
        {
          if (e.Key == Key.F10)
          {
            StepControlManager.IsStepInto = false;
            _tcs.TrySetResult(true);
          }
          else if (e.Key == Key.F11)
          {
            StepControlManager.IsStepInto = true;
            _tcs.TrySetResult(true);
          }
        };

        Window mainWindow = await Application.Current.Dispatcher.InvokeAsync(() => Application.Current.MainWindow);
        mainWindow.PreviewKeyDown += handler;

        await _tcs.Task;

        mainWindow.PreviewKeyDown -= handler;
      }
      catch (Exception ex)
      {
        LogException(ex);
      }
    }

    public static void TriggerStep()
    {
      _tcs?.TrySetResult(true);
    }
  }
}

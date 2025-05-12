using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace UI.Controls.ProtocolNew
{
  public static class KeyboardManager
  {
    private static readonly TaskCompletionSource<bool> _stepSource = new();

    public static async Task WaitForNextStepKeyAsync()
    {
      var tcs = new TaskCompletionSource<bool>();

      KeyEventHandler handler = null;
      handler = (s, e) =>
      {
        if (e.Key == Key.F10)
        {
          StepControlManager.IsStepInto = false;
          StepControlManager.StepMode = true;
          tcs.TrySetResult(true);
        }
        else if (e.Key == Key.F11)
        {
          StepControlManager.IsStepInto = true;
          StepControlManager.StepMode = true;
          tcs.TrySetResult(true);
        }
        else if (e.Key == Key.Escape || e.Key == Key.Enter)
        {
          StepControlManager.StepMode = false;
          tcs.TrySetResult(true);
        }
      };

      Application.Current.MainWindow.PreviewKeyDown += handler;

      await tcs.Task;

      Application.Current.MainWindow.PreviewKeyDown -= handler;
    }
  }

}

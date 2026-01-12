using ConsoleUI.ConsoleCommanding.Core;
using ConsoleUI.ConsoleUI;
using System.Windows;

namespace ConsoleUI.ConsoleCommanding.Commands
{
  public class SplitLogsCommand : ICommand
  {
    public string Name => "logs-split";

    public async Task ExecuteAsync(string[] args, CommandContext context)
    {
      await Application.Current.Dispatcher.InvokeAsync(() =>
      {
        var wa = SystemParameters.WorkArea;
        double gap = 8.0;

        var existing = Application.Current.Windows
          .OfType<ConsoleOverlay>()
          .FirstOrDefault();

        var split = new ConsoleOverlay(startSplit: true)
        {
          WindowStartupLocation = WindowStartupLocation.Manual
        };
        split.Show();
        double desiredLeft = existing != null
          ? existing.Left + existing.Width + gap
          : wa.Left + 100.0;
        double maxLeft = wa.Right - split.ActualWidth;
        split.Left = Math.Min(desiredLeft, maxLeft);
        split.Top = existing?.Top ?? (wa.Top + 100.0);

        if (split.Left >= maxLeft - 1 && existing != null)
        {
          double desiredTop = existing.Top + existing.ActualHeight + gap;
          if (desiredTop + split.ActualHeight <= wa.Bottom)
          {
            split.Left = existing.Left;
            split.Top = desiredTop;
          }
        }
      });
    }
  }
}

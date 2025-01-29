using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using static Utilities.LoggerUtility;

namespace MainWindowProgram
{
  internal class MessageHandler
  {
    private TextBlock _infoBlock;
    private static System.Timers.Timer timer = new System.Timers.Timer();

    public MessageHandler(TextBlock infoBlock)
    {
      _infoBlock = infoBlock;
      timer.AutoReset = true;
      timer.Elapsed += Timer_Elapsed;
    }

    public void SetErrorMessage(string message, bool clearMessage = false)
    {
      SetMessage(message, Brushes.Red, clearMessage);
      LogError($"Ошибка: {message}");
    }

    public void SetWarningMessage(string message, bool clearMessage = false)
    {
      SetMessage(message, Brushes.Yellow, clearMessage);
      LogWarning($"Предупреждение: {message}");
    }

    public void SetInfoMessage(string message, bool clearMessage = false)
    {
      SetMessage(message, (SolidColorBrush)Application.Current.Resources["ForegroundSolidColorBrush"], clearMessage);
      LogInformation($"Информация: {message}");
    }

    private void SetMessage(string message, Brush color, bool clearMessage)
    {
      if (_infoBlock != null)
      {
        timer.Stop();
        _infoBlock.Text = message;
        _infoBlock.Foreground = color;
        if (clearMessage)
        {
          timer.Start();
        }
      }
    }

    private async void Timer_Elapsed(object sender, ElapsedEventArgs e)
    {
      await Task.Delay(2000);
      await Application.Current.Dispatcher.InvokeAsync(async () =>
      {
        if (!string.IsNullOrEmpty(_infoBlock.Text))
        {
          timer.Stop();
          while (_infoBlock.Opacity > 0)
          {
            _infoBlock.Opacity -= 0.1;
            await Task.Delay(5);
          }
          _infoBlock.Text = string.Empty;
          await Task.Delay(10);
          _infoBlock.Opacity = 1;
        }
      });
    }
  }
}

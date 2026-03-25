using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using static Ask.LogLib.LoggerUtility;

namespace MainWindowProgram.Engine
{
  /// <summary>
  /// Класс MessageHandler предназначен для отображения сообщений в элементе TextBlock с использованием таймера.
  /// Предоставляет методы для установки сообщений об ошибке, предупреждении и информационные сообщения с соответствующей цветовой схемой.
  /// При необходимости сообщение может быть автоматически очищено с эффектом плавного исчезновения.
  /// </summary>
  internal class MessageHandler
  {
    static private TextBlock _infoBlock;
    private static System.Timers.Timer timer = new System.Timers.Timer();

    /// <summary>
    /// Инициализирует новый экземпляр MessageHandler с указанным элементом TextBlock.
    /// </summary>
    /// <param name="infoBlock">Элемент TextBlock для отображения сообщений.</param>
    public MessageHandler(TextBlock infoBlock)
    {
      _infoBlock = infoBlock;
      timer.AutoReset = true;
      timer.Elapsed += Timer_Elapsed;
    }

    /// <summary>
    /// Устанавливает сообщение об ошибке красным цветом и регистрирует сообщение в логах.
    /// </summary>
    /// <param name="message">Текст сообщения об ошибке.</param>
    /// <param name="clearMessage">Если true, сообщение будет автоматически очищено.</param>
    public void SetErrorMessage(string message, bool clearMessage = false)
    {
      SetMessage(message, () => Brushes.Red, clearMessage);
      LogError($"Ошибка: {message}");
    }

    /// <summary>
    /// Устанавливает предупреждающее сообщение желтым цветом и регистрирует сообщение в логах.
    /// </summary>
    /// <param name="message">Текст предупреждения.</param>
    /// <param name="clearMessage">Если true, сообщение будет автоматически очищено.</param>
    public void SetWarningMessage(string message, bool clearMessage = false)
    {
      SetMessage(message, () => Brushes.Yellow, clearMessage);
      LogWarning($"Предупреждение: {message}");
    }

    /// <summary>
    /// Устанавливает информационное сообщение с использованием стандартного цвета текста и регистрирует его в логах.
    /// </summary>
    /// <param name="message">Текст информационного сообщения.</param>
    /// <param name="clearMessage">Если true, сообщение будет автоматически очищено.</param>
    public void SetInfoMessage(string message, bool clearMessage = false)
    {
      SetMessage(message, () =>
      {
        if (Application.Current?.Resources["MainTitleSolidColorBrush"] is Brush brush)
          return brush;

        return Brushes.White;
      }, clearMessage);
      LogInformation($"Информация: {message}");
    }

    /// <summary>
    /// Отчищает информационное сообщение.
    /// </summary>
    /// <param name="message">Текст информационного сообщения.</param>
    /// <param name="clearMessage">Если true, сообщение будет автоматически очищено.</param>
    public void ClearMessage()
    {
      SetMessage(string.Empty, () =>
      {
        if (Application.Current?.Resources["MainTitleSolidColorBrush"] is Brush brush)
          return brush;

        return Brushes.White;
      }, false);
    }

    /// <summary>
    /// Устанавливает сообщение в TextBlock с заданным цветом. 
    /// Если параметр clearMessage равен true, запускается таймер для автоматического очищения сообщения.
    /// </summary>
    /// <param name="message">Текст сообщения.</param>
    /// <param name="color">Цвет текста сообщения.</param>
    /// <param name="clearMessage">Если true, сообщение будет автоматически очищено.</param>
    private void SetMessage(string message, Func<Brush> colorFactory, bool clearMessage)
    {
      if (_infoBlock == null)
      {
        return;
      }

      var dispatcher = Application.Current?.Dispatcher ?? _infoBlock.Dispatcher;
      if (dispatcher == null)
        return;

      if (dispatcher.CheckAccess())
      {
        ApplyMessage(message, colorFactory(), clearMessage);
        return;
      }

      _ = dispatcher.InvokeAsync(() => ApplyMessage(message, colorFactory(), clearMessage));
    }

    private void ApplyMessage(string message, Brush color, bool clearMessage)
    {
      timer.Stop();
      _infoBlock.Text = message;
      _infoBlock.Foreground = color;
      if (clearMessage)
      {
        timer.Start();
      }
    }

    /// <summary>
    /// Обработчик события таймера, который через заданную задержку плавно уменьшает непрозрачность TextBlock,
    /// очищает текст и восстанавливает первоначальную непрозрачность.
    /// </summary>
    private async void Timer_Elapsed(object sender, ElapsedEventArgs e)
    {
      await Task.Delay(2000);
      try
      {
        if (Application.Current != null)
        {
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
      catch (Exception)
      {

      }
    }
  }
}

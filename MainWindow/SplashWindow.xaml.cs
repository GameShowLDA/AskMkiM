using System.Windows;
using System.Windows.Media.Animation;

namespace MainWindowProgram
{
  /// <summary>
  /// Окно заставки (SplashScreen), отображаемое при запуске приложения.
  /// </summary>
  public partial class SplashWindow : Window
  {
    /// <summary>
    /// Инициализирует новый экземпляр окна заставки.
    /// </summary>
    public SplashWindow()
    {
      InitializeComponent();
    }

    public void SetStatus(string message)
    {
      StatusTextBlock.Text = string.IsNullOrWhiteSpace(message)
        ? "Подготовка приложения..."
        : message;
    }

    /// <summary>
    /// Асинхронно выполняет плавное закрытие окна с анимацией исчезновения.
    /// </summary>
    /// <remarks>
    /// Метод запускает анимацию изменения прозрачности окна от 1 до 0
    /// и закрывает окно сразу после завершения анимации.  
    /// Возвращаемая задача завершается, когда окно полностью закрыто.
    /// </remarks>
    public async Task WaitForCloseAsync()
    {
      var tcs = new TaskCompletionSource<bool>();

      await Dispatcher.InvokeAsync(() =>
      {
        var fadeOut = new DoubleAnimation
        {
          From = 1,
          To = 0,
          Duration = TimeSpan.FromSeconds(1),
          FillBehavior = FillBehavior.Stop
        };

        fadeOut.Completed += (_, _) =>
        {
          Close();
          tcs.TrySetResult(true);
        };

        BeginAnimation(OpacityProperty, fadeOut);
      });

      await tcs.Task;
    }
  }
}

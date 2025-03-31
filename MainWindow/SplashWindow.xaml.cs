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

    /// <summary>
    /// Асинхронно выполняет плавное закрытие окна с анимацией исчезновения.
    /// </summary>
    /// <returns>Задача, представляющая процесс закрытия окна.</returns>
    public async Task WaitForCloseAsync()
    {
      await Dispatcher.InvokeAsync(async () =>
      {
        var fadeOut = new DoubleAnimation(1, 0, new Duration(TimeSpan.FromSeconds(1)));
        this.BeginAnimation(Window.OpacityProperty, fadeOut);
        await Task.Delay(1000); // Ждём, пока анимация завершится
        this.Close();
      });

      await Task.Delay(1000);
    }
  }
}

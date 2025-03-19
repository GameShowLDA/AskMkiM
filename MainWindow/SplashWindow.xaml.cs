using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Animation;

namespace MainWindowProgram
{
  public partial class SplashWindow : Window
  {
    public SplashWindow()
    {
      InitializeComponent();
    }

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

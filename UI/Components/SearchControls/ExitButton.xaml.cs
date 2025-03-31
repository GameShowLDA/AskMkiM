using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace UI.Components.SearchControls
{
  /// <summary>
  /// Логика взаимодействия для ExitButton.xaml.
  /// </summary>
  public partial class ExitButton : UserControl
  {
    /// <summary>
    /// Инициализирует новый экземпляр <see cref="ExitButton"/> и настраивает внешний вид кнопки.
    /// Устанавливает прозрачный фон для элемента кнопки.
    /// </summary>
    public ExitButton()
    {
      InitializeComponent();
      BackgroundBorder.Background = Brushes.Transparent;
    }

    private void BackgroundBorder_MouseEnter(object sender, MouseEventArgs e)
    {
      BackgroundBorder.Background = Brushes.Red;
    }

    private void BackgroundBorder_MouseLeave(object sender, MouseEventArgs e)
    {
      BackgroundBorder.Background = Brushes.Transparent;
    }
  }
}

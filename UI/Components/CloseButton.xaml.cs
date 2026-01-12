using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace UI.Components
{
  /// <summary>
  /// Логика взаимодействия для CloseButton.xaml.
  /// </summary>
  public partial class CloseButton : UserControl
  {
    /// <summary>
    /// Инициализирует новый экземпляр <see cref="CloseButton"/>.
    /// </summary>
    /// <remarks>
    /// Конструктор инициализирует компонент кнопки и устанавливает фон области вокруг кнопки как прозрачный,
    /// используя свойство <see cref="BackgroundBorder"/>.
    /// </remarks>
    public CloseButton()
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

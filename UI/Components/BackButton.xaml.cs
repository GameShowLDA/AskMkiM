using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace UI.Components
{
  /// <summary>
  /// Логика взаимодействия для BackButton.xaml.
  /// </summary>
  public partial class BackButton : UserControl
  {
    /// <summary>
    /// Инициализирует новый экземпляр <see cref="BackButton"/>.
    /// </summary>
    /// <remarks>
    /// Конструктор инициализирует компонент кнопки и устанавливает её цвет переднего фона, 
    /// используя значение из ресурсов приложения для параметра "BackButtonForegroundColor".
    /// </remarks>
    public BackButton()
    {
      InitializeComponent();
      var primaryColorBrush = (SolidColorBrush)Application.Current.Resources["BackButtonForegroundColor"];
      this.Foreground = primaryColorBrush;
    }

    /// <summary>
    /// Получает или устанавливает цвет переднего фона кнопки.
    /// </summary>
    /// <remarks>
    /// Это свойство позволяет изменять или получать текущий цвет переднего фона кнопки.
    /// Значение по умолчанию — <see cref="Brushes.Wheat"/>.
    /// </remarks>
    public new Brush Foreground
    {
      get { return (Brush)GetValue(ForegroundProperty); }
      set { SetValue(ForegroundProperty, value); }
    }

    /// <summary>
    /// Зависимое свойство для <see cref="Foreground"/>. 
    /// Это свойство управляет цветом текста (переднего фона) на кнопке.
    /// </summary>
    public static readonly new DependencyProperty ForegroundProperty =
        DependencyProperty.Register("Foreground", typeof(Brush), typeof(BackButton), new PropertyMetadata(Brushes.Wheat));
  }
}

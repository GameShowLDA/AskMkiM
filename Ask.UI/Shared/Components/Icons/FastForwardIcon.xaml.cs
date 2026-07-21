using System.Windows;
using System.Windows.Controls;

namespace Ask.UI.Shared.Components.Icons
{
  /// <summary>
  /// Отображает значок перехода к другой команде.
  /// </summary>
  public partial class FastForwardIcon : UserControl
  {
    /// <summary>
    /// Свойство зависимости для размера значка.
    /// </summary>
    public static readonly DependencyProperty SizeProperty =
      DependencyProperty.Register(
        nameof(Size),
        typeof(double),
        typeof(FastForwardIcon),
        new PropertyMetadata(24.0, OnSizeChanged));

    /// <summary>
    /// Размер значка.
    /// </summary>
    public double Size
    {
      get => (double)GetValue(SizeProperty);
      set => SetValue(SizeProperty, value);
    }

    /// <summary>
    /// Инициализирует значок перехода к другой команде.
    /// </summary>
    public FastForwardIcon()
    {
      InitializeComponent();
    }

    /// <summary>
    /// Применяет изменённый размер к элементу управления.
    /// </summary>
    /// <param name="dependencyObject">Элемент управления значком.</param>
    /// <param name="eventArgs">Данные об изменении свойства.</param>
    private static void OnSizeChanged(
      DependencyObject dependencyObject,
      DependencyPropertyChangedEventArgs eventArgs)
    {
      if (dependencyObject is FastForwardIcon icon)
      {
        icon.Width = icon.Size;
        icon.Height = icon.Size;
      }
    }
  }
}

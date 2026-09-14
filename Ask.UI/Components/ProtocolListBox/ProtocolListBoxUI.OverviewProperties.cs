using System.Windows;
using System.Windows.Controls;

namespace Ask.UI.Components.ProtocolListBox
{
  public partial class ProtocolListBoxUI
  {
    /// <summary>
    /// Идентификатор свойства <see cref="OverviewHostStyle"/>.
    /// </summary>
    public static readonly DependencyProperty OverviewHostStyleProperty = DependencyProperty.Register(
      nameof(OverviewHostStyle), typeof(Style), typeof(ProtocolListBoxUI), new PropertyMetadata(null));

    /// <summary>
    /// Стиль внешней рамки полосы обзора; null выбирает стандартное оформление.
    /// </summary>
    public Style? OverviewHostStyle
    {
      get => (Style?)GetValue(OverviewHostStyleProperty);
      set => SetValue(OverviewHostStyleProperty, value);
    }

    /// <summary>
    /// Идентификатор свойства <see cref="OverviewBarStyle"/>.
    /// </summary>
    public static readonly DependencyProperty OverviewBarStyleProperty = DependencyProperty.Register(
      nameof(OverviewBarStyle), typeof(Style), typeof(ProtocolListBoxUI), new PropertyMetadata(null));

    /// <summary>
    /// Стиль полосы обзора команд и ошибок.
    /// </summary>
    public Style? OverviewBarStyle
    {
      get => (Style?)GetValue(OverviewBarStyleProperty);
      set => SetValue(OverviewBarStyleProperty, value);
    }

    /// <summary>
    /// Идентификатор свойства <see cref="OverviewVisibility"/>.
    /// </summary>
    public static readonly DependencyProperty OverviewVisibilityProperty = DependencyProperty.Register(
      nameof(OverviewVisibility), typeof(Visibility), typeof(ProtocolListBoxUI), new PropertyMetadata(Visibility.Visible));

    /// <summary>
    /// Видимость полосы обзора; Collapsed освобождает её место.
    /// </summary>
    public Visibility OverviewVisibility
    {
      get => (Visibility)GetValue(OverviewVisibilityProperty);
      set => SetValue(OverviewVisibilityProperty, value);
    }

    /// <summary>
    /// Идентификатор свойства <see cref="ProtocolVerticalScrollBarVisibility"/>.
    /// </summary>
    public static readonly DependencyProperty ProtocolVerticalScrollBarVisibilityProperty = DependencyProperty.Register(
      nameof(ProtocolVerticalScrollBarVisibility), typeof(ScrollBarVisibility), typeof(ProtocolListBoxUI), new PropertyMetadata(ScrollBarVisibility.Auto));

    /// <summary>
    /// Видимость штатной вертикальной прокрутки протокола; Hidden сохраняет прокрутку.
    /// </summary>
    public ScrollBarVisibility ProtocolVerticalScrollBarVisibility
    {
      get => (ScrollBarVisibility)GetValue(ProtocolVerticalScrollBarVisibilityProperty);
      set => SetValue(ProtocolVerticalScrollBarVisibilityProperty, value);
    }

  }
}

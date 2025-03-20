using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace UI.Components
{
  /// <summary>
  /// Поле ввода с встроенным Placeholder.
  /// </summary>
  public partial class TextBoxPlaceholder : UserControl
  {
    /// <summary>
    /// Свойство зависимости для текста Placeholder.
    /// </summary>
    public static readonly DependencyProperty PlaceholderProperty =
        DependencyProperty.Register(nameof(Placeholder), typeof(string), typeof(TextBoxPlaceholder),
            new PropertyMetadata("Введите текст...", OnPlaceholderChanged));

    /// <summary>
    /// Свойство зависимости для введенного текста.
    /// </summary>
    public static readonly DependencyProperty TextProperty =
        DependencyProperty.Register(nameof(Text), typeof(string), typeof(TextBoxPlaceholder),
            new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnTextChanged));

    /// <summary>
    /// Placeholder, который отображается в поле ввода.
    /// </summary>
    public string Placeholder
    {
      get => (string)GetValue(PlaceholderProperty);
      set => SetValue(PlaceholderProperty, value);
    }

    /// <summary>
    /// Текст, введённый пользователем.
    /// </summary>
    public string Text
    {
      get => (string)GetValue(TextProperty);
      set => SetValue(TextProperty, value);
    }

    /// <summary>
    /// Конструктор.
    /// </summary>
    public TextBoxPlaceholder()
    {
      InitializeComponent();
      Loaded += TextBoxPlaceholder_Loaded;
    }

    /// <summary>
    /// Устанавливаем Placeholder при загрузке.
    /// </summary>
    private void TextBoxPlaceholder_Loaded(object sender, RoutedEventArgs e)
    {
      UpdatePlaceholder();
    }

    /// <summary>
    /// Обновление Placeholder при изменении свойства.
    /// </summary>
    private static void OnPlaceholderChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
      if (d is TextBoxPlaceholder control)
      {
        control.UpdatePlaceholder();
      }
    }

    /// <summary>
    /// Реагирует на изменения текста и скрывает или показывает Placeholder.
    /// </summary>
    private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
      if (d is TextBoxPlaceholder control)
      {
        control.UpdatePlaceholder();
      }
    }

    /// <summary>
    /// Когда поле получает фокус, скрываем Placeholder, если он там.
    /// </summary>
    private void InputBox_GotFocus(object sender, RoutedEventArgs e)
    {
      if (InputBox.Text == Placeholder)
      {
        InputBox.Text = "";
        InputBox.Foreground = Brushes.Black;
      }
    }

    /// <summary>
    /// Когда поле теряет фокус, показываем Placeholder, если поле пустое.
    /// </summary>
    private void InputBox_LostFocus(object sender, RoutedEventArgs e)
    {
      UpdatePlaceholder();
    }

    /// <summary>
    /// Обновление Placeholder: скрыть или показать в зависимости от состояния текста.
    /// </summary>
    private void UpdatePlaceholder()
    {
      if (string.IsNullOrEmpty(InputBox.Text))
      {
        InputBox.Text = Placeholder;
        InputBox.Foreground = Brushes.Gray;
      }
      else
      {
        InputBox.Foreground = Brushes.Black;
      }
    }
  }
}

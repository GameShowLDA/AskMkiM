using System.Printing;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Utilities.Models;

namespace UI.Components
{
  /// <summary>
  /// TextBox с Placeholder внутри самого поля.
  /// </summary>
  public partial class TextBoxPlaceholder : UserControl
  {
    /// <summary>
    /// Свойство зависимости для текста placeholder'а, отображаемого внутри поля ввода.
    /// </summary>
    public static readonly DependencyProperty PlaceholderProperty =
        DependencyProperty.Register(
            nameof(Placeholder),
            typeof(string),
            typeof(TextBoxPlaceholder),
            new PropertyMetadata("Введите текст..."));

    /// <summary>
    /// Свойство зависимости для текста, введённого пользователем.
    /// Поддерживает двустороннюю привязку.
    /// </summary>
    public static readonly DependencyProperty TextProperty =
        DependencyProperty.Register(
            nameof(Text),
            typeof(string),
            typeof(TextBoxPlaceholder),
            new FrameworkPropertyMetadata(
                string.Empty,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    /// <summary>
    /// Текст, отображаемый как Placeholder.
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
    /// Свойство зависимости для включения/отключения проверки на числовой ввод.
    /// </summary>
    public static readonly DependencyProperty IsNumberInputEnabledProperty =
        DependencyProperty.Register(nameof(IsNumberInputEnabled), typeof(bool), typeof(TextBoxPlaceholder),
            new PropertyMetadata(true));

    /// <summary>
    /// Включает или отключает проверку на числовой ввод.
    /// По умолчанию: true (только цифры).
    /// </summary>
    public bool IsNumberInputEnabled
    {
      get => (bool)GetValue(IsNumberInputEnabledProperty);
      set => SetValue(IsNumberInputEnabledProperty, value);
    }

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="TextBoxPlaceholder"/>.
    /// </summary>
    public TextBoxPlaceholder()
    {
      InitializeComponent();
      Loaded += (_, _) => InitPlaceholder();
    }

    private void InitPlaceholder()
    {
      if (string.IsNullOrWhiteSpace(Text))
      {
        InputBox.Text = Placeholder;
        InputBox.Foreground = (Brush)FindResource("ForegroundSolidColorBrush");
      }
      else
      {
        InputBox.Foreground = (Brush)FindResource("ForegroundSolidColorBrush");
      }
    }

    private void InputBox_GotFocus(object sender, RoutedEventArgs e)
    {
      if (InputBox.Text == Placeholder)
      {
        InputBox.Text = "";
      }

      BorderData.Background = (Brush)FindResource("LightPrimarySolidColorBrush");
    }

    private void InputBox_LostFocus(object sender, RoutedEventArgs e)
    {
      if (string.IsNullOrWhiteSpace(InputBox.Text) || InputBox.Text == Placeholder)
      {
        InputBox.Text = Placeholder;
        InputBox.Foreground = (Brush)FindResource("ForegroundSolidColorBrush");
      }
      else
      {
        Text = InputBox.Text;
      }
    }

    /// <summary>
    /// Ограничивает ввод только числовыми значениями и заменяет запятую на точку.
    /// Не позволяет вводить две точки подряд.
    /// </summary>
    /// <param name="sender">Источник события.</param>
    /// <param name="e">Аргументы события ввода текста.</param>
    private void InputBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
      if(BorderData.Background != (Brush)FindResource("LightPrimarySolidColorBrush"))
      {
        BorderData.Background = (Brush)FindResource("LightPrimarySolidColorBrush");
      }

      if (!IsNumberInputEnabled)
      {
        e.Handled = false;
        return;
      }

      string currentText = InputBox.Text;
      int caretPos = InputBox.CaretIndex;

      // Если вводится запятая – заменяем на точку
      if (e.Text == ",")
      {
        e.Handled = true;

        // Проверка: не вставлять точку после точки
        if (caretPos > 0 && currentText[caretPos - 1] == '.')
        {
          return;
        }

        InputBox.Dispatcher.BeginInvoke(new Action(() =>
        {
          InputBox.Text = currentText.Insert(caretPos, ".");
          InputBox.CaretIndex = caretPos + 1;
        }));

        return;
      }

      // Запрет на две точки подряд
      if (e.Text == "." && caretPos > 0 && currentText[caretPos - 1] == '.')
      {
        e.Handled = true;
        return;
      }

      // Разрешаем ввод только цифр и точки
      e.Handled = !Regex.IsMatch(e.Text, "^[0-9.]$");
    }

    /// <summary>
    /// Подсвечивает текст при ошибке.
    /// </summary>
    public void DataError()
    {
      BorderData.Background = new SolidColorBrush(ShowMessageModel.ErrorMessage.Item2);
      Keyboard.ClearFocus();
    }
  }
}

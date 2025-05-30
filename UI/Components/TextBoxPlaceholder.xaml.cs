using System.Globalization;
using System.Printing;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
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
    /// Свойство зависимости для включения/отключения проверки на числовой ввод.
    /// </summary>
    public static readonly DependencyProperty IsNumberInputEnabledProperty =
        DependencyProperty.Register(nameof(IsNumberInputEnabled), typeof(bool), typeof(TextBoxPlaceholder),
            new PropertyMetadata(true));

    /// <summary>
    /// Свойство зависимости для единицы измерения, отображаемой рядом с полем ввода.
    /// </summary>
    public static readonly DependencyProperty UnitProperty =
        DependencyProperty.Register(
            nameof(Unit),
            typeof(string),
            typeof(TextBoxPlaceholder),
            new PropertyMetadata(string.Empty));

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
    /// Единица измерения, отображаемая рядом с полем ввода (например, "В", "Ом", "мА").
    /// </summary>
    public string Unit
    {
      get => (string)GetValue(UnitProperty);
      set => SetValue(UnitProperty, value);
    }

    /// <summary>
    /// Включает или отключает проверку на числовой ввод.
    /// По умолчанию: true (только цифры).
    /// </summary>
    public bool IsNumberInputEnabled
    {
      get => (bool)GetValue(IsNumberInputEnabledProperty);
      set => SetValue(IsNumberInputEnabledProperty, value);
    }

    public static readonly new DependencyProperty BackgroundProperty = DependencyProperty.Register(
        nameof(Background),
        typeof(Brush),
        typeof(TextBoxPlaceholder),
        new PropertyMetadata(Brushes.Transparent, OnBackgroundChanged));

    private static void OnBackgroundChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
      if (d is TextBoxPlaceholder control && control.BorderData != null)
      {
        control.BorderData.Background = (Brush)e.NewValue;
      }
    }

    public new Brush Background
    {
      get => (Brush)GetValue(BackgroundProperty);
      set => SetValue(BackgroundProperty, value);
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
      if (BorderData.Background != (Brush)FindResource("LightPrimarySolidColorBrush"))
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
      BorderData.Background = new SolidColorBrush(ShowMessageModel.ErrorMessage.TitleColor);
      Keyboard.ClearFocus();
    }
  }

  /// <summary>
  /// Конвертер для изменения отступа в зависимости от наличия значения в свойстве Unit.
  /// Если значение Unit не пустое, возвращает отступ в 10 пикселей справа, иначе 0.
  /// </summary>
  public class MarginConverter : IValueConverter
  {
    /// <summary>
    /// Конвертирует значение для задания отступа.
    /// </summary>
    /// <param name="value">Значение, которое нужно конвертировать (в данном случае строка, представляющая Unit).</param>
    /// <param name="targetType">Целевой тип (в данном случае <see cref="Thickness"/>).</param>
    /// <param name="parameter">Параметр для конвертации (не используется в данном случае).</param>
    /// <param name="culture">Культура, используемая для конвертации (не используется в данном случае).</param>
    /// <returns>Возвращает <see cref="Thickness"/> с отступом, основанным на значении Unit.</returns>
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      // Если Unit не пустое, возвращаем отступ 10 пикселей, иначе 0
      return string.IsNullOrEmpty(value as string) ? new Thickness(0) : new Thickness(0, 0, 10, 0);
    }

    /// <summary>
    /// Не реализован, так как конвертация назад не требуется.
    /// </summary>
    /// <param name="value">Значение, которое нужно конвертировать обратно.</param>
    /// <param name="targetType">Целевой тип (не используется).</param>
    /// <param name="parameter">Параметр для конвертации (не используется).</param>
    /// <param name="culture">Культура, используемая для конвертации (не используется).</param>
    /// <returns>Метод не реализован, так как не требуется конвертировать обратно.</returns>
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      throw new NotImplementedException();
    }
  }
}

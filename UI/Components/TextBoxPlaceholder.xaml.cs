using Ask.Core.Services.EventCore.Events;
using Ask.Core.Services.EventCore.Services;
using Ask.Core.Shared.DTO.Protocol;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

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

    public static readonly DependencyProperty AllowRangeProperty =
        DependencyProperty.Register(
            nameof(AllowRange),
            typeof(bool),
            typeof(TextBoxPlaceholder),
            new PropertyMetadata(false));

    public bool AllowRange
    {
      get => (bool)GetValue(AllowRangeProperty);
      set => SetValue(AllowRangeProperty, value);
    }

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
      EventAggregator.Subscribe<ThemeEvent.Change>(OnThemeChanged);
    }

    private void InitPlaceholder()
    {
      if (string.IsNullOrWhiteSpace(Text))
      {
        InputBox.Text = Placeholder;
        InputBox.Foreground = (Brush)FindResource("TestsInputDescriptionSolidColorBrush");
      }
      else
      {
        InputBox.Foreground = (Brush)FindResource("TestsInputHeaderSolidColorBrush");
      }
    }

    /// <summary>
    /// Обработчик события смены темы. Вызывается, когда тема меняется глобально.
    /// </summary>
    private async void OnThemeChanged(ThemeEvent.Change e)
    {
      if (string.IsNullOrWhiteSpace(Text))
      {
        InputBox.Text = Placeholder;
        InputBox.Foreground = (Brush)FindResource("TestsInputDescriptionSolidColorBrush");
      }
      else
      {
        InputBox.Foreground = (Brush)FindResource("TestsInputHeaderSolidColorBrush");
      }
    }

    private void InputBox_GotFocus(object sender, RoutedEventArgs e)
    {
      if (InputBox.Text == Placeholder)
      {
        InputBox.Text = "";
      }

    }

    private void InputBox_LostFocus(object sender, RoutedEventArgs e)
    {
      if (string.IsNullOrWhiteSpace(InputBox.Text) || InputBox.Text == Placeholder)
      {
        InputBox.Text = Placeholder;
        InputBox.Foreground = (Brush)FindResource("TestsInputDescriptionSolidColorBrush");
      }
      else
      {
        Text = InputBox.Text;
      }

    }

    private void InputBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
      if (IsNumberInputEnabled && e.Key == Key.Space)
      {
        e.Handled = true;
      }
    }

    /// <summary>
    /// Обрабатывает ввод в текстовом поле, ограничивая его числовым форматом.
    /// </summary>
    private void InputBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
      if (!IsNumberInputEnabled)
      {
        e.Handled = false;
        return;
      }

      string currentText = InputBox.Text;
      int caretPos = InputBox.CaretIndex;

      if (HandleSpaceInput(e))
        return;

      if (HandleDashInput(e, currentText, caretPos))
        return;

      if (HandleCommaInput(e, currentText, caretPos))
        return;

      if (HandleDoubleDot(e, currentText, caretPos))
        return;

      HandleRegularInput(e);
    }

    /// <summary>
    /// Блокирует ввод пробела, если разрешён только числовой ввод.
    /// </summary>
    private bool HandleSpaceInput(TextCompositionEventArgs e)
    {
      if (e.Text == " ")
      {
        e.Handled = true;
        return true;
      }

      return false;
    }

    /// <summary>
    /// Обрабатывает ввод символа тире, если включён режим диапазона.
    /// Блокирует повторный ввод или ввод не в начале строки.
    /// </summary>
    private bool HandleDashInput(TextCompositionEventArgs e, string currentText, int caretPos)
    {
      if (e.Text != "-")
        return false;

      if (!AllowRange)
      {
        e.Handled = true;
        return true;
      }

      bool alreadyHasDash = currentText.Contains("-");
      bool isFirst = caretPos == 0;
      bool isPrevDash = caretPos > 0 && currentText[caretPos - 1] == '-';

      e.Handled = alreadyHasDash || isFirst || isPrevDash;
      return true;
    }

    /// <summary>
    /// Обрабатывает ввод запятой: блокирует её и подставляет точку,
    /// если предыдущий символ не точка.
    /// </summary>
    private bool HandleCommaInput(TextCompositionEventArgs e, string currentText, int caretPos)
    {
      if (e.Text != ",")
        return false;

      e.Handled = true;

      if (caretPos > 0 && currentText[caretPos - 1] == '.')
        return true;

      InputBox.Dispatcher.BeginInvoke(new Action(() =>
      {
        InputBox.Text = currentText.Insert(caretPos, ".");
        InputBox.CaretIndex = caretPos + 1;
      }));

      return true;
    }

    /// <summary>
    /// Блокирует ввод двух точек подряд.
    /// </summary>
    private bool HandleDoubleDot(TextCompositionEventArgs e, string currentText, int caretPos)
    {
      if (e.Text == "." && caretPos > 0 && currentText[caretPos - 1] == '.')
      {
        e.Handled = true;
        return true;
      }

      return false;
    }

    /// <summary>
    /// Обрабатывает обычный ввод и разрешает только цифры и точку.
    /// </summary>
    private void HandleRegularInput(TextCompositionEventArgs e)
    {
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

using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using UI.Controls.Protocol;

namespace UI.Components
{
  /// <summary>
  /// Компонент для лёгкого ввода тестовых данных:
  /// содержит поля для ввода проверяемого номера, проверяющего и диапазона проверки,
  /// а также логику фильтрации ввода и подсветки ошибок.
  /// </summary>
  public partial class InputFieldLightweight : UserControl
  {
    #region Свойства доступа к данным

    /// <summary>
    /// Получает или задаёт номер проверяемого устройства в формате a.b.
    /// </summary>
    public string TestedNumber
    {
      get => TestedNumberBox.Text;
      set => TestedNumberBox.Text = value;
    }

    /// <summary>
    /// Получает или задаёт номер проверяющего устройства в формате a.b.
    /// </summary>
    public string TesterNumber
    {
      get => TesterNumberBox.Text;
      set => TesterNumberBox.Text = value;
    }

    /// <summary>
    /// Получает или задаёт диапазон проверки в формате списка чисел и диапазонов (например, "1-3,5").
    /// </summary>
    public string TestRange
    {
      get => TestRangeBox.Text;
      set => TestRangeBox.Text = value;
    }

    #endregion

    #region Конструктор и инициализация

    /// <summary>
    /// Инициализирует новый экземпляр <see cref="InputFieldLightweight"/>.
    /// Выполняет настройку фильтров ввода и подписку на события.
    /// </summary>
    public InputFieldLightweight()
    {
      InitializeComponent();
      SubscribeToEvents();

      // Отключаем встроенную числовую валидацию у TextBoxPlaceholder
      TestedNumberBox.IsNumberInputEnabled = false;
      TesterNumberBox.IsNumberInputEnabled = false;
      TestRangeBox.IsNumberInputEnabled = false;

      // Навешиваем фильтры ввода для полей
      AttachCoordinateFilter(TestedNumberBox);
      AttachCoordinateFilter(TesterNumberBox);
      AttachRangeFilter(TestRangeBox);
    }

    #endregion

    #region Привязка фильтра к полям

    /// <summary>
    /// Привязывает фильтр для ввода координат (формат a.b) к указанному <see cref="TextBoxPlaceholder"/>.
    /// </summary>
    /// <param name="placeholder">Элемент-заполнитель, содержащий внутренний TextBox.</param>
    private void AttachCoordinateFilter(TextBoxPlaceholder placeholder)
    {
      placeholder.Loaded += (_, _) =>
      {
        if (placeholder.FindName("InputBox") is TextBox tb)
        {
          // Перехватываем событие PreviewTextInput до внутренних обработчиков
          tb.AddHandler(
            TextBox.PreviewTextInputEvent,
            new TextCompositionEventHandler(Coordinate_PreviewTextInput),
            handledEventsToo: true);

          // Обработка вставки из буфера
          DataObject.AddPastingHandler(tb, new DataObjectPastingEventHandler(Coordinate_Pasting));
        }
      };
    }

    /// <summary>
    /// Привязывает фильтр для ввода диапазона (формат "1,2-5") к указанному <see cref="TextBoxPlaceholder"/>.
    /// </summary>
    /// <param name="placeholder">Элемент-заполнитель, содержащий внутренний TextBox.</param>
    private void AttachRangeFilter(TextBoxPlaceholder placeholder)
    {
      placeholder.Loaded += (_, _) =>
      {
        if (placeholder.FindName("InputBox") is TextBox tb)
        {
          // Перехватываем событие PreviewTextInput до внутренних обработчиков
          tb.AddHandler(
            TextBox.PreviewTextInputEvent,
            new TextCompositionEventHandler(Range_PreviewTextInput),
            handledEventsToo: true);

          // Обработка вставки из буфера
          DataObject.AddPastingHandler(tb, new DataObjectPastingEventHandler(Range_Pasting));
        }
      };
    }

    #endregion

    #region Фильтрация координат (формат a.b)

    /// <summary>
    /// Обработчик события PreviewTextInput для фильтрации ввода координат.
    /// Разрешает ввод цифр и одной точки не в начале строки и не более одного.
    /// </summary>
    private void Coordinate_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
      var tb = (TextBox)sender;
      char c = e.Text.FirstOrDefault();
      string txt = tb.Text;
      int pos = tb.CaretIndex;

      // Разрешаем только цифры и одну точку
      if (!char.IsDigit(c) && c != '.')
      {
        e.Handled = true;
        return;
      }

      if (c == '.')
      {
        // Запрещаем точку в начале
        if (pos == 0)
        {
          e.Handled = true;
          return;
        }
        // Точка только одна
        if (txt.Contains('.'))
        {
          e.Handled = true;
          return;
        }
      }

      // Всё остальное разрешено
      e.Handled = false;
    }

    /// <summary>
    /// Обработчик события вставки для фильтрации координат.
    /// Допускает только строки вида: цифры, точка, цифры.
    /// </summary>
    private void Coordinate_Pasting(object sender, DataObjectPastingEventArgs e)
    {
      if (!e.DataObject.GetDataPresent(DataFormats.Text))
      {
        e.CancelCommand();
        return;
      }

      var paste = (string)e.DataObject.GetData(DataFormats.Text)!;
      // Используем регулярное выражение для проверки формата
      if (!Regex.IsMatch(paste, @"^[0-9]+\.[0-9]+$"))
      {
        e.CancelCommand();
      }
    }

    #endregion

    #region Фильтрация диапазона (формат "1,2-5")

    /// <summary>
    /// Обработчик события PreviewTextInput для фильтрации ввода диапазона.
    /// Ограничивает ввод цифр, запятых, тире и пробелов, запрещая некорректные сочетания.
    /// </summary>
    private void Range_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
      var tb = (TextBox)sender;
      char c = e.Text.FirstOrDefault();
      int pos = tb.CaretIndex;
      string txt = tb.Text;

      // Запрет символов ',' или '-' в начале
      if (pos == 0 && (c == ',' || c == '-'))
      {
        e.Handled = true;
        return;
      }

      // Допускаем только цифры, запятую, тире и пробел
      if (!(char.IsDigit(c) || c == ',' || c == '-' || char.IsWhiteSpace(c)))
      {
        e.Handled = true;
        return;
      }

      // Контекст предыдущего символа
      if (pos > 0)
      {
        char prev = txt[pos - 1];

        // После '-' запрещаем ',', '-' и пробел
        if (prev == '-' && (c == ',' || c == '-' || char.IsWhiteSpace(c)))
        {
          e.Handled = true;
          return;
        }
        // После ',' запрещаем ',' и '-'
        if (prev == ',' && (c == ',' || c == '-'))
        {
          e.Handled = true;
          return;
        }
        // После цифры запрещаем пробел
        if (char.IsDigit(prev) && char.IsWhiteSpace(c))
        {
          e.Handled = true;
          return;
        }
      }

      e.Handled = false;
    }

    /// <summary>
    /// Обработчик события вставки для фильтрации диапазона.
    /// Блокирует вставку текста, содержащего недопустимые символы.
    /// </summary>
    private void Range_Pasting(object sender, DataObjectPastingEventArgs e)
    {
      if (e.DataObject.GetDataPresent(DataFormats.Text))
      {
        var text = (string)e.DataObject.GetData(DataFormats.Text)!;
        if (text.Any(c => !char.IsDigit(c) && c != ',' && c != '-'))
          e.CancelCommand();
      }
      else
      {
        e.CancelCommand();
      }
    }

    #endregion

    #region Событийные методы

    /// <summary>
    /// Подписывается на события ActionExecutor для смены режима редактирования и отображения.
    /// </summary>
    private void SubscribeToEvents()
    {
      ActionExecutor.StartProcessing += ActionExecutor_StartProcessing;
    }

    /// <summary>
    /// Обработчик события начала/окончания обработки:
    /// переключает видимость полей ввода и обновляет заголовки с введёнными или подсказочными значениями.
    /// </summary>
    private void ActionExecutor_StartProcessing(bool isProcessing)
    {
      const string testedBaseText = "Номер проверяемого";
      const string testerBaseText = "Номер проверяющего";
      const string rangeBaseText = "Диапазон проверки";

      // Переключаем видимость полей ввода
      var visibility = isProcessing ? Visibility.Collapsed : Visibility.Visible;
      TestedNumberBox.Visibility = visibility;
      TesterNumberBox.Visibility = visibility;
      TestRangeBox.Visibility = visibility;

      if (isProcessing)
      {
        // При обработке показываем введённые данные
        headerTestedNumber.Text = $"{testedBaseText}: {TestedNumber}";
        headerTesterNumber.Text = $"{testerBaseText}: {TesterNumber}";
        headerTestRange.Text = $"{rangeBaseText}: {TestRange}";
      }
      else
      {
        // В режиме ввода показываем подсказки
        headerTestedNumber.Text = $"{testedBaseText}: вида a.b";
        headerTesterNumber.Text = $"{testerBaseText}: вида a.b";
        headerTestRange.Text = rangeBaseText;
      }
    }

    #endregion

    #region Методы подсветки ошибок

    /// <summary>
    /// Подсвечивает поле ввода проверяемого номера при ошибке.
    /// </summary>
    public void HighlightTestedNumber() => TestedNumberBox.DataError();

    /// <summary>
    /// Подсвечивает поле ввода проверяющего номера при ошибке.
    /// </summary>
    public void HighlightTesterNumber() => TesterNumberBox.DataError();

    /// <summary>
    /// Подсвечивает поле ввода диапазона проверки при ошибке.
    /// </summary>
    public void HighlightTestRange() => TestRangeBox.DataError();

    #endregion
  }
}
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using UI.Controls.Protocol;

namespace UI.Components
{
  /// <summary>
  /// Компонент для лёгкого ввода тестовых данных:
  /// содержит поля для ввода проверяемого номера, проверяющего и диапазона проверки,
  /// а также логику валидации и подсветки ошибок.
  /// </summary>
  public partial class InputFieldLightweight : UserControl
  {
    #region Свойства доступа к данным

    /// <summary>
    /// Получает или задаёт номер проверяемого устройства в формате "a.b".
    /// </summary>
    public string TestedNumber
    {
      get => TestedNumberBox.Text;
      set => TestedNumberBox.Text = value;
    }

    /// <summary>
    /// Получает или задаёт номер проверяющего устройства в формате "a.b".
    /// </summary>
    public string TesterNumber
    {
      get => TesterNumberBox.Text;
      set => TesterNumberBox.Text = value;
    }

    /// <summary>
    /// Получает или задаёт диапазон проверки в формате "x-y".
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
    /// Выполняет инициализацию компонентов и подписку на события.
    /// </summary>
    public InputFieldLightweight()
    {
      InitializeComponent();
      SubscribeToEvents();

      // Подписываемся на события ввода текста и вставки для внутреннего TextBox диапазона
      TestRangeBox.Loaded += (_, _) =>
      {
        if (TestRangeBox.FindName("InputBox") is TextBox inner)
        {
          inner.PreviewTextInput += Range_PreviewTextInput;
          DataObject.AddPastingHandler(inner, Range_Pasting);
        }
      };
    }

    #endregion

    #region Обработка ввода диапазона

    /// <summary>
    /// Обработчик события PreviewTextInput для поля диапазона.
    /// Ограничивает ввод символов: цифр, запятой, тире, пробела;
    /// запрещает некорректные сочетания символов.
    /// </summary>
    /// <param name="sender">Источник события (TextBox).</param>
    /// <param name="e">Данные события ввода текста.</param>
    private void Range_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
      var textBox = (TextBox)sender;
      char c = e.Text.FirstOrDefault();
      int pos = textBox.CaretIndex;
      string txt = textBox.Text;

      // 1) Нельзя начинать с ',' или '-'
      if (pos == 0 && (c == ',' || c == '-'))
      {
        e.Handled = true;
        return;
      }

      // 2) Если вводим не цифру, запятую, тире или пробел — блокируем
      if (!(char.IsDigit(c) || c == ',' || c == '-' || char.IsWhiteSpace(c)))
      {
        e.Handled = true;
        return;
      }

      // 3) Проверяем предыдущий символ для запрета некорректных сочетаний
      if (pos > 0)
      {
        char prev = txt[pos - 1];

        // После тире нельзя вводить ',', '-' или пробел
        if (prev == '-' && (c == ',' || c == '-' || char.IsWhiteSpace(c)))
        {
          e.Handled = true;
          return;
        }

        // После запятой нельзя вводить ',' или '-'
        if (prev == ',' && (c == ',' || c == '-'))
        {
          e.Handled = true;
          return;
        }

        // После цифры нельзя вводить пробел
        if (char.IsDigit(prev) && char.IsWhiteSpace(c))
        {
          e.Handled = true;
          return;
        }
      }

      // Ввод корректен
      e.Handled = false;
    }

    /// <summary>
    /// Обработчик события вставки данных в поле диапазона.
    /// Блокирует вставку текста, содержащего недопустимые символы.
    /// </summary>
    /// <param name="sender">Источник события (TextBox).</param>
    /// <param name="e">Данные события вставки.</param>
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
    /// Подписывается на события запуска обработки данных.
    /// </summary>
    private void SubscribeToEvents()
    {
      ActionExecutor.StartProcessing += ActionExecutor_StartProcessing;
    }

    /// <summary>
    /// Обработчик события начала или завершения процесса обработки.
    /// Переключает видимость полей ввода и обновляет заголовки с текущими значениями.
    /// </summary>
    /// <param name="isProcessing">Признак, что процесс обработки запущен.</param>
    private void ActionExecutor_StartProcessing(bool isProcessing)
    {
      const string testedBaseText = "Номер проверяемого";
      const string testerBaseText = "Номер проверяющего";
      const string rangeBaseText = "Диапазон проверки";

      var visibility = isProcessing ? Visibility.Collapsed : Visibility.Visible;
      TestedNumberBox.Visibility = visibility;
      TesterNumberBox.Visibility = visibility;
      TestRangeBox.Visibility = visibility;

      if (isProcessing)
      {
        headerTestedNumber.Text = $"{testedBaseText}: {TestedNumber}";
        headerTesterNumber.Text = $"{testerBaseText}: {TesterNumber}";
        headerTestRange.Text = $"{rangeBaseText}: {TestRange}";
      }
      else
      {
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
    public void HighlightTestedNumber()
    {
      TestedNumberBox.DataError();
    }

    /// <summary>
    /// Подсвечивает поле ввода проверяющего номера при ошибке.
    /// </summary>
    public void HighlightTesterNumber()
    {
      TesterNumberBox.DataError();
    }

    /// <summary>
    /// Подсвечивает поле ввода диапазона проверки при ошибке.
    /// </summary>
    public void HighlightTestRange()
    {
      TestRangeBox.DataError();
    }

    #endregion
  }
}
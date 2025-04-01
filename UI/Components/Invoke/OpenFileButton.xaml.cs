using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using static AppConfiguration.Base.EventAggregator;

namespace UI.Components.Invoke
{
  /// <summary>
  /// Логика взаимодействия для компонента OpenFileButton.
  /// Кнопка с текстом и возможностью изменения фона, с дополнительной логикой для изменения ширины в зависимости от текста.
  /// </summary>
  public partial class OpenFileButton : UserControl
  {
    /// <summary>
    /// Перечисление типов окон пользовательского интерфейса.
    /// </summary>
    public enum TypeWindow
    {
      /// <summary>
      /// Окно настроек.
      /// </summary>
      Settings,

      /// <summary>
      /// Окно управления файлами.
      /// </summary>
      Files,

      /// <summary>
      /// Окно работы с оборудованием.
      /// </summary>
      DeviceControl
    }

    public TypeWindow? TabType { get; set; }

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="OpenFileButton"/>.
    /// </summary>
    public OpenFileButton()
    {
      InitializeComponent();
      LockedChanged += ApplicationDataHandler_LockedChanged;
    }

    /// <summary>
    /// Возвращает кнопку закрытия для элемента управления.
    /// </summary>
    /// <returns>Кнопка закрытия для данного элемента управления.</returns>
    public CloseButton GetCloseButton()
    {
      return this.CloseButton;
    }

    /// <summary>
    /// Свойство для изменения фона кнопки. Изменяет фон на ButtonPage.
    /// </summary>
    public new Brush Background
    {
      get { return ButtonPage.Background; }
      set { ButtonPage.Background = value; }
    }

    /// <summary>
    /// Получает или задает описание для кнопки.
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// Получает или задает текст, отображаемый на кнопке.
    /// </summary>
    public string Text
    {
      get
      {
        return Header.Text;
      }

      set
      {
        Header.Text = value;
        AdjustWidth();
      }
    }

    /// <summary>
    /// Метод, который регулирует ширину кнопки в зависимости от длины текста.
    /// Ширина кнопки будет автоматически подстраиваться под длину текста, но не превышать 300.
    /// </summary>
    private void AdjustWidth()
    {
      double width = MeasureTextWidth(Text, Header.FontSize);
      if (width > 300 - 20)
      {
        Width = 300; // Максимальная ширина 300
      }
      else
      {
        Width = width + 30; // Подстраивает ширину с небольшим запасом
      }
    }

    /// <summary>
    /// Метод для измерения ширины текста с заданным шрифтом.
    /// </summary>
    private double MeasureTextWidth(string text, double fontSize)
    {
      var formattedText = new FormattedText(
          text,
          System.Globalization.CultureInfo.CurrentCulture,
          FlowDirection.LeftToRight,
          new Typeface(Header.FontFamily, FontStyles.Normal, FontWeights.Normal, FontStretches.Normal),
          fontSize,
          Brushes.Black,
          VisualTreeHelper.GetDpi(this).PixelsPerDip);

      return formattedText.Width;
    }

    /// <summary>
    /// Обработчик изменения состояния LockedChanged. Меняет доступность кнопки и фон в зависимости от состояния.
    /// </summary>
    private void ApplicationDataHandler_LockedChanged(bool newValue)
    {
      Application.Current.Dispatcher.Invoke(() =>
      {
        this.IsEnabled = !newValue; // Кнопка отключается, если состояние "locked" активно

        // Если фон не равен активному цвету, меняем фон в зависимости от состояния кнопки
        if (this.Background != (SolidColorBrush)Application.Current.Resources["ActiveBorderSolidColorBrush"])
        {
          if (!this.IsEnabled)
          {
            this.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#b23a48")); // Красный фон для отключенной кнопки
          }
          else if (this.IsEnabled)
          {
            this.Background = (SolidColorBrush)Application.Current.Resources["IsCheckedColorSolidColorBrush"]; // Стандартный фон для включенной кнопки
          }
        }
      });
    }
  }
}

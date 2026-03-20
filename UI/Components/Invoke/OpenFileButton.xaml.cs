using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.EventCore.Events;
using Ask.Core.Services.EventCore.Services;
using Ask.Core.Shared.Metadata.Enums.UiEnums;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace UI.Components.Invoke
{
  /// <summary>
  /// Логика взаимодействия для компонента OpenFileButton.
  /// Кнопка с текстом и возможностью изменения фона, с дополнительной логикой для изменения ширины в зависимости от текста.
  /// </summary>
  public partial class OpenFileButton : UserControl
  {
    public static readonly DependencyProperty IsActiveProperty =
      DependencyProperty.Register(
        nameof(IsActive),
        typeof(bool),
        typeof(OpenFileButton),
        new FrameworkPropertyMetadata(false));

    /// <summary>
    /// Тип вкладки.
    /// </summary>
    public TypeWindow? TabType { get; set; }

    public bool IsActive
    {
      get => (bool)GetValue(IsActiveProperty);
      set => SetValue(IsActiveProperty, value);
    }

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="OpenFileButton"/>.
    /// </summary>
    public OpenFileButton()
    {
      InitializeComponent();
      EventAggregator.Subscribe<SystemStateEvents.LockedChanged>(e => ApplicationDataHandler_LockedChanged(e.IsLocked));
      ApplyLockState(SystemStateManager.GetIsLocked());
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
      const double tabChromeWidth = 52;
      double width = MeasureTextWidth(Text, Header.FontSize);
      if (width > 300 - tabChromeWidth)
      {
        Width = 300; // Максимальная ширина 300
      }
      else
      {
        Width = width + tabChromeWidth; // Подстраивает ширину с учетом отступов и кнопки закрытия
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
    /// Обработчик изменения состояния LockedChanged. Меняет доступность кнопки.
    /// </summary>
    private void ApplicationDataHandler_LockedChanged(bool newValue)
    {
      ApplyLockState(newValue);
    }

    private void ApplyLockState(bool isLocked)
    {
      Application.Current.Dispatcher.Invoke(() =>
      {
        IsEnabled = !isLocked;
      });
    }
  }
}

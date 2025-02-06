using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using static AppConfig.EventAggregator;


namespace UI.Components.Invoke
{
  /// <summary>
  /// Логика взаимодействия для OpenFileButton.xaml
  /// </summary>
  public partial class OpenFileButton : UserControl
  {
    public OpenFileButton()
    {
      InitializeComponent();
      LockedChanged += ApplicationDataHandler_LockedChanged;
    }
    public CloseButton GetCloseButton()
    {
      return this.CloseButton;
    }

    public new Brush Background
    {
      get { return ButtonPage.Background; }
      set { ButtonPage.Background = value; }
    }

    public string Description { get; set; }
    public string Text
    {
      get { return Header.Text; }
      set
      {
        Header.Text = value;
        AdjustWidth();
      }
    }

    private void AdjustWidth()
    {
      double width = MeasureTextWidth(Text, Header.FontSize);
      if (width > 300 - 20)
      {
        Width = 300;
      }
      else
      {
        Width = width + 30;
      }
    }

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

    private void ApplicationDataHandler_LockedChanged(bool newValue)
    {
      Application.Current.Dispatcher.Invoke(() =>
      {
        this.IsEnabled = !newValue;

        if (this.Background != (SolidColorBrush)Application.Current.Resources["ActiveBorderSolidColorBrush"])
        {
          if (!this.IsEnabled)
          {
            this.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#b23a48"));
          }
          else if (this.IsEnabled)
          {
            this.Background = (SolidColorBrush)Application.Current.Resources["IsCheckedColorSolidColorBrush"];
          }
        }
      });
    }

  }
}

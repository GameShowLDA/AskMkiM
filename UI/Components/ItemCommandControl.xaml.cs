using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Core.Communication;

namespace UI.Components
{
  /// <summary>
  /// Логика взаимодействия для ItemCommandControl.xaml
  /// </summary>
  public partial class ItemCommandControl : UserControl
  {
    private bool activeControl;

    public bool ActiveControl { get => activeControl; set => ActivatedControl(value); }

    public ItemCommandControl()
    {
      InitializeComponent();
      this.Loaded += ItemCommandControl_Loaded;

      nameTextBlock.MouseEnter += (s, a) =>
      {
        if (!activeControl)
        {
          var primaryColorBrush = (SolidColorBrush)Application.Current.Resources["ActiveForegroundSolidColorBrush"];
          nameTextBlock.Foreground = primaryColorBrush;

          var backgroundBrush = (SolidColorBrush)this.FindResource("SecondarySolidColorBrush");
          this.Background = backgroundBrush;

          var borderBrush = (SolidColorBrush)this.FindResource("ActiveBorderSolidColorBrush");
          this.BorderBrush = borderBrush;

          this.BorderThickness = new Thickness(5, 0, 0, 0);
        }
      };

      nameTextBlock.MouseLeave += (s, a) =>
      {
        if (!activeControl)
        {
          var primaryColorBrush = (SolidColorBrush)Application.Current.Resources["ForegroundSolidColorBrush"];
          nameTextBlock.Foreground = primaryColorBrush;

          this.Background = new SolidColorBrush(Colors.Transparent);
          this.BorderThickness = new Thickness(0, 0, 0, 0);
        }
      };
    }

    private void ItemCommandControl_Loaded(object sender, RoutedEventArgs e)
    {
      UpdateToolTipVisibility();
    }

    private void UpdateToolTipVisibility()
    {
      var pixelsPerDip = VisualTreeHelper.GetDpi(this).PixelsPerDip;
      var formattedText = new FormattedText(
          nameTextBlock.Text,
          CultureInfo.CurrentCulture,
          FlowDirection.LeftToRight,
          new Typeface(nameTextBlock.FontFamily, nameTextBlock.FontStyle, nameTextBlock.FontWeight, nameTextBlock.FontStretch),
          nameTextBlock.FontSize,
          nameTextBlock.Foreground,
          pixelsPerDip);

      if (formattedText.Width > nameTextBlock.ActualWidth)
      {
        nameTextBlock.ToolTip = nameTextBlock.Text;
      }
      else
      {
        nameTextBlock.ToolTip = null;
      }

      var primaryColorBrush = (SolidColorBrush)Application.Current.Resources["ForegroundSolidColorBrush"];
      nameTextBlock.Foreground = primaryColorBrush;

      this.Background = new SolidColorBrush(Colors.Transparent);

    }

    private void ActivatedControl(bool value)
    {
      activeControl = value;

      if (activeControl)
      {
        var primaryColorBrush = (SolidColorBrush)Application.Current.Resources["ActiveForegroundSolidColorBrush"];
        nameTextBlock.Foreground = primaryColorBrush;

        var backgroundColor = (SolidColorBrush)Application.Current.Resources["SecondarySolidColorBrush"];
        this.Background = backgroundColor;

        var borderColor = (SolidColorBrush)Application.Current.Resources["ActiveBorderSolidColorBrush"];
        this.BorderBrush = borderColor;

        this.BorderThickness = new Thickness(5, 0, 0, 0);
      }
      else
      {
        var primaryColorBrush = (SolidColorBrush)Application.Current.Resources["ForegroundSolidColorBrush"];
        nameTextBlock.Foreground = primaryColorBrush;

        this.Background = new SolidColorBrush(Colors.Transparent);

        this.BorderThickness = new Thickness(0, 0, 0, 0);
      }
    }
    public ItemCommandControl(CommandModel command) : this()
    {
      nameTextBlock.Text = command?.Name;
    }
  }
}

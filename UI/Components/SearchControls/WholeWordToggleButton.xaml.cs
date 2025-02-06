using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace UI.Components.SearchControls
{
  /// <summary>
  /// Логика взаимодействия для WholeWordToggleButton.xaml
  /// </summary>
  public partial class WholeWordToggleButton : UserControl
  {
    public WholeWordToggleButton()
    {
      InitializeComponent();
    }

    public bool IsChecked { get => GetChecked(); set => SetChecked(value); }

    private void SetChecked(bool value)
    {
      if (value)
      {
        ToggleButton.Foreground = Brushes.Red;
        Border.BorderBrush = Brushes.Red;
      }
      else
      {
        ToggleButton.Foreground = Brushes.Black;
        Border.BorderBrush = Brushes.Black;
      }
    }

    private bool GetChecked()
    {
      if (ToggleButton.Foreground == Brushes.Red)
      {
        return true;
      }
      else
      {
        return false;
      }
    }

    private void ToggleButton_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      IsChecked = !IsChecked;
    }
  }
}

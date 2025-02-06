using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace UI.Components.SearchControls
{
  /// <summary>
  /// Логика взаимодействия для CaseToggleButton.xaml
  /// </summary>
  public partial class CaseToggleButton : UserControl
  {


    public CaseToggleButton()
    {
      InitializeComponent();
    }

    public bool IsChecked { get => GetChecked(); set => SetChecked(value); }

    private void SetChecked(bool value)
    {
      if (value)
      {
        ToggleButton.Foreground = Brushes.Red;
      }
      else
      {
        ToggleButton.Foreground = Brushes.Black;
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

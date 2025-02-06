using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using YamlDotNet.Core.Tokens;

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
        var color = (Brush)Application.Current.Resources["ActiveForegroundSolidColorBrush"];
        ToggleButton.Foreground = color;
      }
      else
      {
        var color = (Brush)Application.Current.Resources["ForegroundSolidColorBrush"];
        ToggleButton.Foreground = color;
      }
    }

    private bool GetChecked()
    {
      var color = (Brush)Application.Current.Resources["ActiveForegroundSolidColorBrush"];
      if (ToggleButton.Foreground == color)
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

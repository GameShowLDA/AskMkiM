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

namespace UI.Components.SearchControls
{
  /// <summary>
  /// Логика взаимодействия для WholeWordToggleButton.xaml.
  /// </summary>
  public partial class WholeWordToggleButton : UserControl
  {
    /// <summary>
    /// Инициализирует новый экземпляр <see cref="WholeWordToggleButton"/>.
    /// </summary>
    public WholeWordToggleButton()
    {
      InitializeComponent();
    }

    /// <summary>
    /// Получает или устанавливает значение состояния кнопки-переключателя (включена или выключена).
    /// </summary>
    public bool IsChecked
    {
      get => GetChecked();  // Получаем состояние из метода GetChecked()
      set => SetChecked(value);  // Устанавливаем состояние с помощью метода SetChecked()
    }

    private void SetChecked(bool value)
    {
      if (value)
      {
        var color = (Brush)Application.Current.Resources["ActiveForegroundSolidColorBrush"];
        ToggleButton.Foreground = color;
        Border.BorderBrush = color;
      }
      else
      {
        var color = (Brush)Application.Current.Resources["ForegroundSolidColorBrush"];
        ToggleButton.Foreground = color;
        Border.BorderBrush = color;
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

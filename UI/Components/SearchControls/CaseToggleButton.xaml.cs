using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace UI.Components.SearchControls
{
  /// <summary>
  /// Логика взаимодействия для CaseToggleButton.xaml.
  /// </summary>
  public partial class CaseToggleButton : UserControl
  {
    /// <summary>
    /// Инициализирует новый экземпляр <see cref="CaseToggleButton"/>.
    /// </summary>
    public CaseToggleButton()
    {
      InitializeComponent();
    }

    /// <summary>
    /// Получает или задает текущее состояние кнопки.
    /// Если <c>true</c>, кнопка активна (переключен режим с учётом регистра), если <c>false</c>, режим без учёта регистра.
    /// </summary>
    public bool IsChecked { get => GetChecked(); set => SetChecked(value); }

    /// <summary>
    /// Метод для установки состояния кнопки.
    /// </summary>
    /// <param name="value">Новое состояние кнопки: <c>true</c> для активного состояния, <c>false</c> для неактивного.</param>
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

    /// <summary>
    /// Метод для получения текущего состояния.
    /// </summary>
    /// <returns><c>true</c>, если кнопка активна, <c>false</c> в противном случае.</returns>
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

    /// <summary>
    /// Обрабатывает событие предварительного нажатия кнопки мыши на переключателе.
    /// Переключает состояние элемента управления.
    /// </summary>
    /// <param name="sender">Источник события.</param>
    /// <param name="e">Данные события мыши.</param>
    private void ToggleButton_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      IsChecked = !IsChecked;
    }
  }
}

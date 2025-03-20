using System.Windows;
using System.Windows.Input;
using UI.Components.SearchControls;

namespace UI.Controls.Search
{
  /// <summary>
  /// Окно поиска, содержащее функции поиска и замены, а также возможность сворачивания/разворачивания строки замены.
  /// </summary>
  public partial class SearchWindow : Window
  {
    /// <summary>
    /// Флаг, указывающий, развернута ли строка замены.
    /// </summary>
    private bool _isExpanded = false;

    /// <summary>
    /// Минимальная высота окна без строки замены.
    /// </summary>
    private double MinWindowHeight => 80;

    /// <summary>
    /// Высота окна при развернутой строке замены.
    /// </summary>
    private double ExpandedWindowHeight => 120;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="SearchWindow"/>.
    /// </summary>
    public SearchWindow()
    {
      InitializeComponent();
    }

    /// <summary>
    /// Обработчик события загрузки окна.
    /// Устанавливает начальную высоту окна и скрывает строку замены.
    /// </summary>
    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
      ReplaceRow.Height = new GridLength(0);
      UpdateWindowHeight(MinWindowHeight);
    }

    /// <summary>
    /// Обработчик нажатия на кнопку переключения стрелки.
    /// Разворачивает или сворачивает строку замены.
    /// </summary>
    private async void ToggleArrow_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      _isExpanded = !_isExpanded;

      ToggleArrow.IsArrowUp = !_isExpanded;

      await Task.Delay(250);
      ReplaceRow.Height = _isExpanded ? new GridLength(1, GridUnitType.Star) : new GridLength(0);

      UpdateWindowHeight(_isExpanded ? ExpandedWindowHeight : MinWindowHeight);
    }

    /// <summary>
    /// Обновляет высоту окна в соответствии с заданным значением.
    /// </summary>
    /// <param name="newHeight">Новое значение высоты окна.</param>
    private void UpdateWindowHeight(double newHeight)
    {
      this.Height = newHeight;
      this.MinHeight = newHeight;
      this.MaxHeight = newHeight;
    }

    /// <summary>
    /// Обработчик изменения размера окна.
    /// Ограничивает изменение высоты окна.
    /// </summary>
    private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
    {
      this.Height = this.MinHeight;
    }

    /// <summary>
    /// Обработчик изменения состояния кнопки регистра (чувствительность к регистру).
    /// </summary>
    private void OnCaseChanged(object sender, EventArgs e)
    {
      var button = sender as CaseToggleButton;
      if (button != null)
      {
        MessageBox.Show($"Кнопка включена: {button.IsChecked}");
      }
    }
  }
}

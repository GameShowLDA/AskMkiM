using System.Windows;
using System.Windows.Input;
using UI.Components.SearchControls;

namespace UI.Controls.Search
{
  public partial class SearchWindow : Window
  {
    private bool _isExpanded = false; // По умолчанию строка скрыта

    private const double MinWindowHeight = 80;  // Высота окна без строки 2
    private const double ExpandedWindowHeight = 120; // Высота окна со строкой 2

    public SearchWindow()
    {
      InitializeComponent();
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
      ReplaceRow.Height = new GridLength(0);
      UpdateWindowHeight(MinWindowHeight);
    }

    private async void ToggleArrow_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      _isExpanded = !_isExpanded;


      ToggleArrow.IsArrowUp = !_isExpanded;

      await Task.Delay(250);
      ReplaceRow.Height = _isExpanded ? new GridLength(1, GridUnitType.Star) : new GridLength(0);

      UpdateWindowHeight(_isExpanded ? ExpandedWindowHeight : MinWindowHeight);
    }

    private void UpdateWindowHeight(double newHeight)
    {
      this.Height = newHeight;
      this.MinHeight = newHeight;
      this.MaxHeight = newHeight;
    }

    private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
    {
      this.Height = this.MinHeight;
    }

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

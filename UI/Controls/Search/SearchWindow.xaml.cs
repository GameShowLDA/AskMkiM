using System.Windows;
using System.Windows.Controls;

namespace UI.Controls.Search
{
  public partial class SearchWindow : Window
  {
    private bool _isExpanded = false; // Отслеживаем состояние

    private const double MinWindowHeight = 100; // Высота без строки 2
    private const double ExpandedWindowHeight = 140; // Высота со строкой 2

    public SearchWindow()
    {
      InitializeComponent();
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {

    }

  }
}

using AppConfig;
using System.Windows;
using System.Windows.Controls;

namespace UI.Components.SearchControls
{
  /// <summary>
  /// Логика взаимодействия для SearchResultsWindow.xaml
  /// </summary>
  public partial class SearchResultsWindow : Window
  {
    public SearchResultsWindow()
    {
      InitializeComponent();
    }

    public void ShowResults(Dictionary<string, Dictionary<int, string>> results)
    {
      List<SearchResultItem> items = new List<SearchResultItem>();

      foreach (var file in results)
      {
        foreach (var occurrence in file.Value)
        {
          items.Add(new SearchResultItem
          {
            FileName = file.Key,
            LineNumber = occurrence.Key,
            LineText = occurrence.Value
          });
        }
      }

      ResultsDataGrid.ItemsSource = items;
    }

    private void ResultsDataGrid_PreviewMouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
      var row = (sender as DataGrid).SelectedItem as SearchResultItem;
      if (row != null)
      {
        var fileName = row.FileName;
        var lineNumber = row.LineNumber;
        var lineLength = row.LineText.Length;
        
        EventAggregator.RaiseFoundTextSelectRow(fileName, lineNumber, lineLength);
      }
    }
  }
}

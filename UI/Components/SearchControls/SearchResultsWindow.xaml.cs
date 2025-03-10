using System.Windows;

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
          var item = new SearchResultItem
          {
            FileName = file.Key,
            LineNumber = occurrence.Key,
            LineText = occurrence.Value
          };

          Console.WriteLine($"Добавлен элемент: {item.FileName} | {item.LineNumber} | {item.LineText}");
          items.Add(item);
        }
      }

      ResultsListView.ItemsSource = null;
      ResultsListView.ItemsSource = items;
    }
  }
}

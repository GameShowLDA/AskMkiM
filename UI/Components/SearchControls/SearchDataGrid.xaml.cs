using AppConfig;
using System.Windows;
using System.Windows.Controls;
using static Utilities.LoggerUtility;

namespace UI.Components.SearchControls
{
  /// <summary>
  /// Логика взаимодействия для SearchDataGrid.xaml.
  /// </summary>
  public partial class SearchDataGrid : UserControl
  {
    /// <summary>
    /// Конструктор для создания экземпляра класса SearchDataGrid.
    /// Инициализирует компоненты интерфейса.
    /// </summary>
    public SearchDataGrid()
    {
      InitializeComponent();
    }

    private void ResultsDataGrid_PreviewMouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
      var row = (sender as DataGrid).SelectedItem as SearchResult;
      if (row != null)
      {
        var fileName = row.FileName;
        var lineNumber = row.LineNumber;
        var startOffset = row.StartOffset; 
        var lineText = row.SubstringFromWord;

        EventAggregator.RaiseFoundTextSelectRow(fileName, lineNumber, startOffset, lineText, row.SearchText);
        LogInformation("Сработало событие нажатия на строку dataGrid с результатами поиска");
      }
    }

    private void ResultsDataGrid_RequestBringIntoView(object sender, RequestBringIntoViewEventArgs e)
    {
      e.Handled = true;
    }

    /// <summary>
    /// Устанавливает источник данных для DataGrid в элементе управления ResultsDataGrid.
    /// </summary>
    /// <param name="items">Список объектов SearchResult, которые будут отображены в ResultsDataGrid.</param>
    public void SetItemSourse(List<SearchResult> items)
    {
      ResultsDataGrid.ItemsSource = items;
    }
  }
}

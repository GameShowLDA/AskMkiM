using AppConfig;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using UI.Components.Invoke;
using UI.Controls.TextEditor;
using static Utilities.LoggerUtility;

namespace UI.Components.SearchControls
{
  /// <summary>
  /// Логика взаимодействия для SearchDataGrid.xaml
  /// </summary>
  public partial class SearchDataGrid : UserControl
  {
    public SearchDataGrid()
    {
      InitializeComponent();
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
        LogInformation("Сработало событие нажатия на строку dataGrid с результатами поиска");
      }
    }

    private void ResultsDataGrid_RequestBringIntoView(object sender, RequestBringIntoViewEventArgs e)
    {
      e.Handled = true;
    }

    public void SetItemSourse(List<SearchResultItem> items)
    {
      ResultsDataGrid.ItemsSource = items;
    }

  }
}

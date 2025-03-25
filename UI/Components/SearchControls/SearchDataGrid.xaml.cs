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
    public string SearchText
    {
      get { return (string)GetValue(SearchTextProperty); }
      set { SetValue(SearchTextProperty, value); }
    }

    public static readonly DependencyProperty SearchTextProperty =
        DependencyProperty.Register("SearchText", typeof(string), typeof(SearchDataGrid), new PropertyMetadata(string.Empty));


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

        EventAggregator.RaiseFoundTextSelectRow(fileName, lineNumber, startOffset, lineText);
        LogInformation("Сработало событие нажатия на строку dataGrid с результатами поиска");
      }
    }

    private void ResultsDataGrid_RequestBringIntoView(object sender, RequestBringIntoViewEventArgs e)
    {
      e.Handled = true;
    }

    public void SetItemSourse(List<SearchResult> items)
    {
      ResultsDataGrid.ItemsSource = items;
    }

  }
}

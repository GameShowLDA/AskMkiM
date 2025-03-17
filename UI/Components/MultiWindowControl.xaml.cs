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
using UI.Components.SearchControls;
using UI.Controls.Search;
using static Utilities.LoggerUtility;

namespace UI.Components
{
  /// <summary>
  /// Логика взаимодействия для MultiWindowControl.xaml
  /// </summary>
  public partial class MultiWindowControl : UserControl
  {
    public MultiWindowControl()
    {
      InitializeComponent();
    }
    /// <summary>
    /// Добавляет новый MultiEditorControl в контейнер.
    /// </summary>
    public void OpenFileInEditor(string filePath)
    {
      if (MultiEditor == null)
      {
        MessageBox.Show("Редактор не инициализирован!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        LogError("Редактор не инициализирован");
        return;
      }
      MultiEditor.OpenFile(filePath);
    }

    /// <summary>
    /// Обрабатывает перемещение разделителя GridSplitter для изменения размера области результатов поиска.
    /// </summary>
    private void GridSplitter_DragDelta(object sender, DragDeltaEventArgs e)
    {
      if (SearchResultsRow == null)
      {
        return;
      }

      double totalHeight = ActualHeight; 
      double editorsHeight = MultiEditor.ActualHeight; 
      double minEditorsHeight = 100;
      double splitterHeight = MultiWindowSplitter.ActualHeight;
      SearchResultsRow.MinHeight = 35;

      double maxSearchResultsHeight = totalHeight - minEditorsHeight - splitterHeight;
      double newSearchHeight = totalHeight - editorsHeight - splitterHeight;

      if (newSearchHeight > maxSearchResultsHeight)
      {
        newSearchHeight = maxSearchResultsHeight;
      }

      SearchResultsRow.Height = new GridLength(newSearchHeight, GridUnitType.Pixel);
    }

    /// <summary>
    /// Скрывает или отображает панель результатов поиска при нажатии на кнопку "свернуть".
    /// </summary>
    private void minimizeButton_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      if (ResultsDataGrid.Visibility == Visibility.Visible)
      {
        SearchResultsRow.Height = new GridLength(35);
        ResultsDataGrid.Visibility = Visibility.Collapsed;
        MultiWindowSplitter.Visibility = Visibility.Collapsed;
      }
      else
      {
        SearchResultsRow.Height = new GridLength(200);
        ResultsDataGrid.Visibility = Visibility.Visible;
        MultiWindowSplitter.Visibility = Visibility.Visible;
      }
    }

    private void exitButton_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      SearchResultsRow.Height = new GridLength(0);
      SearchResultsRow.MinHeight = 0;
      SearchResults.Visibility = Visibility.Collapsed;
      MultiWindowSplitter.Visibility = Visibility.Collapsed;
    }


    public void AddControl(string name, UserControl userControl)
    {
      MultiEditor.AddControl(name, userControl);
    }

    public void CreateNewFile()
    {
      if (MultiEditor == null)
      {
        MessageBox.Show("Редактор не инициализирован!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        LogError("Редактор не инициализирован");
        return;
      }
      MultiEditor.CreateNewFile();
    }

    public void SaveFile()
    {
      if (MultiEditor == null)
      {
        MessageBox.Show("Редактор не инициализирован!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error); 
        LogError("Редактор не инициализирован");
        return;
      }
      MultiEditor.SaveFile();
    }

    public void SaveFileAs()
    {
      if (MultiEditor == null)
      {
        MessageBox.Show("Редактор не инициализирован!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        LogError("Редактор не инициализирован");
        return;
      }
      MultiEditor.SaveFileAs();
    }

    public void PrintFile()
    {
      if (MultiEditor == null)
      {
        MessageBox.Show("Редактор не инициализирован!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        LogError("Редактор не инициализирован");
        return;
      }
      MultiEditor.PrintFile();
    }

    public void SearchData(string searchText, bool? wholeWord, bool? caseWord, int searchArea, string searchParameters)
    {
      if (MultiEditor == null)
      {
        MessageBox.Show("Редактор не инициализирован!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        LogError("Редактор не инициализирован");

        return;
      }
      LogInformation("Начат поиск по тексту");
      MultiEditor.SearchData(searchText, wholeWord, caseWord, searchArea, searchParameters);
    }

    public void OnSearchWindowClosing()
    {
      MultiEditor.OnSearchWindowClosing();
    }

    public void ShowSearchResults(string searchText, Dictionary<string, Dictionary<int, string>> results)
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
      var searchResultsText = $"Результаты поиска по \"{searchText}\". Найдено {items.Count} строк";
      searchResultsTextBlock.Text = searchResultsText;
      LogInformation(searchResultsText);
      MultiWindowSplitter.Visibility = Visibility.Visible;
      SearchResultsRow.Height = new GridLength(200);
      SearchResults.Visibility = Visibility.Visible;
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

    public override void OnApplyTemplate()
    {
      base.OnApplyTemplate();

      if (MultiEditor != null)
      {
        MultiEditor.SearchResultsReady += ShowSearchResults;
      }
    }

    private void ResultsDataGrid_RequestBringIntoView(object sender, RequestBringIntoViewEventArgs e)
    {
      e.Handled = true;
    }
  }
}

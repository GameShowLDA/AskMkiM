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
        return;
      }
      MultiEditor.OpenFile(filePath);
    }

    /// <summary>
    /// Обрабатывает перемещение разделителя GridSplitter для изменения размера области результатов поиска.
    /// </summary>
    private void GridSplitter_DragDelta(object sender, DragDeltaEventArgs e)
    {
      if (SearchResultsRow == null) return; // Предотвращает исключение

      double newHeight = SearchResultsRow.Height.Value - e.VerticalChange;

      if (newHeight > 50) // Минимальная высота области результатов поиска
      {
        SearchResultsRow.Height = new GridLength(newHeight);
      }
    }

    /// <summary>
    /// Показывает панель результатов поиска.
    /// </summary>
    public void ShowSearchResults(string searchText)
    {
      SearchResultsRow.Height = new GridLength(200);
      SearchResults.Visibility = Visibility.Visible;
      ShowResultsPanel.Visibility = Visibility.Collapsed;
    }

    /// <summary>
    /// Скрывает панель результатов поиска.
    /// </summary>
    public void HideSearchResults()
    {
      SearchResultsRow.Height = new GridLength(0);
      SearchResults.Visibility = Visibility.Collapsed;
      ShowResultsPanel.Visibility = Visibility.Visible;
    }

    /// <summary>
    /// Разворачивает панель результатов при клике на кнопку "Результаты поиска".
    /// </summary>
    private void ShowResultsPanel_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
      ShowSearchResults("Последний запрос");
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
        return;
      }
      MultiEditor.CreateNewFile();
    }

    public void SaveFile()
    {
      if (MultiEditor == null)
      {
        MessageBox.Show("Редактор не инициализирован!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        return;
      }
      MultiEditor.SaveFile();
    }

    public void SaveFileAs()
    {
      if (MultiEditor == null)
      {
        MessageBox.Show("Редактор не инициализирован!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        return;
      }
      MultiEditor.SaveFileAs();
    }

    public void PrintFile()
    {
      if (MultiEditor == null)
      {
        MessageBox.Show("Редактор не инициализирован!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        return;
      }
      MultiEditor.PrintFile();
    }

    public void SearchData(string searchText, bool? wholeWord, bool? caseWord, int searchArea, string searchParameters)
    {
      if (MultiEditor == null)
      {
        MessageBox.Show("Редактор не инициализирован!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        return;
      }
      MultiEditor.SearchData(searchText, wholeWord, caseWord, searchArea, searchParameters);
    }

    /// <summary>
    /// Обработчик поиска текста в активном редакторе.
    /// </summary>
    private void SearchWindow_SearchTextHandler(string searchText, bool? wholeWord, bool? caseWord, int searchArea, string searchParameters)
    {
      if (MultiEditor == null)
      {
        MessageBox.Show("Редактор не инициализирован!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        return;
      }
      MultiEditor.SearchData(searchText, wholeWord, caseWord, searchArea, searchParameters);
    }

    public void OnSearchWindowClosing()
    {
      MultiEditor.OnSearchWindowClosing();
    }

    public void ShowSearchResults(Dictionary<string, Dictionary<int, string>> results)
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
      SearchResultsRow.Height = new GridLength(200);
      SearchResults.Visibility = Visibility.Visible;
      ShowResultsPanel.Visibility = Visibility.Collapsed;
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

    public override void OnApplyTemplate()
    {
      base.OnApplyTemplate();

      if (MultiEditor != null)
      {
        MultiEditor.SearchResultsReady += ShowSearchResults;
      }
    }

  }
}

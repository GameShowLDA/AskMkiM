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
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using UI.Components.Invoke;
using UI.Components.SearchControls;
using UI.Controls.Search;
using UI.Controls.TextEditor;
using static Utilities.LoggerUtility;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;
using UserControl = System.Windows.Controls.UserControl;

namespace UI.Components
{
  /// <summary>
  /// Логика взаимодействия для MultiWindowControl.xaml
  /// </summary>
  public partial class MultiWindowControl : UserControl
  {

    List<OpenFileButton> openPages = new List<OpenFileButton>();
    List<UserControl> userControls = new List<UserControl>();

    public MultiWindowControl()
    {
      InitializeComponent();
      EventAggregator.TextEditorClosing += OnTextEditorClosig;
    }

    private void OnTextEditorClosig(bool textEditorClosing, string textEditorName)
    {
      if (textEditorClosing)
      {
        RemoveCorrespondingSearchDataGrid(textEditorName);
        CloseSearchResultsActions();
      }
    }

    private void CloseSearchResultsActions()
    {
      if (openPages.Count <= 0 && userControls.Count <= 0)
      {
        CloseSearchResults();
        EventAggregator.RaiseCloseSearchWindow();
      }
    }

    /// <summary>
    /// Удаляет DataGrid с результатами поиска, соответствующий закрытому текстовому редактору.
    /// </summary>
    /// <param name="textEditorName">Имя закрываемого текстового редактора.</param>
    private void RemoveCorrespondingSearchDataGrid(string textEditorName)
    {
      var foundPage = openPages.FirstOrDefault(page => page.Text == textEditorName);
      if (foundPage != null)
      {
        int index = openPages.IndexOf(foundPage);
        if (userControls.Count > index && userControls[index] is SearchDataGrid activeDataGrid)
        {
          RemoveControl(foundPage, activeDataGrid);
        }
      }
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
      if (searchDataGrid.Visibility == Visibility.Visible)
      {
        SearchResultsRow.Height = new GridLength(35);
        searchDataGrid.Visibility = Visibility.Collapsed;
        MultiWindowSplitter.Visibility = Visibility.Collapsed;
      }
      else
      {
        SearchResultsRow.Height = new GridLength(200);
        searchDataGrid.Visibility = Visibility.Visible;
        MultiWindowSplitter.Visibility = Visibility.Visible;
      }
    }

    private void exitButton_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
      CloseSearchResults();
    }

    private void CloseSearchResults()
    {
      SearchResultsRow.Height = new GridLength(0);
      SearchResultsRow.MinHeight = 0;
      SearchResults.Visibility = Visibility.Collapsed;
      MultiWindowSplitter.Visibility = Visibility.Collapsed;
    }

    public TextEditorUI GetActiveTextEditor()
    {
      return MultiEditor.GetActiveTextEditor();
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
      LogInformation($"Начат поиск по тексту. Искомый текст: {searchText}");
      MultiEditor.SearchData(searchText, wholeWord, caseWord, searchArea, searchParameters);
    }

    public void OnSearchWindowClosing()
    {
      MultiEditor.OnSearchWindowClosing();
    }

    public void ShowSearchResults(string searchText, Dictionary<string, List<SearchResult>> results)
    {
      int totalCount = 0;
      SearchResultsTopPanel.Children.Clear();
      ContentPanel.Children.Clear();
      openPages.Clear();
      userControls.Clear();
      searchResultsTextBlock.Text = string.Empty;
      foreach (var file in results)
      {
        List<SearchResult> items = new List<SearchResult>();
        var searchResultsForFile = new SearchDataGrid();
        foreach (var occurrence in file.Value)
        {

          items.Add(new SearchResult(
            occurrence.StartOffset, 
            occurrence.Length, 
            occurrence.LineNumber, 
            occurrence.WordStartOffset, 
            occurrence.SubstringFromWord, 
            file.Key));
          
        }
        totalCount += items.Count;
        var searchResultsText = $"Результаты поиска по \"{searchText}\" в файле \"{file.Key}\". Найдено {items.Count} строк";
        LogInformation(searchResultsText);
        AddControlInSearchArea(file.Key, searchResultsForFile);
        searchResultsForFile.SetItemSourse(items);
      }

      string overallSearchText = $"Результаты поиска по \"{searchText}\". Всего найдено {totalCount} строк";
      searchResultsTextBlock.Text = overallSearchText;
      MultiWindowSplitter.Visibility = Visibility.Visible;
      SearchResultsRow.Height = new GridLength(200);
      SearchResults.Visibility = Visibility.Visible;
    }

    public override void OnApplyTemplate()
    {
      base.OnApplyTemplate();

      if (MultiEditor != null)
      {
        MultiEditor.SearchResultsReady += ShowSearchResults;
      }
    }

    /// <summary>
    /// Добавляет элемент управления и кнопку в соответствующие панели.
    /// </summary>
    /// <param name="header">Заголовок для кнопки.</param>
    /// <param name="control">Элемент управления для отображения.</param>
    public void AddControlInSearchArea(string header, UserControl control, string description = null)
    {
      OpenFileButton tabButton = new OpenFileButton();
      tabButton.Header.Text = header;
      if (description != null)
      {
        tabButton.Description = description;

        foreach (OpenFileButton page in openPages)
        {
          if (page.Description == description)
          {
            var index = openPages.IndexOf(page);
            var userControl = userControls[index];
            ShowControl(userControl, page);
            return;
          }
        }
      }
      else
      {
        foreach (OpenFileButton page in openPages)
        {
          if (page.Header.Text == header)
          {
            var index = openPages.IndexOf(page);
            var userControl = userControls[index];
            ShowControl(userControl, page);

            return;
          }
        }
      }

      tabButton.PreviewMouseDown += (s, e) => ShowControl(control, tabButton);
      tabButton.GetCloseButton().PreviewMouseDown += (s, e) => RemoveControl(tabButton, control);
      tabButton.MouseDown += (s, e) =>
      {
        if (e.ChangedButton == MouseButton.Middle)
        {
          RemoveControl(tabButton, control);
        }
      };

      openPages.Add(tabButton);
      userControls.Add(control);

      try
      {
        ContentPanel.Children.Add(control);
        SearchResultsTopPanel.Children.Add(tabButton);
      }
      finally
      {
        ShowControl(control, tabButton);
      }
    }

    /// <summary>
    /// Отображает указанный элемент управления, скрывая остальные.
    /// </summary>
    /// <param name="control">Элемент управления для отображения.</param>
    private void ActivePage(OpenFileButton control)
    {
      foreach (OpenFileButton child in SearchResultsTopPanel.Children)
      {
        if (control == child)
        {
          child.Background = (Brush)Application.Current.Resources["ActiveBorderSolidColorBrush"];
        }
        else
        {
          child.Background = (Brush)Application.Current.Resources["SecondarySolidColorBrush"];
        }
      }
    }

    /// <summary>
    /// Отображает указанный элемент управления, скрывая остальные.
    /// </summary>
    /// <param name="control">Элемент управления для отображения.</param>
    private void ShowControl(UserControl control, OpenFileButton openPage)
    {
      foreach (UIElement child in ContentPanel.Children)
      {
        child.Visibility = child == control ? Visibility.Visible : Visibility.Collapsed;
      }

      ActivePage(openPage);
    }

    /// <summary>
    /// Удаляет указанный элемент управления и соответствующую вкладку.
    /// </summary>
    /// <param name="tabButton">Вкладка для удаления.</param>
    /// <param name="control">Элемент управления для удаления.</param>
    private void RemoveControl(OpenFileButton tabButton, UserControl control)
    {
      if (openPages.Contains(tabButton) && userControls.Contains(control))
      {
        int index = ContentPanel.Children.IndexOf(control);

        if (index > 0)
        {
          index--;
        }

        openPages.Remove(tabButton);
        userControls.Remove(control);

        SearchResultsTopPanel.Children.Remove(tabButton);
        ContentPanel.Children.Remove(control);

        if (ContentPanel.Children.Count > 0)
        {
          ShowControl(userControls[index], openPages[index]);
        }
        CloseSearchResultsActions();

      }
    }
  }
}

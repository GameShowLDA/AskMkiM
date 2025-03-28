using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using UI.Components.Invoke;
using UI.Controls.TextEditor;
using Application = System.Windows.Application;
using UserControl = System.Windows.Controls.UserControl;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using AppConfig;
using UI.Components.SearchControls;
using UI.Components.MultiEditorMethods;

namespace UI.Components
{
  /// <summary>
  /// Логика взаимодействия для MultiEditorControl.xaml
  /// </summary>
  public partial class MultiEditorControl : UserControl, IFileManagerControl
  {
    private int _clickCount = 0;
    private DispatcherTimer _clickTimer;

    public event Action<string, bool?, Dictionary<string, List<SearchResult>>> SearchResultsReady;
    FileManager fileManager;

    public void InitializeFileManager()
    {
      fileManager = new FileManager(this);
    }

    public MultiEditorControl()
    {
      InitializeComponent();
      _clickTimer = new DispatcherTimer
      {
        Interval = TimeSpan.FromMilliseconds(300)
      };

      _clickTimer.Tick += (s, e) =>
      {
        _clickCount = 0;
        _clickTimer.Stop();
      };

      this.KeyDown += MultiWindowControl_KeyDown;
      EventAggregator.FoundTextSelectRow += OnFoundTextSelectRow;
      InitializeFileManager();
    }

    private void OnFoundTextSelectRow(string fileName, int lineNumber, int startOffset, string lineText)
    {
      TextSearchManager textSearchMethods = new TextSearchManager(fileManager, this);
      textSearchMethods.GetLineOccurrences(fileName, lineNumber, startOffset, lineText);
    }

    private void TopPanel_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
      _clickCount++;

      if (_clickCount == 1)
      {
        _clickTimer.Start();
      }
      else if (_clickCount == 2)
      {
        _clickTimer.Stop();
        _clickCount = 0;
        CreateNewFile();
      }
    }

    public TextEditorUI GetActiveTextEditor()
    {
      var activePage = fileManager.OpenPages.FirstOrDefault(page =>
                        page.Background == (Brush)Application.Current.Resources["ActiveBorderSolidColorBrush"]);
      if (activePage != null)
      {
        int index = fileManager.OpenPages.IndexOf(activePage);
        if (fileManager.UserControls[index] is TextEditorUI activeEditor)
        {
          return activeEditor;
        }
      }
      return null;
    }

    /// <summary>
    /// Добавляет элемент управления и кнопку в соответствующие панели.
    /// </summary>
    /// <param name="header">Заголовок для кнопки.</param>
    /// <param name="control">Элемент управления для отображения.</param>
    public void AddControl(string header, UserControl control, string description = null)
    {
      var controlManager = new ControlManager(this.fileManager, this);
      controlManager.AddControl(header, control, description);
    }

    // TODO: FileManager
    public void OpenFile(string path)
    {
      fileManager.OpenFile(path);
    }

    /// <summary>
    /// Создаёт новый файл.
    /// </summary>
    public void CreateNewFile()
    {
      fileManager.CreateNewFile();
    }

    public bool SaveFileAs()
    {
      var saveFileManager = new SaveFileManager(fileManager);
      return saveFileManager.SaveFileAs();
    }

    /// <summary>
    /// Удаляет указанный элемент управления и соответствующую вкладку.
    /// </summary>
    /// <param name="tabButton">Вкладка для удаления.</param>
    /// <param name="control">Элемент управления для удаления.</param>
    private void RemoveControl(OpenFileButton tabButton, UserControl control)
    {
      var controlManager = new ControlManager(fileManager, this);
      controlManager.RemoveControl(tabButton, control);
    }

    private void MultiWindowControl_KeyDown(object sender, KeyEventArgs e)
    {
      Console.WriteLine($"e.Key = {e.Key}; e.SystemKey = {e.SystemKey}; Keyboard.Modifiers = {Keyboard.Modifiers}");

      if (e.Key == Key.System && e.SystemKey == Key.X && Keyboard.Modifiers == ModifierKeys.Alt)
      {
        var activeTab = fileManager.OpenPages.FirstOrDefault(page => 
          page.Background == (Brush)Application.Current.Resources["ActiveBorderSolidColorBrush"]);
        if (activeTab != null)
        {
          int index = fileManager.OpenPages.IndexOf(activeTab);
          RemoveControl(activeTab, fileManager.UserControls[index]);
        }
      }
    }

    public void OnSearchWindowClosing()
    {
      var textSearchManager = new TextSearchManager(fileManager, this);
      textSearchManager.OnSearchWindowClosing();
    }


    public bool SaveFile()
    {
      var activeTab = fileManager.OpenPages.FirstOrDefault(page => 
        page.Background == (Brush)Application.Current.Resources["ActiveBorderSolidColorBrush"]);
      var saveFileManager = new SaveFileManager(this.fileManager);
      return saveFileManager.SaveFile(activeTab);
    }

    public void PrintFile()
    {
      PrintFileManager.PrintFile(fileManager.OpenPages, fileManager.UserControls);
    }

    /// <summary>
    /// Выполняет поиск по тектсу.
    /// </summary>
    /// <param name="searchText">Текст, который мы ищем.</param>
    /// <param name="wholeWord">Если true - ищем только слово целиком, false - ищем все вхождения заданного текста.</param>
    /// <param name="caseWord">Если true - учитываем регистр, false - не учитываем.</param>
    /// <param name="searchArea">Параметры поиска: найти  далее, найти предыдущее, найти все.</param>
    /// <param name="searchParameters">Область поиска: поиск в текущем документе, во всех открытых документах, в файле.</param>
    public async Task SearchData(string searchText, bool? wholeWord, bool? caseWord, int searchArea, string searchParameters)
    {
      TextSearchManager textSearchMethods = new TextSearchManager(fileManager, this);
      await textSearchMethods.SearchData(searchText, wholeWord, caseWord, searchArea, searchParameters);
    }

    
  }
}

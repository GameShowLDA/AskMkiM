using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Threading;
using ICSharpCode.AvalonEdit;
using UI.Components.Invoke;
using UI.Controls.TextEditor;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;
using PrintDialog = System.Windows.Controls.PrintDialog;
using UserControl = System.Windows.Controls.UserControl;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using AppConfig;
using System.Text.RegularExpressions;
using UI.Components.SearchControls;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TextBox;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;
using System;
using System.Collections.Generic;

namespace UI.Components
{
  /// <summary>
  /// Логика взаимодействия для MultiEditorControl.xaml
  /// </summary>
  public partial class MultiEditorControl : UserControl
  {
    List<OpenFileButton> openPages = new List<OpenFileButton>();
    List<UserControl> userControls = new List<UserControl>();

    Dictionary<string, string> filePaths = new Dictionary<string, string>();

    private List<SearchResult> foundResults = new List<SearchResult>();
    private int currentIndex = -1;
    private TextMarkerService textMarkerService;

    string _searchText;
    string _fullText;
    bool? _wholeWord;
    bool? _caseWord;
    string _searchParameters;
    int _searchArea;

    bool hasChanged;

    public event Action SelectFileForSearch;

    private int _clickCount = 0;
    private DispatcherTimer _clickTimer;

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
    }

    private void InitializeTextMarkerService()
    {
      var activeTab = openPages.FirstOrDefault(page => page.Background == (Brush)Application.Current.Resources["ActiveBorderSolidColorBrush"]);
      int index = openPages.IndexOf(activeTab);
      if (userControls[index] is TextEditorUI)
      {
        var textEditorUI = userControls[index] as TextEditorUI;
        var textEditor = textEditorUI.TextEditor;
        textMarkerService = new TextMarkerService(textEditor.Document, textEditor);
        textEditor.TextArea.TextView.BackgroundRenderers.Add(textMarkerService);
        textEditor.TextArea.TextView.LineTransformers.Add(textMarkerService);
      }
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

    /// <summary>
    /// Добавляет элемент управления и кнопку в соответствующие панели.
    /// </summary>
    /// <param name="header">Заголовок для кнопки.</param>
    /// <param name="control">Элемент управления для отображения.</param>
    public void AddControl(string header, UserControl control, string description = null)
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
        TopPanel.Children.Add(tabButton);
      }
      finally
      {
        ShowControl(control, tabButton);
      }
    }

    public void OpenFile(string path)
    {
      var nameFile = GetNameFile(path);
      if (string.IsNullOrEmpty(nameFile))
      {
        MessageBox.Show("Ошибка", "Ошибка при открытии файла");
        return;
      }

      try
      {
        string fileContent = System.IO.File.ReadAllText(path);

        var textEditor = new TextEditorUI();
        textEditor.Text = fileContent;

        AddControl(nameFile, textEditor);
        if (!filePaths.ContainsKey(nameFile))
        {
          filePaths.Add(nameFile, path);
        }
        InitializeTextMarkerService();
      }
      catch (Exception ex)
      {
        MessageBox.Show($"Ошибка при чтении файла: {ex.Message}", "Ошибка");
      }
    }

    /// <summary>
    /// Создаёт новый файл.
    /// </summary>
    public void CreateNewFile()
    {
      var controlName = "Новый";
      var counter = 0;
      while (filePaths.ContainsKey(controlName))
      {
        counter++;
        if (controlName != "Новый")
        {
          controlName = controlName.Remove(controlName.Length - (counter - 1).ToString().Length, (counter - 1).ToString().Length);
        }
        controlName += $"{counter}";
      }
      AddControl(controlName, new TextEditorUI() /*{ Text  = "Новый файл"}*/);
      filePaths.Add(controlName, string.Empty);
      InitializeTextMarkerService();
    }

    /// <summary>
    /// Получает имя файла по пути к файлу.
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    private string GetNameFile(string path)
    {
      if (string.IsNullOrEmpty(path))
      {
        return string.Empty;
      }
      try
      {
        return System.IO.Path.GetFileName(path).ToString();
      }
      catch (Exception ex)
      {
        return string.Empty;
      }
    }

    /// <summary>
    /// Отображает указанный элемент управления, скрывая остальные.
    /// </summary>
    /// <param name="control">Элемент управления для отображения.</param>
    private void ActivePage(OpenFileButton control)
    {
      foreach (OpenFileButton child in TopPanel.Children)
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

      EventAggregator.RaiseTextEditorActive(control is TextEditorUI);
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
        var result = MessageBoxResult.No;
        var saveFileResult = false;
        int index = ContentPanel.Children.IndexOf(control);
        if (control is TextEditorUI)
        {
          SaveFileDialog(ref result, ref saveFileResult, index);
        }
        if (saveFileResult == true || !(control is TextEditorUI) || result == MessageBoxResult.No)
        {
          if (index > 0)
          {
            index--;
          }
          EventAggregator.RaiseTextEditorClosing(control is TextEditorUI);


          openPages.Remove(tabButton);
          userControls.Remove(control);

          TopPanel.Children.Remove(tabButton);
          ContentPanel.Children.Remove(control);

          if (ContentPanel.Children.Count > 0)
          {
            ShowControl(userControls[index], openPages[index]);
          }
        }
      }
    }

    private void SaveFileDialog(ref MessageBoxResult result, ref bool saveFileResult, int index)
    {
      var needToSave = CompareFiles(openPages[index]);
      if (needToSave)
      {
        result = MessageBox.Show(
            $"Сохранить файл {openPages[index].Text} перед закрытием?",
            "Подтверждение",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);
        if (result == MessageBoxResult.Yes)
        {
          saveFileResult = SaveFile(openPages[index]);
        }
      }
    }

    private void MultiWindowControl_KeyDown(object sender, KeyEventArgs e)
    {
      Console.WriteLine($"e.Key = {e.Key}; e.SystemKey = {e.SystemKey}; Keyboard.Modifiers = {Keyboard.Modifiers}");

      if (e.Key == Key.System && e.SystemKey == Key.X && Keyboard.Modifiers == ModifierKeys.Alt)
      {
        var activeTab = openPages.FirstOrDefault(page => page.Background == (Brush)Application.Current.Resources["ActiveBorderSolidColorBrush"]);
        if (activeTab != null)
        {
          int index = openPages.IndexOf(activeTab);
          RemoveControl(activeTab, userControls[index]);
        }
      }
    }

    private bool CompareFiles(OpenFileButton openPage)
    {
      var activeTab = openPages.FirstOrDefault(page => page.Background == (Brush)Application.Current.Resources["ActiveBorderSolidColorBrush"]);
      if (activeTab != null)
      {
        var fileName = activeTab.Text;
        if (filePaths[fileName] == string.Empty)
        {
          return true;
        }
        else
        {
          var filePath = filePaths[fileName];
          var content = File.ReadAllText(filePath);
          int index = openPages.IndexOf(activeTab);

          if (userControls[index] is TextEditorUI)
          {
            var textEditor = userControls[index] as TextEditorUI;
            return content != textEditor.Text;
          }

          return false;
        }
      }
      return false;
    }

    #region Сохранение файлов

    public bool SaveFile()
    {
      var activeTab = openPages.FirstOrDefault(page => page.Background == (Brush)Application.Current.Resources["ActiveBorderSolidColorBrush"]);
      return SaveFile(activeTab);
    }

    private bool SaveFile(OpenFileButton activeTab)
    {
      if (activeTab != null)
      {
        var fileName = activeTab.Text;
        if (filePaths[fileName] == string.Empty)
        {
          return SaveFileAs();
        }
        else
        {
          var filePath = filePaths[fileName];
          return SaveDataFromTextEditor(activeTab, filePath);
        }
      }
      return false;
    }

    // TODO: добавить сохранение файлов при закрытии приложения
    public bool SaveFileAs()
    {
      using (SaveFileDialog saveFileDialog = new SaveFileDialog())
      {
        var activeTab = openPages.FirstOrDefault(page => page.Background == (Brush)Application.Current.Resources["ActiveBorderSolidColorBrush"]);
        if (activeTab != null)
        {
          saveFileDialog.Filter = "Text Files (*.txt)|*.txt|RTF Files (*.rtf)|*.rtf";
          saveFileDialog.Title = "Сохранить файл как";
          saveFileDialog.FileName = activeTab.Text;
          saveFileDialog.FileName = Path.GetFileNameWithoutExtension(activeTab.Text);
          if (saveFileDialog.ShowDialog() == DialogResult.OK)
          {
            string filePath = saveFileDialog.FileName;
            SaveDataFromTextEditor(activeTab, filePath);
            RenamePage(activeTab, filePath);
            var fileName = Path.GetFileName(filePath);
            if (!filePaths.ContainsKey(fileName))
            {
              filePaths.Add(fileName, filePath);
            }
            else
            {
              filePaths[fileName] = filePath;
            }
            return true;
          }
          else
          {
            return false;
          }
        }
        return false;
      }
    }

    private bool SaveDataFromTextEditor(OpenFileButton activeTab, string filePath)
    {
      string fileData = string.Empty;

      int index = openPages.IndexOf(activeTab);
      if (userControls[index] is TextEditorUI)
      {
        var textEditor = userControls[index] as TextEditorUI;
        fileData = textEditor.Text;
        File.WriteAllText(filePath, fileData);
        //LoggerService.LogInformation($"Файл {filePath} сохранен");
        MessageBox.Show($"Файл {filePath} сохранен");
        return true;
      }
      return false;
    }

    #endregion

    private void RenamePage(OpenFileButton activeTab, string filePath)
    {
      var acivePage = openPages.FirstOrDefault(p => p == activeTab);
      if (acivePage != null)
      {
        activeTab.Header.Text = System.IO.Path.GetFileName(filePath);
      }
    }

    #region Печать файлов

    public void PrintFile()
    {
      var activeTab = openPages.FirstOrDefault(page => page.Background == (Brush)Application.Current.Resources["ActiveBorderSolidColorBrush"]);
      PrintDialog printDialog = new PrintDialog();
      FlowDocument flowDocument = new FlowDocument();


      if (printDialog.ShowDialog() == true)
      {
        int index = openPages.IndexOf(activeTab);

        if (userControls[index] is TextEditorUI)
        {
          var textEditor = userControls[index] as TextEditorUI;
          flowDocument.Blocks.Add(new Paragraph(new Run(textEditor.Text)));
          IDocumentPaginatorSource idocument = flowDocument;
          printDialog.PrintDocument(idocument.DocumentPaginator, "Печать документа");
        }
      }
    }

    #endregion

    #region Поиск по тексту

    // TODO: поиск по тексту делать тут
    /// <summary>
    /// Выполняет поиск по тектсу.
    /// </summary>
    /// <param name="searchText">Текст, который мы ищем.</param>
    /// <param name="wholeWord">Если true - ищем только слово целиком, false - ищем все вхождения заданного текста.</param>
    /// <param name="caseWord">Если true - учитываем регистр, false - не учитываем.</param>
    /// <param name="searchArea">Параметры поиска: найти  далее, найти предыдущее, найти все.</param>
    /// <param name="searchParameters">Область поиска: поиск в текущем документе, во всех открытых документах, в файле.</param>
    public void SearchData(string searchText, bool? wholeWord, bool? caseWord, int searchArea, string searchParameters)
    {
      if (searchArea == 2)
      {
        FindInFile();
        searchArea = 0;
      }
      var fullText = GetText();
      if (!string.Equals(searchText, _searchText))
      {
        ClearHighlights();
      }
      InitializeSearch(fullText, searchText, wholeWord, caseWord, searchArea, searchParameters);
      if (hasChanged)
      {
        FindAllOccurrences(fullText, searchText, wholeWord, caseWord, searchArea);
      }
      else
      {
        if (searchParameters == "FindNext")
        {
          NextOccurrence();
        }
        if (searchParameters == "FindPrevious")
        {
          PreviousOccurrence();
        }
        if (searchParameters == "FindAll")
        {
          MessageBox.Show("Когда-нибудь тут будет нормальная реализация", "Заглушка");
        }
      }
    }

    private string GetText()
    {
      var activeTab = openPages.FirstOrDefault(page => page.Background == (Brush)Application.Current.Resources["ActiveBorderSolidColorBrush"]);
      int pageIndex = openPages.IndexOf(activeTab);
      // TODO: задавать тут область поиска текста
      string fullText = string.Empty;

      if (userControls[pageIndex] is TextEditorUI textEditor)
      {
        fullText = textEditor.Text;
      }
      return fullText;
    }

    /// <summary>
    /// Инициализирует параметры поиска по тектсу.
    /// </summary>
    /// <param name="searchText">Текст, который мы ищем.</param>
    /// <param name="wholeWord">Если true - ищем только слово целиком, false - ищем все вхождения заданного текста.</param>
    /// <param name="caseWord">Если true - учитываем регистр, false - не учитываем.</param>
    /// <param name="searchArea">Параметры поиска: найти  далее, найти предыдущее, найти все.</param>
    /// <param name="searchParameters">Область поиска: поиск в текущем документе, во всех открытых документах, в файле.</param>
    private void InitializeSearch(string fullText, string searchText, bool? wholeWord, bool? caseWord, int searchArea, string searchParameters)
    {
      if ((!string.Equals(_fullText, fullText))
              || (!string.Equals(_searchText, searchText)
              || _wholeWord != wholeWord
              || _caseWord != caseWord
              || _searchArea != searchArea))
      {
        _fullText = fullText;
        _searchText = searchText;
        _wholeWord = wholeWord;
        _caseWord = caseWord;
        _searchArea = searchArea;
        _searchParameters = searchParameters;
        hasChanged = true;
      }
      else
      {
        hasChanged = false;
      }
    }

    public void FindInFile()
    {
      SelectFileForSearch?.Invoke();
    }

    // TODO: доработать поиск с учетом регистра и полных слов
    private void FindAllOccurrences(string fullText, string searchText, bool? wholeWord, bool? caseWord, int searchArea)
    {
      ClearHighlights();

      if (string.IsNullOrEmpty(searchText))
      {
        MessageBox.Show("Введите текст для поиска.");
        return;
      }
      RegexOptions options = caseWord == true ? RegexOptions.None : RegexOptions.IgnoreCase;
      searchText = Regex.Escape(searchText);
      string pattern = wholeWord == true ? $@"\b{searchText}\b" : searchText;
      MatchCollection matches = Regex.Matches(fullText, pattern, options);

      foreach (Match match in matches)
      {
        foundResults.Add(new SearchResult(match.Index, match.Length));
      }

      if (foundResults.Count > 0)
      {
        if (currentIndex == -1)
        {
          currentIndex = 0;
        }
        GoToOccurrence(currentIndex);
      }
      else
      {
        MessageBox.Show("Текст не найден.");
      }
    }

    private void HighlightText(int startOffset, int length)
    {
      var marker = textMarkerService.Create(startOffset, length);
      marker.BackgroundColor = (Color)Application.Current.Resources["ActiveColor"];
      marker.ForegroundColor = Colors.Black;
    }

    /// <summary>
    /// Переход к следующему вхождению.
    /// </summary>
    private void NextOccurrence()
    {
      if (foundResults.Count == 0)
      {
        return;
      }
      textMarkerService.RemoveAll();
      currentIndex = (currentIndex + 1) % foundResults.Count;
      GoToOccurrence(currentIndex);
    }

    /// <summary>
    /// Переход к предыдущему вхождению.
    /// </summary>
    private void PreviousOccurrence()
    {
      if (foundResults.Count == 0) return;

      textMarkerService.RemoveAll();
      currentIndex = (currentIndex - 1 + foundResults.Count) % foundResults.Count;
      GoToOccurrence(currentIndex);
    }

    /// <summary>
    /// Переход к определееному вхождению.
    /// </summary>
    private void GoToOccurrence(int index)
    {
      if (index >= 0 && index < foundResults.Count)
      {
        var activeTab = openPages.FirstOrDefault(page => page.Background == (Brush)Application.Current.Resources["ActiveBorderSolidColorBrush"]);
        int pageIndex = openPages.IndexOf(activeTab);

        if (userControls[pageIndex] is TextEditorUI textEditor)
        {
          var result = foundResults[index];
          HighlightText(result.StartOffset, result.Length);
          int lineNumber = textEditor.Document.GetLineByOffset(result.StartOffset).LineNumber;
          textEditor.ScrollToLine(lineNumber);
          textEditor.Select(result.StartOffset, result.Length);
          textEditor.Focus();
        }
      }
    }

    /// <summary>
    /// Очистка подстветки.
    /// </summary>
    private void ClearHighlights()
    {
      textMarkerService.RemoveAll();
      foundResults.Clear();
      currentIndex = -1;
    }

    /// <summary>
    /// Очистка подстветки.
    /// </summary>
    public void OnSearchWindowClosing()
    {
      ClearHighlights();
      _fullText = string.Empty;
      _searchText = string.Empty;
      _wholeWord = false;
      _caseWord = false;
      _searchArea = 0;
      _searchParameters = string.Empty;
      hasChanged = true;
    }

    #endregion
  }
}

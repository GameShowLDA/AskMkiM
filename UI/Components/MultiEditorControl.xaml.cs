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
    List<int> foundWordStartPositions = new List<int>();

    string _searchText;
    bool? _wholeWord;
    bool? _caseWord;
    string _searchArea;
    int _searchParameters;

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
      if (filePaths.ContainsKey(controlName))
      {
        counter++;
        controlName += $"{counter}";
      }
      AddControl(controlName, new TextEditorUI() /*{ Text  = "Новый файл"}*/);
      filePaths.Add(controlName, string.Empty);
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
    public void SearchData(string searchText, bool? wholeWord, bool? caseWord, int searchArea, string searchParameters)
    {
      InitializeSearch(searchText, wholeWord, caseWord, searchArea, searchParameters);

      switch (searchArea)
      {
        //найти в текущем документе
        case 0:
          FindInThisFile();
          break;
        //во всех открытых файлах
        case 1:
          FindInOpnedFiles();
          break;
        //найти в файле
        case 2:
          FindInFile();
          break;
      }
    }

    /// <summary>
    /// Инициализирует параметры поиска по тектсу.
    /// </summary>
    /// <param name="searchText">Текст, который мы ищем.</param>
    /// <param name="wholeWord">Если true - ищем только слово целиком, false - ищем все вхождения заданного текста.</param>
    /// <param name="caseWord">Если true - учитываем регистр, false - не учитываем.</param>
    /// <param name="searchParameters">Параметры поиска: найти  далее, найти предыдущее, найти все.</param>
    /// <param name="searchArea">Область поиска: поиск в текущем документе, во всех открытых документах, в файле.</param>
    private void InitializeSearch(string searchText, bool? wholeWord, bool? caseWord, int searchParameters, string searchArea)
    {
      if (_searchText == null
              || (!string.Equals(_searchText, searchText)
              || _wholeWord != wholeWord
              || _caseWord != caseWord
              || _searchArea != searchArea
              || _searchParameters != searchParameters))
      {
        _searchText = searchText;
        _wholeWord = wholeWord;
        _caseWord = caseWord;
        _searchParameters = searchParameters;
        _searchArea = searchArea;
        if (foundWordStartPositions.Count > 0)
        {
          foundWordStartPositions.Clear();
        }
      }
    }

    private void FindWordIndexes(TextEditorUI textEditor, string text)
    {
      var regex = $@"{Regex.Escape(_searchText)}";
      if (_wholeWord == true)
      {
        // TODO: неправильно работает 
        regex = $@"\b{Regex.Escape(_searchText)}\b";
      }
      if (_caseWord != true)
      {
        FindMatches(text, regex, RegexOptions.IgnoreCase);
      }
      else
      {
        FindMatches(text, regex, RegexOptions.None);
      }
    }

    private void FindMatches(string text, string regex, RegexOptions options)
    {
      foreach (Match match in Regex.Matches(text, regex, options))
      {
        foundWordStartPositions.Add(match.Index);
      }
    }

    private void FindInThisFile()
    {
      var activeTab = openPages.FirstOrDefault(page => page.Background == (Brush)Application.Current.Resources["ActiveBorderSolidColorBrush"]);
      int index = openPages.IndexOf(activeTab);
      TextEditorUI textEditor = new TextEditorUI();
      if (userControls[index] is TextEditorUI)
      {
        textEditor = userControls[index] as TextEditorUI;
      }
      var text = textEditor.Text;
      if (string.IsNullOrEmpty(textEditor.Text))
      {
        return;
      }
      FindWordIndexes(textEditor, text);
    }

    private void FindInOpnedFiles()
    {
      MessageBox.Show("найти в открытых файлах", "заглушка");
    }
    public void FindInFile()
    {
      SelectFileForSearch?.Invoke();
    }

    #endregion
  }
}

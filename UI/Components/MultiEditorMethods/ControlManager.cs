using AppConfiguration.Base;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using UI.Components.FileComparerControls;
using UI.Components.Invoke;
using UI.Controls.TextEditor;
using UI.Windows.WpfDocking.Windows.Docking;
using static UI.Components.Invoke.OpenFileButton;
using UserControl = System.Windows.Controls.UserControl;

namespace UI.Components.MultiEditorMethods
{
  /// <summary>
  /// Класс для работы с контролами.
  /// </summary>
  public class ControlManager
  {
    private Dictionary<string, (int lineNumber, int lineLength)> _pendingHighlights = new Dictionary<string, (int lineNumber, int lineLength)>();

    internal FileManager fileManager { get; set; }

    private MultiEditorControl multiEditorControl { get; set; }

    /// <summary>
    /// Удаляет указанный элемент управления и соответствующую вкладку.
    /// </summary>
    /// <param name="tabButton">Вкладка для удаления.</param>
    /// <param name="control">Элемент управления для удаления.</param>
    public async Task RemoveControl(OpenFileButton tabButton, UserControl control, bool isTranslation = false)
    {
      if (fileManager.OpenPages.Contains(tabButton) && fileManager.UserControls.Contains(control)
        || control is TextEditorUI && isTranslation == false)
      {
        int index = -1;
        EditorType editorType = null;
        if (control is TextEditorUI)
        {
          if (tabButton.Text == EditorType.TextEditor.ToString())
          {
            editorType = EditorType.TextEditor;
            var container = fileManager.OpenPages.FirstOrDefault(textEditorContainer
              => textEditorContainer.Text == editorType.ToString());
            var containerIndex = fileManager.OpenPages.IndexOf(container);
            if (fileManager.UserControls[containerIndex] is TextEditorContainer foundContainer)
            {
              var foundDockItem = foundContainer.DockManager.DockItems.FirstOrDefault(dockItem => dockItem.Content == control);
              if (foundDockItem != null)
              {
                ShowSaveDialogForControl(foundDockItem);
                return;
              }
            }
          }
        }
        else
        {
          index = multiEditorControl.ContentPanel.Children.IndexOf(control);
        }
        if (control is TextEditorContainer)
        {
          HandleClosingEvents(control, tabButton);
        }

        RemoveTabAndControl(tabButton, control);
        ShowNextTab(index);
        var activeTab = fileManager.OpenPages.FirstOrDefault(page => page.Background == (Brush)Application.Current.Resources["ActiveBorderSolidColorBrush"]);
        if (fileManager.UserControls.OfType<TextEditorContainer>().Count() == 0 || activeTab == null
          || !(fileManager.UserControls[fileManager.OpenPages.IndexOf(activeTab)] is TextEditorContainer))
        {
          EventAggregator.RaiseCloseSearchWindow();
        }
      }
    }

    /// <summary>
    /// Отображает диалоговое окно для сохранения файла, если это требуется.
    /// </summary>
    /// <param name="control">Элемент управления для проверки.</param>
    /// <returns>Возвращает <c>true</c>, если файл был сохранен, <c>false</c> в противном случае.</returns>
    private bool ShowSaveDialogForControl(DockItem control)
    {
      var result = MessageBoxResult.No;
      var saveFileResult = false;
      if (control.Content is TextEditorUI)
      { 
        var saveFileManager = new SaveFileManager(fileManager);
        saveFileManager.SaveFileDialog(ref result, ref saveFileResult, control);
      }

      return saveFileResult;
    }

    /// <summary>
    /// Обрабатывает события закрытия для контрола и вкладки.
    /// </summary>
    /// <param name="control">Элемент управления, который закрывается.</param>
    /// <param name="tabButton">Вкладка, которая будет закрыта.</param>
    private void HandleClosingEvents(UserControl control, OpenFileButton tabButton)
    {
      EventAggregator.RaiseTextEditorContainerClosing(control is TextEditorContainer, tabButton.Text);
    }

    /// <summary>
    /// Удаляет вкладку и контрол из соответствующих коллекций и панелей.
    /// </summary>
    /// <param name="tabButton">Вкладка для удаления.</param>
    /// <param name="control">Элемент управления для удаления.</param>
    private void RemoveTabAndControl(OpenFileButton tabButton, UserControl control)
    {
      fileManager.OpenPages.Remove(tabButton);
      fileManager.UserControls.Remove(control);

      multiEditorControl.TopPanel.Children.Remove(tabButton);
      multiEditorControl.ContentPanel.Children.Remove(control);
    }

    /// <summary>
    /// Показывает следующую вкладку, если она существует.
    /// </summary>
    /// <param name="index">Индекс удаленного контрола.</param>
    private void ShowNextTab(int index)
    {
      if (multiEditorControl.ContentPanel.Children.Count > 0)
      {
        ShowControl(fileManager.UserControls[index > 0 ? index - 1 : 0], fileManager.OpenPages[index > 0 ? index - 1 : 0]);
      }
    }

    /// <summary>
    /// Отображает указанный элемент управления, скрывая остальные.
    /// </summary>
    /// <param name="control">Элемент управления для отображения.</param>
    /// <param name="openPage">Вкладка для отображения плоьзовательского элемента управления.</param>
    public void ShowControl(UserControl control, OpenFileButton openPage)
    {
      foreach (UIElement child in multiEditorControl.ContentPanel.Children)
      {
        child.Visibility = child == control ? Visibility.Visible : Visibility.Collapsed;
      }

      ActivePage(openPage, multiEditorControl);

      if (control is TextEditorUI textEditor)
      {
        string fileName = openPage.Text;
        if (_pendingHighlights.TryGetValue(fileName, out var highlightInfo))
        {
          textEditor.ScrollToLine(highlightInfo.lineNumber);

          int startOffset = textEditor.Document.GetOffset(
              highlightInfo.lineNumber, 1);

          _pendingHighlights.Remove(fileName);
        }
        EventAggregator.RaiseTextEditorActivated(control);

      }

      bool isTextEditorContainer = control is TextEditorContainer;
      EventAggregator.RaiseTextEditorActive(isTextEditorContainer);
      EventAggregator.RaiseActiveEditorChanged(isTextEditorContainer);
    }

    /// <summary>
    /// Добавляет новый контрол в панель и создает соответствующую вкладку.
    /// При этом удаляет все ранее открытые.
    /// </summary>
    /// <param name="header">Заголовок для вкладки.</param>
    /// <param name="control">Контрол, который будет добавлен.</param>
    /// <param name="description">Описание вкладки, если необходимо.</param>
    public void AddControl(string header, UserControl control, TypeWindow tabType, string description = null)
    {
      if (tabType == TypeWindow.DeviceControl)
      {
        RemoveControlsByType(TypeWindow.DeviceControl);
      }

      OpenFileButton tabButton = CreateTabButton(header, description, tabType);

      if (control is FileCompareControl)
      {
        AddFileCompareControl(header, control);
        return;
      }

      if (CheckExistingPage(tabButton, description))
      {
        return;
      }

      ConfigureTabEvents(tabButton, control);
      AddTabAndControl(tabButton, control);
      ShowControl(control, tabButton);
    }

    /// <summary>
    /// Добавляет контрол сравнения файлов в панель и создает соответствующую вкладку.
    /// </summary>
    /// <param name="header">Название вкладки.</param>
    /// <param name="control">Пользовательский элемент сранения файлов.</param>
    private void AddFileCompareControl(string header, UserControl control)
    {
      var fileManager = new FileManager(multiEditorControl);
      var textEditorContainer = fileManager.GetContainer(EditorType.TextEditor);
      if (textEditorContainer == null)
      {
        textEditorContainer = fileManager.CreateContainer(EditorType.TextEditor);
      }

      fileManager.ShowNewDockItem(header, textEditorContainer, control);
    }

    /// <summary>
    /// Удаляет все вкладки и контролы определенного типа.
    /// </summary>
    /// <param name="tabType">Тип вкладки для удаления.</param>
    private void RemoveControlsByType(TypeWindow tabType)
    {
      for (int i = fileManager.OpenPages.Count - 1; i >= 0; i--)
      {
        var tab = fileManager.OpenPages[i];
        if (tab.TabType == tabType)
        {
          var control = fileManager.UserControls[i];
          RemoveControl(tab, control);
        }
      }
    }

    /// <summary>
    /// Создает кнопку вкладки для нового контрола.
    /// </summary>
    /// <param name="header">Заголовок для вкладки.</param>
    /// <param name="description">Описание вкладки.</param>
    /// <returns>Созданная кнопка вкладки.</returns>
    private OpenFileButton CreateTabButton(string header, string description, TypeWindow tabType)
    {
      OpenFileButton tabButton = new OpenFileButton
      {
        TabType = tabType,
      };
      tabButton.Header.Text = header;
      if (description != null)
      {
        tabButton.Description = description;
      }

      return tabButton;
    }

    /// <summary>
    /// Проверяет, существует ли вкладка с таким же описанием или заголовком.
    /// </summary>
    /// <param name="tabButton">Кнопка вкладки для поиска.</param>
    /// <param name="description">Описание вкладки для поиска.</param>
    /// <returns><c>true</c>, если вкладка найдена; в противном случае <c>false</c>.</returns>
    private bool CheckExistingPage(OpenFileButton tabButton, string description)
    {
      if (description != null)
      {
        foreach (OpenFileButton page in fileManager.OpenPages)
        {
          if (page.Description == description)
          {
            var index = fileManager.OpenPages.IndexOf(page);
            var userControl = fileManager.UserControls[index];
            ShowControl(userControl, page);
            return true;
          }
        }
      }
      else
      {
        foreach (OpenFileButton page in fileManager.OpenPages)
        {
          if (page.Header.Text == tabButton.Header.Text)
          {
            var index = fileManager.OpenPages.IndexOf(page);
            var userControl = fileManager.UserControls[index];
            ShowControl(userControl, page);
            return true;
          }
        }
      }

      return false;
    }

    /// <summary>
    /// Настроить обработчики событий для вкладки.
    /// </summary>
    /// <param name="tabButton">Кнопка вкладки.</param>
    /// <param name="control">Контрол, с которым связана вкладка.</param>
    private void ConfigureTabEvents(OpenFileButton tabButton, UserControl control)
    {
      tabButton.PreviewMouseDown += (s, e) => ShowControl(control, tabButton);
      tabButton.GetCloseButton().PreviewMouseDown += (s, e) => RemoveControl(tabButton, control);
      tabButton.MouseDown += (s, e) =>
      {
        if (e.ChangedButton == MouseButton.Middle)
        {
          RemoveControl(tabButton, control);
        }
      };
    }

    /// <summary>
    /// Добавляет вкладку и контрол в соответствующие панели.
    /// </summary>
    /// <param name="tabButton">Кнопка вкладки.</param>
    /// <param name="control">Контрол, который будет добавлен.</param>
    private void AddTabAndControl(OpenFileButton tabButton, UserControl control)
    {
      fileManager.OpenPages.Add(tabButton);
      fileManager.UserControls.Add(control);

      try
      {
        multiEditorControl.ContentPanel.Children.Add(control);
        multiEditorControl.TopPanel.Children.Add(tabButton);
      }
      finally
      {
        ShowControl(control, tabButton);
      }
    }

    /// <summary>
    /// Вовзращает true, если вкладок нет. 
    /// </summary>
    /// <returns></returns>
    public bool GetEmtyControl()
    {
      return fileManager.OpenPages.Count == 0 && fileManager.UserControls.Count == 0;
    }

    /// <summary>
    /// Отображает указанный элемент управления, скрывая остальные.
    /// </summary>
    /// <param name="control">Элемент управления для отображения.</param>
    public void ActivePage(OpenFileButton control, MultiEditorControl multiEditorControl)
    {
      foreach (OpenFileButton child in multiEditorControl.TopPanel.Children)
      {
        if (control.Text == child.Text)
        {
          child.Background = (Brush)Application.Current.Resources["ActiveBorderSolidColorBrush"];
          if (control.Text == "Текстовый редактор")
          {
            EventAggregator.RaiseTranslatorActivated(true);
          }
          else
          {
            EventAggregator.RaiseTranslatorActivated(false);
          }
        }
        else
        {
          child.Background = (Brush)Application.Current.Resources["SecondarySolidColorBrush"];
        }
      }
    }

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="ControlManager"/> с передачей экземпляра <see cref="FileManager"/> и <see cref="MultiEditorControl"/>.
    /// </summary>
    /// <param name="fileManager">Экземпляр <see cref="FileManager"/> для управления файлами.</param>
    /// <param name="multiEditorControl">Экземпляр <see cref="MultiEditorControl"/> для взаимодействия с редактором.</param>
    public ControlManager(FileManager fileManager, MultiEditorControl multiEditorControl)
    {
      this.fileManager = fileManager;
      this.multiEditorControl = multiEditorControl;
    }

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="ControlManager"/> с передачей списка открытых страниц, пользовательских контролов, путей к файлам и экземпляра <see cref="MultiEditorControl"/>.
    /// </summary>
    /// <param name="openPages">Список открытых страниц, представленных кнопками <see cref="OpenFileButton"/>.</param>
    /// <param name="userControls">Список пользовательских контролов, представленных элементами <see cref="UserControl"/>.</param>
    /// <param name="filePaths">Словарь, содержащий пути к файлам, где ключ — имя файла, а значение — путь к файлу.</param>
    /// <param name="multiEditorControl">Экземпляр <see cref="MultiEditorControl"/> для взаимодействия с редактором.</param>
    public ControlManager(ObservableCollection<OpenFileButton> openPages, ObservableCollection<UserControl> userControls, Dictionary<string, string> filePaths, MultiEditorControl multiEditorControl)
    {
      this.fileManager = new FileManager(multiEditorControl);
      this.fileManager.OpenPages = openPages;
      this.fileManager.UserControls = userControls;
      this.fileManager.FilePaths = filePaths;
      this.multiEditorControl = multiEditorControl;
    }
  }
}

using AppConfiguration.Base;
using DevZest.Windows.Docking;
using ICSharpCode.AvalonEdit;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using UI.Components.FileComparerControls;
using UI.Components.Invoke;
using UI.Controls;
using UI.Controls.TextEditor;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
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

    FileManager fileManager { get; set; }

    private MultiEditorControl multiEditorControl { get; set; }

    /// <summary>
    /// Удаляет указанный элемент управления и соответствующую вкладку.
    /// </summary>
    /// <param name="tabButton">Вкладка для удаления.</param>
    /// <param name="control">Элемент управления для удаления.</param>
    public void RemoveControl(OpenFileButton tabButton, UserControl control)
    {
      if (fileManager.OpenPages.Contains(tabButton) && fileManager.UserControls.Contains(control) || control is TextEditorUI)
      {
        int index = -1;
        if (control is TextEditorUI)
        {
          if (tabButton.Text == "Текстовый редактор")
          {
            var container = fileManager.UserControls.FirstOrDefault(textEditorContainer => textEditorContainer.GetType() == typeof(TextEditorContainer));
            if (container is TextEditorContainer)
            {
              var foundContainer = container as TextEditorContainer;
              var foundDockItem = foundContainer.DockManager.DockItems.FirstOrDefault(dockItem => dockItem.Content == control);
              if (foundDockItem != null)
              {
                if (control is TextEditorUI)
                {
                  ShowSaveDialogForControl(foundDockItem);
                  return;
                }
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
          // ShowSaveDialogForControl(control);
          HandleClosingEvents(control, tabButton);
        }

        RemoveTabAndControl(tabButton, control);
        ShowNextTab(index);
      }
    }

    /// <summary>
    /// Отображает диалоговое окно для сохранения файла, если это требуется.
    /// </summary>
    /// <param name="control">Элемент управления для проверки.</param>
    /// <returns>Возвращает <c>true</c>, если файл был сохранен, <c>false</c> в противном случае.</returns>
    private bool

      ShowSaveDialogForControl(DockItem control)
    {
      var result = MessageBoxResult.No;
      var saveFileResult = false;
      if (control.Content is TextEditorUI)
      { // TODO: индекс изменить
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

    private void AddFileCompareControl(string header, UserControl control)
    {
      var fileManager = new FileManager(multiEditorControl);
      var pageName = "Текстовый редактор";
      var textEditorContainer = fileManager.GetContainer(pageName);
      if (textEditorContainer == null)
      {
        textEditorContainer = fileManager.CreateContainer(pageName);
      }
      var dockItem = new DockItem
      {
        Title = header,
        TabText = header,
        Content = control
      };
      fileManager.ShowDockItem(textEditorContainer, dockItem);
    }

    internal void AddTranslatorItem(TextEditorUI editor, TextEditorUI translateEditor, string pageName)
    {
      var fileManager = new FileManager(multiEditorControl);
      var translationContainer = fileManager.GetContainer(pageName);
      if (translationContainer == null)
      {
        translationContainer = fileManager.CreateContainer(pageName);
      }
      var translatorItem = new TranslatorItem();
      translatorItem.SetLeftEditor(editor);
      translatorItem.SetRightEditor(translateEditor);
      var dockItem = new DockItem
      {
        Title = "Заглушка",
        TabText = "Заглушка",
        Content = translatorItem
      };
      fileManager.ShowDockItem(translationContainer, dockItem);
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
    public ControlManager(List<OpenFileButton> openPages, List<UserControl> userControls, Dictionary<string, string> filePaths, MultiEditorControl multiEditorControl)
    {
      this.fileManager = new FileManager(multiEditorControl);
      this.fileManager.OpenPages = openPages;
      this.fileManager.UserControls = userControls;
      this.fileManager.FilePaths = filePaths;
      this.multiEditorControl = multiEditorControl;
    }
  }
}

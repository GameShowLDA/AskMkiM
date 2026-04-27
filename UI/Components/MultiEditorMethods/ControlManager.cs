using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.EventCore.Adapters;
using Ask.Core.Shared.Metadata.Enums.UiEnums;
using Ask.Core.Shared.Metadata.Static;
using Ask.Core.Shared.Metadata.View.EditorHost;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using UI.Components.Invoke;
using UI.Controls;
using UI.Controls.Runner;
using UI.Controls.TextEditorControl;
using UI.Services.FileManager;
using UI.Windows.WpfDocking.Windows.Docking;
using UserControl = System.Windows.Controls.UserControl;

namespace UI.Components.MultiEditorMethods
{
  /// <summary>
  /// Класс для работы с контролами.
  /// </summary>
  public class ControlManager : IWorkspaceService
  {
    private Dictionary<string, (int lineNumber, int lineLength)> _pendingHighlights = new Dictionary<string, (int lineNumber, int lineLength)>();

    internal FileManager fileManager { get; set; }

    private MultiEditorControl multiEditorControl { get; set; }

    /// <summary>
    /// Удаляет указанный элемент управления и соответствующую вкладку.
    /// </summary>
    public void RemoveControl(EditorType editorType)
    {
      var control = fileManager.ContainerService.GetEditorContainer(editorType);
      var page = fileManager.EditorWorkspaceModel.OpenPages.FirstOrDefault(item => item.Text == editorType.ToString());
      if (control != null && page != null)
      {
        RemoveControl(page, control).ConfigureAwait(true);
      }
    }

    /// <summary>
    /// Удаляет указанный элемент управления и соответствующую вкладку.
    /// </summary>
    /// <param name="tabButton">Вкладка для удаления.</param>
    /// <param name="control">Элемент управления для удаления.</param>
    public async Task RemoveControl(OpenFileButton tabButton, UserControl control, bool isTranslation = false)
    {
      var containsOpenPage = fileManager.EditorWorkspaceModel.OpenPages.Contains(tabButton);
      var containsUserControl = fileManager.EditorWorkspaceModel.UserControls.Contains(control);
      if ((containsOpenPage && containsUserControl)
        || (control is TextEditorUI && isTranslation == false)
        || control is RunControl
        || control is TranslatorItem)
      {
        int index = -1;
        EditorType editorType = null;
        if (control is TextEditorUI || control is TranslatorItem)
        {
          editorType = SetEditorType(tabButton);
          CloseControl(tabButton, control, editorType);

          return;
        }
        else if (control is RunControl)
        {
          editorType = SetEditorType(tabButton);
          CloseControl(tabButton, control, editorType);
          var container = fileManager.EditorWorkspaceModel.OpenPages.FirstOrDefault(textEditorContainer
                    => textEditorContainer.Text == editorType.ToString()); // находим вкладку контейнера, содержащую нужный контрол
          var containerIndex = fileManager.EditorWorkspaceModel.OpenPages.IndexOf(container);
          if (containerIndex != null && fileManager.EditorWorkspaceModel.UserControls[containerIndex] is TextEditorContainer foundContainer)
          {
            control = foundContainer;
          }

        }
        else if (control is TextEditorContainer foundContainer) // если контрол сам является контейнером
        {
          if (!CloseContainer(tabButton, control, ref index, ref editorType, foundContainer))
          {
            return;
          }
        }
        else
        {
          index = multiEditorControl.ContentPanel.Children.IndexOf(control);
        }

        if (index < 0)
        {
          index = multiEditorControl.ContentPanel.Children.IndexOf(tabButton);
        }

        if (control is TextEditorContainer)
        {
          HandleClosingEvents(control, tabButton);
        }

        RemoveTabAndControl(tabButton, control);
        if (index > -1)
        {
          ShowNextTab(index);
        }
        var activeTab = fileManager.EditorWorkspaceModel.OpenPages.FirstOrDefault(page => page.IsActive);
        if (fileManager.EditorWorkspaceModel.UserControls.OfType<TextEditorContainer>().Count() == 0 || activeTab == null
          || !(fileManager.EditorWorkspaceModel.UserControls[fileManager.EditorWorkspaceModel.OpenPages.IndexOf(activeTab)] is TextEditorContainer))
        {
          SearchEventAdapter.RaiseCloseSearchWindow();
        }
      }
    }

    private void CloseControl(OpenFileButton tabButton, UserControl control, EditorType editorType)
    {
      var container = fileManager.EditorWorkspaceModel.OpenPages.FirstOrDefault(textEditorContainer
                    => textEditorContainer.Text == editorType.ToString()); // находим вкладку контейнера, содержащую нужный контрол
      var containerIndex = fileManager.EditorWorkspaceModel.OpenPages.IndexOf(container);
      if (fileManager.EditorWorkspaceModel.UserControls[containerIndex] is TextEditorContainer foundContainer)
      {
        var foundDockItem = foundContainer.DockManager.DockItems.FirstOrDefault(dockItem => dockItem.Content == control);
        if (foundDockItem != null)
        {
          CloseDockItem(tabButton, foundContainer, foundDockItem);
        }
      }
    }

    private bool CloseContainer(OpenFileButton tabButton, UserControl control, ref int index, ref EditorType editorType, TextEditorContainer foundContainer)
    {
      if (!CloseContainerItems(foundContainer))
      {
        return false;
      }

      var foundDockItem = foundContainer.DockManager.DockItems.FirstOrDefault(item => item.IsActiveItem == true);
      if (foundDockItem != null && foundDockItem.Content is RunControl runControl)
      {
        editorType = EditorType.Run;
        foundDockItem.Close();
        if (foundContainer.DockManager.DockItems.Count == 0)
        {
          HandleClosingEvents(control, tabButton);
          RemoveControl(tabButton, foundContainer);
          RemoveTabAndControl(tabButton, control);

          return false;
        }
      }
      else
      {
        index = multiEditorControl.ContentPanel.Children.IndexOf(control);
      }

      return true;
    }

    private bool CloseContainerItems(TextEditorContainer foundContainer)
    {
      if (foundContainer.DockManager.DockItems.Count > 0)
      {
        foreach (var item in foundContainer.DockManager.DockItems.ToList())
        {
          if (!ShowSaveDialogForControl(item))
          {
            return false;
          }

          fileManager.EditorWorkspaceModel.FilePaths.Remove(item.TabText);
          EditorEventAdapter.RaiseTextEditorContainerClosing(true, item.TabText);
        }
      }

      return true;
    }

    private void CloseDockItem(OpenFileButton tabButton, TextEditorContainer foundContainer, DockItem foundDockItem)
    {
      if (!ShowSaveDialogForControl(foundDockItem))
      {
        return;
      }

      foundDockItem.PerformClose();
    }

    private static EditorType SetEditorType(OpenFileButton tabButton)
    {
      EditorType editorType;
      if (tabButton.Text == EditorType.TextEditor.ToString())
      {
        editorType = EditorType.TextEditor;
      }
      else if (tabButton.Text == EditorType.Run.ToString())
      {
        editorType = EditorType.Run;
      }
      else
      {
        editorType = EditorType.Translator;
      }

      return editorType;
    }

    private static void SetPathAndContent(DockItem foundDockItem, ref EditorType foundDockItemType, ref string path, ref string content)
    {
      if (foundDockItem.Content is TextEditorUI textEditor)
      {
        path = textEditor.TextEditorModel.FilePath;
        content = textEditor.Text;
      }
      else if (foundDockItem.Content is TranslatorItem translator)
      {
        var leftEditor = translator.GetLeftBox().GetTextEditor();
        path = leftEditor.TextEditorModel.FilePath;
        content = leftEditor.Text;
        foundDockItemType = EditorType.Translator;
      }
    }

    /// <summary>
    /// Отображает диалоговое окно для сохранения файла, если это требуется.
    /// </summary>
    /// <param name="control">Элемент управления для проверки.</param>
    /// <returns>Возвращает <c>true</c>, если файл был сохранен, <c>false</c> в противном случае.</returns>
    private bool ShowSaveDialogForControl(DockItem control)
    {
      var saveFileManager = new SaveFileManager(fileManager);
      return saveFileManager.ConfirmClose(control);
    }

    /// <summary>
    /// Обрабатывает события закрытия для контрола и вкладки.
    /// </summary>
    /// <param name="control">Элемент управления, который закрывается.</param>
    /// <param name="tabButton">Вкладка, которая будет закрыта.</param>
    private void HandleClosingEvents(UserControl control, OpenFileButton tabButton)
    {
      EditorEventAdapter.RaiseTextEditorContainerClosing(control is TextEditorContainer, tabButton.Text);
    }

    /// <summary>
    /// Удаляет вкладку и контрол из соответствующих коллекций и панелей.
    /// </summary>
    /// <param name="tabButton">Вкладка для удаления.</param>
    /// <param name="control">Элемент управления для удаления.</param>
    private void RemoveTabAndControl(OpenFileButton tabButton, UserControl control)
    {
      fileManager.EditorWorkspaceModel.OpenPages.Remove(tabButton);
      fileManager.EditorWorkspaceModel.UserControls.Remove(control);

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
        ShowControl(fileManager.EditorWorkspaceModel.UserControls[index > 0 ? index - 1 : 0], fileManager.EditorWorkspaceModel.OpenPages[index > 0 ? index - 1 : 0]);
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
        EditorEventAdapter.RaiseTextEditorActivated(control);
      }

      bool isTextEditorActive = control is TextEditorUI;

      var isControlProgramActive = false;
      if (openPage.Text == EditorType.Run.ToString() || openPage.Text == EditorType.Translator.ToString() || openPage.Text == EditorType.TextEditor.ToString())
      {
        isControlProgramActive = true;
      }

      if (control is TextEditorContainer container)
      {
        var foundDockItem = container.DockManager.DockItems.FirstOrDefault(item => item.IsActiveItem == true);
        if (foundDockItem != null)
        {
          foundDockItem.Focus();
          isTextEditorActive = foundDockItem.Content is TextEditorUI || foundDockItem.Content is TranslatorItem;
          if (foundDockItem.Title.Contains(".pk") || foundDockItem.Title.Contains(".opk"))
          {
            isControlProgramActive = true;
          }
          else
          {
            isControlProgramActive = false;
          }
        }

        container.SyncActiveEditorState();
      }
      else
      {
        EditorEventAdapter.RaiseTextEditorActive(isTextEditorActive);
      }

      EditorEventAdapter.RaiseActiveEditorChanged(isTextEditorActive);

      SystemStateManager.SetIsControlProgramActive(isControlProgramActive);
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

      if (CheckExistingPage(tabButton, description))
      {
        return;
      }

      ConfigureTabEvents(tabButton, control);
      AddTabAndControl(tabButton, control);
      ShowControl(control, tabButton);
    }

    /// <summary>
    /// Удаляет все вкладки и контролы определенного типа.
    /// </summary>
    /// <param name="tabType">Тип вкладки для удаления.</param>
    private void RemoveControlsByType(TypeWindow tabType)
    {
      for (int i = fileManager.EditorWorkspaceModel.OpenPages.Count - 1; i >= 0; i--)
      {
        var tab = fileManager.EditorWorkspaceModel.OpenPages[i];
        if (tab.TabType == tabType)
        {
          var control = fileManager.EditorWorkspaceModel.UserControls[i];
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
        foreach (OpenFileButton page in fileManager.EditorWorkspaceModel.OpenPages)
        {
          if (page.Description == description)
          {
            var index = fileManager.EditorWorkspaceModel.OpenPages.IndexOf(page);
            var userControl = fileManager.EditorWorkspaceModel.UserControls[index];
            ShowControl(userControl, page);
            return true;
          }
        }
      }
      else
      {
        foreach (OpenFileButton page in fileManager.EditorWorkspaceModel.OpenPages)
        {
          if (page.Header.Text == tabButton.Header.Text)
          {
            var index = fileManager.EditorWorkspaceModel.OpenPages.IndexOf(page);
            var userControl = fileManager.EditorWorkspaceModel.UserControls[index];
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
      tabButton.GetCloseButton().PreviewMouseDown += (s, e) => RemoveControl(tabButton, control).ConfigureAwait(true);
      tabButton.MouseDown += (s, e) =>
      {
        if (e.ChangedButton == MouseButton.Middle)
        {
          RemoveControl(tabButton, control).ConfigureAwait(true);
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
      fileManager.EditorWorkspaceModel.OpenPages.Add(tabButton);
      fileManager.EditorWorkspaceModel.UserControls.Add(control);

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
      return fileManager.EditorWorkspaceModel.OpenPages.Count == 0 && fileManager.EditorWorkspaceModel.UserControls.Count == 0;
    }

    /// <summary>
    /// Отображает указанный элемент управления, скрывая остальные.
    /// </summary>
    /// <param name="control">Элемент управления для отображения.</param>
    public void ActivePage(OpenFileButton control, MultiEditorControl multiEditorControl)
    {
      foreach (OpenFileButton child in multiEditorControl.TopPanel.Children)
      {
        child.IsActive = control.Text == child.Text;

        if (child.IsActive)
        {
          if (control.Text == EditorType.TextEditor.ToString())
          {
            EditorEventAdapter.RaiseTranslatorActive(true);
          }
          else
          {
            EditorEventAdapter.RaiseTranslatorActive(false);
          }
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
      this.fileManager.EditorWorkspaceModel.OpenPages = openPages;
      this.fileManager.EditorWorkspaceModel.UserControls = userControls;
      this.fileManager.EditorWorkspaceModel.FilePaths = filePaths;
      this.multiEditorControl = multiEditorControl;
    }

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="ControlManager"/> с передачей списка открытых страниц, пользовательских контролов, путей к файлам и экземпляра <see cref="MultiEditorControl"/>.
    /// </summary>
    /// <param name="openPages">Список открытых страниц, представленных кнопками <see cref="OpenFileButton"/>.</param>
    /// <param name="userControls">Список пользовательских контролов, представленных элементами <see cref="UserControl"/>.</param>
    /// <param name="filePaths">Словарь, содержащий пути к файлам, где ключ — имя файла, а значение — путь к файлу.</param>
    /// <param name="multiEditorControl">Экземпляр <see cref="MultiEditorControl"/> для взаимодействия с редактором.</param>
    public ControlManager(EditorWorkspaceModel editorWorkspaceModel)
    {
      this.fileManager = new FileManager(multiEditorControl);
      this.fileManager.EditorWorkspaceModel.OpenPages = editorWorkspaceModel.OpenPages;
      this.fileManager.EditorWorkspaceModel.UserControls = editorWorkspaceModel.UserControls;
      this.fileManager.EditorWorkspaceModel.FilePaths = editorWorkspaceModel.FilePaths;
      this.multiEditorControl = editorWorkspaceModel.MultiEditorControl;
    }
  }
}

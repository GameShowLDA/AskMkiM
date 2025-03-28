using AppConfig;
using System;
using System.Windows;
using UI.Components.Invoke;
using UI.Controls.TextEditor;
using UserControl = System.Windows.Controls.UserControl;
using System.Windows.Media;
using System.Windows.Input;


namespace UI.Components.MultiEditorMethods
{
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
      if (fileManager.OpenPages.Contains(tabButton) && fileManager.UserControls.Contains(control))
      {
        var result = MessageBoxResult.No;
        var saveFileResult = false;
        int index = multiEditorControl.ContentPanel.Children.IndexOf(control);
        if (control is TextEditorUI)
        {
          var saveFileManager = new SaveFileManager(fileManager);
          saveFileManager.SaveFileDialog(ref result, ref saveFileResult, index);
        }
        if (saveFileResult == true || !(control is TextEditorUI) || result == MessageBoxResult.No)
        {
          if (index > 0)
          {
            index--;
          }
          EventAggregator.RaiseTextEditorClosing(control is TextEditorUI, tabButton.Text);

          fileManager.OpenPages.Remove(tabButton);
          fileManager.UserControls.Remove(control);

          multiEditorControl.TopPanel.Children.Remove(tabButton);
          multiEditorControl.ContentPanel.Children.Remove(control);

          if (multiEditorControl.ContentPanel.Children.Count > 0)
          {
            ShowControl(fileManager.UserControls[index], fileManager.OpenPages[index]);
          }
        }
      }
    }

    /// <summary>
    /// Отображает указанный элемент управления, скрывая остальные.
    /// </summary>
    /// <param name="control">Элемент управления для отображения.</param>
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

      bool isTextEditor = control is TextEditorUI;
      EventAggregator.RaiseTextEditorActive(isTextEditor);
      EventAggregator.RaiseActiveEditorChanged(isTextEditor);
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

        foreach (OpenFileButton page in fileManager.OpenPages)
        {
          if (page.Description == description)
          {
            var index = fileManager.OpenPages.IndexOf(page);
            var userControl = fileManager.UserControls[index];
            ShowControl(userControl, page);
            return;
          }
        }
      }
      else
      {
        foreach (OpenFileButton page in fileManager.OpenPages)
        {
          if (page.Header.Text == header)
          {
            var index = fileManager.OpenPages.IndexOf(page);
            var userControl = fileManager.UserControls[index];
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
    private void ActivePage(OpenFileButton control, MultiEditorControl multiEditorControl)
    {
      foreach (OpenFileButton child in multiEditorControl.TopPanel.Children)
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

    public ControlManager(FileManager fileManager, MultiEditorControl multiEditorControl)
    {
      this.fileManager = fileManager;
      this.multiEditorControl = multiEditorControl;
    }
    
    public ControlManager(List<OpenFileButton> openPages, List<UserControl> userControls, Dictionary<string, string> filePaths, MultiEditorControl multiEditorControl)
    {
      this.fileManager.OpenPages = openPages;
      this.fileManager.UserControls = userControls;
      this.fileManager.FilePaths = filePaths;
      this.multiEditorControl = multiEditorControl;
    }
  }
}

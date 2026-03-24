using Ask.Core.Services.EventCore.Adapters;
using Ask.Core.Shared.Interfaces.ExecutionInterfaces;
using System.Windows.Controls;
using UI.Windows.WpfDocking.Windows.Docking;

namespace UI.Controls.TextEditor
{
  /// <summary>
  /// Логика взаимодействия для TextEditorContainer.xaml
  /// </summary>
  public partial class TextEditorContainer : UserControl, ITextAdapter
  {
    private bool _dockEventsAttached;

    public TextEditorContainer()
    {
      InitializeComponent();
      Loaded += (_, _) => AttachDockManagerEvents();
      IsVisibleChanged += (_, _) =>
      {
        if (IsVisible)
        {
          SyncActiveEditorState();
        }
      };
    }

    public string GetText()
    {
      var foundDockItem = this.DockManager.DockItems.FirstOrDefault(item => item.IsActiveItem == true);
      if (foundDockItem != null)
      {
        if (foundDockItem.Content is TextEditorUI)
        {
          var textEditor = (TextEditorUI)foundDockItem.Content;
          return textEditor.Text;
        }

        if (foundDockItem.Content is TranslatorItem translatorItem)
        {
          return translatorItem.GetLeftBox()?.GetTextEditor()?.Text ?? string.Empty;
        }
      }
      return string.Empty;
    }

    public TextEditorUI GetTextEditor()
    {
      var foundDockItem = this.DockManager.DockItems.FirstOrDefault(item => item.IsActiveItem == true);
      if (foundDockItem != null)
      {
        if (foundDockItem.Content is TextEditorUI textEditor)
        {
          return textEditor;
        }
      }
      return null;
    }

    public DockControl GetDockControl()
    {
      return DockManager;
    }

    /// <summary>
    /// Удаляет вкладку, содержащую указанный TranslatorItem, из DockManager.
    /// </summary>
    /// <param name="translatorItem">Экземпляр TranslatorItem для удаления.</param>
    /// <returns>True, если вкладка была найдена и удалена, иначе false.</returns>
    public bool RemoveTranslatorItem(TranslatorItem translatorItem)
    {
      if (translatorItem == null)
        return false;

      var dockItem = this.DockManager.DockItems
        .FirstOrDefault(item => item.Content == translatorItem);

      if (dockItem != null)
      {
        dockItem.Close();
        return true;
      }

      return false;
    }

    public void SyncActiveEditorState()
    {
      if (!IsLoaded || !IsVisible)
      {
        return;
      }

      var activeDockItem = DockManager.DockItems
        .FirstOrDefault(item => item.IsActiveDocument || item.IsActiveItem);

      if (activeDockItem?.Content is TextEditorUI textEditor)
      {
        EditorEventAdapter.RaiseTextEditorActive(true);
        EditorEventAdapter.RaiseTextEditorActivated(textEditor);
        return;
      }

      if (activeDockItem?.Content is TranslatorItem translatorItem)
      {
        var leftEditor = translatorItem.GetLeftBox()?.GetTextEditor();
        if (leftEditor != null)
        {
          EditorEventAdapter.RaiseTextEditorActive(true);
          EditorEventAdapter.RaiseTextEditorActivated(leftEditor);
          return;
        }
      }

      EditorEventAdapter.RaiseTextEditorActive(false);
    }

    private void AttachDockManagerEvents()
    {
      if (_dockEventsAttached)
      {
        return;
      }

      DockManager.ActiveItemChanged += DockManager_ActiveItemChanged;
      DockManager.ActiveDocumentChanged += DockManager_ActiveItemChanged;
      _dockEventsAttached = true;

      SyncActiveEditorState();
    }

    private void DockManager_ActiveItemChanged(object? sender, EventArgs e)
    {
      SyncActiveEditorState();
    }
  }
}

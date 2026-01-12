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
    public TextEditorContainer()
    {
      InitializeComponent();
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
        dockItem.Close(); // закрывает и удаляет из DockManager
        return true;
      }

      return false;
    }

  }
}

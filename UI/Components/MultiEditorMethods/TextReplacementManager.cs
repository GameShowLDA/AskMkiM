using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using UI.Components.SearchControls;
using UI.Controls;
using UI.Controls.TextEditor;

namespace UI.Components.MultiEditorMethods
{
  public class TextReplacementManager
  {
    /// <summary>
    /// Экземпляр класса <see cref="FileManager"/> для управления файлами.
    /// </summary>
    private FileManager fileManager { get; set; }
    /// <summary>
    /// Текст для поиска.
    /// </summary>
    internal string _searchText;

    /// <summary>
    /// Текст для поиска.
    /// </summary>
    internal string _replacementText;

    /// <summary>
    /// Словарь, который хранит полный текст для каждого элемента управления <see cref="UserControl"/>.
    /// Ключ - элемент управления, значение - полный текст.
    /// </summary>
    internal Dictionary<UserControl, string> _fullText = new Dictionary<UserControl, string>();

    internal void ReplaceWord(string fileName, SearchResult searchResult, int startOffset, string replaceText, string searchText)
    {
      var activeTab = fileManager.EditorWorkspaceModel.OpenPages.FirstOrDefault(page => page.Background == (Brush)Application.Current.Resources["ActiveBorderSolidColorBrush"]);
      if (activeTab != null && fileManager.EditorWorkspaceModel.UserControls[fileManager.EditorWorkspaceModel.OpenPages.IndexOf(activeTab)] is TextEditorContainer textEditorContainer)
      {
        if (textEditorContainer != null)
        {
          var foundPage = textEditorContainer.DockManager.DockItems.FirstOrDefault(page => page.Title == fileName);
          if (foundPage != null && (foundPage.Content is TextEditorUI || foundPage.Content is TranslatorItem))
          {
            if (foundPage.Content is TextEditorUI textEditor)
            {
              ReplaceTextInEditor(textEditor, searchResult, startOffset, replaceText, searchText);
            }
            else if (foundPage.Content is TranslatorItem translatorItem)
            {
              ReplaceTextInEditor(translatorItem.GetLeftEditor(), searchResult, startOffset, replaceText, searchText);
            }
            else
            {
              return;
            }
          }
        }
      }
    }

    private void ReplaceTextInEditor(TextEditorUI textEditor, SearchResult searchResult, int startOffset, string replaceText, string searchText)
    {
      if (searchResult != null)
      {
        var document = textEditor.Document;
        var text = document.Text;
        var endOffset = startOffset + searchResult.Length;
        var beforeText = text.Substring(0, startOffset);
        var afterText = text.Substring(startOffset + searchText.Length);
        var newText = beforeText + replaceText + afterText;
        document.Text = newText;
      }
    }

    public TextReplacementManager(FileManager fileManager)
    {
      this.fileManager = fileManager;
    }
  }
}

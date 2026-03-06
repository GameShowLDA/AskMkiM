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
              ReplaceTextInEditor(translatorItem.GetLeftBox().GetTextEditor(), searchResult, startOffset, replaceText, searchText);
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
      if (searchResult == null || textEditor?.Document == null)
      {
        return;
      }

      var document = textEditor.Document;
      if (startOffset < 0 || startOffset + searchResult.Length > document.TextLength)
      {
        return;
      }

      string replacement = replaceText ?? string.Empty;
      document.Replace(startOffset, searchResult.Length, replacement);

      int nextCaretOffset = Math.Min(startOffset + replacement.Length, document.TextLength);
      textEditor.TextArea.ClearSelection();
      textEditor.TextArea.Caret.Offset = nextCaretOffset;
      textEditor.TextArea.Caret.BringCaretToView();
    }

    public TextReplacementManager(FileManager fileManager)
    {
      this.fileManager = fileManager;
    }
  }
}

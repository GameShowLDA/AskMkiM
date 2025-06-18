using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using UI.Components.SearchControls;
using UI.Controls.TextEditor;
using static Utilities.LoggerUtility;

namespace UI.Components.MultiEditorMethods
{
  public class TextReplacementManager
  {
    /// <summary>
    /// Экземпляр класса <see cref="FileManager"/> для управления файлами.
    /// </summary>
    private FileManager fileManager { get; set; }
    /// <summary>
    /// Экземпляр класса <see cref="FileManager"/> для управления файлами.
    /// </summary>
    private ControlManager controlManager { get; set; }

    /// <summary>
    /// Экземпляр класса <see cref="MultiEditorControl"/> для управления мульти-редактором.
    /// </summary>
    private MultiEditorControl multiEditorControl { get; set; }

    /// <summary>
    /// Словарь, хранящий результаты поиска для каждого открытого файла.
    /// Ключ - имя файла, значение - список результатов поиска для этого файла.
    /// </summary>
    Dictionary<string, List<SearchResult>> foundInOpenedFiles = new Dictionary<string, List<SearchResult>>();

    /// <summary>
    /// Список результатов поиска, полученных для текущего документа.
    /// </summary>
    private List<SearchResult> foundResults = new List<SearchResult>();

    /// <summary>
    /// Индекс текущего результата поиска, на который нужно перейти.
    /// </summary>
    private int currentIndex = -1;

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
      var textEditorContainer = fileManager.UserControls.OfType<TextEditorContainer>().FirstOrDefault();
      if (textEditorContainer != null)
      {
        var foundPage = textEditorContainer.DockManager.DockItems.FirstOrDefault(page => page.Title == fileName);
        if (foundPage != null && foundPage.Content.GetType() == typeof(TextEditorUI))
        {
          var foundTextEditor = foundPage.Content as TextEditorUI;
          ReplaceTextInEditor(foundTextEditor, searchResult, startOffset, replaceText, searchText);
        }
      }
    }

    private void ReplaceTextInEditor(UserControl editor, SearchResult searchResult, int startOffset, string replaceText, string searchText)
    {
      if (editor is TextEditorUI textEditor && searchResult != null)
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

    public TextReplacementManager(FileManager fileManager, MultiEditorControl multiEditorControl, ControlManager controlManager)
    {
      this.fileManager = fileManager;
      this.multiEditorControl = multiEditorControl;
      this.controlManager = controlManager;
    }
  }
}

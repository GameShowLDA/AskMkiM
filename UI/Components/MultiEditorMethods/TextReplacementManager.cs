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

    internal void ReplaceWord(KeyValuePair<string, List <SearchResult>> searchResult, int startOffset, string replaceText, string searchText)
    {
      var foundPage = fileManager.OpenPages.FirstOrDefault(page => page.Text == searchResult.Key);
      if (foundPage != null)
      {
        var pageIndex = fileManager.OpenPages.IndexOf(foundPage);
        var foundTextEditor = fileManager.UserControls[pageIndex];
        ReplaceTextInEditor(foundTextEditor, searchResult, startOffset, replaceText, searchText);
        //controlManager.ShowControl(foundTextEditor, foundPage);
      }
    }

    private void ReplaceTextInEditor(UserControl editor, KeyValuePair<string, List<SearchResult>> searchResult, int startOffset, string replaceText, string searchText)
    {
      if (editor is TextEditorUI textEditor)
      {
        var result = searchResult.Value.FirstOrDefault();
        var document = textEditor.Document; 
        var text = document.Text; 
        var endOffset = result.StartOffset + result.Length;
        text = text.Substring(0, startOffset) + string.Empty + text.Substring(endOffset+2);
        text = text.Substring(0, startOffset) + replaceText + text.Substring(endOffset + 3);
        document.Text = text; 
        textEditor.InvalidateVisual(); 
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

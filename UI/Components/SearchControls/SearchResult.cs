using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UI.Components.SearchControls
{
  public class SearchResult
  {
    public int StartOffset { get; }
    public int Length { get; }
    public string FileName { get; }
    public string SearchText { get; }
    public int LineNumber { get; }
    /// <summary>
    /// Смещение начала слова (относительно начала строки)
    /// </summary>
    public int WordStartOffset { get; }
    /// <summary>
    /// Подстрока строки, начиная с найденного слова
    /// </summary>
    public string SubstringFromWord { get; }
    public bool IsCaseSensitive { get; set; }

    public SearchResult(int startOffset, int length)
    {
      StartOffset = startOffset;
      Length = length;
    }

    public SearchResult(int startOffset, int length, int lineNumber, string substringFromWord) : this(startOffset, length)
    {
      LineNumber = lineNumber;
      SubstringFromWord = substringFromWord;
    }

    public SearchResult(int startOffset, int length, int lineNumber, string substringFromWord, string fileName, string searchText, bool isCaseSensitive = false)
      : this(startOffset, length, lineNumber, substringFromWord)
    {
      FileName = fileName;
      SearchText = searchText;
      IsCaseSensitive = isCaseSensitive;
    }

  }
}


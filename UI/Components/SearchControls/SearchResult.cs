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
    public int LineNumber { get; }
    /// <summary>
    /// Смещение начала слова (относительно начала строки)
    /// </summary>
    public int WordStartOffset { get; }
    /// <summary>
    /// Подстрока строки, начиная с найденного слова
    /// </summary>
    public string SubstringFromWord { get; }

    public SearchResult(int startOffset, int length, int lineNumber, int wordStartOffset, string substringFromWord)
    {
      StartOffset = startOffset;
      Length = length;
      LineNumber = lineNumber;
      WordStartOffset = wordStartOffset;
      SubstringFromWord = substringFromWord;
    }

    public SearchResult(int startOffset, int length, int lineNumber, int wordStartOffset, string substringFromWord, string fileName)
    {
      StartOffset = startOffset;
      Length = length;
      LineNumber = lineNumber;
      WordStartOffset = wordStartOffset;
      SubstringFromWord = substringFromWord;
      FileName = fileName;
    }

    public SearchResult(int startOffset, int length)
    {
      StartOffset = startOffset;
      Length = length;
    }
  }
}


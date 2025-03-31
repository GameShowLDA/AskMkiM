namespace UI.Components.SearchControls
{
  /// <summary>
  /// Класс, представляющий результаты поиска текста в документе.
  /// </summary>
  /// <remarks>
  /// Этот класс используется для хранения информации о найденных вхождениях текста в документе, таких как смещение, длина, строка и другие параметры.
  /// </remarks>
  public class SearchResult
  {
    /// <summary>
    /// Смещение начала найденного текста относительно начала документа.
    /// </summary>
    public int StartOffset { get; }

    /// <summary>
    /// Длина найденного текста.
    /// </summary>
    public int Length { get; }

    /// <summary>
    /// Имя файла, в котором найдено вхождение текста.
    /// </summary>
    public string FileName { get; }

    /// <summary>
    /// Искомый текст, который был найден.
    /// </summary>
    public string SearchText { get; }

    /// <summary>
    /// Номер строки, в которой найдено вхождение текста.
    /// </summary>
    public int LineNumber { get; }

    /// <summary>
    /// Смещение начала найденного слова относительно начала строки.
    /// </summary>
    public int WordStartOffset { get; }

    /// <summary>
    /// Подстрока строки, начиная с найденного слова.
    /// </summary>
    public string SubstringFromWord { get; }

    /// <summary>
    /// Флаг, указывающий, учитывается ли регистр при поиске.
    /// </summary>
    public bool IsCaseSensitive { get; set; }

    /// <summary>
    /// Флаг, указывающий, что поиск должен учитывать только полные слова.
    /// </summary>
    public bool IsWholeWord { get; set; }

    /// <summary>
    /// Инициализирует новый экземпляр <see cref="SearchResult"/> с заданным смещением и длиной.
    /// </summary>
    /// <param name="startOffset">Смещение начала найденного текста.</param>
    /// <param name="length">Длина найденного текста.</param>
    public SearchResult(int startOffset, int length)
    {
      StartOffset = startOffset;
      Length = length;
    }

    /// <summary>
    /// Инициализирует новый экземпляр <see cref="SearchResult"/> с заданным смещением, длиной, номером строки и подстрокой.
    /// </summary>
    /// <param name="startOffset">Смещение начала найденного текста.</param>
    /// <param name="length">Длина найденного текста.</param>
    /// <param name="lineNumber">Номер строки, в которой найдено вхождение.</param>
    /// <param name="substringFromWord">Подстрока строки, начиная с найденного слова.</param>
    public SearchResult(int startOffset, int length, int lineNumber, string substringFromWord) : this(startOffset, length)
    {
      LineNumber = lineNumber;
      SubstringFromWord = substringFromWord;
    }

    /// <summary>
    /// Инициализирует новый экземпляр <see cref="SearchResult"/> с полным набором параметров.
    /// </summary>
    /// <param name="startOffset">Смещение начала найденного текста.</param>
    /// <param name="length">Длина найденного текста.</param>
    /// <param name="lineNumber">Номер строки, в которой найдено вхождение.</param>
    /// <param name="substringFromWord">Подстрока строки, начиная с найденного слова.</param>
    /// <param name="fileName">Имя файла, в котором найдено вхождение текста.</param>
    /// <param name="searchText">Искомый текст.</param>
    /// <param name="isCaseSensitive">Флаг, указывающий, учитывается ли регистр при поиске.</param>
    /// <param name="isWholeWord">Флаг, указывающий, что поиск должен учитывать только полные слова.</param>
    public SearchResult(int startOffset, int length, int lineNumber, string substringFromWord, string fileName,
      string searchText, bool isCaseSensitive = false, bool isWholeWord = false)
      : this(startOffset, length, lineNumber, substringFromWord)
    {
      FileName = fileName;
      SearchText = searchText;
      IsCaseSensitive = isCaseSensitive;
      IsWholeWord = isWholeWord;
    }
  }
}
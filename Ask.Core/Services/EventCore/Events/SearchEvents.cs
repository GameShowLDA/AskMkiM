using Ask.Core.Shared.Interfaces.EventInterfaces;

namespace Ask.Core.Services.EventCore.Events
{
  /// <summary>
  /// Содержит события, связанные с поиском и заменой текста в редакторе,
  /// включая управление окнами поиска, выполнение операций поиска и работу с результатами.
  /// </summary>
  public static class SearchEvents
  {
    /// <summary>
    /// Событие, обозначающее закрытие окна поиска.
    /// </summary>
    public class SearchWindowClosing : IEvent
    {
      /// <summary>
      /// Указывает, закрывается ли окно поиска.
      /// </summary>
      public bool IsClosing { get; }

      /// <summary>
      /// Создаёт событие закрытия окна поиска.
      /// </summary>
      /// <param name="isClosing">true — окно поиска закрывается.</param>
      public SearchWindowClosing(bool isClosing)
      {
        IsClosing = isClosing;
      }
    }

    /// <summary>
    /// Событие, обозначающее активацию или деактивацию окна поиска.
    /// </summary>
    public class SearchWindowActivated : IEvent
    {
      /// <summary>
      /// Указывает, активно ли окно поиска.
      /// </summary>
      public bool IsActive { get; }

      /// <summary>
      /// Создаёт событие активации окна поиска.
      /// </summary>
      /// <param name="isActive">true — окно поиска активно, false — неактивно.</param>
      public SearchWindowActivated(bool isActive)
      {
        IsActive = isActive;
      }
    }

    /// <summary>
    /// Событие, обозначающее запрос на закрытие окна поиска.
    /// </summary>
    public class CloseSearchWindow : IEvent { }

    /// <summary>
    /// Событие, обозначающее нажатие кнопки поиска текста.
    /// </summary>
    public class SearchButtonPressed : IEvent
    {
      /// <summary>
      /// Строка с параметрами поиска.
      /// </summary>
      public string SearchParameters { get; }

      /// <summary>
      /// Создаёт событие нажатия кнопки поиска.
      /// </summary>
      /// <param name="searchParameters">Параметры поиска.</param>
      public SearchButtonPressed(string searchParameters)
      {
        SearchParameters = searchParameters;
      }
    }

    /// <summary>
    /// Событие, обозначающее нажатие кнопки поиска по выделенному тексту.
    /// </summary>
    public class SearchTextRequested : IEvent
    {
      /// <summary>
      /// Выделенный текст, переданный в окно поиска.
      /// </summary>
      public string SelectedText { get; }

      /// <summary>
      /// Создаёт событие запроса поиска по выделенному тексту.
      /// </summary>
      /// <param name="selectedText">Выделенный текст.</param>
      public SearchTextRequested(string selectedText)
      {
        SelectedText = selectedText;
      }
    }

    /// <summary>
    /// Событие, обозначающее выполнение поиска текста.
    /// </summary>
    public class SearchText : IEvent
    {
      public string SearchString { get; }
      public bool? WholeWord { get; }
      public bool? MatchCase { get; }
      public int SearchArea { get; }
      public string SearchParameters { get; }

      /// <summary>
      /// Создаёт событие выполнения поиска текста.
      /// </summary>
      public SearchText(string searchString, bool? wholeWord, bool? matchCase, int searchArea, string searchParameters)
      {
        SearchString = searchString;
        WholeWord = wholeWord;
        MatchCase = matchCase;
        SearchArea = searchArea;
        SearchParameters = searchParameters;
      }
    }

    /// <summary>
    /// Событие, обозначающее выполнение замены текста.
    /// </summary>
    public class ReplaceText : IEvent
    {
      public string ReplaceString { get; }
      public string SearchString { get; }
      public bool? WholeWord { get; }
      public bool? MatchCase { get; }
      public int SearchArea { get; }
      public string SearchParameters { get; }

      /// <summary>
      /// Создаёт событие замены текста.
      /// </summary>
      public ReplaceText(string replaceString, string searchString, bool? wholeWord, bool? matchCase, int searchArea, string searchParameters)
      {
        ReplaceString = replaceString;
        SearchString = searchString;
        WholeWord = wholeWord;
        MatchCase = matchCase;
        SearchArea = searchArea;
        SearchParameters = searchParameters;
      }
    }

    /// <summary>
    /// Событие, обозначающее выбор строки в таблице результатов поиска.
    /// </summary>
    public class FoundTextSelectRow : IEvent
    {
      public string FileName { get; }
      public int LineNumber { get; }
      public int StartOffset { get; }
      public string LineText { get; }
      public string SearchText { get; }

      /// <summary>
      /// Создаёт событие выбора найденной строки.
      /// </summary>
      public FoundTextSelectRow(string fileName, int lineNumber, int startOffset, string lineText, string searchText)
      {
        FileName = fileName;
        LineNumber = lineNumber;
        StartOffset = startOffset;
        LineText = lineText;
        SearchText = searchText;
      }
    }

    /// <summary>
    /// Событие, обозначающее нажатие кнопки замены одного вхождения текста.
    /// </summary>
    public class ReplaceWordButtonPressed : IEvent { }

    /// <summary>
    /// Событие, обозначающее нажатие кнопки замены всех найденных вхождений текста.
    /// </summary>
    public class ReplaceAllWordsButtonPressed : IEvent { }
  }
}

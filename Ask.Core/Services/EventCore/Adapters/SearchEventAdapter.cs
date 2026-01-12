using Ask.Core.Services.EventCore.Events;
using Ask.Core.Services.EventCore.Services;

namespace Ask.Core.Services.EventCore.Adapters
{
  /// <summary>
  /// Адаптер для генерации событий <see cref="SearchEvents"/>,
  /// обеспечивающий обратную совместимость со старой системой событий <see cref="EventAggregator"/>.
  /// </summary>
  /// <remarks>
  /// Предоставляет методы для вызова всех поисковых событий:
  /// закрытие окна поиска, активация, нажатие кнопок, выполнение поиска и замены текста.
  /// </remarks>
  public static class SearchEventAdapter
  {
    /// <summary>
    /// Генерирует событие закрытия окна поиска.
    /// </summary>
    /// <param name="isClosing">true — окно поиска закрывается; false — остаётся открытым.</param>
    /// <example>
    /// <code>
    /// SearchEventAdapter.RaiseSearchWindowClosing(true);
    /// </code>
    /// </example>
    public static void RaiseSearchWindowClosing(bool isClosing)
      => EventAggregator.Publish(new SearchEvents.SearchWindowClosing(isClosing));

    /// <summary>
    /// Генерирует событие активации или деактивации окна поиска.
    /// </summary>
    /// <param name="isActive">true — окно поиска активно; false — неактивно.</param>
    /// <example>
    /// <code>
    /// SearchEventAdapter.RaiseSearchWindowActivated(true);
    /// </code>
    /// </example>
    public static void RaiseSearchWindowActivated(bool isActive)
      => EventAggregator.Publish(new SearchEvents.SearchWindowActivated(isActive));

    /// <summary>
    /// Генерирует событие запроса на закрытие окна поиска.
    /// </summary>
    /// <example>
    /// <code>
    /// SearchEventAdapter.RaiseCloseSearchWindow();
    /// </code>
    /// </example>
    public static void RaiseCloseSearchWindow()
      => EventAggregator.Publish(new SearchEvents.CloseSearchWindow());

    /// <summary>
    /// Генерирует событие нажатия кнопки поиска текста.
    /// </summary>
    /// <param name="searchParameters">Параметры поиска (например, чувствительность к регистру, область поиска и т.д.).</param>
    /// <example>
    /// <code>
    /// SearchEventAdapter.RaiseSearchButtonPressed("case-insensitive");
    /// </code>
    /// </example>
    public static void RaiseSearchButtonPressed(string searchParameters)
      => EventAggregator.Publish(new SearchEvents.SearchButtonPressed(searchParameters));

    /// <summary>
    /// Генерирует событие запроса поиска по выделенному тексту.
    /// </summary>
    /// <param name="selectedText">Выделенный текст, передаваемый в окно поиска.</param>
    /// <example>
    /// <code>
    /// SearchEventAdapter.RaiseSearchTextRequested("Voltage");
    /// </code>
    /// </example>
    public static void RaiseSearchTextRequested(string selectedText)
      => EventAggregator.Publish(new SearchEvents.SearchTextRequested(selectedText));

    /// <summary>
    /// Генерирует событие выполнения поиска текста.
    /// </summary>
    /// <param name="searchString">Текст для поиска.</param>
    /// <param name="wholeWord">true — искать целые слова; false — частичные совпадения.</param>
    /// <param name="matchCase">true — учитывать регистр; false — игнорировать.</param>
    /// <param name="searchArea">Область поиска (например, 0 — весь документ, 1 — выделенный блок).</param>
    /// <param name="searchParameters">Дополнительные параметры поиска.</param>
    /// <example>
    /// <code>
    /// SearchEventAdapter.RaiseSearchText("current", true, false, 0, "global-search");
    /// </code>
    /// </example>
    public static void RaiseSearchText(string searchString, bool? wholeWord, bool? matchCase, int searchArea, string searchParameters)
      => EventAggregator.Publish(new SearchEvents.SearchText(searchString, wholeWord, matchCase, searchArea, searchParameters));

    /// <summary>
    /// Генерирует событие выполнения замены текста.
    /// </summary>
    /// <param name="replaceString">Текст, на который производится замена.</param>
    /// <param name="searchString">Текст, который требуется заменить.</param>
    /// <param name="wholeWord">true — учитывать целые слова; false — искать частичные совпадения.</param>
    /// <param name="matchCase">true — учитывать регистр; false — игнорировать.</param>
    /// <param name="searchArea">Область, в которой выполняется замена.</param>
    /// <param name="searchParameters">Дополнительные параметры замены.</param>
    /// <example>
    /// <code>
    /// SearchEventAdapter.RaiseReplaceText("I", "Current", true, false, 0, "whole-word");
    /// </code>
    /// </example>
    public static void RaiseReplaceText(string replaceString, string searchString, bool? wholeWord, bool? matchCase, int searchArea, string searchParameters)
      => EventAggregator.Publish(new SearchEvents.ReplaceText(replaceString, searchString, wholeWord, matchCase, searchArea, searchParameters));

    /// <summary>
    /// Генерирует событие выбора строки в таблице результатов поиска.
    /// </summary>
    /// <param name="fileName">Имя файла, в котором найдено совпадение.</param>
    /// <param name="lineNumber">Номер строки, содержащей совпадение.</param>
    /// <param name="startOffset">Позиция начала совпадения в строке.</param>
    /// <param name="lineText">Текст строки, в которой найдено совпадение.</param>
    /// <param name="searchText">Текст, который искался.</param>
    /// <example>
    /// <code>
    /// SearchEventAdapter.RaiseFoundTextSelectRow("main.cs", 42, 10, "Voltage = 5.0;", "Voltage");
    /// </code>
    /// </example>
    public static void RaiseFoundTextSelectRow(string fileName, int lineNumber, int startOffset, string lineText, string searchText)
      => EventAggregator.Publish(new SearchEvents.FoundTextSelectRow(fileName, lineNumber, startOffset, lineText, searchText));

    /// <summary>
    /// Генерирует событие нажатия кнопки замены одного найденного вхождения текста.
    /// </summary>
    /// <example>
    /// <code>
    /// SearchEventAdapter.RaiseReplaceWordButtonPressed();
    /// </code>
    /// </example>
    public static void RaiseReplaceWordButtonPressed()
      => EventAggregator.Publish(new SearchEvents.ReplaceWordButtonPressed());

    /// <summary>
    /// Генерирует событие нажатия кнопки замены всех найденных вхождений текста.
    /// </summary>
    /// <example>
    /// <code>
    /// SearchEventAdapter.RaiseReplaceAllWordsButtonPressed();
    /// </code>
    /// </example>
    public static void RaiseReplaceAllWordsButtonPressed()
      => EventAggregator.Publish(new SearchEvents.ReplaceAllWordsButtonPressed());
  }
}

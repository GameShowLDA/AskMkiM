using Ask.Core.Shared.DTO.Executor;
using ICSharpCode.AvalonEdit.Document;
using System.Text;
using System.Text.RegularExpressions;

namespace UI.Controls.TextEditorControl.Syntax
{
  /// <summary>
  /// Строит представление тела команды без заголовка и комментариев
  /// и сопоставляет позиции этого тела с абсолютными позициями исходного документа.
  /// </summary>
  internal sealed class CommandBodyMap
  {
    private static readonly Regex CommandHeaderRegex = new Regex(
      @"^\s*\d+\s+[\p{L}_]{2,}(?=\s|$)\s*(?<body>.*)$",
      RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private readonly IReadOnlyList<int> _compactIndexMap;
    private readonly IReadOnlyList<CommandBodySegment> _segments;
    private readonly string _compactText;

    /// <summary>
    /// Создаёт карту тела команды по нормализованному тексту и его сегментам.
    /// </summary>
    /// <param name="text">Текст тела команды без заголовка команды.</param>
    /// <param name="segments">Сегменты исходного документа, из которых собрано тело команды.</param>
    private CommandBodyMap(
      string text,
      IReadOnlyList<CommandBodySegment> segments)
    {
      Text = text;
      _segments = segments;
      (_compactText, _compactIndexMap) = BuildCompactIndex(text);
    }

    /// <summary>
    /// Текст тела команды без заголовка и комментариев.
    /// </summary>
    public string Text { get; }

    /// <summary>
    /// Индекс первого символа блока точек в теле команды, либо -1, если блок точек не найден.
    /// </summary>
    public int PointIndex => Text.IndexOf('*');

    /// <summary>
    /// Признак наличия непустого блока точек между первой и последней звёздочками.
    /// </summary>
    public bool HasPointContent
    {
      get
      {
        int start = PointIndex;
        int end = Text.LastIndexOf('*');
        return start >= 0 && end > start + 1 &&
               Text[(start + 1)..end].Any(ch => !char.IsWhiteSpace(ch));
      }
    }

    /// <summary>
    /// Часть тела команды до блока точек. Обычно содержит параметры команды.
    /// </summary>
    public string ParameterText => PointIndex >= 0 ? Text[..PointIndex] : Text;

    /// <summary>
    /// Создаёт карту тела команды по строкам документа.
    /// </summary>
    /// <param name="document">Документ AvalonEdit.</param>
    /// <param name="model">Модель команды, для которой строится карта.</param>
    /// <param name="modelEndLineNumber">Последняя строка команды в исходном документе.</param>
    /// <param name="commentSpans">Диапазоны комментариев, которые нужно исключить из поиска.</param>
    /// <param name="bodyMap">Построенная карта тела команды.</param>
    /// <returns>Значение true, если тело команды содержит хотя бы один непустой сегмент.</returns>
    public static bool TryCreate(
      TextDocument document,
      BaseCommandModel model,
      int modelEndLineNumber,
      IReadOnlyList<TextSpan> commentSpans,
      out CommandBodyMap bodyMap)
    {
      var textBuilder = new StringBuilder();
      var segments = new List<CommandBodySegment>();
      int startLineNumber = Math.Clamp(model.StartLineNumber, 1, document.LineCount);
      int endLineNumber = Math.Clamp(modelEndLineNumber, startLineNumber, document.LineCount);

      for (int lineNumber = startLineNumber; lineNumber <= endLineNumber; lineNumber++)
      {
        var line = document.GetLineByNumber(lineNumber);
        string lineText = SyntaxCommentScanner.RemoveCommentsFromLine(
          document.GetText(line),
          line.Offset,
          commentSpans);

        if (!TryGetCommandBodyPart(
              lineText,
              lineNumber == startLineNumber,
              out int sourceStartIndex,
              out int sourceLength))
        {
          continue;
        }

        int bodyStart = textBuilder.Length;
        textBuilder.Append(lineText, sourceStartIndex, sourceLength);
        segments.Add(new CommandBodySegment(
          bodyStart,
          sourceLength,
          line.Offset + sourceStartIndex,
          lineNumber,
          sourceStartIndex + 1));
        textBuilder.Append('\n');
      }

      bodyMap = new CommandBodyMap(textBuilder.ToString(), segments);
      return segments.Count > 0;
    }

    /// <summary>
    /// Ищет текстовый фрагмент в теле команды и преобразует найденный участок
    /// в диапазон исходного документа.
    /// </summary>
    /// <param name="value">Фрагмент, который нужно найти.</param>
    /// <param name="span">Диапазон найденного фрагмента в исходном документе.</param>
    /// <returns>Значение true, если фрагмент найден и успешно сопоставлен с документом.</returns>
    public bool TryResolveText(string value, out CommandIssueSpan span)
    {
      span = default;
      value = TrimCandidate(value);

      int index = string.IsNullOrWhiteSpace(value)
        ? -1
        : Text.IndexOf(value, StringComparison.OrdinalIgnoreCase);

      return index >= 0 && TryResolve(index, value.Length, out span);
    }

    /// <summary>
    /// Ищет фрагмент в теле команды без учёта пробелов и переносов строк.
    /// Используется для сообщений парсеров, где многострочное тело было склеено.
    /// </summary>
    /// <param name="value">Фрагмент, который нужно найти.</param>
    /// <param name="span">Диапазон найденного фрагмента в исходном документе.</param>
    /// <returns>Значение true, если фрагмент найден и успешно сопоставлен с документом.</returns>
    public bool TryResolveCompactText(string value, out CommandIssueSpan span)
    {
      value = TrimCandidate(value);
      string compactCandidate = new string(value.Where(ch => !char.IsWhiteSpace(ch)).ToArray());

      int compactIndex = compactCandidate.Length == 0
        ? -1
        : _compactText.IndexOf(compactCandidate, StringComparison.OrdinalIgnoreCase);

      if (compactIndex < 0)
      {
        span = default;
        return false;
      }

      int bodyStart = _compactIndexMap[compactIndex];
      int bodyEnd = _compactIndexMap[compactIndex + compactCandidate.Length - 1] + 1;
      return TryResolve(bodyStart, bodyEnd - bodyStart, out span);
    }

    /// <summary>
    /// Возвращает диапазон первого непустого сегмента тела команды.
    /// </summary>
    /// <param name="span">Диапазон первого непустого сегмента.</param>
    /// <returns>Значение true, если в теле команды есть непустой сегмент.</returns>
    public bool TryResolveFirstSegment(out CommandIssueSpan span)
    {
      return TryResolveSegment(
        _segments.FirstOrDefault(segment => segment.Length > 0),
        0,
        out span);
    }

    /// <summary>
    /// Возвращает диапазон сегмента тела команды, расположенного на указанной строке.
    /// </summary>
    /// <param name="lineNumber">Номер строки исходного документа.</param>
    /// <param name="span">Диапазон сегмента на указанной строке.</param>
    /// <returns>Значение true, если на строке есть сегмент тела команды.</returns>
    public bool TryResolveLine(int lineNumber, out CommandIssueSpan span)
    {
      return TryResolveSegment(
        _segments.FirstOrDefault(segment => segment.LineNumber == lineNumber && segment.Length > 0),
        0,
        out span);
    }

    /// <summary>
    /// Возвращает диапазон блока точек, начиная с первого символа '*'.
    /// Если блок точек отсутствует, возвращает первый непустой сегмент тела команды.
    /// </summary>
    /// <param name="span">Диапазон блока точек или первого сегмента.</param>
    /// <returns>Значение true, если найден диапазон для подсветки.</returns>
    public bool TryResolvePointRegion(out CommandIssueSpan span)
    {
      if (PointIndex < 0)
      {
        span = default;
        return false;
      }

      int end = Text.IndexOf('\n', PointIndex);
      int length = (end >= 0 ? end : Text.Length) - PointIndex;
      return TryResolve(PointIndex, Math.Max(1, length), out span);
    }

    /// <summary>
    /// Возвращает диапазон непустой части указанного фрагмента тела команды.
    /// </summary>
    /// <param name="textRegion">Фрагмент тела команды.</param>
    /// <param name="span">Диапазон непустой части фрагмента.</param>
    /// <returns>Значение true, если фрагмент содержит непустой текст.</returns>
    public bool TryResolveRegion(string textRegion, out CommandIssueSpan span)
    {
      span = default;

      int start = 0;
      int end = textRegion.Length;

      while (start < end && char.IsWhiteSpace(textRegion[start]))
        start++;

      while (end > start && char.IsWhiteSpace(textRegion[end - 1]))
        end--;

      return end > start && TryResolve(start, end - start, out span);
    }

    /// <summary>
    /// Преобразует позицию в тексте тела команды в диапазон исходного документа.
    /// </summary>
    /// <param name="bodyStart">Индекс начала диапазона в тексте тела команды.</param>
    /// <param name="length">Длина диапазона в тексте тела команды.</param>
    /// <param name="span">Диапазон исходного документа.</param>
    /// <returns>Значение true, если позицию удалось сопоставить с исходным документом.</returns>
    public bool TryResolve(int bodyStart, int length, out CommandIssueSpan span)
    {
      length = Math.Max(1, length);
      int bodyEnd = bodyStart + length;

      foreach (var segment in _segments)
      {
        int segmentEnd = segment.BodyStart + segment.Length;
        if (bodyStart >= segmentEnd || bodyEnd <= segment.BodyStart)
          continue;

        int startInSegment = Math.Max(0, bodyStart - segment.BodyStart);
        int endInSegment = Math.Min(segment.Length, bodyEnd - segment.BodyStart);
        return TryResolveSegment(segment, startInSegment, out span, endInSegment - startInSegment);
      }

      var nearest = _segments
        .Where(segment => segment.Length > 0)
        .OrderBy(segment => Math.Abs(segment.BodyStart - bodyStart))
        .FirstOrDefault();

      int nearestStart = nearest.Length > 0
        ? Math.Clamp(bodyStart - nearest.BodyStart, 0, nearest.Length - 1)
        : 0;

      return TryResolveSegment(nearest, nearestStart, out span);
    }

    private static bool TryResolveSegment(
      CommandBodySegment segment,
      int startInSegment,
      out CommandIssueSpan span,
      int length = -1)
    {
      if (segment.Length <= 0)
      {
        span = default;
        return false;
      }

      int safeStart = Math.Clamp(startInSegment, 0, Math.Max(0, segment.Length - 1));
      int safeLength = length < 0
        ? segment.Length - safeStart
        : Math.Clamp(length, 1, segment.Length - safeStart);

      span = new CommandIssueSpan(
        segment.SourceOffset + safeStart,
        safeLength,
        segment.LineNumber,
        segment.ColumnNumber + safeStart);
      return true;
    }

    private static bool TryGetCommandBodyPart(
      string lineText,
      bool isFirstLine,
      out int startIndex,
      out int length)
    {
      startIndex = 0;
      length = lineText.Length;

      if (isFirstLine)
      {
        var headerMatch = CommandHeaderRegex.Match(lineText);
        if (headerMatch.Success)
        {
          startIndex = headerMatch.Groups["body"].Index;
          length = headerMatch.Groups["body"].Length;
        }
      }

      TrimRange(lineText, ref startIndex, ref length);
      return length > 0;
    }

    private static void TrimRange(string text, ref int startIndex, ref int length)
    {
      int endIndex = startIndex + length;

      while (startIndex < endIndex && char.IsWhiteSpace(text[startIndex]))
        startIndex++;

      while (endIndex > startIndex && char.IsWhiteSpace(text[endIndex - 1]))
        endIndex--;

      length = endIndex - startIndex;
    }

    private static (string Text, IReadOnlyList<int> IndexMap) BuildCompactIndex(string text)
    {
      var compactTextBuilder = new StringBuilder();
      var compactIndexMap = new List<int>();

      for (int i = 0; i < text.Length; i++)
      {
        if (char.IsWhiteSpace(text[i]))
          continue;

        compactIndexMap.Add(i);
        compactTextBuilder.Append(text[i]);
      }

      return (compactTextBuilder.ToString(), compactIndexMap);
    }

    private static string TrimCandidate(string value)
    {
      return (value ?? string.Empty).Trim().Trim('.', ',', ';', ':', ' ', '"', '\'');
    }

    /// <summary>
    /// Описывает непрерывный фрагмент исходного документа,
    /// вошедший в нормализованное тело команды.
    /// </summary>
    private readonly struct CommandBodySegment
    {
      /// <summary>
      /// Создаёт сегмент тела команды.
      /// </summary>
      /// <param name="bodyStart">Индекс начала сегмента в тексте тела команды.</param>
      /// <param name="length">Длина сегмента.</param>
      /// <param name="sourceOffset">Абсолютное смещение сегмента в исходном документе.</param>
      /// <param name="lineNumber">Номер строки сегмента в исходном документе.</param>
      /// <param name="columnNumber">Номер колонки сегмента в исходном документе.</param>
      public CommandBodySegment(
        int bodyStart,
        int length,
        int sourceOffset,
        int lineNumber,
        int columnNumber)
      {
        BodyStart = bodyStart;
        Length = length;
        SourceOffset = sourceOffset;
        LineNumber = lineNumber;
        ColumnNumber = columnNumber;
      }

      /// <summary>
      /// Индекс начала сегмента в тексте тела команды.
      /// </summary>
      public int BodyStart { get; }

      /// <summary>
      /// Длина сегмента.
      /// </summary>
      public int Length { get; }

      /// <summary>
      /// Абсолютное смещение сегмента в исходном документе.
      /// </summary>
      public int SourceOffset { get; }

      /// <summary>
      /// Номер строки сегмента в исходном документе.
      /// </summary>
      public int LineNumber { get; }

      /// <summary>
      /// Номер колонки сегмента в исходном документе.
      /// </summary>
      public int ColumnNumber { get; }
    }
  }
}

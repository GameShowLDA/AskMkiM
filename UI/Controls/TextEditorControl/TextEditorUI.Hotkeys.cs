using Ask.Core.Services.FilesUtility;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Input;
using Ask.Core.Shared.Metadata.Enums.FileEnums;

namespace UI.Controls.TextEditorControl
{
  /// <summary>
  /// Часть класса <see cref="TextEditorUI"/>, отвечающая за обработку
  /// горячих клавиш текстового редактора. Включает логику:
  /// 
  /// • обработки Ctrl+M с поддержкой двойного нажатия для сворачивания блоков;
  /// • масштабирования текста через Ctrl + '+', Ctrl + '-' и Ctrl + '0';
  /// • выполнения печати содержимого по Ctrl+P;
  /// • маршрутизации нажатий клавиш в специализированные обработчики;
  /// • управления вспомогательным состоянием (флагом повторного Ctrl+M).
  /// 
  /// Логика вынесена в отдельный partial-файл для повышения читаемости,
  /// удобства поддержки и разделения ответственности внутри класса редактора.
  /// </summary>
  public partial class TextEditorUI
  {
    private static readonly Regex CommandHeaderRegex = new(@"^\s*\d+\s+\S+", RegexOptions.Compiled);
    private const string LineCommentPrefix = "//";
    private bool _ctrlMPressed = false;
    private bool _ctrlKPressed = false;
    private DateTime _lastCtrlMTime = DateTime.MinValue;
    private DateTime _lastCtrlKTime = DateTime.MinValue;
    private const int CtrlMTimeoutMs = 1000;
    private const int CtrlKTimeoutMs = 1500;

    /// <summary>
    /// Главный обработчик нажатия клавиш в текстовом редакторе.
    /// Делегирует обработку хоткеев в специализированные методы.
    /// </summary>
    private void TextEditor_PreviewKeyDown(object sender, KeyEventArgs e)
    {
      if (HandleAutoIndentOnEnter(e)) return;
      if (HandleRedoShortcut(e)) return;
      if (HandleMoveLinesShortcut(e)) return;
      if (HandleBreakpointShortcut(e)) return;
      if (HandleCtrlM(e)) return;
      if (HandleCtrlKChord(e)) return;
      ResetCtrlMFlagIfNeeded(e);
      if (HandleZoomShortcuts(e)) return;
      if (HandlePrintShortcut(e)) return;
    }

    /// <summary>
    /// Обрабатывает комбинацию Ctrl+M с поддержкой двойного нажатия.
    /// При быстром повторном нажатии выполняет сворачивание/разворачивание блока.
    /// Возвращает true, если событие обработано.
    /// </summary>
    private bool HandleCtrlM(KeyEventArgs e)
    {
      if (e.Key != Key.M || Keyboard.Modifiers != ModifierKeys.Control)
        return false;

      var now = DateTime.Now;

      if (_ctrlMPressed && (now - _lastCtrlMTime).TotalMilliseconds < CtrlMTimeoutMs)
      {
        ToggleCurrentFolding();
        _ctrlMPressed = false;
        e.Handled = true;
      }
      else
      {
        _ctrlMPressed = true;
        _lastCtrlMTime = now;
        e.Handled = true;
      }

      return true;
    }

    /// <summary>
    /// Сбрасывает флаг ожидания второго нажатия Ctrl+M,
    /// если пользователь нажал любую другую клавишу.
    /// </summary>
    private void ResetCtrlMFlagIfNeeded(KeyEventArgs e)
    {
      if (e.Key != Key.M)
        _ctrlMPressed = false;
    }

    private bool HandleCtrlKChord(KeyEventArgs e)
    {
      var key = e.Key == Key.System ? e.SystemKey : e.Key;

      if (_ctrlKPressed)
      {
        if ((DateTime.Now - _lastCtrlKTime).TotalMilliseconds >= CtrlKTimeoutMs)
        {
          _ctrlKPressed = false;
        }
        else if (Keyboard.Modifiers == ModifierKeys.Control)
        {
          if (key == Key.C)
          {
            _ctrlKPressed = false;
            ToggleLineComments(comment: true);
            e.Handled = true;
            return true;
          }

          if (key == Key.U)
          {
            _ctrlKPressed = false;
            ToggleLineComments(comment: false);
            e.Handled = true;
            return true;
          }

          if (key == Key.D)
          {
            _ctrlKPressed = false;
            FormatProgramText();
            e.Handled = true;
            return true;
          }
        }

        if (key is not Key.LeftCtrl and not Key.RightCtrl)
        {
          _ctrlKPressed = false;
        }
      }

      if (key != Key.K || Keyboard.Modifiers != ModifierKeys.Control)
        return false;

      _ctrlKPressed = true;
      _lastCtrlKTime = DateTime.Now;
      e.Handled = true;
      return true;
    }

    private bool HandleRedoShortcut(KeyEventArgs e)
    {
      var key = e.Key == Key.System ? e.SystemKey : e.Key;

      if (key != Key.Z || Keyboard.Modifiers != (ModifierKeys.Control | ModifierKeys.Shift))
        return false;

      if (!textEditor.CanRedo)
        return true;

      textEditor.Redo();
      e.Handled = true;
      return true;
    }

    private bool HandleMoveLinesShortcut(KeyEventArgs e)
    {
      var key = e.Key == Key.System ? e.SystemKey : e.Key;

      if (Keyboard.Modifiers != ModifierKeys.Alt)
        return false;

      if (key == Key.Up)
      {
        e.Handled = MoveSelectedLines(moveUp: true);
        return e.Handled;
      }

      if (key == Key.Down)
      {
        e.Handled = MoveSelectedLines(moveUp: false);
        return e.Handled;
      }

      return false;
    }

    private bool MoveSelectedLines(bool moveUp)
    {
      if (textEditor.IsReadOnly || textEditor.Document == null)
        return false;

      var document = textEditor.Document;
      var selection = textEditor.TextArea.Selection;
      int selectionStart = textEditor.SelectionStart;
      int selectionLength = textEditor.SelectionLength;
      int caretOffset = textEditor.CaretOffset;
      var (startLineNumber, endLineNumber) = selection is ICSharpCode.AvalonEdit.Editing.RectangleSelection rectangleSelection && HasRectangularLineSelection(rectangleSelection)
        ? GetSelectedLineRange(rectangleSelection, document, caretOffset)
        : GetSelectedLineRange(document, selectionStart, selectionLength, caretOffset);

      if ((moveUp && startLineNumber <= 1) || (!moveUp && endLineNumber >= document.LineCount))
        return false;

      var firstSelectedLine = document.GetLineByNumber(startLineNumber);
      int selectionStartWithinBlock = selectionStart - firstSelectedLine.Offset;
      int caretOffsetWithinBlock = caretOffset - firstSelectedLine.Offset;

      int rangeStartLineNumber = moveUp ? startLineNumber - 1 : startLineNumber;
      int rangeEndLineNumber = moveUp ? endLineNumber : endLineNumber + 1;
      int lineDelta = moveUp ? -1 : 1;

      var rangeFirstLine = document.GetLineByNumber(rangeStartLineNumber);
      var rangeLastLine = document.GetLineByNumber(rangeEndLineNumber);
      int replaceOffset = rangeFirstLine.Offset;
      int replaceLength = GetLineBlockLength(rangeFirstLine, rangeLastLine);

      string replacementText = BuildMovedLineBlock(document, rangeStartLineNumber, rangeEndLineNumber, moveUp);

      document.BeginUpdate();
      try
      {
        document.Replace(replaceOffset, replaceLength, replacementText);
      }
      finally
      {
        document.EndUpdate();
      }

      int newStartLineNumber = startLineNumber + lineDelta;

      if (TryRestoreRectangularSelection(selection, lineDelta))
      {
        textEditor.ScrollToLine(newStartLineNumber);
        return true;
      }

      int newSelectionStart = document.GetLineByNumber(newStartLineNumber).Offset + selectionStartWithinBlock;

      if (selectionLength > 0)
      {
        textEditor.Select(newSelectionStart, selectionLength);
      }
      else
      {
        textEditor.CaretOffset = newSelectionStart + caretOffsetWithinBlock - selectionStartWithinBlock;
      }

      textEditor.ScrollToLine(newStartLineNumber);
      return true;
    }

    private bool ToggleLineComments(bool comment)
    {
      if (textEditor.IsReadOnly || textEditor.Document == null)
        return false;

      var document = textEditor.Document;
      int selectionStart = textEditor.SelectionStart;
      int selectionLength = textEditor.SelectionLength;
      int selectionEnd = selectionStart + selectionLength;
      int caretOffset = textEditor.CaretOffset;

      int startLineNumber = selectionLength == 0
        ? document.GetLineByOffset(caretOffset).LineNumber
        : document.GetLineByOffset(selectionStart).LineNumber;
      int endLineNumber = selectionLength == 0
        ? startLineNumber
        : document.GetLineByOffset(Math.Max(selectionStart, selectionEnd - 1)).LineNumber;

      var firstLine = document.GetLineByNumber(startLineNumber);
      var lastLine = document.GetLineByNumber(endLineNumber);

      var lineUpdates = new List<(int EditOffset, int Delta)>(endLineNumber - startLineNumber + 1);

      document.BeginUpdate();
      try
      {
        for (int lineNumber = endLineNumber; lineNumber >= startLineNumber; lineNumber--)
        {
          var line = document.GetLineByNumber(lineNumber);
          string lineText = document.GetText(line.Offset, line.Length);
          var update = comment
            ? CommentLine(document, line, lineText)
            : UncommentLine(document, line, lineText);

          lineUpdates.Add(update);
        }
      }
      finally
      {
        document.EndUpdate();
      }

      if (selectionLength == 0)
      {
        textEditor.CaretOffset = ShiftOffset(caretOffset, lineUpdates);
        return true;
      }

      firstLine = document.GetLineByNumber(startLineNumber);
      lastLine = document.GetLineByNumber(endLineNumber);
      textEditor.Select(firstLine.Offset, lastLine.EndOffset - firstLine.Offset);
      return true;
    }

    private static (int EditOffset, int Delta) CommentLine(ICSharpCode.AvalonEdit.Document.TextDocument document, ICSharpCode.AvalonEdit.Document.DocumentLine line, string lineText)
    {
      int insertOffset = line.Offset + GetLeadingWhitespace(lineText).Length;
      document.Insert(insertOffset, LineCommentPrefix);
      return (insertOffset, LineCommentPrefix.Length);
    }

    private static (int EditOffset, int Delta) UncommentLine(ICSharpCode.AvalonEdit.Document.TextDocument document, ICSharpCode.AvalonEdit.Document.DocumentLine line, string lineText)
    {
      int whitespaceLength = GetLeadingWhitespace(lineText).Length;
      if (!lineText.AsSpan(whitespaceLength).StartsWith(LineCommentPrefix, StringComparison.Ordinal))
        return (line.Offset, 0);

      int removeOffset = line.Offset + whitespaceLength;
      document.Remove(removeOffset, LineCommentPrefix.Length);
      return (removeOffset, -LineCommentPrefix.Length);
    }

    private static int ShiftOffset(int offset, IEnumerable<(int EditOffset, int Delta)> lineUpdates)
    {
      int shiftedOffset = offset;

      foreach (var (editOffset, delta) in lineUpdates)
      {
        if (delta == 0 || editOffset >= shiftedOffset)
          continue;

        shiftedOffset += delta;
      }

      return shiftedOffset;
    }

    private bool TryRestoreRectangularSelection(ICSharpCode.AvalonEdit.Editing.Selection selection, int lineDelta)
    {
      if (selection is not ICSharpCode.AvalonEdit.Editing.RectangleSelection rectangleSelection || !HasRectangularLineSelection(rectangleSelection))
        return false;

      var newStartPosition = ShiftTextViewPosition(rectangleSelection.StartPosition, lineDelta);
      var newEndPosition = ShiftTextViewPosition(rectangleSelection.EndPosition, lineDelta);

      textEditor.TextArea.Selection = new ICSharpCode.AvalonEdit.Editing.RectangleSelection(
        textEditor.TextArea,
        newStartPosition,
        newEndPosition);
      textEditor.TextArea.Caret.Position = newEndPosition;
      return true;
    }

    private static ICSharpCode.AvalonEdit.TextViewPosition ShiftTextViewPosition(
      ICSharpCode.AvalonEdit.TextViewPosition position,
      int lineDelta)
    {
      return new ICSharpCode.AvalonEdit.TextViewPosition(
        position.Line + lineDelta,
        position.Column,
        position.VisualColumn);
    }

    private static bool HasRectangularLineSelection(ICSharpCode.AvalonEdit.Editing.RectangleSelection selection)
    {
      return !selection.IsEmpty || selection.StartPosition.Line != selection.EndPosition.Line;
    }

    private static (int StartLineNumber, int EndLineNumber) GetSelectedLineRange(
      ICSharpCode.AvalonEdit.Document.TextDocument document,
      int selectionStart,
      int selectionLength,
      int caretOffset)
    {
      int selectionEnd = selectionStart + selectionLength;
      int startLineNumber = selectionLength == 0
        ? document.GetLineByOffset(caretOffset).LineNumber
        : document.GetLineByOffset(selectionStart).LineNumber;
      int endLineNumber = selectionLength == 0
        ? startLineNumber
        : document.GetLineByOffset(Math.Max(selectionStart, selectionEnd - 1)).LineNumber;

      return (startLineNumber, endLineNumber);
    }

    private static (int StartLineNumber, int EndLineNumber) GetSelectedLineRange(
      ICSharpCode.AvalonEdit.Editing.Selection selection,
      ICSharpCode.AvalonEdit.Document.TextDocument document,
      int caretOffset)
    {
      if (selection == null)
      {
        int lineNumber = document.GetLineByOffset(caretOffset).LineNumber;
        return (lineNumber, lineNumber);
      }

      if (selection.IsEmpty && selection.StartPosition.Line == selection.EndPosition.Line)
      {
        int lineNumber = document.GetLineByOffset(caretOffset).LineNumber;
        return (lineNumber, lineNumber);
      }

      return (
        Math.Min(selection.StartPosition.Line, selection.EndPosition.Line),
        Math.Max(selection.StartPosition.Line, selection.EndPosition.Line));
    }

    private static int GetLineBlockLength(
      ICSharpCode.AvalonEdit.Document.DocumentLine firstLine,
      ICSharpCode.AvalonEdit.Document.DocumentLine lastLine)
    {
      return lastLine.Offset + lastLine.TotalLength - firstLine.Offset;
    }

    // Keeps existing line separators on their original positions, including the EOF case without a trailing newline.
    private static string BuildMovedLineBlock(
      ICSharpCode.AvalonEdit.Document.TextDocument document,
      int startLineNumber,
      int endLineNumber,
      bool moveUp)
    {
      var segments = new List<(string Content, string Delimiter)>(endLineNumber - startLineNumber + 1);

      for (int lineNumber = startLineNumber; lineNumber <= endLineNumber; lineNumber++)
      {
        var line = document.GetLineByNumber(lineNumber);
        segments.Add((
          document.GetText(line.Offset, line.Length),
          document.GetText(line.EndOffset, line.DelimiterLength)));
      }

      var builder = new StringBuilder();
      int contentStartIndex = moveUp ? 1 : segments.Count - 1;

      for (int i = 0; i < segments.Count; i++)
      {
        int contentIndex = moveUp
          ? (contentStartIndex + i) % segments.Count
          : (contentStartIndex + i) % segments.Count;

        builder.Append(segments[contentIndex].Content);
        builder.Append(segments[i].Delimiter);
      }

      return builder.ToString();
    }

    private void FormatProgramText()
    {
      if (textEditor.IsReadOnly || textEditor.Document == null)
        return;

      if (!SupportsProgramFormatting())
        return;

      string sourceText = textEditor.Text ?? string.Empty;
      string formattedText = NormalizeProgramWhitespace(sourceText);

      if (string.Equals(sourceText, formattedText, StringComparison.Ordinal))
        return;

      var document = textEditor.Document;
      int selectionStart = Math.Min(textEditor.SelectionStart, formattedText.Length);
      int selectionLength = textEditor.SelectionLength;
      int caretOffset = Math.Min(textEditor.CaretOffset, formattedText.Length);

      document.Replace(0, document.TextLength, formattedText);

      if (selectionLength > 0)
      {
        int maxSelectionLength = Math.Max(0, formattedText.Length - selectionStart);
        textEditor.Select(selectionStart, Math.Min(selectionLength, maxSelectionLength));
        return;
      }

      textEditor.CaretOffset = caretOffset;
    }

    private static string NormalizeProgramWhitespace(string text)
    {
      if (string.IsNullOrEmpty(text))
        return string.Empty;

      var lines = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
      var formattedLines = new List<string>(lines.Length);
      string? blockCommentIndent = null;
      string? blockCommentCloseToken = null;
      bool hasReachedFirstCommand = false;
      bool hasReachedEndCommand = false;

      for (int i = 0; i < lines.Length; i++)
      {
        string rawLine = lines[i];
        string line = rawLine.TrimEnd(' ', '\t');
        if (!hasReachedFirstCommand)
        {
          if (CommandHeaderRegex.IsMatch(line))
          {
            hasReachedFirstCommand = true;
          }
          else
          {
            formattedLines.Add(rawLine);
            continue;
          }
        }
        else if (hasReachedEndCommand)
        {
          formattedLines.Add(rawLine);
          continue;
        }

        if (string.IsNullOrWhiteSpace(line))
        {
          formattedLines.Add(string.Empty);
          continue;
        }

        string trimmedLine = line.TrimStart(' ', '\t');

        if (blockCommentIndent != null)
        {
          if (trimmedLine.StartsWith(blockCommentCloseToken, StringComparison.Ordinal))
          {
            formattedLines.Add(blockCommentIndent + blockCommentCloseToken);
            blockCommentIndent = null;
            blockCommentCloseToken = null;
          }
          else
          {
            formattedLines.Add(blockCommentIndent + " " + trimmedLine);
          }

          continue;
        }

        string originalIndent = GetLeadingWhitespace(line);

        if (trimmedLine.StartsWith("{", StringComparison.Ordinal))
        {
          blockCommentIndent = originalIndent;
          blockCommentCloseToken = "}";
          formattedLines.Add(blockCommentIndent + "{");

          string commentBody = trimmedLine[1..].TrimStart(' ', '\t');
          if (!string.IsNullOrEmpty(commentBody))
          {
            if (commentBody == "}")
            {
              formattedLines.Add(blockCommentIndent + "}");
              blockCommentIndent = null;
              blockCommentCloseToken = null;
            }
            else if (commentBody.EndsWith("}", StringComparison.Ordinal))
            {
              string inlineBody = commentBody[..^1].TrimEnd(' ', '\t');
              if (!string.IsNullOrEmpty(inlineBody))
              {
                formattedLines.Add(blockCommentIndent + " " + inlineBody);
              }

              formattedLines.Add(blockCommentIndent + "}");
              blockCommentIndent = null;
              blockCommentCloseToken = null;
            }
            else
            {
              formattedLines.Add(blockCommentIndent + " " + commentBody);
            }
          }

          continue;
        }

        if (trimmedLine.StartsWith("/*", StringComparison.Ordinal))
        {
          blockCommentIndent = originalIndent;
          blockCommentCloseToken = "*/";
          formattedLines.Add(blockCommentIndent + "/*");

          string commentBody = trimmedLine[2..].TrimStart(' ', '\t');
          if (!string.IsNullOrEmpty(commentBody))
          {
            if (commentBody == "*/")
            {
              formattedLines.Add(blockCommentIndent + "*/");
              blockCommentIndent = null;
              blockCommentCloseToken = null;
            }
            else if (commentBody.EndsWith("*/", StringComparison.Ordinal))
            {
              string inlineBody = commentBody[..^2].TrimEnd(' ', '\t');
              if (!string.IsNullOrEmpty(inlineBody))
              {
                formattedLines.Add(blockCommentIndent + " " + inlineBody);
              }

              formattedLines.Add(blockCommentIndent + "*/");
              blockCommentIndent = null;
              blockCommentCloseToken = null;
            }
            else
            {
              formattedLines.Add(blockCommentIndent + " " + commentBody);
            }
          }

          continue;
        }

        if (CommandHeaderRegex.IsMatch(line))
        {
          formattedLines.Add(NormalizeCommandHeader(line));
        }
        else
        {
          formattedLines.Add("\t" + trimmedLine);
        }

        if (IsEndCommandLine(line))
        {
          hasReachedEndCommand = true;
        }
      }

      return string.Join(Environment.NewLine, formattedLines);
    }

    private static string NormalizeCommandHeader(string line)
    {
      string trimmedLine = line.TrimStart(' ', '\t');
      var match = Regex.Match(trimmedLine, @"^(\d+)\s+(\S+)(.*)$");
      if (!match.Success)
        return trimmedLine;

      string tail = match.Groups[3].Value;
      return $"{match.Groups[1].Value} {match.Groups[2].Value}{tail}";
    }

    private static bool IsEndCommandLine(string line)
    {
      string trimmedLine = line.TrimStart(' ', '\t');
      var match = Regex.Match(trimmedLine, @"^(\d+)\s+(\S+)(.*)$");
      return match.Success && string.Equals(match.Groups[2].Value, "КЦ", StringComparison.OrdinalIgnoreCase);
    }

    private bool SupportsProgramFormatting()
    {
      if (FileType is FileType.PK or FileType.PKW or FileType.OPK or FileType.OPKW)
        return true;

      string? fileName = TextEditorModel?.FileName ?? TextEditorModel?.FilePath;
      if (string.IsNullOrWhiteSpace(fileName))
        return false;

      string extension = System.IO.Path.GetExtension(fileName);
      return extension.Equals(".pk", StringComparison.OrdinalIgnoreCase)
        || extension.Equals(".pkw", StringComparison.OrdinalIgnoreCase)
        || extension.Equals(".opk", StringComparison.OrdinalIgnoreCase)
        || extension.Equals(".opkw", StringComparison.OrdinalIgnoreCase)
        || extension.Equals(".acs", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Обрабатывает масштабирование редактора:
    /// Ctrl + '+' — увеличение масштаба;
    /// Ctrl + '-' — уменьшение масштаба;
    /// Ctrl + '0' — сброс масштаба.
    /// Возвращает true, если событие обработано.
    /// </summary>
    private bool HandleZoomShortcuts(KeyEventArgs e)
    {
      if ((Keyboard.Modifiers & ModifierKeys.Control) != ModifierKeys.Control)
        return false;

      switch (e.Key)
      {
        case Key.OemPlus:
        case Key.Add:
          Zoom(true);
          e.Handled = true;
          return true;

        case Key.OemMinus:
        case Key.Subtract:
          Zoom(false);
          e.Handled = true;
          return true;

        case Key.D0:
        case Key.NumPad0:
          ResetZoom();
          e.Handled = true;
          return true;
      }

      return false;
    }

    /// <summary>
    /// Обрабатывает сочетание Ctrl+P для отправки текста в печать.
    /// Возвращает true, если событие обработано.
    /// </summary>
    private bool HandlePrintShortcut(KeyEventArgs e)
    {
      if (e.Key == Key.P && Keyboard.Modifiers == ModifierKeys.Control)
      {
        e.Handled = true;
        TextPrintHelper.PrintText(textEditor.Text, "Печать редактора");
        return true;
      }

      return false;
    }

    /// <summary>
    /// Обрабатывает клавишу F9 — переключение точки остановки
    /// на текущей строке каретки.
    /// </summary>
    private bool HandleBreakpointShortcut(KeyEventArgs e)
    {
      if (e.Key != Key.F9)
        return false;

      if (!ToggleBreakpointFromMousePosition())
        ToggleBreakpointFromKeyboardAtCaret();

      e.Handled = true;
      return true;
    }

    public bool ToggleBreakpointFromMousePosition()
    {
      if (!_executionMargin.BreakpointsInteractive)
        return true;

      if (!TryGetDocumentLineUnderMouse(out int lineNumber))
        return false;

      ToggleBreakpointFromKeyboardAtLine(lineNumber);
      return true;
    }

    public void ToggleBreakpointFromKeyboardAtCaret()
    {
      int lineNumber = textEditor.TextArea.Caret.Line;
      ToggleBreakpointFromKeyboardAtLine(lineNumber);
    }

    public void ToggleBreakpointFromKeyboardAtLine(int lineNumber)
    {
      if (!_executionMargin.BreakpointsInteractive)
        return;

      _executionMargin.ToggleBreakpointFromKeyboard(lineNumber);
    }

    private bool TryGetDocumentLineUnderMouse(out int lineNumber)
    {
      lineNumber = -1;

      var document = textEditor.Document;
      var textView = textEditor.TextArea?.TextView;
      if (document == null || textView == null || !textEditor.IsMouseOver)
        return false;

      textView.EnsureVisualLines();

      var position = Mouse.GetPosition(textView);
      if (position.Y < 0 || position.Y > textView.ActualHeight)
        return false;

      var documentLine = textView.GetDocumentLineByVisualTop(position.Y + textView.ScrollOffset.Y);
      if (documentLine == null || documentLine.LineNumber < 1 || documentLine.LineNumber > document.LineCount)
        return false;

      lineNumber = documentLine.LineNumber;
      return true;
    }

    /// <summary>
    /// Автоотступ при переносе строки:
    /// копирует текущий отступ и добавляет Tab после заголовка команды.
    /// </summary>
    private bool HandleAutoIndentOnEnter(KeyEventArgs e)
    {
      if (e.Key != Key.Return && e.Key != Key.Enter)
        return false;

      if (textEditor.IsReadOnly || textEditor.Document == null)
        return false;

      var caret = textEditor.TextArea.Caret;
      int lineNumber = caret.Line;
      if (lineNumber <= 0 || lineNumber > textEditor.Document.LineCount)
        return false;

      var line = textEditor.Document.GetLineByNumber(lineNumber);
      string lineText = textEditor.Document.GetText(line.Offset, line.Length);
      string indent = GetLeadingWhitespace(lineText);

      if (indent.Length == 0 && CommandHeaderRegex.IsMatch(lineText))
      {
        indent = "\t";
      }

      string newLine = textEditor.Document.GetLineByNumber(1).DelimiterLength > 0 ? "\r\n" : Environment.NewLine;
      textEditor.Document.Insert(caret.Offset, $"{newLine}{indent}");
      caret.Offset += newLine.Length + indent.Length;
      e.Handled = true;
      return true;
    }

    private static string GetLeadingWhitespace(string text)
    {
      int i = 0;
      while (i < text.Length && char.IsWhiteSpace(text[i]))
      {
        i++;
      }

      return i == 0 ? string.Empty : text[..i];
    }
  }
}

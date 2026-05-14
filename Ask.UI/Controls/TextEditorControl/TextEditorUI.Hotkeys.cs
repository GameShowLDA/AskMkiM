using Ask.Core.Services.FilesUtility;
using System.Text.RegularExpressions;
using System.Windows.Input;

namespace Ask.UI.Controls.TextEditorControl
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

      if (!_executionMargin.BreakpointsInteractive)
        return true;

      int lineNumber = textEditor.TextArea.Caret.Line;

      _executionMargin.ToggleBreakpointFromKeyboard(lineNumber);

      e.Handled = true;
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

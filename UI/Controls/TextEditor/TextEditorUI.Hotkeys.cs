using Ask.Core.Services.FilesUtility;
using System.Windows.Input;

namespace UI.Controls.TextEditor
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
    private bool _ctrlMPressed = false;
    private DateTime _lastCtrlMTime = DateTime.MinValue;
    private const int CtrlMTimeoutMs = 1000;

    /// <summary>
    /// Главный обработчик нажатия клавиш в текстовом редакторе.
    /// Делегирует обработку хоткеев в специализированные методы.
    /// </summary>
    private void TextEditor_PreviewKeyDown(object sender, KeyEventArgs e)
    {
      if (HandleBreakpointShortcut(e)) return;
      if (HandleCtrlM(e)) return;
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
  }
}

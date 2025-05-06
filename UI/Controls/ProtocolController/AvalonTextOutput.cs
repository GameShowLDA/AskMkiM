using ICSharpCode.AvalonEdit;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using UI.Controls.ProtocolController;
using Utilities.Models;
using static Utilities.LoggerUtility;

public class AvalonTextOutput
{
  private readonly TextEditor _editor;
  private readonly Stopwatch _stopwatch;
  private readonly Protocol _protocol;

  public AvalonTextOutput(TextEditor editor, Protocol protocol)
  {
    _editor = editor ?? throw new ArgumentNullException(nameof(editor));
    _stopwatch = Stopwatch.StartNew();
    _protocol = protocol;
  }

  /// <summary>
  /// Асинхронно добавляет строку в AvalonEdit.
  /// </summary>
  /// <param name="model">Модель сообщения.</param>
  public async Task AppendLineAsync(ShowMessageModel model)
  {
    (string header, Color? headerColor, Color? messageColor) = SetDefaultValues(model.Header, model.HeaderColor, model.MessageColor);
    string indent = new string(' ', model.IndentLevel * 2);

    await Application.Current.Dispatcher.InvokeAsync(() =>
    {
      string timePart = model.IsDeviceMessage ? $" [{_stopwatch.Elapsed:mm\\:ss\\.fff}]" : "";
      string idlePart = model.IsDeviceMessage && AppConfiguration.Execution.ExecutionConfig.GetIsIdleModeEnabled().Result
                        ? " | Холостой режим"
                        : "";

      string line = string.Empty;

      if (!string.IsNullOrWhiteSpace(header) && !string.IsNullOrWhiteSpace(model.Message))
        line = $"{indent}{header}: {model.Message}{timePart}{idlePart}";
      else if (!string.IsNullOrWhiteSpace(header))
        line = $"{indent}{header}{timePart}{idlePart}";
      else if (!string.IsNullOrWhiteSpace(model.Message))
        line = $"{indent}{model.Message}{timePart}{idlePart}";

      // Добавляем строку
      _editor.AppendText(line + Environment.NewLine);

      // Очищаем предыдущие трансформеры и подключаем новый
      _editor.TextArea.TextView.LineTransformers.Clear();
      _editor.TextArea.TextView.LineTransformers.Add(
        new ProtocolColorizingTransformer(headerColor.Value, messageColor.Value)
      );

      _editor.ScrollToEnd();
    }, DispatcherPriority.Background);

    await _protocol.protocolExecutionRunner?.PauseManager.WaitWhilePausedAsync(_protocol);
    await _protocol.StepManager?.WaitIfStepModeAsync();
  }


  /// <summary>
  /// Устанавливает значения по умолчанию для заголовка и цветов.
  /// </summary>
  private (string header, Color? headerColor, Color? messageColor) SetDefaultValues(string header, Color? headerColor, Color? messageColor)
  {
    return (
        header ?? string.Empty,
        headerColor ?? Colors.White,
        messageColor ?? Colors.White
    );
  }

  /// <summary>
  /// Асинхронно удаляет указанное количество последних строк из AvalonEdit.
  /// </summary>
  /// <param name="count">Количество строк для удаления. По умолчанию 1.</param>
  /// <returns>Количество фактически удалённых строк.</returns>
  public async Task<int> RemoveLastLinesAsync(int count = 1)
  {
    return await Application.Current.Dispatcher.InvokeAsync(() =>
    {
      try
      {
        var document = _editor.Document;
        var lines = document.Lines;
        if (lines.Count == 0)
          return 0;

        int linesToRemove = Math.Min(count, lines.Count);
        for (int i = 0; i < linesToRemove; i++)
        {
          var line = document.Lines[document.LineCount - 1 - i];
          document.Remove(line.Offset, line.TotalLength);
        }

        _editor.ScrollToEnd();
        return linesToRemove;
      }
      catch (Exception ex)
      {
        LogException("Ошибка при удалении строк", ex);
        return 0;
      }
    });
  }

  /// <summary>
  /// Асинхронно удаляет строку, содержащую указанный текст, из AvalonEdit.
  /// </summary>
  /// <param name="textToRemove">Строка для поиска и удаления.</param>
  /// <returns>True, если строка была найдена и удалена; иначе False.</returns>
  public async Task<bool> RemoveLineContainingTextAsync(string textToRemove)
  {
    return await Application.Current.Dispatcher.InvokeAsync(() =>
    {
      try
      {
        var document = _editor.Document;
        foreach (var line in document.Lines.Reverse())
        {
          var text = document.GetText(line);
          if (text.Contains(textToRemove))
          {
            document.Remove(line.Offset, line.TotalLength);
            _editor.ScrollToEnd();
            LogInformation($"Строка '{textToRemove}' найдена и удалена.");
            return true;
          }
        }

        foreach (var line in document.Lines.Reverse())
        {
          var text = document.GetText(line);
          if (textToRemove.Length > 5 && text.Contains(textToRemove.Substring(0, Math.Min(20, textToRemove.Length))))
          {
            document.Remove(line.Offset, line.TotalLength);
            _editor.ScrollToEnd();
            LogInformation($"Часть строки '{textToRemove}' найдена и удалена.");
            return true;
          }
        }

        LogWarning($"Строка '{textToRemove}' не найдена.");
        return false;
      }
      catch (Exception ex)
      {
        LogException("Ошибка при удалении строки", ex);
        return false;
      }
    });
  }

  /// <summary>
  /// Полностью очищает содержимое AvalonEdit и сбрасывает трансформеры.
  /// </summary>
  public async Task ClearAllAsync()
  {
    await Application.Current.Dispatcher.InvokeAsync(() =>
    {
      try
      {
        _editor.Clear();
        _editor.TextArea.TextView.LineTransformers.Clear();
        _stopwatch.Restart();
      }
      catch (Exception ex)
      {
        LogException("Ошибка при полной очистке редактора", ex);
      }
    });
  }

}

using System;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using ControlCommandAnalyser.Domain;
using ControlCommandAnalyser.Parsing;
using static Utilities.LoggerUtility;

namespace ControlCommandAnalyser.Translation
{
  /// <summary>
  /// Отвечает за трансляцию текста ПК в команды.
  /// </summary>
  public class TranslationManager
  {
    /// <summary>
    /// Делегат для передачи подсветки обратно в UI.
    /// </summary>
    public Action<List<HighlightRange>>? HighlightCallback { get; set; }

    public async Task Translate(string text)
    {
      if (string.IsNullOrWhiteSpace(text))
      {
        LogError("Пустой входной текст для трансляции.");
        return;
      }

      var lines = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
      var splitter = new CommandSplitter();
      var blocks = splitter.Split(lines);

      var recognizer = new CommandRecognizer();
      var results = await recognizer.RecognizeAsync(blocks);

      var highlights = new List<HighlightRange>();

      foreach (var result in results)
      {
        // Подсветка номера команды
        highlights.Add(new HighlightRange(
          line: result.LineIndex,
          start: 0,
          length: result.CommandNumber.Length,
          target: HighlightTarget.CommandNumber)
        {
          ColorOverride = Colors.DeepSkyBlue
        });

        // Подсветка мнемоники
        highlights.Add(new HighlightRange(
          line: result.LineIndex,
          start: result.CommandNumber.Length + 1, // пробел между номером и мнемоникой
          length: result.Mnemonic.Length,
          target: HighlightTarget.Mnemonic)
        {
          ColorOverride = result.IsRecognized ? Colors.LightGreen : Colors.Gray
        });

        if (result.ExtraHighlights is not null)
          highlights.AddRange(result.ExtraHighlights);
      }

      LogDebug($"Передаётся {highlights.Count} участков подсветки");

      await Application.Current.Dispatcher.InvokeAsync(async () =>
      {
        await Task.Delay(1);
        HighlightCallback?.Invoke(highlights);
      }, System.Windows.Threading.DispatcherPriority.Render);
    }
  }

}

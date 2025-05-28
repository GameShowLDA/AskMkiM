using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using ControlCommandAnalyser.Domain;
using ControlCommandAnalyser.Parsing;
using static Utilities.LoggerUtility;

namespace ControlCommandAnalyser.Translation
{
  /// <summary>
  /// Отвечает за трансляцию текста ПК в команды.
  /// Выполняет распознавание, форматирование, сборку текста и подсветку.
  /// </summary>
  public class TranslationManager
  {
    /// <summary>
    /// Делегат для передачи подсветки обратно в UI.
    /// </summary>
    public Action<List<HighlightRange>>? HighlightCallback { get; set; }

    /// <summary>
    /// Выполняет трансляцию текста, возвращает блоки и подсветку.
    /// </summary>
    /// <param name="text">Исходный текст из редактора.</param>
    /// <returns>Кортеж: список блоков и подсветок.</returns>
    public async Task<(List<CommandBlock> Blocks, List<HighlightRange> Highlights)> Translate(string text)
    {
      if (string.IsNullOrWhiteSpace(text))
      {
        LogError("Пустой входной текст для трансляции.");
        return (new List<CommandBlock>(), new List<HighlightRange>());
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
          ColorOverride = System.Windows.Media.Colors.DeepSkyBlue
        });

        // Подсветка мнемоники
        highlights.Add(new HighlightRange(
            line: result.LineIndex,
            start: result.CommandNumber.Length + 1,
            length: result.Mnemonic.Length,
            target: HighlightTarget.Mnemonic)
        {
          ColorOverride = result.IsRecognized ? System.Windows.Media.Colors.LightGreen : System.Windows.Media.Colors.Gray
        });

        if (result.ExtraHighlights is not null)
          highlights.AddRange(result.ExtraHighlights);
      }

      LogDebug($"Передаётся {highlights.Count} участков подсветки");

      return (blocks, highlights);
    }

    /// <summary>
    /// Собирает новый текст из блоков после трансляции.
    /// Использует отформатированные строки, если команда распознана, или исходные строки.
    /// </summary>
    /// <param name="blocks">Список блоков после трансляции.</param>
    /// <returns>Обновлённый текст для редактора.</returns>
    public string GetFormattedText(List<CommandBlock> blocks)
    {
      var result = new List<string>();

      foreach (var block in blocks)
      {
        if (block.IsRecognized && block.FormattedLines.Count > 0)
        {
          result.AddRange(block.FormattedLines);
        }
        else
        {
          result.AddRange(block.Lines);
        }
      }

      return string.Join(Environment.NewLine, result);
    }
  }
}

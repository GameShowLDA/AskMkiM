using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using ControlCommandAnalyser.Domain;
using ControlCommandAnalyser.Parsing;
using ControlCommandAnalyser.Parsing.Interface;
using Utilities.TextEditor;

namespace ControlCommandAnalyser
{
  /// <summary>
  /// Главный сервис, управляющий анализом и обработкой командного текста.
  /// Отвечает за разбиение текста на блоки, парсинг, подсветку и вывод.
  /// </summary>
  public class CommandAnalysisService
  {
    private readonly CommandParserRegistry _parserRegistry = new CommandParserRegistry();

    /// <summary>
    /// Находит и возвращает все блоки команд в исходном тексте.
    /// </summary>
    /// <param name="text">Исходный текст.</param>
    /// <returns>Список блоков команд.</returns>
    public List<CommandBlock> GetBlocks(string text)
    {
      return Services.BlockSplitter.Split(text);
    }

    /// <summary>
    /// Выполняет парсинг всех блоков: применяет подсветку номера, мнемоники и параметров,
    /// формирует новые блоки, если парсер их возвращает.
    /// </summary>
    /// <param name="blocks">Список исходных блоков команд.</param>
    /// <returns>
    /// Кортеж: список новых (или модифицированных) блоков и словарь с подсветкой по каждому блоку.
    /// </returns>
    public (List<CommandBlock> ParsedBlocks, Dictionary<CommandBlock, List<HighlightRange>> Highlights) ParseBlocks(List<CommandBlock> blocks)
    {
      var parsedBlocks = new List<CommandBlock>();
      var highlightsByBlock = new Dictionary<CommandBlock, List<HighlightRange>>();

      foreach (var block in blocks)
      {
        var highlights = new List<HighlightRange>();

        // Подсветка номера команды (всегда DeepSkyBlue)
        highlights.Add(new HighlightRange(0, 0, block.CommandNumber.Length, HighlightTarget.CommandNumber)
        {
          ColorOverride = Colors.DeepSkyBlue
        });

        // Поиск парсера для этой мнемоники
        var parser = _parserRegistry.FindParser(block.Mnemonic);

        // Подсветка мнемоники: зелёный — есть парсер, серый — нет
        highlights.Add(new HighlightRange(0, block.CommandNumber.Length + 1, block.Mnemonic.Length, HighlightTarget.Mnemonic)
        {
          ColorOverride = parser != null ? Colors.LightGreen : Colors.Gray
        });

        // Если есть парсер — применяем его (он может вернуть модифицированный блок)
        CommandBlock parsedBlock = block;
        if (parser != null)
        {
          var result = parser.Parse(block, out var extra);
          if (result != null)
            parsedBlock = result;
          if (extra != null)
            highlights.AddRange(extra);
        }

        parsedBlocks.Add(parsedBlock);
        highlightsByBlock[parsedBlock] = highlights;
      }

      return (parsedBlocks, highlightsByBlock);
    }

    /// <summary>
    /// Полный цикл: разбивает текст, парсит, подсвечивает и отображает в редакторе.
    /// </summary>
    /// <param name="text">Исходный текст.</param>
    /// <param name="editor">Интерфейс текстового редактора.</param>
    public void AnalyzeAndDisplay(string text, ITextEditorAdapter editor)
    {
      // 1. Разбиваем на блоки
      var blocks = GetBlocks(text);

      // 2. Парсим и подсвечиваем блоки, получаем новые блоки и подсветку
      var (parsedBlocks, highlightsByBlock) = ParseBlocks(blocks);

      // 3. Собираем финальный текст для отображения (используем FormattedLines, если есть)
      var lines = parsedBlocks.SelectMany(b =>
        b.FormattedLines != null && b.FormattedLines.Count > 0
          ? b.FormattedLines
          : b.Lines
      );
      var displayText = string.Join(Environment.NewLine, lines);

      // 4. Объединяем все подсветки с учётом смещения по строкам
      var highlights = new List<HighlightRange>();
      int lineIndex = 0;
      foreach (var block in parsedBlocks)
      {
        if (highlightsByBlock.TryGetValue(block, out var blockHighlights))
        {
          foreach (var range in blockHighlights)
          {
            highlights.Add(new HighlightRange(lineIndex + range.Line, range.Start, range.Length, range.Target)
            {
              ColorOverride = range.ColorOverride
            });
          }
        }
        // Для учёта многострочных блоков:
        lineIndex += (block.FormattedLines != null && block.FormattedLines.Count > 0)
          ? block.FormattedLines.Count
          : block.Lines.Count;
      }

      // 5. Выводим в редактор (один вызов)
      editor.SetTextAndHighlighting(displayText, highlights);
    }
  }
}

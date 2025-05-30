using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ControlCommandAnalyser.Domain;
using ControlCommandAnalyser.Parsing.Interface;
using static Utilities.LoggerUtility;

namespace ControlCommandAnalyser.Parsing
{
  /// <summary>
  /// Отвечает за распознавание команд в блоках текста.
  /// Вызывает парсеры команд и форматтеры строк на основе атрибутов.
  /// </summary>
  public class CommandRecognizer
  {
    private readonly Dictionary<string, ICommandParser> _parsers;
    private readonly Dictionary<string, ICommandFormatter> _formatters;

    /// <summary>
    /// Инициализирует распознаватель команд.
    /// Находит все реализации ICommandParser и ICommandFormatter по атрибутам.
    /// </summary>
    public CommandRecognizer()
    {
      // Инициализация парсеров команд
      _parsers = Assembly
          .GetExecutingAssembly()
          .GetTypes()
          .Where(t => typeof(ICommandParser).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
          .Select(t => (ICommandParser)Activator.CreateInstance(t)!)
          .ToDictionary(parser => parser.Mnemonic.ToUpperInvariant());

      // Инициализация форматтеров команд по атрибутам
      _formatters = Assembly
          .GetExecutingAssembly()
          .GetTypes()
          .Where(t => typeof(ICommandFormatter).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
          .Where(t =>
              t.GetCustomAttributes(typeof(CommandFormatterAttribute), true)
              .OfType<CommandFormatterAttribute>()
              .Any())
          .Select(t =>
          {
            var instance = (ICommandFormatter)Activator.CreateInstance(t)!;
            var mnemonic = t.GetCustomAttributes(typeof(CommandFormatterAttribute), true)
                      .OfType<CommandFormatterAttribute>()
                      .First().Mnemonic;
            return new { mnemonic, formatter = instance };
          })
          .ToDictionary(x => x.mnemonic.ToUpperInvariant(), x => x.formatter);
    }

    /// <summary>
    /// Выполняет распознавание команд в списке блоков.
    /// Для каждого блока вызывает соответствующий парсер и форматтер.
    /// </summary>
    /// <param name="blocks">Список блоков текста для обработки.</param>
    /// <returns>Список результатов распознавания команд.</returns>
    public async Task<List<CommandParseResult>> RecognizeAsync(List<CommandBlock> blocks)
    {
      var results = new List<CommandParseResult>();

      foreach (var block in blocks)
      {
        var firstLine = block.Lines.FirstOrDefault()?.Trim();
        if (string.IsNullOrWhiteSpace(firstLine)) continue;

        var match = Regex.Match(firstLine, @"^\s*(\d+)\s+(\S+)");
        if (!match.Success) continue;

        string number = match.Groups[1].Value;
        string mnemonic = match.Groups[2].Value.ToUpperInvariant();

        bool recognized = _parsers.TryGetValue(mnemonic, out var parser);

        var result = new CommandParseResult
        {
          LineIndex = block.StartLine,
          CommandNumber = number,
          Mnemonic = mnemonic,
          IsRecognized = recognized
        };

        if (recognized)
        {
          await parser!.ParseAsync(block);

          // Сохраняем номер и мнемонику всегда, даже если нет форматтера
          block.CommandNumber = number;
          block.Mnemonic = mnemonic;

          // Если парсинг успешен, вызываем форматтер
          if (block.IsRecognized && _formatters.TryGetValue(mnemonic, out var formatter))
          {
            formatter.Format(block);
            LogInformation($"Форматирование команды {mnemonic} выполнено.");
          }

          result.ExtraHighlights = block.ExtraHighlights.ToList();
        }

        results.Add(result);
      }

      return results;
    }
  }
}

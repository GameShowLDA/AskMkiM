using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using ControlCommandAnalyser.Domain;
using ControlCommandAnalyser.Parsing.Interface;
using ControlCommandAnalyser.Services;
using Utilities.TextEditor;
using System.Windows.Media;

namespace ControlCommandAnalyser.Parsing.Command.Si
{
  /// <summary>
  /// Парсер для команд "СИ".
  /// Формирует новый блок с правильным порядком: номер, мнемоника, напряжение, остальное.
  /// </summary>
  public class SiCommandParser : ICommandParser
  {
    public string Mnemonic => "СИ";

    /// <summary>
    /// Парсит блок команды СИ, переставляет параметры, строит новый блок и собирает подсветку.
    /// </summary>
    /// <param name="block">Исходный блок</param>
    /// <param name="highlights">Список подсветки</param>
    /// <returns>Новый CommandBlock в нужном порядке или null если ничего не найдено</returns>
    public CommandBlock? Parse(CommandBlock block, out List<HighlightRange> highlights)
    {
      highlights = new List<HighlightRange>();
      if (block.Lines == null || block.Lines.Count == 0)
        return null;

      string line = block.Lines[0];

      // Извлекаем номер команды
      string? commandNumber = ParserHelp.TryGetCommandNumber(line);
      if (string.IsNullOrWhiteSpace(commandNumber))
        return null;

      // Извлекаем мнемонику
      string? mnemonic = ParserHelp.TryGetMnemonic(line);

      // Извлекаем напряжение (например, 250В, 100В, 5кВ)
      var voltageMatch = Regex.Match(line, @"(\d{1,5})\s*(В|кВ|мВ|MV|KV)", RegexOptions.IgnoreCase);
      string? voltage = voltageMatch.Success ? voltageMatch.Value : null;

      // Удаляем из строки всё, что нашли, чтобы собрать остаток (но оставляем символы-разделители)
      string rest = line;
      if (!string.IsNullOrEmpty(commandNumber))
        rest = RemoveFirst(rest, $@"\b{Regex.Escape(commandNumber)}\b");
      if (!string.IsNullOrEmpty(mnemonic))
        rest = RemoveFirst(rest, $@"\b{Regex.Escape(mnemonic)}\b");
      if (!string.IsNullOrEmpty(voltage))
        rest = RemoveFirst(rest, Regex.Escape(voltage));
      rest = rest.Trim(' ', ',', ';');

      // Собираем новый порядок строки
      var formattedLine = $"{commandNumber} {mnemonic}";
      if (!string.IsNullOrEmpty(voltage))
        formattedLine += $" {voltage}";
      if (!string.IsNullOrEmpty(rest))
        formattedLine += $" {rest}";

      // Записываем новую строку в новый блок
      var newBlock = new CommandBlock
      {
        CommandNumber = commandNumber,
        Mnemonic = mnemonic ?? "",
        Lines = new List<string> { formattedLine }
      };

      // Добавляем все остальные строки (если есть) в новый блок
      if (block.Lines.Count > 1)
        newBlock.Lines.AddRange(block.Lines.GetRange(1, block.Lines.Count - 1));

      // Мнемоника — зелёный если есть напряжение, иначе красный
      highlights.Add(new HighlightRange(0, commandNumber.Length + 1, mnemonic?.Length ?? 0, HighlightTarget.Mnemonic)
      {
        ColorOverride = !string.IsNullOrEmpty(voltage) ? Colors.LightGreen : Colors.Red
      });

      return newBlock;
    }

    private static string RemoveFirst(string input, string pattern)
    {
      var match = Regex.Match(input, pattern, RegexOptions.IgnoreCase);
      if (match.Success)
        return input.Remove(match.Index, match.Length);
      return input;
    }
  }
}

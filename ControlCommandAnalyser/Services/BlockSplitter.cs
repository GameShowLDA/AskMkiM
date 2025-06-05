using System.Collections.Generic;
using System.Text.RegularExpressions;
using ControlCommandAnalyser.Domain;

namespace ControlCommandAnalyser.Services
{
  /// <summary>
  /// Вспомогательный класс для разбиения исходного текста на блоки команд.
  /// </summary>
  internal static class BlockSplitter
  {
    /// <summary>
    /// Разбивает текст на блоки команд по признаку "номер команда".
    /// </summary>
    /// <param name="text">Исходный текст.</param>
    /// <returns>Список найденных блоков команд.</returns>
    public static List<CommandBlock> Split(string text)
    {
      var lines = text.Replace("\r\n", "\n").Split('\n');
      var blocks = new List<CommandBlock>();
      CommandBlock? currentBlock = null;

      var regex = new Regex(@"^\s*(\d+)\s+(\S+)", RegexOptions.Compiled);

      foreach (var line in lines)
      {
        var match = regex.Match(line);
        if (match.Success)
        {
          // Новый блок
          if (currentBlock != null)
            blocks.Add(currentBlock);

          currentBlock = new CommandBlock
          {
            CommandNumber = match.Groups[1].Value,
            Mnemonic = match.Groups[2].Value,
            Lines = new List<string> { line }
          };
        }
        else if (currentBlock != null)
        {
          currentBlock.Lines.Add(line);
        }
      }
      if (currentBlock != null)
        blocks.Add(currentBlock);

      return blocks;
    }
  }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ControlCommandAnalyser.Domain;

namespace ControlCommandAnalyser.Parsing
{
  public class CommandSplitter
  {
    /// <summary>
    /// Разбивает полный текст на блоки команд по признаку начала (номер + пробел + мнемоника).
    /// </summary>
    public List<CommandBlock> Split(string[] lines)
    {
      var blocks = new List<CommandBlock>();
      CommandBlock? current = null;

      for (int i = 0; i < lines.Length; i++)
      {
        string line = lines[i];
        if (Regex.IsMatch(line, @"^\s*\d{2,3}\s+\S{2,4}\b"))
        {
          if (current != null)
            blocks.Add(current);

          current = new CommandBlock
          {
            StartLine = i,
            Lines = new List<string> { line }
          };
        }
        else if (current != null)
        {
          current.Lines.Add(line);
        }
      }

      if (current != null)
        blocks.Add(current);

      return blocks;
    }
  }
}

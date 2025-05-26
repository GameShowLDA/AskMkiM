using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ControlCommandAnalyser.Domain;
using ControlCommandAnalyser.Parsing.Commands;
using static Utilities.LoggerUtility;

namespace ControlCommandAnalyser.Parsing
{
  public class CommandRecognizer
  {
    private readonly Dictionary<string, ICommandParser> _parsers;
    public CommandRecognizer()
    {
      _parsers = Assembly
        .GetExecutingAssembly()
        .GetTypes()
        .Where(t => typeof(ICommandParser).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
        .Select(t => (ICommandParser)Activator.CreateInstance(t)!)
        .ToDictionary(parser => parser.Mnemonic.ToUpperInvariant());
    }

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
          result.ExtraHighlight = block.ExtraHighlight;
        }

        results.Add(result);
      }

      return results;
    }
  }
}

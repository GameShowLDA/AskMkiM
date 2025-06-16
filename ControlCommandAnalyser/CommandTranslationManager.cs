using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using ControlCommandAnalyser.Model;
using ControlCommandAnalyser.Parser;
using ControlCommandAnalyser.Formatter;
using Utilities.TextEditor;
using System.Windows.Media;

namespace ControlCommandAnalyser
{
  public class CommandTranslationManager
  {
    private readonly List<ICommandParser> _parsers;
    private readonly List<ICommandFormatter> _formatters;

    public CommandTranslationManager()
    {
      _parsers = GetAllParsers();
      _formatters = GetAllFormatters();
    }

    private static List<ICommandParser> GetAllParsers()
    {
      var iface = typeof(ICommandParser);
      return Assembly.GetExecutingAssembly()
        .GetTypes()
        .Where(t => !t.IsAbstract && iface.IsAssignableFrom(t))
        .Select(t => (ICommandParser)Activator.CreateInstance(t))
        .ToList();
    }

    private static List<ICommandFormatter> GetAllFormatters()
    {
      var iface = typeof(ICommandFormatter);
      return Assembly.GetExecutingAssembly()
        .GetTypes()
        .Where(t => !t.IsAbstract && iface.IsAssignableFrom(t))
        .Select(t => (ICommandFormatter)Activator.CreateInstance(t))
        .ToList();
    }

    /// <summary>
    /// Парсит, форматирует, выводит в адаптер. Возвращает модели команд.
    /// </summary>
    public List<BaseCommandModel> ParseAllAndDisplay(string text, ITextEditorAdapter adapter)
    {
      var models = ParseAll(text);

      var formattedLines = new List<string>();
      var highlights = new List<HighlightRange>();
      int globalLine = 0;

      foreach (var model in models)
      {
        // Найти подходящий форматтер (по типу модели)
        var formatter = _formatters.FirstOrDefault(f => f.CanFormat(model));
        IEnumerable<string> lines;
        if (formatter != null)
        {
          lines = formatter.Format(model);
        }
        else
        {
          // Если нет форматтера — пытаемся взять исходные строки
          var sourceLinesProp = model.GetType().GetProperty("SourceLines");
          lines = sourceLinesProp != null
            ? sourceLinesProp.GetValue(model) as IEnumerable<string> ?? new List<string>()
            : new List<string>();
        }

        foreach (var line in lines)
        {
          formattedLines.Add(line);

          // Подсветка для первой строки (номер/мнемоника), если это исходные строки
          if (globalLine == 0 && !string.IsNullOrWhiteSpace(model.CommandNumber))
          {
            int cmdIdx = line.IndexOf(model.CommandNumber, StringComparison.Ordinal);
            if (cmdIdx >= 0)
              highlights.Add(new HighlightRange(globalLine, cmdIdx, model.CommandNumber.Length, HighlightTarget.CommandNumber));
          }
          if (globalLine == 0 && !string.IsNullOrWhiteSpace(model.Mnemonic))
          {
            int mnemIdx = line.IndexOf(model.Mnemonic, StringComparison.Ordinal);
            if (mnemIdx >= 0)
            {
              var isUnknown = model.GetType().Name == "UnknownCommandModel";
              highlights.Add(new HighlightRange(globalLine, mnemIdx, model.Mnemonic.Length, HighlightTarget.Mnemonic)
              {
                ColorOverride = isUnknown ? Colors.Gray : (Color?)null
              });
            }
          }

          globalLine++;
        }
      }

      string outText = string.Join("\n", formattedLines);
      adapter.SetTextAndHighlighting(outText, highlights);

      return models;
    }

    /// <summary>
    /// Парсит текст программы в список моделей команд.
    /// </summary>
    public List<BaseCommandModel> ParseAll(string text)
    {
      var lines = text.Replace("\r\n", "\n").Split('\n');
      var commands = new List<BaseCommandModel>();

      string commandNumber = null;
      string mnemonic = null;
      var commandLines = new List<string>();

      var cmdRegex = new Regex(@"^\s*(\d+)\s+([А-ЯA-Z]{2,})\b", RegexOptions.Compiled);

      foreach (var line in lines)
      {
        var match = cmdRegex.Match(line);
        if (match.Success)
        {
          if (commandLines.Count > 0 && commandNumber != null && mnemonic != null)
            commands.Add(ParseSingle(commandNumber, mnemonic, commandLines));

          commandNumber = match.Groups[1].Value;
          mnemonic = match.Groups[2].Value;
          commandLines = new List<string> { line };
        }
        else if (commandLines.Count > 0)
        {
          commandLines.Add(line);
        }
      }
      if (commandLines.Count > 0 && commandNumber != null && mnemonic != null)
        commands.Add(ParseSingle(commandNumber, mnemonic, commandLines));

      return commands;
    }

    private BaseCommandModel ParseSingle(string commandNumber, string mnemonic, List<string> lines)
    {
      foreach (var parser in _parsers)
        if (parser.CanParse(mnemonic))
          return parser.Parse(commandNumber, mnemonic, lines);

      return new UnknownCommandModel
      {
        CommandNumber = commandNumber,
        Mnemonic = mnemonic,
        SourceLines = new List<string>(lines)
      };
    }
  }

  // Для совместимости — вынеси в отдельный файл в реальном проекте!
  public class UnknownCommandModel : BaseCommandModel{}
}

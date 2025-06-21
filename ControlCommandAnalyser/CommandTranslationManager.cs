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
      AppConfiguration.Base.EventAggregator.RaiseInfoMessage($"Начало трансляции");
      var models = ParseAll(text);

      var formattedLines = new List<string>();
      var highlights = new List<HighlightRange>();
      int globalLine = 0;

      AppConfiguration.Base.EventAggregator.RaiseInfoMessage($"Формирование данных");
      foreach (var model in models)
      {
        var formatter = _formatters.FirstOrDefault(f => f.CanFormat(model));
        IEnumerable<string> lines;
        if (formatter != null)
        {
          lines = formatter.Format(model);
        }
        else
        {
          var sourceLinesProp = model.GetType().GetProperty("SourceLines");
          lines = sourceLinesProp != null
            ? sourceLinesProp.GetValue(model) as IEnumerable<string> ?? new List<string>()
            : new List<string>();
        }

        foreach (var line in lines)
        {
          formattedLines.Add(line);
          globalLine++;
        }
      }

      string outText = string.Join("\n", formattedLines);
      adapter.SetTextAndHighlighting(outText, highlights);

      AppConfiguration.Base.EventAggregator.RaiseInfoMessage($"Проверка взаимосвязей");
      CommandPostAnalyzer.Analyze(models);

      var totalErrorCount = models.Sum(m => m?.Errors?.Count() ?? 0);
      if (totalErrorCount > 0)
      {
        AppConfiguration.Base.EventAggregator.RaiseInfoMessage($"Ошибка трансляции");
      }
      else
      {
        AppConfiguration.Base.EventAggregator.RaiseInfoMessage($"Готово");
      }

      return models;
    }

    /// <summary>
    /// Парсит текст программы в список моделей команд.
    /// </summary>
    public List<BaseCommandModel> ParseAll(string text)
    {
      AppConfiguration.Base.EventAggregator.RaiseInfoMessage($"Сбор данных...");
      var lines = text.Replace("\r\n", "\n").Split('\n');
      var commands = new List<BaseCommandModel>();

      string commandNumber = null;
      string mnemonic = null;
      var commandLines = new List<string>();
      int currentStartLine = -1;
      int lineNumer = -1;

      var cmdRegex = new Regex(@"^\s*(\d+)\s+([А-ЯA-Z]{2,})\b", RegexOptions.Compiled);

      for (int i = 0; i < lines.Length; i++)
      {
        var line = lines[i];
        var match = cmdRegex.Match(line);
        if (match.Success)
        {
          if (commandLines.Count > 0 && commandNumber != null && mnemonic != null)
          {
            var model = ParseSingle(commandNumber, mnemonic, currentStartLine + 1, commandLines);
            model.StartLineNumber = currentStartLine + 1; // +1 если строки 1-based
            commands.Add(model);
          }

          lineNumer = currentStartLine + 1;
          commandNumber = match.Groups[1].Value;
          mnemonic = match.Groups[2].Value;
          commandLines = new List<string> { line };
          currentStartLine = i;
        }
        else if (commandLines.Count > 0)
        {
          commandLines.Add(line);
        }
      }

      if (commandLines.Count > 0 && commandNumber != null && mnemonic != null)
      {
        var model = ParseSingle(commandNumber, mnemonic, lineNumer, commandLines);
        model.StartLineNumber = currentStartLine + 1;
        commands.Add(model);
      }

      return commands;
    }


    private BaseCommandModel ParseSingle(string commandNumber, string mnemonic, int lineNumber, List<string> lines)
    {
      foreach (var parser in _parsers)
        if (parser.CanParse(mnemonic))
          return parser.Parse(commandNumber, mnemonic, lineNumber, lines);

      return new UnknownCommandModel
      {
        CommandNumber = commandNumber,
        Mnemonic = mnemonic,
        SourceLines = new List<string>(lines),
        Errors = new List<Utilities.Models.ErrorItem>() { new Utilities.Models.ErrorItem() { Command = $"{commandNumber} {mnemonic}", LineNumber = lineNumber, Description = $"Неизвестная команда {mnemonic}" } }
      };
    }
  }

  public class UnknownCommandModel : BaseCommandModel { }
}

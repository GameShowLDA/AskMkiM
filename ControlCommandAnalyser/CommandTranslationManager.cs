using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows.Media;
using AppConfiguration.Error.Translation;
using ControlCommandAnalyser.Formatter;
using ControlCommandAnalyser.Model;
using ControlCommandAnalyser.Parser;
using Utilities.Errors;
using Utilities.Models;
using Utilities.TextEditor;

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
      AppConfiguration.Base.EventAggregator.RaiseInfoMessage("Начало трансляции");
      var models = ParseAndLog(text);

      AppConfiguration.Base.EventAggregator.RaiseInfoMessage("Формирование данных");
      FormatAndDisplay(models, adapter);

      AppConfiguration.Base.EventAggregator.RaiseInfoMessage("Проверка взаимосвязей");
      AnalyzeAndLog(models);

      AppConfiguration.Base.EventAggregator.RaiseInfoMessage("Формирование данных");
      FormatAndDisplay(models, adapter);

      return models;
    }

    private List<BaseCommandModel> ParseAndLog(string text)
    {
      return ParseAll(text);
    }

    private void FormatAndDisplay(List<BaseCommandModel> models, ITextEditorAdapter adapter)
    {
      var formattedLines = new List<string>();
      var highlights = new List<HighlightRange>();

      // 1. Формируем текст справа и строим mapping
      var lineMapping = BuildFormattedTextAndMapping(models, formattedLines);

      // 2. Заполняем номера строк для ошибок
      AssignFormattedLineNumbers(models, lineMapping);

      // 3. Отправляем текст в адаптер
      string outText = string.Join("\n", formattedLines);
      adapter.SetTextAndHighlighting(outText, highlights);
    }

    /// <summary>
    /// Формирует форматированный текст и строит mapping строк исходник → трансляция.
    /// </summary>
    private List<(int SourceLineNumber, int FormattedLineNumber)> BuildFormattedTextAndMapping(
        List<BaseCommandModel> models, List<string> formattedLines)
    {
      var lineMapping = new List<(int SourceLineNumber, int FormattedLineNumber)>();
      int formattedLineNumber = 1;

      foreach (var model in models)
      {
        var formatter = _formatters.FirstOrDefault(f => f.CanFormat(model));
        IEnumerable<string> lines;

        // Получаем исходные строки для текущей команды
        List<string> sourceLines = GetSourceLines(model, out int startSourceLineNumber);

        lines = formatter != null ? formatter.Format(model) : sourceLines;

        int countSourceLines = sourceLines.Count;
        int localSourceLineIdx = 0;
        foreach (var line in lines)
        {
          formattedLines.Add(line);
          int sourceLineNumber = (localSourceLineIdx < countSourceLines)
              ? startSourceLineNumber + localSourceLineIdx
              : startSourceLineNumber;

          lineMapping.Add((sourceLineNumber, formattedLineNumber));
          formattedLineNumber++;
          localSourceLineIdx++;
        }
      }
      return lineMapping;
    }

    /// <summary>
    /// Возвращает строки исходника и номер первой строки.
    /// </summary>
    private List<string> GetSourceLines(BaseCommandModel model, out int startSourceLineNumber)
    {
      var sourceLines = new List<string>();
      startSourceLineNumber = 1;

      var sourceLinesProp = model.GetType().GetProperty("SourceLines");
      if (sourceLinesProp != null)
      {
        var srcLines = sourceLinesProp.GetValue(model) as IEnumerable<string>;
        if (srcLines != null)
          sourceLines = srcLines.ToList();
      }
      var startLineProp = model.GetType().GetProperty("StartLineNumber");
      if (startLineProp != null)
      {
        var start = startLineProp.GetValue(model);
        if (start is int i && i > 0)
          startSourceLineNumber = i;
      }
      return sourceLines;
    }

    /// <summary>
    /// Проставляет FormattedLineNumber для всех ошибок.
    /// </summary>
    private void AssignFormattedLineNumbers(List<BaseCommandModel> models, List<(int SourceLineNumber, int FormattedLineNumber)> lineMapping)
    {
      foreach (var model in models)
      {
        if (model.Errors == null) continue;
        foreach (var error in model.Errors)
        {
          var match = lineMapping.FirstOrDefault(m => m.SourceLineNumber == error.SourceLineNumber);
          if (match != default)
            error.FormattedLineNumber = match.FormattedLineNumber;
          else
            error.FormattedLineNumber = -1;
        }
      }
    }



    private void AnalyzeAndLog(List<BaseCommandModel> models)
    {
      CommandPostAnalyzer.Analyze(models);

      var totalErrorCount = models.Sum(m => m?.Errors?.Count() ?? 0);
      if (totalErrorCount > 0)
      {
        AppConfiguration.Base.EventAggregator.RaiseInfoMessage("Ошибка трансляции");
      }
      else
      {
        AppConfiguration.Base.EventAggregator.RaiseInfoMessage("Готово");
      }
    }

    /// <summary>
    /// Парсит текст программы в список моделей команд.
    /// </summary>
    public List<BaseCommandModel> ParseAll(string text)
    {
      AppConfiguration.Base.EventAggregator.RaiseInfoMessage($"Сбор данных...");

      text = PreprocessText(text);
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

      var unknownCommandModel = new UnknownCommandModel
      {
        CommandNumber = commandNumber,
        Mnemonic = mnemonic,
        SourceLines = new List<string>(lines),
        Errors = new List<ErrorItem>
        {
          GeneralErrors.UnknownCommand(mnemonic, lineNumber, $"{commandNumber} {mnemonic}")
        }
      };
      unknownCommandModel.SourceLines[0] = unknownCommandModel.SourceLines[0] + " (Неизвестная команда!)";
      for (int i = 1; i < unknownCommandModel.SourceLines.Count; i++)
      {
        if (!string.IsNullOrEmpty(unknownCommandModel.SourceLines[i]) && !string.IsNullOrWhiteSpace(unknownCommandModel.SourceLines[i]))
        {
          unknownCommandModel.SourceLines[i] += " !";
        }
      }

      return unknownCommandModel;
    }

    private string PreprocessText(string text)
    {
      text = Regex.Replace(text, @"/\*.*?\*/", "", RegexOptions.Singleline);
      text = Regex.Replace(text, @"//.*?$", "", RegexOptions.Multiline);
      text = Regex.Replace(text, @"\{[^{}]*\}", "", RegexOptions.Multiline);

      return text;
    }
  }

  public class UnknownCommandModel : BaseCommandModel { }
}

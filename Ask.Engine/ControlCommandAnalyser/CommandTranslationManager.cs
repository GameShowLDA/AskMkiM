using Ask.Core.Services.Errors.Models;
using Ask.Core.Services.Errors.Translation;
using Ask.Core.Services.EventCore.Adapters;
using Ask.Core.Services.Extensions;
using Ask.Core.Shared.DTO.Executor;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums.Commands;
using Ask.Core.Shared.Metadata.View.EditorHost.TextEditor;
using Ask.DataBase.Engine.Static.Devices;
using Ask.Engine.ControlCommandAnalyser.ComandBody;
using Ask.Engine.ControlCommandAnalyser.Formatter;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandAnalyser.Parser;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using static Ask.LogLib.LoggerUtility;

namespace Ask.Engine.ControlCommandAnalyser
{
  public class CommandTranslationManager
  {
    private readonly List<ICommandParser> _parsers;
    private readonly List<ICommandFormatter> _formatters;
    private readonly List<ICommandBody> _commandBodyBuilders;

    public CommandTranslationManager()
    {
      _parsers = GetAllParsers();
      _formatters = GetAllFormatters();
      _commandBodyBuilders = GetAllCommandBuilders();
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

    private static List<ICommandBody> GetAllCommandBuilders()
    {
      var iface = typeof(ICommandBody);
      return Assembly.GetExecutingAssembly()
        .GetTypes()
        .Where(t => !t.IsAbstract && iface.IsAssignableFrom(t))
        .Select(t => (ICommandBody)Activator.CreateInstance(t))
        .ToList();
    }

    /// <summary>
    /// Парсит, форматирует, выводит в адаптер. Возвращает модели команд.
    /// </summary>
    public List<BaseCommandModel> ParseAllAndDisplay(string text, ITextEditorView adapter)
    {
      var result = BuildTranslation(text);
      adapter.Text = result.FormattedText;
      return result.Models;
    }

    /// <summary>
    /// Строит результат трансляции без прямой записи в UI-редактор.
    /// Это позволяет безопасно выполнять тяжёлую часть трансляции в фоновом потоке.
    /// </summary>
    /// <param name="text">Исходный текст программы контроля.</param>
    /// <param name="progress">Необязательный канал для передачи этапов трансляции в UI.</param>
    /// <returns>Скомпилированные модели и итоговый текст трансляции.</returns>
    public TranslationBuildResult BuildTranslation(string text, IProgress<string>? progress = null)
    {
      try
      {
        ReportProgress(progress, "Начало трансляции");
        var models = ParseAll(text);

        try
        {
          CheckVshModel(models);
        }
        catch (Exception ex)
        {
          LogError($"Ошибка при добавлении ВШ: {ex}");
          AddGlobalTranslationError(models, text, "добавлении команды ВШ", ex);
        }

        ReportProgress(progress, "Формирование данных");
        _ = BuildFormattedTextSafely(models, text);

        ReportProgress(progress, "Проверка взаимосвязей");
        Analyze(models, text);

        ReportProgress(progress, "Формирование данных");
        string formattedText = BuildFormattedTextSafely(models, text);

        ReportProgress(progress, "Готово");
        return new TranslationBuildResult(models, formattedText);
      }
      catch (Exception ex)
      {
        LogError($"Критическая ошибка трансляции: {ex}");
        ReportProgress(progress, "Готово");
        return BuildUnexpectedFailureResult(text, ex, "общей трансляции");
      }
    }

    public static TranslationBuildResult BuildUnexpectedFailureResult(
        string text,
        Exception ex,
        string stage = "выполнении трансляции")
    {
      var model = CreateFatalTranslationModel(text, stage, ex);
      var formattedText = string.Join("\n", model.SourceLines);
      return new TranslationBuildResult(new List<BaseCommandModel> { model }, formattedText);
    }

    private static void CheckVshModel(List<BaseCommandModel> models)
    {
      if (models.FirstOrDefault(model => (model is VshCommandModel) == true) == null)
      {
        var rmIndex = models.FindLastIndex(model => model is RmCommandModel);
        if (rmIndex >= 0)
        {
          if (!int.TryParse(models[rmIndex].CommandNumber, out var commandNumber))
          {
            AddInternalTranslationError(
                models[rmIndex],
                "добавлении команды ВШ",
                new FormatException("Некорректный номер команды РМ."));
            return;
          }

          commandNumber++;
          while (models.Contains(models.FirstOrDefault(m => m.CommandNumber == commandNumber.ToString())))
          {
            commandNumber++;
          }
          var mnemonic = EnumExtensions.GetDisplayOrganizationalInfo(OrganizationalComands.VSH).DisplayName;
          var vshModel = new VshCommandModel
          {
            CommandNumber = commandNumber.ToString(),
            Mnemonic = mnemonic,
            SourceLines = new List<string> { $"{commandNumber} {mnemonic} 2Ш" },
            StartLineNumber = models[rmIndex].StartLineNumber + 1,
            BusStructure = new Dictionary<BusStructureEnum.Type, List<int?>>
            {
              { BusStructureEnum.Type.Bus2, new List<int?> () },
            }
          };
          var managerShassi = ChassisManagers.GetAllAsync().GetAwaiter().GetResult().FirstOrDefault();

          if (managerShassi != null)
          {
            vshModel.BusStructure[BusStructureEnum.Type.Bus2].Add(managerShassi.Number);
          }
          var managerRack = Racks.GetAllAsync().GetAwaiter().GetResult();
          if (managerRack != null && managerRack.Count > 0)
          {
            foreach (var rack in managerRack)
            {
              vshModel.BusStructure[BusStructureEnum.Type.Bus2].Add(rack.Number);
            }
          }
          vshModel.Warnings.Add(GeneralWarnings.VshCommandAddedAutomatically(vshModel.StartLineNumber, $"{vshModel.CommandNumber} {vshModel.Mnemonic}"));
          models.Insert(rmIndex + 1, vshModel);
        }
      }
    }

    /// <summary>
    /// Форматирует модели команд и выводит их через адаптер.
    /// </summary>
    private void FormatAndDisplay(List<BaseCommandModel> models, ITextEditorView adapter)
    {
      adapter.Text = BuildFormattedText(models);
    }

    /// <summary>
    /// Формирует итоговый текст трансляции и синхронизирует mapping строк.
    /// </summary>
    private string BuildFormattedText(List<BaseCommandModel> models)
    {
      var formattedLines = new List<string>();

      var lineMapping = BuildFormattedTextAndMapping(models, formattedLines);
      AssignFormattedLineNumbers(models, lineMapping);

      return string.Join("\n", formattedLines);
    }

    private string BuildFormattedTextSafely(List<BaseCommandModel> models, string text)
    {
      try
      {
        return BuildFormattedText(models);
      }
      catch (Exception ex)
      {
        LogError($"Критическая ошибка при формировании текста трансляции: {ex}");
        AddGlobalTranslationError(models, text, "формировании текста трансляции", ex);
        return BuildRawFallbackText(models, text);
      }
    }

    /// <summary>
    /// Формирует форматированный текст и строит mapping строк исходник → трансляция.
    /// </summary>
    private List<(int SourceLineNumber, int FormattedLineNumber)> BuildFormattedTextAndMapping(List<BaseCommandModel> models, List<string> formattedLines)
    {
      var lineMapping = new List<(int SourceLineNumber, int FormattedLineNumber)>();
      int formattedLineNumber = 0;

      foreach (var model in models)
      {
        try
        {
          var formatter = _formatters.FirstOrDefault(f => f.CanFormat(model));
          IEnumerable<string> lines;

          model.FormattedStartLineNumber = formattedLineNumber;

          // Получаем исходные строки для текущей команды
          List<string> sourceLines = GetSourceLines(model, out int startSourceLineNumber);
          if (sourceLines.Count == 0)
          {
            sourceLines = BuildFallbackLines(model);
          }

          lines = formatter != null
              ? FormatModelSafely(formatter, model, sourceLines)
              : sourceLines;

          int countSourceLines = sourceLines.Count;
          int localSourceLineIdx = 0;
          foreach (var line in lines)
          {
            var splitStr = line.Split("\n");

            foreach (var item in splitStr)
            {
              if (!string.IsNullOrEmpty(item))
              {
                formattedLines.Add(item);
                formattedLineNumber++;
              }
            }

            int sourceLineNumber = (localSourceLineIdx < countSourceLines)
                ? startSourceLineNumber + localSourceLineIdx
                : startSourceLineNumber;

            lineMapping.Add((sourceLineNumber, formattedLineNumber));
            localSourceLineIdx++;
          }
        }
        catch (Exception ex)
        {
          AddInternalTranslationError(model, "формировании текста трансляции", ex);

          model.FormattedStartLineNumber = formattedLineNumber;
          var fallbackLines = BuildFallbackLines(model);
          int fallbackStartLine = model.StartLineNumber > 0 ? model.StartLineNumber : 1;

          foreach (var fallbackLine in fallbackLines)
          {
            formattedLines.Add(fallbackLine);
            formattedLineNumber++;
            lineMapping.Add((fallbackStartLine, formattedLineNumber));
          }
        }
      }
      return lineMapping;
    }

    /// <summary>
    /// Безопасно форматирует команду: при сбое добавляет ошибку и возвращает строки исходника.
    /// </summary>
    private static IEnumerable<string> FormatModelSafely(
        ICommandFormatter formatter,
        BaseCommandModel model,
        List<string> fallbackLines)
    {
      try
      {
        var lines = formatter.Format(model)?.ToList();
        return lines is { Count: > 0 } ? lines : fallbackLines;
      }
      catch (Exception ex)
      {
        AddFormattingError(model, ex);
        LogError(
            $"Ошибка форматирования команды {model.CommandNumber} {model.Mnemonic} " +
            $"(строка {model.StartLineNumber}): {ex}");
        return fallbackLines;
      }
    }

    /// <summary>
    /// Возвращает безопасный fallback-текст, если исходные строки команды недоступны.
    /// </summary>
    private static List<string> BuildFallbackLines(BaseCommandModel model)
    {
      var header = $"{model.CommandNumber} {model.Mnemonic}".Trim();
      return new List<string> { string.IsNullOrWhiteSpace(header) ? "Ошибка форматирования команды" : header };
    }

    /// <summary>
    /// Добавляет ошибку форматирования только один раз для команды.
    /// </summary>
    private static void AddFormattingError(BaseCommandModel model, Exception ex)
    {
      string command = GetCommandDisplay(model);
      string description = $"Не удалось отформатировать команду {command}. Исправьте текст и повторите трансляцию.";

      if (model.Errors.Any(error =>
          error.Code == ErrorCode.Unknown &&
          string.Equals(error.Description, description, StringComparison.Ordinal)))
      {
        return;
      }

      model.Errors.Add(new ErrorItem
      {
        SourceLineNumber = model.StartLineNumber > 0 ? model.StartLineNumber : 1,
        Command = string.IsNullOrWhiteSpace(command) ? "Трансляция программы" : command,
        Code = ErrorCode.Unknown,
        DebugInfo = $"{ex.GetType().Name}: {ex.Message}",
        Description = description
      });
    }

    /// <summary>
    /// Возвращает строки исходника и номер первой строки.
    /// </summary>
    public List<string> GetSourceLines(BaseCommandModel model, out int startSourceLineNumber)
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
        foreach (var warning in model.Warnings)
        {
          var match = lineMapping.FirstOrDefault(m => m.SourceLineNumber == warning.SourceLineNumber);
          if (match != default)
            warning.FormattedLineNumber = match.FormattedLineNumber;
          else
            warning.FormattedLineNumber = -1;
        }
      }
    }

    /// <summary>
    /// Анализирует собранные модели команд.
    /// </summary>
    private void Analyze(List<BaseCommandModel> models, string text)
    {
      try
      {
        CommandPostAnalyzer.Analyze(models);
      }
      catch (Exception ex)
      {
        LogError($"Ошибка пост-анализа трансляции: {ex}");
        AddGlobalTranslationError(models, text, "пост-анализе", ex);
      }

      var totalErrorCount = models.Sum(m => m?.Errors?.Count() ?? 0);
      if (totalErrorCount > 0)
      {
        MessageEventAdapter.RaiseInfoMessage("Ошибка трансляции");
      }
      else
      {
        MessageEventAdapter.RaiseInfoMessage("Готово");
      }
    }

    private static void ReportProgress(IProgress<string>? progress, string message)
    {
      MessageEventAdapter.RaiseInfoMessage(message);
      progress?.Report(message);
    }

    /// <summary>
    /// Преобразует текст в список моделей команд.
    /// </summary>
    public List<BaseCommandModel> ParseAll(string text)
    {
      MessageEventAdapter.RaiseInfoMessage("Сбор данных...");

      Dictionary<int, string> lines;
      List<(int LineIndex, string Text)> comments;

      try
      {
        (lines, comments) = PreprocessText.PreprocessTextAndExtractComments(text ?? string.Empty);
      }
      catch (Exception ex)
      {
        LogError($"Ошибка предобработки текста трансляции: {ex}");
        return new List<BaseCommandModel>
        {
          CreateFatalTranslationModel(text, "предобработке текста программы", ex)
        };
      }

      var commands = new List<BaseCommandModel>();

      if (CommandsModel.CommandModels.Count > 0)
        CommandsModel.CommandModels.Clear();

      string commandNumber = null;
      string mnemonic = null;
      var commandLines = new List<string>();
      int currentStartLine = -1;
      int lastCommandLine = -1;

      var cmdRegex = new Regex(@"^\s*(\d+)\s+([А-ЯA-Z]{2,})\b", RegexOptions.Compiled);

      foreach (var kvp in lines.OrderBy(x => x.Key))
      {
        int lineNumber = kvp.Key;
        string line = kvp.Value;

        var match = cmdRegex.Match(line);
        if (match.Success)
        {
          // --- закрываем предыдущую команду ---
          if (commandLines.Count > 0 && commandNumber != null && mnemonic != null)
          {
            var model = ParseSingle(commandNumber, mnemonic, currentStartLine + 1, commandLines);
            model.StartLineNumber = currentStartLine + 1;

            // Комментарии между предыдущей и текущей командой
            var toAssign = comments
              .Where(c => c.LineIndex >= lastCommandLine && c.LineIndex < lineNumber)
              .ToList();

            foreach (var c in toAssign)
            {
              model.Comment.Add(c.Text);
              comments.Remove(c); // удаляем из общего списка
            }

            if (commands.Any(c => c.Mnemonic == mnemonic && c.CommandNumber == commandNumber))
              model.Errors.Add(GeneralErrors.CommandAlreadyExists(mnemonic, currentStartLine + 1, $"{commandNumber} {mnemonic}"));

            commands.Add(model);
            CommandsModel.CommandModels.Add(model);
          }

          // --- начинаем новую команду ---
          commandNumber = match.Groups[1].Value;
          mnemonic = match.Groups[2].Value;
          commandLines = new List<string> { line };
          currentStartLine = lineNumber;
          lastCommandLine = lineNumber;
        }
        else if (commandLines.Count > 0)
        {
          commandLines.Add(line);
        }
      }

      // --- закрываем последнюю команду ---
      if (commandLines.Count > 0 && commandNumber != null && mnemonic != null)
      {
        var model = ParseSingle(commandNumber, mnemonic, currentStartLine + 1, commandLines);
        model.StartLineNumber = currentStartLine + 1;

        // Все оставшиеся комментарии → последней команде
        foreach (var c in comments.ToList())
        {
          model.Comment.Add(c.Text);
          comments.Remove(c);
        }

        commands.Add(model);
        CommandsModel.CommandModels.Add(model);
      }

      return commands;
    }

    private BaseCommandModel ParseSingle(string commandNumber, string mnemonic, int lineNumber, List<string> lines)
    {
      try
      {
        foreach (var parser in _parsers)
        {
          if (parser.CanParse(mnemonic))
          {
            return parser.Parse(commandNumber, mnemonic, lineNumber, lines);
          }
        }
      }
      catch (Exception ex)
      {
        LogError($"Ошибка разбора команды {commandNumber} {mnemonic}: {ex}");
        return CreateCommandFailureModel(commandNumber, mnemonic, lineNumber, lines, "разборе команды", ex);
      }

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

    public void SetSourseLines(List<BaseCommandModel> models)
    {
      foreach (var model in models)
      {
        try
        {
          var newSourseLines = new StringBuilder();
          var commandNumberProp = model.GetType().GetProperty("CommandNumber");
          if (commandNumberProp != null)
          {
            var commandNumber = commandNumberProp.GetValue(model) as string;
            if (commandNumber != null && !string.IsNullOrEmpty(commandNumber))
            {
              newSourseLines.Append($"{commandNumber} ");
            }
          }
          var mnemonicProp = model.GetType().GetProperty("Mnemonic");
          if (mnemonicProp != null)
          {
            var mnemonic = mnemonicProp.GetValue(model) as string;
            if (mnemonic != null && !string.IsNullOrEmpty(mnemonic))
            {
              newSourseLines.Append($"{mnemonic}  ");
            }
          }
          var algorithmKeyProp = model.GetType().GetProperty("AlgorithmKey");
          if (algorithmKeyProp != null)
          {
            var algorithmKey = algorithmKeyProp.GetValue(model) as IEnumerable<string>;
            if (algorithmKey != null)
            {
              var algorithmKeysList = algorithmKey.ToList();
              foreach (var key in algorithmKeysList)
              {
                if (!string.IsNullOrEmpty(key) && !string.IsNullOrWhiteSpace(key))
                {
                  newSourseLines.Append($"{key}, ");
                }
              }
            }
          }
          var pointsLine = new StringBuilder();
          var pointsLineProp = model.GetType().GetProperty("PointsSourse");
          if (pointsLineProp != null)
          {
            var points = pointsLineProp.GetValue(model) as string;
            if (points == null || string.IsNullOrEmpty(points))
            {
              points = string.Empty;
            }
            pointsLine.Append($"{points} ");
          }

          var commentsLine = new StringBuilder();
          var commentsLineProp = model.GetType().GetProperty("Comment");
          if (commentsLineProp != null)
          {
            var comments = commentsLineProp.GetValue(model) as IEnumerable<string>;
            if (comments != null)
            {
              var commentsList = comments.ToList();
              for (int i = 0; i < commentsList.Count; i++)
              {
                if (!string.IsNullOrEmpty(commentsList[i]) && !string.IsNullOrWhiteSpace(commentsList[i]) && i < commentsList.Count - 1)
                {
                  commentsLine.Append($"{commentsList[i]}\n");
                }
                else
                {
                  commentsLine.Append($"\t{commentsList[i]}\n");
                }
              }
            }
          }

          var bodyCreator = _commandBodyBuilders.FirstOrDefault(f => f.CanCreate(model));
          if (bodyCreator != null)
          {
            newSourseLines = bodyCreator.Create(model, newSourseLines);
          }
          else
          {
            foreach (var line in model.SourceLines)
            {
              newSourseLines.AppendLine(line);
            }
          }

          model.SourceLines = new List<string> { newSourseLines.ToString() };
          if (!string.IsNullOrEmpty(pointsLine.ToString()) && !string.IsNullOrWhiteSpace(pointsLine.ToString()))
          {
            model.SourceLines.Add($"\t{pointsLine.ToString()}");
          }
          if (!string.IsNullOrEmpty(commentsLine.ToString()) && !string.IsNullOrWhiteSpace(commentsLine.ToString()))
          {
            model.SourceLines.Add($"{commentsLine.ToString()}");
          }
        }
        catch (Exception ex)
        {
          LogError($"Ошибка формирования SourceLines для команды {model.CommandNumber} {model.Mnemonic}: {ex}");
          AddInternalTranslationError(model, "формировании исходных строк", ex);
          model.SourceLines = BuildFallbackLines(model);
        }
      }
    }

    private static void AddGlobalTranslationError(
        List<BaseCommandModel> models,
        string text,
        string stage,
        Exception ex)
    {
      if (models.Count == 0)
      {
        models.Add(CreateFatalTranslationModel(text, stage, ex));
        return;
      }

      AddInternalTranslationError(models[0], stage, ex);
    }

    private static void AddInternalTranslationError(
        BaseCommandModel model,
        string stage,
        Exception ex,
        string? prefix = null)
    {
      string command = GetCommandDisplay(model);
      string description = BuildInternalErrorDescription(command, stage, prefix);

      if (model.Errors.Any(error =>
          error.Code == ErrorCode.Unknown &&
          string.Equals(error.Description, description, StringComparison.Ordinal)))
      {
        return;
      }

      model.Errors.Add(new ErrorItem
      {
        SourceLineNumber = model.StartLineNumber > 0 ? model.StartLineNumber : 1,
        Command = string.IsNullOrWhiteSpace(command) ? "Трансляция программы" : command,
        Code = ErrorCode.Unknown,
        DebugInfo = $"{ex.GetType().Name}: {ex.Message}",
        Description = description
      });
    }

    private static string BuildInternalErrorDescription(
        string command,
        string stage,
        string? prefix = null)
    {
      string beginning = string.IsNullOrWhiteSpace(prefix)
          ? "Внутренняя ошибка транслятора"
          : prefix;

      return string.IsNullOrWhiteSpace(command)
          ? $"{beginning} на этапе {stage}. Исправьте текст и повторите трансляцию."
          : $"{beginning} на этапе {stage} для команды {command}. Исправьте текст и повторите трансляцию.";
    }

    private static string GetCommandDisplay(BaseCommandModel model)
    {
      var command = $"{model.CommandNumber} {model.Mnemonic}".Trim();
      return command;
    }

    private static BaseCommandModel CreateCommandFailureModel(
        string commandNumber,
        string mnemonic,
        int lineNumber,
        List<string>? lines,
        string stage,
        Exception ex)
    {
      var model = new UnknownCommandModel
      {
        CommandNumber = commandNumber,
        Mnemonic = mnemonic,
        StartLineNumber = lineNumber,
        SourceLines = lines != null && lines.Count > 0
            ? new List<string>(lines)
            : new List<string> { $"{commandNumber} {mnemonic}".Trim() }
      };

      AddInternalTranslationError(model, stage, ex);
      return model;
    }

    private static BaseCommandModel CreateFatalTranslationModel(string text, string stage, Exception ex)
    {
      var model = new UnknownCommandModel
      {
        CommandNumber = string.Empty,
        Mnemonic = string.Empty,
        StartLineNumber = 1,
        SourceLines = BuildFatalSourceLines(text, stage)
      };

      AddInternalTranslationError(model, stage, ex);
      return model;
    }

    private static List<string> BuildFatalSourceLines(string text, string stage)
    {
      if (!string.IsNullOrWhiteSpace(text))
      {
        return text.Replace("\r\n", "\n").Split('\n').ToList();
      }

      return new List<string>
      {
        "// Трансляция завершилась внутренней ошибкой.",
        $"// Этап: {stage}"
      };
    }

    private static string BuildRawFallbackText(List<BaseCommandModel> models, string text)
    {
      var lines = models
          .SelectMany(model => model.SourceLines ?? Enumerable.Empty<string>())
          .Where(line => !string.IsNullOrWhiteSpace(line))
          .ToList();

      if (lines.Count > 0)
      {
        return string.Join("\n", lines);
      }

      return text ?? string.Empty;
    }
  }

  public sealed class TranslationBuildResult
  {
    public TranslationBuildResult(List<BaseCommandModel> models, string formattedText)
    {
      Models = models;
      FormattedText = formattedText;
    }

    public List<BaseCommandModel> Models { get; }

    public string FormattedText { get; }
  }

  public class UnknownCommandModel : BaseCommandModel { }
}

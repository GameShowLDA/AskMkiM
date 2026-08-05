using Ask.Core.Services.Errors.Models;
using Ask.Core.Shared.DTO.Executor;
using Ask.Engine.ControlCommandAnalyser;
using Ask.Engine.ControlCommandAnalyser.Diagnostics;
using ICSharpCode.AvalonEdit.Document;
using System.Text.RegularExpressions;

namespace UI.Controls.TextEditorControl.Syntax
{
  /// <summary>
  /// Выполняет командный анализ текста редактора через общий движок разбора
  /// программ контроля и преобразует найденные ошибки в диапазоны AvalonEdit.
  /// </summary>
  public sealed class CommandTranslationSyntaxAnalyzer
  {
    private static readonly Regex InternalIdentifierPattern = new Regex(
      @"\b[A-Za-z_]*[a-z][A-Za-z0-9_]*\b|\b[A-Za-z_][A-Za-z0-9_]*(?:\.[A-Za-z_][A-Za-z0-9_]*)+\b|\.cs\b",
      RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private readonly CommandTranslationManager _translationManager;

    /// <summary>
    /// Создаёт анализатор командных диагностик.
    /// </summary>
    /// <param name="translationManager">Менеджер трансляции, используемый как источник правил разбора.</param>
    public CommandTranslationSyntaxAnalyzer(CommandTranslationManager translationManager)
    {
      _translationManager = translationManager ?? throw new ArgumentNullException(nameof(translationManager));
    }

    /// <summary>
    /// Анализирует документ и возвращает диагностики, построенные по ошибкам
    /// и предупреждениям командных моделей.
    /// </summary>
    /// <param name="document">Документ AvalonEdit.</param>
    /// <returns>Список диагностик с абсолютными смещениями в документе.</returns>
    public IReadOnlyList<TextSyntaxDiagnostic> Analyze(TextDocument document)
    {
      if (document == null || string.IsNullOrWhiteSpace(document.Text))
      {
        return Array.Empty<TextSyntaxDiagnostic>();
      }

      try
      {
        var models = _translationManager.ParseForDiagnostics(document.Text);
        var commentSpans = SyntaxCommentScanner.Scan(document);
        return BuildDiagnostics(document, models, commentSpans);
      }
      catch (Exception)
      {
        // Сбой инфраструктуры анализатора не является ошибкой текста и не
        // должен создавать ложное подчёркивание в документе.
        return Array.Empty<TextSyntaxDiagnostic>();
      }
    }

    /// <summary>
    /// Формирует список диагностик редактора по моделям команд и найденным комментариям.
    /// </summary>
    /// <param name="document">Документ AvalonEdit.</param>
    /// <param name="models">Модели команд, полученные из командного анализатора.</param>
    /// <param name="commentSpans">Диапазоны комментариев в исходном документе.</param>
    /// <returns>Список диагностик редактора.</returns>
    private static IReadOnlyList<TextSyntaxDiagnostic> BuildDiagnostics(
      TextDocument document,
      IEnumerable<BaseCommandModel> models,
      IReadOnlyList<TextSpan> commentSpans)
    {
      var diagnostics = new List<TextSyntaxDiagnostic>();
      var modelList = models
        .OrderBy(model => model.StartLineNumber <= 0 ? int.MaxValue : model.StartLineNumber)
        .ToList();

      for (int i = 0; i < modelList.Count; i++)
      {
        var model = modelList[i];
        int endLineNumber = GetModelEndLineNumber(document, modelList, i);

        diagnostics.AddRange(CommandKeySyntaxAnalyzer.Analyze(
          document,
          model,
          endLineNumber,
          commentSpans));

        foreach (var issue in GetIssues(model))
        {
          var diagnostic = CreateDiagnostic(document, model, issue, endLineNumber, commentSpans);
          if (diagnostic != null)
            diagnostics.Add(diagnostic);
        }
      }

      return diagnostics;
    }

    /// <summary>
    /// Возвращает общий поток ошибок и предупреждений команды.
    /// </summary>
    /// <param name="model">Модель команды.</param>
    /// <returns>Последовательность диагностик команды.</returns>
    private static IEnumerable<IDisplayIssue> GetIssues(BaseCommandModel model)
    {
      return model.Errors
        .Cast<IDisplayIssue>()
        .Concat(model.Warnings)
        .Where(issue => !TranslationDiagnosticClassifier.IsEquipmentRelated(issue));
    }

    /// <summary>
    /// Определяет последнюю строку команды по её исходным строкам и позиции следующей команды.
    /// </summary>
    /// <param name="document">Документ AvalonEdit.</param>
    /// <param name="models">Список моделей команд в порядке исходного текста.</param>
    /// <param name="index">Индекс текущей модели команды.</param>
    /// <returns>Номер последней строки текущей команды.</returns>
    private static int GetModelEndLineNumber(
      TextDocument document,
      IReadOnlyList<BaseCommandModel> models,
      int index)
    {
      var model = models[index];
      int startLine = model.StartLineNumber;
      int sourceLineCount = model.SourceLines?.Count ?? 0;
      int sourceEndLine = sourceLineCount > 0
        ? startLine + sourceLineCount - 1
        : startLine;

      for (int i = index + 1; i < models.Count; i++)
      {
        if (models[i].StartLineNumber > startLine)
        {
          int previousLine = models[i].StartLineNumber - 1;
          return Math.Clamp(Math.Min(sourceEndLine, previousLine), 1, document.LineCount);
        }
      }

      return Math.Clamp(sourceEndLine, 1, document.LineCount);
    }

    /// <summary>
    /// Создаёт UI-диагностику AvalonEdit из диагностики командного анализатора.
    /// </summary>
    /// <param name="document">Документ AvalonEdit.</param>
    /// <param name="model">Модель команды, к которой относится диагностика.</param>
    /// <param name="issue">Ошибка или предупреждение командного анализатора.</param>
    /// <param name="modelEndLineNumber">Последняя строка команды в исходном документе.</param>
    /// <param name="commentSpans">Диапазоны комментариев.</param>
    /// <returns>Диагностика для редактора или null, если диапазон не найден.</returns>
    private static TextSyntaxDiagnostic? CreateDiagnostic(
      TextDocument document,
      BaseCommandModel model,
      IDisplayIssue issue,
      int modelEndLineNumber,
      IReadOnlyList<TextSpan> commentSpans)
    {
      if (!CommandIssueSpanResolver.TryResolve(
            document,
            model,
            issue,
            modelEndLineNumber,
            commentSpans,
            out var span))
      {
        return null;
      }

      return new TextSyntaxDiagnostic
      {
        Code = issue.CodeString ?? (issue.IsWarning ? "WRN_UNKNOWN" : "ERR_UNKNOWN"),
        Message = GetUserMessage(issue),
        Severity = issue.IsWarning ? TextSyntaxSeverity.Warning : TextSyntaxSeverity.Error,
        StartOffset = span.StartOffset,
        Length = span.Length,
        LineNumber = span.LineNumber,
        ColumnNumber = span.ColumnNumber
      };
    }

    /// <summary>
    /// Возвращает пользовательский текст подсказки для диагностики.
    /// </summary>
    /// <param name="issue">Ошибка или предупреждение командного анализатора.</param>
    /// <returns>Текст, пригодный для отображения в подсказке редактора.</returns>
    private static string GetUserMessage(IDisplayIssue issue)
    {
      var message = issue.Description?.Trim();
      if (IsUserMessage(message))
      {
        return message!;
      }

      return issue.IsWarning
        ? "Проверьте запись команды: возможно, в ней есть неточность."
        : "Проверьте запись команды: в ней найдена ошибка.";
    }

    /// <summary>
    /// Проверяет, что сообщение выглядит как пользовательский русский текст,
    /// а не как технический идентификатор или имя метода.
    /// </summary>
    /// <param name="message">Текст сообщения диагностики.</param>
    /// <returns>Значение true, если сообщение можно показывать пользователю без замены.</returns>
    private static bool IsUserMessage(string? message)
    {
      if (string.IsNullOrWhiteSpace(message))
      {
        return false;
      }

      return message.Any(IsCyrillic)
             && !InternalIdentifierPattern.IsMatch(message);
    }

    /// <summary>
    /// Проверяет, относится ли символ к русскому алфавиту.
    /// </summary>
    /// <param name="ch">Проверяемый символ.</param>
    /// <returns>Значение true, если символ является кириллическим.</returns>
    private static bool IsCyrillic(char ch)
    {
      return ch is >= 'А' and <= 'я' or 'Ё' or 'ё';
    }

  }
}

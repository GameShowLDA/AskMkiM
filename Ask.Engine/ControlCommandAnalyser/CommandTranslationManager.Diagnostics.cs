using Ask.Core.Services.Errors.Models;
using Ask.Core.Shared.DTO.Executor;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandAnalyser.Parser;
using Ask.Engine.ControlCommandAnalyser.Validation;

namespace Ask.Engine.ControlCommandAnalyser;

public partial class CommandTranslationManager
{
  /// <summary>
  /// Checks an editor snapshot using the translation rules, without publishing
  /// progress, formatting the source or replacing the current translation models.
  /// Equipment configuration is read by the regular parsers; no commands are executed.
  /// </summary>
  public IReadOnlyList<SourceDiagnostic> AnalyzeSource(string text, CancellationToken cancellationToken = default)
  {
    cancellationToken.ThrowIfCancellationRequested();
    if (string.IsNullOrWhiteSpace(text)) return Array.Empty<SourceDiagnostic>();

    using var scope = CommandsModel.BeginAnalysisScope();
    var models = ParseAll(text, emitMessages: false, cancellationToken);
    var sourceModels = models.ToArray();
    cancellationToken.ThrowIfCancellationRequested();

    if (!HasCriticalStructuralErrors(models))
    {
      // Only the bus type is needed for validation. Resolve devices when translating.
      CheckVshModel(models, resolveEquipment: false);
      CkCommandValidator.ValidateVshCompatibility(models);
    }
    CommandPostAnalyzer.Analyze(models);
    cancellationToken.ThrowIfCancellationRequested();

    // An automatically inserted VSH has no source span. Attach its warning to RM.
    foreach (var generated in models.Except(sourceModels))
    {
      var rm = sourceModels.OfType<RmCommandModel>().LastOrDefault();
      if (rm == null) continue;
      foreach (var warning in generated.Warnings)
      {
        warning.SourceLineNumber = rm.StartLineNumber;
        rm.Warnings.Add(warning);
      }
    }

    return MapDiagnostics(text, sourceModels, cancellationToken);
  }

  private static IReadOnlyList<SourceDiagnostic> MapDiagnostics(
    string text, IReadOnlyList<BaseCommandModel> models, CancellationToken cancellationToken)
  {
    var lines = new List<(int Offset, string Code)>();
    for (int start = 0, i = 0; i <= text.Length; i++)
    {
      if (i < text.Length && text[i] is not ('\r' or '\n')) continue;
      lines.Add((start, text[start..i]));
      if (i < text.Length && text[i] == '\r' && i + 1 < text.Length && text[i + 1] == '\n') i++;
      start = i + 1;
    }

    // Mask the exact comment fragments extracted by the same preprocessor used by
    // translation, retaining columns for inline comments and multi-line blocks.
    var diagnostics = new List<SourceDiagnostic>();
    var (_, comments) = PreprocessText.PreprocessTextAndExtractComments(text, (lineIndex, column) =>
    {
      var line = lines[lineIndex];
      diagnostics.Add(new SourceDiagnostic(line.Offset + column, line.Code[column] == '/' ? 2 : 1,
        lineIndex + 1, true, "Комментарий не закрыт. Оставшийся текст будет считаться комментарием.", null));
    });
    foreach (var (lineIndex, comment) in comments)
    {
      if (lineIndex < 0 || lineIndex >= lines.Count || comment.Length == 0) continue;
      var line = lines[lineIndex];
      int index = line.Code.IndexOf(comment, StringComparison.Ordinal);
      if (index >= 0)
      {
        lines[lineIndex] = (line.Offset, line.Code[..index] + new string(' ', comment.Length) + line.Code[(index + comment.Length)..]);
      }
    }

    for (int modelIndex = 0; modelIndex < models.Count; modelIndex++)
    {
      cancellationToken.ThrowIfCancellationRequested();
      var model = models[modelIndex];
      int first = Math.Clamp(model.StartLineNumber - 1, 0, lines.Count - 1);
      int end = modelIndex + 1 < models.Count ? models[modelIndex + 1].StartLineNumber - 1 : lines.Count;
      end = Math.Clamp(end, first + 1, lines.Count);

      foreach (var issue in model.Errors.Cast<IDisplayIssue>().Concat(model.Warnings))
      {
        int target = issue.SourceLineNumber - 1;
        if (target < first || target >= end) target = first;
        IssueSelectionHint hint = default;
        bool found = IssueSelectionHintResolver.TryResolve(issue, lines[target].Code, out hint);
        // Some parsers attach body errors to the command header. Search its body,
        // never another command or a comment containing the same parameter.
        if (!found)
        {
          for (int lineIndex = first; lineIndex < end; lineIndex++)
          {
            if (!IssueSelectionHintResolver.TryResolve(issue, lines[lineIndex].Code, out hint)) continue;
            target = lineIndex;
            found = true;
            break;
          }
        }
        if (!found)
        {
          var code = lines[target].Code;
          int indent = code.Length - code.TrimStart().Length;
          hint = new IssueSelectionHint(indent, code.TrimEnd().Length - indent);
        }
        if (hint.Length > 0)
          diagnostics.Add(new SourceDiagnostic(lines[target].Offset + hint.StartIndex, hint.Length,
            target + 1, issue.IsWarning, issue.Description, issue.CodeString));
      }
    }
    return diagnostics.Distinct().OrderBy(d => d.Offset).ThenBy(d => d.IsWarning).ToArray();
  }
}

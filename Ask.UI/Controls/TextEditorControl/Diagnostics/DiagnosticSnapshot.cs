using Ask.Engine.ControlCommandAnalyser;
using ICSharpCode.AvalonEdit.Document;

namespace Ask.UI.Controls.TextEditorControl.Diagnostics;

/// <summary>Built by the analysis worker, then published without further mutation.</summary>
internal sealed class DiagnosticSnapshot
{
  internal static readonly DiagnosticSnapshot Empty = new();
  private readonly TextSegmentCollection<DiagnosticGroup> _groups = new();
  private readonly TextSegmentCollection<TextSegment> _errors = new();
  private readonly TextSegmentCollection<TextSegment> _warnings = new();
  private IReadOnlyList<SourceDiagnostic> _allNavigation = Array.Empty<SourceDiagnostic>();
  private IReadOnlyList<SourceDiagnostic> _errorNavigation = Array.Empty<SourceDiagnostic>();
  private IReadOnlyList<SourceDiagnostic> _warningNavigation = Array.Empty<SourceDiagnostic>();

  internal static DiagnosticSnapshot Create(IReadOnlyList<SourceDiagnostic> diagnostics,
    CancellationToken token = default)
  {
    var snapshot = new DiagnosticSnapshot();
    var groups = new Dictionary<(int Offset, int Length), List<SourceDiagnostic>>();
    foreach (var diagnostic in diagnostics)
    {
      token.ThrowIfCancellationRequested();
      if (diagnostic.Offset < 0 || diagnostic.Length <= 0
        || diagnostic.Offset > int.MaxValue - diagnostic.Length) continue;
      var key = (diagnostic.Offset, diagnostic.Length);
      if (!groups.TryGetValue(key, out var messages)) groups.Add(key, messages = new());
      messages.Add(diagnostic);
    }

    var allNavigation = new List<SourceDiagnostic>(groups.Count);
    var errorNavigation = new List<SourceDiagnostic>();
    var warningNavigation = new List<SourceDiagnostic>();
    TextSegment? error = null, warning = null;
    foreach (var pair in groups.OrderBy(p => p.Key.Offset).ThenBy(p => p.Key.Length))
    {
      token.ThrowIfCancellationRequested();
      var group = new DiagnosticGroup(pair.Key.Offset, pair.Key.Length, pair.Value.ToArray());
      snapshot._groups.Add(group);
      var firstError = group.Diagnostics.FirstOrDefault(d => !d.IsWarning);
      var firstWarning = group.Diagnostics.FirstOrDefault(d => d.IsWarning);
      if (firstError != null)
      {
        errorNavigation.Add(firstError);
        Merge(snapshot._errors, ref error, group);
      }
      if (firstWarning != null)
      {
        warningNavigation.Add(firstWarning);
        Merge(snapshot._warnings, ref warning, group);
      }
      allNavigation.Add(firstError ?? firstWarning!);
    }
    snapshot._allNavigation = allNavigation.ToArray();
    snapshot._errorNavigation = errorNavigation.ToArray();
    snapshot._warningNavigation = warningNavigation.ToArray();
    token.ThrowIfCancellationRequested();
    return snapshot;
  }

  private static void Merge(TextSegmentCollection<TextSegment> segments,
    ref TextSegment? previous, DiagnosticGroup group)
  {
    // Rendering cost depends on covered text, not the number of overlapping messages.
    if (previous != null && group.StartOffset <= previous.EndOffset)
      previous.Length = Math.Max(previous.EndOffset, group.EndOffset) - previous.StartOffset;
    else
    {
      previous = new TextSegment { StartOffset = group.StartOffset, Length = group.Length };
      segments.Add(previous);
    }
  }

  internal IEnumerable<TextSegment> GetUnderlines(int offset, int length, bool warnings,
    bool suppressWarningsUnderErrors = true)
  {
    if (!warnings || !suppressWarningsUnderErrors)
    {
      var segments = warnings ? _warnings : _errors;
      return segments.FindOverlappingSegments(offset, length);
    }

    return GetWarningsOutsideErrors(offset, length);
  }

  private IEnumerable<TextSegment> GetWarningsOutsideErrors(int offset, int length)
  {
    // A later red draw call cannot fully hide a yellow squiggle when their
    // ranges begin at different columns: the wave phases do not coincide.
    // Split warning ranges around errors so no yellow geometry exists under
    // any red diagnostic in the first place.
    foreach (var warning in _warnings.FindOverlappingSegments(offset, length))
    {
      int cursor = warning.StartOffset;
      int warningEnd = warning.EndOffset;

      foreach (var error in _errors.FindOverlappingSegments(warning.StartOffset, warning.Length))
      {
        if (error.EndOffset <= cursor) continue;
        if (error.StartOffset >= warningEnd) break;

        int errorStart = Math.Max(error.StartOffset, warning.StartOffset);
        if (errorStart > cursor)
          yield return new TextSegment { StartOffset = cursor, Length = errorStart - cursor };

        cursor = Math.Max(cursor, Math.Min(error.EndOffset, warningEnd));
        if (cursor >= warningEnd) break;
      }

      if (cursor < warningEnd)
        yield return new TextSegment { StartOffset = cursor, Length = warningEnd - cursor };
    }
  }

  internal IEnumerable<DiagnosticGroup> GetGroups(int offset, int length) =>
    _groups.FindOverlappingSegments(offset, length);

  internal IReadOnlyList<SourceDiagnostic> GetNavigationDiagnostics(bool showErrors, bool showWarnings) =>
    (showErrors, showWarnings) switch
    {
      (true, true) => _allNavigation,
      (true, false) => _errorNavigation,
      (false, true) => _warningNavigation,
      _ => Array.Empty<SourceDiagnostic>(),
    };

  internal sealed class DiagnosticGroup : TextSegment
  {
    internal IReadOnlyList<SourceDiagnostic> Diagnostics { get; }
    internal DiagnosticGroup(int offset, int length, IReadOnlyList<SourceDiagnostic> diagnostics)
    {
      StartOffset = offset;
      Length = length;
      Diagnostics = diagnostics;
    }
  }
}

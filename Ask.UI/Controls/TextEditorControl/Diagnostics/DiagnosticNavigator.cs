using Ask.Engine.ControlCommandAnalyser;

namespace Ask.UI.Controls.TextEditorControl.Diagnostics;

/// <summary>Finds the next visible diagnostic without scanning the complete list.</summary>
internal static class DiagnosticNavigator
{
  internal static SourceDiagnostic? FindTarget(
    IReadOnlyList<SourceDiagnostic> diagnostics,
    int caretOffset,
    int selectionStart,
    int selectionLength,
    bool previous)
  {
    if (diagnostics.Count == 0) return null;

    int selectedIndex = FindSelectedIndex(diagnostics, selectionStart, selectionLength);
    if (selectedIndex >= 0)
    {
      int adjacentIndex = previous ? selectedIndex - 1 : selectedIndex + 1;
      if (adjacentIndex < 0) adjacentIndex = diagnostics.Count - 1;
      else if (adjacentIndex >= diagnostics.Count) adjacentIndex = 0;
      return diagnostics[adjacentIndex];
    }

    int index = previous
      ? FindFirstAfter(diagnostics, caretOffset) - 1
      : FindFirstAtOrAfter(diagnostics, caretOffset);

    if (index < 0) index = diagnostics.Count - 1;
    else if (index >= diagnostics.Count) index = 0;
    return diagnostics[index];
  }

  private static int FindSelectedIndex(
    IReadOnlyList<SourceDiagnostic> diagnostics,
    int selectionStart,
    int selectionLength)
  {
    if (selectionLength <= 0) return -1;

    int index = FindFirstAtOrAfter(diagnostics, selectionStart);
    while (index < diagnostics.Count && diagnostics[index].Offset == selectionStart)
    {
      if (diagnostics[index].Length == selectionLength) return index;
      index++;
    }
    return -1;
  }

  private static int FindFirstAtOrAfter(IReadOnlyList<SourceDiagnostic> diagnostics, int offset)
  {
    int low = 0, high = diagnostics.Count;
    while (low < high)
    {
      int middle = low + ((high - low) / 2);
      if (diagnostics[middle].Offset < offset) low = middle + 1;
      else high = middle;
    }
    return low;
  }

  private static int FindFirstAfter(IReadOnlyList<SourceDiagnostic> diagnostics, int offset)
  {
    int low = 0, high = diagnostics.Count;
    while (low < high)
    {
      int middle = low + ((high - low) / 2);
      if (diagnostics[middle].Offset <= offset) low = middle + 1;
      else high = middle;
    }
    return low;
  }
}

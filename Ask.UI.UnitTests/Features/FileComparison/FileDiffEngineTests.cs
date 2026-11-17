using Ask.Core.Shared.DTO.TextEditor;
using Ask.UI.Features.FileComparison.Views;

namespace Ask.UI.UnitTests.Features.FileComparison;

/// <summary>
/// Проверяет построение построчных различий файлов.
/// </summary>
public sealed class FileDiffEngineTests
{
  /// <summary>
  /// Выделяет только изменённые символы и сохраняет точный текст обеих сторон.
  /// </summary>
  /// <param name="sourceText">Исходная строка.</param>
  /// <param name="targetText">Изменённая строка.</param>
  /// <param name="removed">Выделенные символы слева.</param>
  /// <param name="added">Выделенные символы справа.</param>
  [Theory]
  [InlineData("НР-12", "НР-1", "2", "")]
  [InlineData("*71-75", "*71-756", "", "6")]
  [InlineData("Режим: АВТО", "Режим: АВТА", "О", "А")]
  [InlineData("A B", "A  B", "", " ")]
  [InlineData("ABC", "AbC", "B", "b")]
  [InlineData("AA11BB22CC", "AA31BB24CC", "12", "34")]
  public void Compare_ModifiedLine_HighlightsChangedCharacters(
    string sourceText, string targetText, string removed, string added)
  {
    var result = FileDiffEngine.Compare(
      new OpenTextEditorDescriptor("source.txt", "source.txt", sourceText),
      new OpenTextEditorDescriptor("target.txt", "target.txt", targetText), CancellationToken.None);

    var source = Assert.Single(result.SourceLines);
    var target = Assert.Single(result.TargetLines);
    Assert.Equal(DiffLineKind.Modified, source.Kind);
    Assert.Equal(DiffLineKind.Modified, target.Kind);
    Assert.Equal(sourceText, string.Concat(source.Segments.Select(segment => segment.Text)));
    Assert.Equal(targetText, string.Concat(target.Segments.Select(segment => segment.Text)));
    Assert.Equal(removed, string.Concat(source.Segments.Where(segment => segment.IsChanged).Select(segment => segment.Text)));
    Assert.Equal(added, string.Concat(target.Segments.Where(segment => segment.IsChanged).Select(segment => segment.Text)));
    Assert.Same(DiffBrushes.GetModifiedBackground(), source.BackgroundBrush);
    Assert.Same(source.BackgroundBrush, target.BackgroundBrush);
    Assert.Equal("~", source.Prefix);
    Assert.Equal("~", target.Prefix);
    Assert.Equal(1, result.ModifiedLines);
    Assert.Equal(0, result.AddedLines);
    Assert.Equal(0, result.RemovedLines);
  }

  /// <summary>
  /// Сохраняет отдельные цвета добавленных и удалённых строк без посимвольного выделения.
  /// </summary>
  [Fact]
  public void Compare_AddedAndRemovedLines_KeepTheirColors()
  {
    var result = FileDiffEngine.Compare(
      new OpenTextEditorDescriptor("source.txt", "source.txt", "A\nX\nB\nC"),
      new OpenTextEditorDescriptor("target.txt", "target.txt", "A\nB\nC\nY"), CancellationToken.None);

    var removed = Assert.Single(result.SourceLines, line => line.Kind == DiffLineKind.Removed);
    var added = Assert.Single(result.TargetLines, line => line.Kind == DiffLineKind.Added);
    Assert.Same(DiffBrushes.GetMissingBackground(), removed.BackgroundBrush);
    Assert.Same(DiffBrushes.GetPresentBackground(), added.BackgroundBrush);
    Assert.Equal("-", removed.Prefix);
    Assert.Equal("+", added.Prefix);
    Assert.All(removed.Segments.Concat(added.Segments), segment => Assert.False(segment.IsChanged));
  }

  /// <summary>
  /// Сохраняет выравнивание строк после вставки в исходный текст.
  /// </summary>
  [Fact]
  public void Compare_InsertionBetweenLines_PreservesFollowingAlignment()
  {
    var source = new OpenTextEditorDescriptor("source.txt", "source.txt", "A\nB\nC");
    var target = new OpenTextEditorDescriptor("target.txt", "target.txt", "A\nX\nB\nC");

    var result = FileDiffEngine.Compare(source, target, CancellationToken.None);

    Assert.Equal(result.SourceLines.Count, result.TargetLines.Count);
    Assert.Equal(DiffLineKind.Unchanged, result.SourceLines[0].Kind);
    Assert.Equal(DiffLineKind.Empty, result.SourceLines[1].Kind);
    Assert.Equal(DiffLineKind.Added, result.TargetLines[1].Kind);
    Assert.Equal("X", result.TargetLines[1].Text);
    Assert.Equal("B", result.SourceLines[2].Text);
    Assert.Equal(DiffLineKind.Unchanged, result.SourceLines[2].Kind);
    Assert.Equal("C", result.SourceLines[3].Text);
    Assert.Equal(DiffLineKind.Unchanged, result.SourceLines[3].Kind);
    Assert.Equal(result.SourceLines.Count, result.Rows.Count);
    Assert.Same(result.SourceLines[1], result.Rows[1].Source);
    Assert.Same(result.TargetLines[1], result.Rows[1].Target);
  }

  /// <summary>
  /// Сохраняет пустую позицию напротив удалённой строки и следующую пару строк.
  /// </summary>
  [Fact]
  public void Compare_Deletion_PairsDeletedLineWithEmptyTarget()
  {
    var source = new OpenTextEditorDescriptor("source.txt", "source.txt", "A\nX\nB\nC");
    var target = new OpenTextEditorDescriptor("target.txt", "target.txt", "A\nB\nC");

    var result = FileDiffEngine.Compare(source, target, CancellationToken.None);

    Assert.Equal(4, result.Rows.Count);
    Assert.Equal(DiffLineKind.Removed, result.Rows[1].Source.Kind);
    Assert.Equal("X", result.Rows[1].Source.Text);
    Assert.Equal(DiffLineKind.Empty, result.Rows[1].Target.Kind);
    Assert.Equal("B", result.Rows[2].Source.Text);
    Assert.Equal("B", result.Rows[2].Target.Text);
  }
}

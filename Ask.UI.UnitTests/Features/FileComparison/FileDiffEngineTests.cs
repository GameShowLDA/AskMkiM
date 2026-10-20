using Ask.Core.Shared.DTO.TextEditor;
using Ask.UI.Features.FileComparison.Views;

namespace Ask.UI.UnitTests.Features.FileComparison;

/// <summary>
/// Проверяет построение построчных различий файлов.
/// </summary>
public sealed class FileDiffEngineTests
{
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
  }
}

using Ask.Core.Services.Translator;
using ICSharpCode.AvalonEdit.Document;

namespace Ask.UI.UnitTests.Controls.TextEditorControl;

public class NestedCommentTests
{
  private const string Text = "180 ПР\r\n\tКомментарии:\r\n"
    + "\t\t{190 ПР\r\n\t\t*К1/31-35*\r\n"
    + "\t\t200 ПР 15<Ом, И, Г, ЗС {проверка И, Г,ЗС}\r\n"
    + "\t\t*К1/11-15* {проверка П}\r\n\t\t210 ПР П\r\n\t\t}\r\n"
    + "\tВремя выдержки не задано.\r\n215 ПР\r\n\tТочки";

  [Fact]
  public void CommentColorRange_CoversEntireOuterBlock()
  {
    var range = Assert.Single(ProgramCommentRanges.Parse(Text));
    Assert.Equal(Text.IndexOf('{'), range.start);
    Assert.Equal(Text.LastIndexOf('}') + 1, range.end);
    Assert.Contains("210 ПР П", Text[range.start..range.end]);
    var masked = ProgramCommentRanges.Mask(Text);
    Assert.Equal(Text.Length, masked.Length);
    Assert.DoesNotContain("210 ПР П", masked);
    Assert.Contains("Время выдержки не задано.", masked);
  }

  [Fact]
  public void Folding_BothEditorsCreateOneFoldFor180()
  {
    var document = new TextDocument(Text);
    var modern = new Ask.UI.Controls.TextEditorControl.OpkwFoldingStrategy().CreateFoldings(document);
    var legacy = new global::UI.Controls.TextEditorControl.OpkwFoldingStrategy().CreateFoldings(document);
    foreach (var folds in new[] { modern, legacy })
    {
      Assert.Equal(new[] { "180 ПР", "215 ПР" }, folds.Select(fold => fold.Name));
      Assert.Equal(0, folds[0].StartOffset);
      Assert.Equal(Text.IndexOf("215 ПР") - 2, folds[0].EndOffset);
    }
  }

  [Theory]
  [InlineData("{", "}")]
  [InlineData("/*", "*/")]
  public void MixedBlocks_IgnoreDelimitersInsideLineComments(string open, string close)
  {
    var text = $"{open} /* nested {{ deep }} */\r\n// }} */ {{ /*\r\n210 ПР\r\n{close}\r\n215 ПР";
    var range = Assert.Single(ProgramCommentRanges.Parse(text));
    Assert.Equal(text.IndexOf("\r\n215 ПР"), range.end);
    Assert.DoesNotContain("210 ПР", ProgramCommentRanges.Mask(text));
    Assert.Contains("215 ПР", ProgramCommentRanges.Mask(text));
  }

  [Fact]
  public void UnclosedOuterBlock_RemainsColoredToEndOfDocument()
  {
    const string text = "{ /* nested */\r\n210 ПР";
    Assert.Equal((0, text.Length), Assert.Single(ProgramCommentRanges.Parse(text)));
  }
}

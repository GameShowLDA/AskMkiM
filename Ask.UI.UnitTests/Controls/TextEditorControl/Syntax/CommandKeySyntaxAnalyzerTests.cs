using System.Collections.Immutable;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums;
using Ask.Engine.ControlCommandAnalyser.Model.Ks;
using ICSharpCode.AvalonEdit.Document;
using UI.Controls.TextEditorControl.Syntax;

namespace Ask.UI.UnitTests.Controls.TextEditorControl.Syntax;

public sealed class CommandKeySyntaxAnalyzerTests
{
  [Fact]
  public void Analyze_KeyUnknownForCommand_ReturnsKeyDiagnostic()
  {
    const string source = "1 КС ЗР 10<Ом<15 *X1,X2*";
    var document = new TextDocument(source);
    var model = new KsCommandModel
    {
      StartLineNumber = 1,
      SourceLines = { source },
      AllowedAlgorithmKeys = ImmutableHashSet.Create(AlgorithmKey.Б, AlgorithmKey.Д)
    };

    var diagnostics = CommandKeySyntaxAnalyzer.Analyze(
      document,
      model,
      endLineNumber: 1,
      commentSpans: Array.Empty<TextSpan>());

    var diagnostic = Assert.Single(diagnostics);
    Assert.Equal("KEY001", diagnostic.Code);
    Assert.Equal(source.IndexOf("ЗР", StringComparison.Ordinal), diagnostic.StartOffset);
    Assert.Equal(2, diagnostic.Length);
  }

  [Fact]
  public void Analyze_EmptyAllowedAlgorithmKeys_UsesModelAttributeFallback()
  {
    const string source = "1 КС Д 10<Ом<15 *X1,X2*";
    var document = new TextDocument(source);
    var model = new KsCommandModel
    {
      StartLineNumber = 1,
      SourceLines = { source }
    };

    Assert.Empty(model.AllowedAlgorithmKeys);

    var diagnostics = CommandKeySyntaxAnalyzer.Analyze(
      document,
      model,
      endLineNumber: 1,
      commentSpans: Array.Empty<TextSpan>());

    Assert.Empty(diagnostics);
  }
}

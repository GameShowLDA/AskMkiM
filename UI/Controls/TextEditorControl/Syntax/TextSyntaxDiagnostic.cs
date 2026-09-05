using System;

namespace UI.Controls.TextEditorControl.Syntax
{
  public sealed class TextSyntaxDiagnostic
  {
    public string Code { get; init; } = string.Empty;

    public string Message { get; init; } = string.Empty;

    public TextSyntaxSeverity Severity { get; init; } = TextSyntaxSeverity.Error;

    public int StartOffset { get; init; }

    public int Length { get; init; }

    public int LineNumber { get; init; }

    public int ColumnNumber { get; init; }
  }
}

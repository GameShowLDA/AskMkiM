using System;

namespace UI.Controls.TextEditorControl.Syntax
{
  public sealed class CommandHeaderInfo
  {
    public string CommandNumber { get; init; } = string.Empty;

    public string Mnemonic { get; init; } = string.Empty;

    public int LineNumber { get; init; }

    public int LineOffset { get; init; }

    public int NumberStartOffset { get; init; }

    public int MnemonicStartOffset { get; init; }

    public int MnemonicLength { get; init; }

    public string SourceLine { get; init; } = string.Empty;
  }
}

using System;

namespace UI.Controls.TextEditorControl.Syntax
{
  public readonly record struct TextSpan(int StartOffset, int Length)
  {
    public int EndOffset => StartOffset + Length;

    public bool Contains(int offset)
    {
      return offset >= StartOffset && offset < EndOffset;
    }
  }
}

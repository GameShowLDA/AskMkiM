using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;
using Microsoft.EntityFrameworkCore.Infrastructure.Internal;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;
using System.Windows.Media;


namespace UI.Controls.ProtocolController
{
  public class ProtocolColorizingTransformer : DocumentColorizingTransformer
  {
    private readonly SolidColorBrush _headerBrush;
    private readonly SolidColorBrush _messageBrush;
    private readonly SolidColorBrush _timeBrush = new SolidColorBrush(Colors.YellowGreen);
    private readonly SolidColorBrush _idleBrush;

    public ProtocolColorizingTransformer(Color headerColor, Color messageColor)
    {
      _headerBrush = new SolidColorBrush(headerColor);
      _messageBrush = new SolidColorBrush(messageColor);
      _idleBrush = new SolidColorBrush(headerColor);
    }

    protected override void ColorizeLine(DocumentLine line)
    {
      string text = CurrentContext.Document.GetText(line);

      int headerEnd = text.IndexOf(":");
      int timeStart = text.IndexOf("[");
      int idleStart = text.IndexOf("|");

      if (headerEnd > 0)
      {
        ChangeLinePart(line.Offset, line.Offset + headerEnd, element =>
        {
          if (element is VisualLineText textElement)
            textElement.TextRunProperties.SetForegroundBrush(_headerBrush);
        });
      }

      if (headerEnd > 0 && (timeStart > headerEnd || timeStart == -1))
      {
        int msgStart = line.Offset + headerEnd + 2;
        int msgEnd = timeStart > 0 ? line.Offset + timeStart : line.EndOffset;
        if (msgEnd > msgStart)
        {
          ChangeLinePart(msgStart, msgEnd, element =>
          {
            if (element is VisualLineText textElement)
              textElement.TextRunProperties.SetForegroundBrush(_messageBrush);
          });
        }
      }

      if (timeStart > 0)
      {
        int timeEnd = text.IndexOf("]", timeStart);
        if (timeEnd > timeStart)
        {
          ChangeLinePart(line.Offset + timeStart, line.Offset + timeEnd + 1, element =>
          {
            if (element is VisualLineText textElement)
              textElement.TextRunProperties.SetForegroundBrush(_timeBrush);
          });
        }
      }

      if (idleStart > 0)
      {
        ChangeLinePart(line.Offset + idleStart, line.EndOffset, element =>
        {
          if (element is VisualLineText textElement)
            textElement.TextRunProperties.SetForegroundBrush(_idleBrush);
        });
      }
    }
  }
}

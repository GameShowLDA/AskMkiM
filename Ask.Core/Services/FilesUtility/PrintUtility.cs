using Ask.Core.Shared.DTO.Protocol;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using static Ask.LogLib.LoggerUtility;

namespace Ask.Core.Services.FilesUtility
{
  public static class PrintUtility
  {
    /// <summary>
    /// Выводит протокол на печать.
    /// </summary>
    public static void PrintProtocol(IEnumerable<ShowMessageModel> messages)
    {
      PrintDialog printDialog = new PrintDialog();
      if (printDialog.ShowDialog() != true)
        return;

      FlowDocument document = new FlowDocument
      {
        PagePadding = new Thickness(50),
        ColumnWidth = double.PositiveInfinity
      };
      PrintSettingsService.ApplyTo(document);

      foreach (var model in messages)
      {
        var paragraph = new Paragraph();

        if (!string.IsNullOrWhiteSpace(model.Header))
        {
          paragraph.Inlines.Add(new Run(model.Header)
          {
            Foreground = new SolidColorBrush(Colors.Black),
            FontSize = document.FontSize,
            FontWeight = FontWeights.Bold
          });
        }

        if (!string.IsNullOrWhiteSpace(model.Message))
        {
          paragraph.Inlines.Add(new Run(": "));

          paragraph.Inlines.Add(new Run(model.Message)
          {
            Foreground = new SolidColorBrush(Colors.Black),
            FontSize = document.FontSize
          });
        }

        document.Blocks.Add(paragraph);
      }

      IDocumentPaginatorSource source = document;
      printDialog.PrintDocument(source.DocumentPaginator, "Печать протокола...");
    }

    public static void PrintProtocol(ProtocolModel protocolModel, string protocolText)
    {
      try
      {
        PrintDialog printDialog = new PrintDialog();
        if (printDialog.ShowDialog() != true)
          return;

        FlowDocument document = new FlowDocument
        {
          PagePadding = new Thickness(50),
          TextAlignment = TextAlignment.Left,
          ColumnWidth = double.PositiveInfinity
        };
        PrintSettingsService.ApplyTo(document);

        var protocolArray = protocolText.Split('\n');

        var paragraph = new Paragraph();
        foreach (var str in protocolArray)
        {
          if (!string.IsNullOrWhiteSpace(str))
          {
            paragraph.Inlines.Add(new Run(str)
            {
              Foreground = new SolidColorBrush(Colors.Black),
              FontSize = document.FontSize,
            });
          }

          document.Blocks.Add(paragraph);
        }

        IDocumentPaginatorSource source = document;
        printDialog.PrintDocument(source.DocumentPaginator, "Печать протокола...");
      }
      catch (Exception ex)
      {
        LogException(ex, $"Произошла ошибка");
      }
    }

  }
}

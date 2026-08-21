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

      FlowDocument document = CreateDocument();

      foreach (var model in messages)
      {
        var paragraph = new Paragraph
        {
          LineHeight = 1,
          Margin = new Thickness(0),
        };
        string line = Ask.Core.Services.Protocols.ExecutionProtocolLineFormatter.Format(model);

        if (!string.IsNullOrWhiteSpace(line))
        {
          paragraph.Inlines.Add(new Run(line)
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

    /// <summary>
    /// Выводит текст протокола на печать.
    /// </summary>
    /// <param name="protocolModel">Модель протокола.</param>
    /// <param name="protocolText">Текст протокола.</param>
    public static void PrintProtocol(ProtocolModel protocolModel, string protocolText)
    {
      PrintProtocol(protocolText);
    }

    /// <summary>
    /// Выводит текст протокола на печать.
    /// </summary>
    /// <param name="protocolText">Текст протокола.</param>
    public static void PrintProtocol(string protocolText)
    {
      try
      {
        PrintDialog printDialog = new PrintDialog();
        if (printDialog.ShowDialog() != true)
          return;

        FlowDocument document = CreateDocument();

        var protocolArray = protocolText.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

        foreach (var str in protocolArray)
        {
          var paragraph = new Paragraph
          { 
            LineHeight = 1,
            Margin = new Thickness(0),
          };
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

    /// <summary>
    /// Создаёт документ печати с параметрами шрифта из настроек протокола.
    /// </summary>
    /// <returns>Настроенный документ печати.</returns>
    private static FlowDocument CreateDocument()
    {
      var document = new FlowDocument
      {
        PagePadding = new Thickness(50),
        TextAlignment = TextAlignment.Left,
        ColumnWidth = double.PositiveInfinity
      };

      PrintSettingsService.ApplyTo(document);
      return document;
    }

  }
}

using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Xps;
using System.Windows.Xps.Packaging;
using AppConfiguration.Base;
using UI.Components.Invoke.InvokeRichTextBox;

namespace UI.Controls.ProtocolController.Export
{
  /// <summary>
  /// Сервис экспорта протокола: сохранение в файл и печать.
  /// </summary>
  public class ProtocolExportService
  {
    private readonly InvokeRichTextBoxUI _richTextBox;
    private readonly string _fileName;

    /// <summary>
    /// Инициализирует новый экземпляр <see cref="ProtocolExportService"/>.
    /// </summary>
    /// <param name="richTextBox">RichTextBox, содержащий протокол.</param>
    public ProtocolExportService(InvokeRichTextBoxUI richTextBox, string header)
    {
      _richTextBox = richTextBox;
      _fileName = header;
    }

    /// <summary>
    /// Сохраняет протокол в файл, создавая папку с датой внутри директории сохранения.
    /// </summary>
    public async Task SaveProtocolAsync()
    {
      string baseDirectory = FileLocations.DataSaveDirectory;

      string dateFolder = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CurrentCulture);
      string fullDirectory = Path.Combine(baseDirectory, dateFolder);

      if (!Directory.Exists(fullDirectory))
      {
        Directory.CreateDirectory(fullDirectory);
      }

      string timePart = DateTime.Now.ToString("HH_mm_ss", CultureInfo.CurrentCulture);
      string filename = $"{_fileName}_{timePart}.rtf";
      string fullPath = Path.Combine(fullDirectory, filename);

      TextRange range = new TextRange(_richTextBox.Document.ContentStart, _richTextBox.Document.ContentEnd);

      await Task.Run(() =>
      {
        using (FileStream fileStream = new FileStream(fullPath, FileMode.Create))
        {
          range.Save(fileStream, DataFormats.Rtf);
        }
      }).ConfigureAwait(true);
    }

    /// <summary>
    /// Печатает текущий протокол.
    /// </summary>
    public void PrintProtocol()
    {
      PrintDialog printDialog = new PrintDialog();
      if (printDialog.ShowDialog() == true)
      {
        FlowDocument document = new FlowDocument
        {
          PagePadding = new Thickness(50),
          ColumnWidth = double.PositiveInfinity
        };

        TextRange sourceRange = new TextRange(_richTextBox.Document.ContentStart, _richTextBox.Document.ContentEnd);

        using (MemoryStream stream = new MemoryStream())
        {
          sourceRange.Save(stream, DataFormats.Xaml);
          stream.Position = 0;

          TextRange targetRange = new TextRange(document.ContentStart, document.ContentEnd);
          targetRange.Load(stream, DataFormats.Xaml);

          targetRange.ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.Black);
          targetRange.ApplyPropertyValue(TextElement.BackgroundProperty, Brushes.White);
        }

        IDocumentPaginatorSource paginator = document;
        printDialog.PrintDocument(paginator.DocumentPaginator, "Печать протокола...");
      }
    }
  }
}

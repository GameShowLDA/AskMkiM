using MigraDoc.DocumentObjectModel;
using MigraDoc.Rendering;
using System.Diagnostics;
using System.IO;
using static Ask.LogLib.LoggerUtility;

namespace UI.Components.MultiEditorMethods
{
  internal class PdfProtocolGenerator
  {
    public void SaveProtocol(string programName, string content)
    {
      var directory = new DirectoryInfo(AppContext.BaseDirectory);
      var parent1 = directory.Parent;
      var parent2 = parent1?.Parent;
      var historyPath = Path.Combine(parent2.FullName, "History");
      var dateFolderName = DateTime.Now.ToString("yyyy-MM-dd");
      var datePath = Path.Combine(historyPath, dateFolderName);
      var fileName = $"{programName}_{DateTime.Now.ToString("HHmmss")}.pdf";
      var fullFilePath = Path.Combine(datePath, fileName);

      // Создаём документ
      var document = new Document();
      var section = document.AddSection();

      // Заголовок
      var paragraphContent = section.AddParagraph(content);
      paragraphContent.Format.Font.Size = 12;
      paragraphContent.Format.Font.Name = "Consolas";

      // Генерация PDF
      var renderer = new PdfDocumentRenderer(true)
      {
        Document = document
      };

      try
      {
        renderer.RenderDocument();
        renderer.PdfDocument.Save(fullFilePath);
        Process.Start(new ProcessStartInfo(fullFilePath) { UseShellExecute = true });
      }
      catch (Exception ex)
      {
        Message.MessageBoxCustom.Show("Ошибка при сохранении PDF: " + ex.Message);
        LogError("Ошибка при сохранении PDF: " + ex.Message);
      }
    }
  }
}

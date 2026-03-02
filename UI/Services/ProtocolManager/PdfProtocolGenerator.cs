using Ask.Core.Shared.Metadata.Static;
using Ask.UI.Features.Notifications.Models;
using Ask.UI.Infrastructure.UI.Overlay.Notifications.Runtime;
using MigraDoc.DocumentObjectModel;
using MigraDoc.Rendering;
using System.Diagnostics;
using System.IO;
using static Ask.LogLib.LoggerUtility;

namespace UI.Services.ProtocolManager
{
  /// <summary>
  /// Сервис для генерации и сохранения протоколов проверки в формате PDF.
  /// 
  /// Основные задачи:
  /// <list type="bullet">
  ///   <item>Создание PDF-документа на основе переданного содержимого.</item>
  ///   <item>Формирование структуры папок для хранения протоколов (по дате запуска).</item>
  ///   <item>Автоматическое открытие сгенерированного файла после сохранения.</item>
  ///   <item>Логирование ошибок в процессе генерации и сохранения PDF.</item>
  /// </list>
  /// </summary>
  public class PdfProtocolGenerator
  {
    /// <summary>
    /// Генерирует PDF-документ протокола и сохраняет его в папке <c>History</c>, создавая подкаталог с текущей датой.
    /// </summary>
    /// <param name="programName">Имя программы или теста, по которому формируется протокол.</param>
    /// <param name="content">Текстовое содержимое протокола, которое будет записано в PDF.</param>
    public void GenerateAndSavePdfProtocol(string programName, string content)
    {
      try
      {
        string fullFilePath = BuildOutputPath(programName);
        Document document = CreateDocument(content);
        SavePdfDocument(document, fullFilePath);
        OpenPdfInViewer(fullFilePath);
      }
      catch (Exception ex)
      {
        HandleGenerationError(ex);
      }
    }

    #region 📁 1. Подготовка пути для сохранения

    /// <summary>
    /// Формирует путь сохранения PDF-файла на основе текущей даты и имени программы.
    /// </summary>
    private string BuildOutputPath(string programName)
    {

      var baseDir = new DirectoryInfo(AppContext.BaseDirectory);
      var root = baseDir?.Parent;
      if (root != null)
      {
        var historyDir = Path.Combine(root.FullName, FileLocations.DataSaveDirectory);

        var dateDir = Path.Combine(historyDir, DateTime.Now.ToString("yyyy-MM-dd"));

        Directory.CreateDirectory(dateDir); // ✅ гарантируем, что папка существует

        var fileName = $"{programName}_{DateTime.Now:HHmmss}.pdf";
        return Path.Combine(dateDir, fileName);
      }
      return null;
    }

    #endregion

    #region 📄 2. Создание документа

    /// <summary>
    /// Создаёт PDF-документ на основе переданного текстового содержимого.
    /// </summary>
    private Document CreateDocument(string content)
    {
      var document = new Document();
      var section = document.AddSection();

      var paragraph = section.AddParagraph(content);
      paragraph.Format.Font.Size = 12;
      paragraph.Format.Font.Name = "Consolas";

      return document;
    }

    #endregion

    #region 💾 3. Сохранение PDF

    /// <summary>
    /// Сохраняет PDF-документ по указанному пути.
    /// </summary>
    private void SavePdfDocument(Document document, string fullFilePath)
    {
      var renderer = new PdfDocumentRenderer(unicode: true)
      {
        Document = document
      };

      renderer.RenderDocument();
      renderer.PdfDocument.Save(fullFilePath);
      LogInformation($"PDF успешно сохранён: {fullFilePath}");
    }

    #endregion

    #region 📂 4. Автоматическое открытие PDF

    /// <summary>
    /// Открывает сгенерированный PDF в стандартном приложении просмотра.
    /// </summary>
    private void OpenPdfInViewer(string filePath)
    {
      Process.Start(new ProcessStartInfo(filePath)
      {
        UseShellExecute = true
      });
    }

    #endregion

    #region ❌ 5. Обработка ошибок

    /// <summary>
    /// Обрабатывает ошибки генерации и сохраняет их в лог.
    /// </summary>
    private void HandleGenerationError(Exception ex)
    {
      NotificationHostService.Instance.Show(
        "Ошибка сохранения PDF",
        ex.Message,
        NotificationType.Error);
      LogException("Ошибка при сохранении PDF", ex);
    }

    #endregion
  }
}

using System.IO.Compression;
using UI.Components.ArchiveManager.ArchiveFiles.ApkwArchive;
using UI.Components.ArchiveManager.ArchiveFiles.Index;
using UI.Components.ArchiveManager.Models;
using static Utilities.LoggerUtility;


namespace UI.Components.ArchiveManager.ArchiveFiles
{
  public class OpkEditor
  {
    /// <summary>
    /// Создает новый файл .opk.
    /// </summary>
    public async Task CreateOpk(string path, OpkFile newOpkFile, List<string> content)
    {
      var archiveEditor = new ArchiveEditor();
      var fileEditor = new FileEditor();
      var indexEditor = new IndexEditor();
      var isFileAdded = false;

      var isArchiveOpened = await archiveEditor.OpenArchive(path);
      if (isArchiveOpened)
      {
        isFileAdded = await fileEditor.AddFileToArchive(path, content, newOpkFile.OpkFilename, FileFormatEnum.Pk);
      }
      if (isFileAdded)
      {
        OpkFileForIndex indexOpkFile = new OpkFileForIndex(newOpkFile.Marking, newOpkFile.Name, newOpkFile.Order, newOpkFile.OpkFilename, newOpkFile.Creation,
          newOpkFile.Department, newOpkFile.Description);

        if (await DataValidation.CheckFileInArchive(path, indexOpkFile))
        {
          await indexEditor.WriteDataToIndex(path, indexOpkFile);
        }
        await ControlSum.RewriteYamlControlSum(path);
      }
      else
      {
        LogError("Ошибка при добавлении файла");
      }
    }

    /// <summary>
    /// Удаляет opk-файл из архива.
    /// </summary>
    /// <param name="path">Путь к архиву из которого необходимо удалить файл.</param>
    public bool DeleteOpk(string path, string fileName)
    {
      var archiveEditor = new ArchiveEditor();
      if (archiveEditor.OpenArchiveInternal(path))
      {
        using (ZipArchive archive = ZipFile.Open(path, ZipArchiveMode.Update))
        {
          ZipArchiveEntry foundEntry = archive.GetEntry(fileName);
          if (foundEntry != null)
          {
            foundEntry.Delete();
            LogInformation("Файл удален из архива.");
            return archive.GetEntry(fileName) == null;
          }
          else
          {
            LogInformation("Файл с таким именем не существует в архиве");
            return false;
          }
        }
      }
      return false;
    }
  }
}

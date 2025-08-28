using Newtonsoft.Json;
using System.IO;
using System.IO.Compression;
using UI.Components.ArchiveManager.ArchiveFiles.ApkwArchive;
using UI.Components.ArchiveManager.Models;
using static Utilities.LoggerUtility;


namespace UI.Components.ArchiveManager
{
  public static class DataValidation
  {
    /// <summary>
    /// Проверяет наличие файла в архиве и вычисляет его контрольную сумму.
    /// </summary>
    /// <param name="path">Путь к архиву, в котором производится поиск файла.</param>
    /// <param name="indexOpkFile">Объект OpkFileForIndex, содержащий информацию о проверяемом файле.</param>
    /// <returns>true, если файл найден и контрольная сумма успешно вычислена; в противном случае выбрасывает исключение.</returns>
    /// <exception cref="FileNotFoundException">Выбрасывается, если файл не найден в архиве.</exception>
    public static async Task<bool> CheckFileInArchive(string path, OpkFileForIndex indexOpkFile)
    {
      var archiveEncryption = new ArchiveEncryption();
      return await archiveEncryption.ExecuteSecureOperation<bool>(async tempPath =>
      {
        return await CheckFileInArchiveInternal(tempPath, indexOpkFile);
      },
      path);
    }

    public static async Task<bool> CheckFileInArchiveInternal(string tempPath, OpkFileForIndex indexOpkFile)
    {
      using (ZipArchive archive = ZipFile.OpenRead(tempPath))
      {
        var foundFile = archive.GetEntry(indexOpkFile.OpkFilename);
        if (foundFile == null)
        {
          throw new FileNotFoundException($"Файл {indexOpkFile.OpkFilename} не найден в архиве");
        }
        else
        {
          indexOpkFile.ControlSum = ControlSum.ComputeControlSum(foundFile, tempPath);
          return true;
        }
      }
    }

    /// <summary>
    /// Получает файлы из архива и выполняет проверку. 
    /// </summary>
    /// <param name="tempPath">Путь к временной директории расшифрованного архива.</param>
    /// <param name="archive">Архив.</param>
    public static void CheckArchiveEntries(string tempPath, ZipArchive archive)
    {
      List<OpkFileForIndex> data = GetArchiveEntries(archive);

      foreach (ZipArchiveEntry entry in archive.Entries)
      {
        if (entry.Name != ArchiveSettings.IndexName && entry.Name != ArchiveSettings.YamlName)
        {
          CompareArchiveEntriesWithIndex(tempPath, data, entry);
        }
      }
    }

    /// <summary>
    /// Сравнивает обнаруженные в архиве файлы с записями в index.json, а также проверяет контрольные суммы файлов.
    /// </summary>
    /// <param name="path">Путь к архиву.</param>
    /// <param name="data">Данные, считанные из индекса.</param>
    /// <param name="entry">Файл, считанный из архива.</param>
    private static bool CompareArchiveEntriesWithIndex(string path, List<OpkFileForIndex> data, ZipArchiveEntry entry)
    {
      using (StreamReader reader = new StreamReader(entry.Open()))
      {
        try
        {
          if (!(data.FirstOrDefault(opkName => opkName.OpkFilename == entry.Name) != null
          && ControlSum.ComputeControlSum(entry, path) == data.FirstOrDefault(opkFile => opkFile.OpkFilename == entry.Name).ControlSum))
          {
            LogError($"Файл: {entry.Name} поврежден");
            return false;
          }
        }
        catch
        {
          LogError($"Архив поврежден");
          return false;
        }

        return true;
      }
    }

    /// <summary>
    /// Получает список файлов, находящихся в архиве.
    /// </summary>
    /// <param name="archive"></param>
    /// <returns></returns>
    private static List<OpkFileForIndex> GetArchiveEntries(ZipArchive archive)
    {
      List<OpkFileForIndex> data = new List<OpkFileForIndex>();
      var fileEntry = archive.GetEntry(ArchiveSettings.IndexName);
      if (fileEntry != null)
      {
        using (StreamReader reader = new StreamReader(fileEntry.Open()))
        {
          string content = reader.ReadToEnd();
          if (!(string.IsNullOrEmpty(content) || content == "{}" || content == "[]" || string.IsNullOrWhiteSpace(content)))
          {
            data = JsonConvert.DeserializeObject<List<OpkFileForIndex>>(content);
            LogInformation("Файлы архива считаны");
          }
          else
          {
            LogWarning("Архив не содержит файлов");
          }
        }
      }
      return data;
    }
  }
}

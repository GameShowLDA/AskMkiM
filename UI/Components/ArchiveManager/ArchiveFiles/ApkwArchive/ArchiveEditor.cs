using Newtonsoft.Json.Linq;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using UI.Components.ArchiveControls;
using UI.Components.ArchiveManager.ArchiveFiles.Index;
using UI.Components.ArchiveManager.Models;
using Utilities.Encrypter;
using YamlDotNet.Serialization;
using static Utilities.LoggerUtility;


namespace UI.Components.ArchiveManager.ArchiveFiles.ApkwArchive
{
  public class ArchiveEditor
  {
    /// <summary>
    /// Создает архив с расширением apkw и необходимые для работы архива файлы.
    /// </summary>
    /// <param name="archivePath">Путь к директории, в которой необходимо создать архив.</param>
    public async Task<bool> CreateArchive(string archivePath, string archiveName, string archiveDescription = "", bool isMain = false)
    {
      try
      {
        string archiveFullPath = Path.Combine(archivePath, $"{archiveName}.apkw");
        if (File.Exists(archiveFullPath))
        {
          LogError($"Файл {archiveFullPath} уже существует");
          return false;
        }
        var archiveEncryption = new ArchiveEncryption();
        return await archiveEncryption.ExecuteSecureOperation<bool>(async tempPath =>
        {
          using (ZipArchive archive = ZipFile.Open(tempPath, ZipArchiveMode.Create))
          { }
          var fileEditor = new FileEditor();
          if (fileEditor.AddFileProcess(tempPath))
          {
            var apkFile = new ApkArchive()
            {
              ArchiveName = archiveName,
              Description = archiveDescription,
              IsMain = isMain,
            };
            var indexEditor = new IndexEditor();
            var indexPath = Path.Combine(ArchiveSettings.ArchivePath, ArchiveSettings.IndexName);
            if (!File.Exists(indexPath))
            {
              using (FileStream fs = File.Create(indexPath)) { }
              var indexText = File.ReadAllText(indexPath);
              var newData = FileEncryptionManager.Encrypt(indexText);
              File.WriteAllText(indexPath, newData);
            }
            JObject apkFileJson = JObject.FromObject(apkFile);
            TableAllArchivesControl.indexData.Add(apkFileJson);
            return await indexEditor.RewriteApkwIndex(apkFile);
          }
          return false;
        },
        archiveFullPath, 
        isNewArchive: true);
      }
      catch (Exception ex)
      {
        LogError($"Ошибка при создании архива: {ex.Message}");
        return false;
      }
    }

    /// <summary>
    /// Открывает архив, проверяет контрольную сумму всего архива, проверяет контрольную сумму каждого файла.
    /// При обнаружении несоответсвий выводит уведомление об ошибке.
    /// </summary>
    /// <param name="path">Путь к зашифрованному архиву.</param>
    public async Task<bool> OpenArchive(string path)
    {
      try
      {
        var archiveEncryption = new ArchiveEncryption();
        return await archiveEncryption.ExecuteSecureOperation<bool>(async tempPath =>
        {
          return OpenArchiveInternal(tempPath);
        }, 
        path);
      }
      catch (CryptographicException ex)
      {
        LogError($"Ошибка при расшифровке архива: {ex.Message}");
        return false;
      }
      catch (Exception ex)
      {
        LogError($"Ошибка при открытии архива: {ex.Message}");
        return false;
      }
    }

    /// <summary>
    /// Открывает архив, проверяет контрольную сумму всего архива, проверяет контрольную сумму каждого файла.
    /// При обнаружении несоответсвий выводит уведомление об ошибке.
    /// </summary>
    /// <param name="path">Путь к зашифрованному архиву.</param>
    public async Task<string> GetArchiveEntry(string path, string fileName)
    {
      try
      {
        var archiveEncryption = new ArchiveEncryption();
        ZipArchiveEntry arhiveEntry;
        return await archiveEncryption.ExecuteSecureOperation<string>(async tempPath =>
        {
          return GetFileArchiveEntry(tempPath, fileName);
        }, path
        );
      }
      catch (CryptographicException ex)
      {
        LogError($"Ошибка при расшифровке архива: {ex.Message}");
        return null;
      }
      catch (Exception ex)
      {
        LogError($"Ошибка при открытии архива: {ex.Message}");
        return null;
      }
    }

    public bool OpenArchiveInternal(string tempPath)
    {
      using (ZipArchive archive = ZipFile.OpenRead(tempPath))
      {
        GetYamlControlData(archive, out YamlControlData yamlData);

        if (yamlData == null)
        {
          return false;
        }
        else
        {
          return ValidateControlSum(tempPath, archive, yamlData);
        }
      }
    }

    private static bool ValidateControlSum(string tempPath, ZipArchive archive, YamlControlData yamlData)
    {
      var controlSumOriginal = yamlData.TotalControlSum;
      var controlSumFound1 = ControlSum.ComputeControlSum(tempPath, ArchiveSettings.YamlName);
      LogInformation($"Original checksum: {controlSumOriginal}");
      LogInformation($"Computed checksum: {controlSumFound1}");
        
      if (yamlData.TotalControlSum == ControlSum.ComputeControlSum(tempPath, ArchiveSettings.YamlName))
      {
        LogInformation("Контрольная сумма архива совпала.");
        DataValidation.CheckArchiveEntries(tempPath, archive);
        return true;
      }
      else
      {
        LogWarning("Контрольная сумма архива не совпала.");
        return false;
      }
    }

    /// <summary>
    /// Получает данные из config.yaml.
    /// </summary>
    /// <param name="archive">Архив.</param>
    /// <param name="yamlData">Считанные данные из yaml-файла.</param>
    private static void GetYamlControlData(ZipArchive archive, out YamlControlData yamlData)
    {
      yamlData = new YamlControlData(string.Empty);
      var yamlEntry = archive.GetEntry(ArchiveSettings.YamlName);
      if (yamlEntry != null)
      {
        LogInformation("Yaml файл найден.");
        yamlData = ReadYaml(yamlEntry);
      }
    }

    /// <summary>
    /// Получает Opk файл из архива.
    /// </summary>
    /// <param name="archive">Архив.</param>
    /// <param name="fileName">Название файла.</param>
    private static string GetFileArchiveEntry(string archivePath, string fileName)
    {
      string destinationPath = string.Empty;
      using (ZipArchive archive = ZipFile.OpenRead(archivePath))
      {
        foreach (ZipArchiveEntry entry in archive.Entries)
        {
          if (entry.FullName == fileName)
          {
            destinationPath = Path.Combine(Path.GetTempPath(), ArchiveSettings.TempArchivePath, entry.FullName);
            if (!File.Exists(destinationPath))
            {
              using (FileStream fs = File.Create(destinationPath)) { }
            }
            entry.ExtractToFile(destinationPath, overwrite: true);

            LogInformation($"Файл {fileName} найден.");
            return destinationPath;
          }
        }
        return destinationPath;
      }
    }

    /// <summary>
    /// Считывает yaml файл и десериализует его содержимое.
    /// </summary>
    /// <param name="yamlEntry">Файл yaml, найденный в архиве.</param>
    /// <returns>Данные, считанные из yaml-файла.</returns>
    private static YamlControlData ReadYaml(ZipArchiveEntry yamlEntry)
    {
      YamlControlData yamlData;
      using (StreamReader reader = new StreamReader(yamlEntry.Open()))
      {
        string content = reader.ReadToEnd();
        var deserializer = new DeserializerBuilder().Build();

        yamlData = deserializer.Deserialize<YamlControlData>(content);
      }
      return yamlData;
    }
  }
}

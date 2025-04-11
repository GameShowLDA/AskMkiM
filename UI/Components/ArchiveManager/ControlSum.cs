using System.IO.Compression;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using static Utilities.LoggerUtility;
using YamlDotNet.Serialization;
using UI.Components.ArchiveManager.Models;
using UI.Components.ArchiveManager.ArchiveFiles.ApkwArchive;


namespace UI.Components.ArchiveManager
{
  public static class ControlSum
  {
    /// <summary>
    /// Подсчет контрольной суммы файла.
    /// </summary>
    /// <param name="foundFile">Файл из архива.</param>
    /// <param name="archivePath">Путь к архиву.</param>
    /// <returns>Строку контрольной суммы.</returns>
    public static string ComputeControlSum(ZipArchiveEntry foundFile, string archivePath)
    {
      try
      {
        using (ZipArchive archive = ZipFile.OpenRead(archivePath))
        {
          using (var md5 = MD5.Create())
          using (var stream = foundFile.Open())
          {
            byte[] hash = md5.ComputeHash(stream);
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
          }
        }
      }
      catch (Exception ex)
      {
        throw new Exception($"Ошибка при подсчете контрольной суммы файла {foundFile.Name}: {ex.Message}", ex);
      }
    }

    /// <summary>
    /// Подсчитывает контрольную сумму для всех файлов в архиве, без учета yaml.
    /// </summary>
    /// <param name="archivePath">Путь к архиву.</param>
    /// <returns>Строку контрольной суммы.</returns>
    public static string ComputeControlSum(string archivePath, string yamlName)
    {
      using (var md5 = MD5.Create())
      using (var memoryStream = new MemoryStream())
      using (var archive = ZipFile.OpenRead(archivePath))
      {
        var entries = archive.Entries
            .Where(entry => !entry.Name.Equals(yamlName, StringComparison.OrdinalIgnoreCase))
            .OrderBy(entry => entry.FullName)
            .ToList();

        foreach (var entry in entries)
        {
          using (var entryStream = entry.Open())
          {
            byte[] nameBytes = Encoding.UTF8.GetBytes(entry.FullName);
            memoryStream.Write(nameBytes, 0, nameBytes.Length);

            entryStream.CopyTo(memoryStream);
          }
        }

        memoryStream.Position = 0;
        byte[] hash = md5.ComputeHash(memoryStream);
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
      }
    }

    /// <summary>
    /// Пересчитывает контрольную сумму архива и записывает ее в yaml.
    /// </summary>
    /// <param name="path">Путь к архиву.</param>
    /// <summary>
    /// Перезаписывает контрольную сумму в YAML-файле архива.
    /// </summary>
    /// <param name="path">Путь к архиву</param>
    public static bool RewriteYamlControlSumInternal(string path)
    {
      try
      {
        LogInformation($"Временный путь RewriteYamlControlSum {path}");
        var newControlSum = ComputeControlSum(path, ArchiveSettings.YamlName);

        using (ZipArchive archive = ZipFile.Open(path, ZipArchiveMode.Update))
        {
          ZipArchiveEntry yamlEntry = archive.GetEntry(ArchiveSettings.YamlName);
          yamlEntry?.Delete();

          yamlEntry = archive.CreateEntry(ArchiveSettings.YamlName);
          using (var writer = new StreamWriter(yamlEntry.Open()))
          {
            var serializer = new SerializerBuilder().Build();
            var data = new YamlControlData(newControlSum);
            LogInformation($"Записанная контрольная сумма: {data.TotalControlSum}");
            serializer.Serialize(writer, data);
          }
        }

        return true;
      }
      catch (Exception ex)
      {
        LogError($"Ошибка при обновлении контрольной суммы: {ex.Message}");
        throw;
      }
    }

    /// <summary>
    /// Перезаписывает контрольную сумму в YAML-файле архива.
    /// </summary>
    /// <param name="path">Путь к зашифрованному архиву.</param>
    /// <returns>
    /// <see langword="true"/> - если контрольная сумма успешно перезаписана;
    /// <see langword="false"/> - если произошла ошибка при перезаписи.
    /// </returns>
    public static async Task<bool> RewriteYamlControlSum(string path)
    {
      try
      {
        var archiveEncryption = new ArchiveEncryption();
        return await archiveEncryption.ExecuteSecureOperation<bool>(async tempPath =>
        {
          return RewriteYamlControlSumInternal(tempPath);
        },
        path);
      }
      catch (Exception ex)
      {
        LogError($"Ошибка при добавлении файла: {ex.Message}");
        return false;
      }
    }
  }
}

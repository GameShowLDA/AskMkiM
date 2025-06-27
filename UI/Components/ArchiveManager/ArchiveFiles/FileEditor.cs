using Newtonsoft.Json;
using System.IO.Compression;
using System.Windows;
using UI.Components.ArchiveManager.ArchiveFiles.ApkwArchive;
using YamlDotNet.Serialization;
using static Utilities.LoggerUtility;
using Microsoft.Win32;
using System.IO;
using UI.Components.ArchiveManager.ArchiveFiles.Index;
using UI.Components.ArchiveManager.Models;
using static UI.Controls.Message.MessageBox;


namespace UI.Components.ArchiveManager.ArchiveFiles
{
  public class FileEditor
  {
    #region Добавление файла

    /// <summary>
    /// Процесс добавления файла в архив.
    /// </summary>
    /// <param name="tempPath">Временный путь к расшифрованному архиву.</param>
    /// <returns>
    /// <see langword="true"/> - если архив успешно создан;
    /// <see langword="false"/> - если архив не удалось создать.
    /// </returns>
    public bool AddFileProcess(string tempPath)
    {
      if (AddFileToArchiveInternal<OpkFileForIndex>(tempPath, null, ArchiveSettings.IndexName, FileFormatEnum.Json))
      {
        var controlSum = ControlSum.ComputeControlSum(tempPath, ArchiveSettings.YamlName);
        if (AddFileToArchiveInternal(tempPath, new YamlControlData(controlSum), ArchiveSettings.YamlName, FileFormatEnum.Yaml))
        {
          LogInformation($"Архив успешно создан и зашифрован");
          return true;
        }
      }
      return false;
    }

    /// <summary>
    /// Записывает данные в файл и затем добавляет файл в архив.
    /// </summary>
    /// <param name="zipPath">Путь к архиву.</param>
    /// <param name="fileToAdd">Объект класса OpkFile, который необходимо добавить в архив.</param>
    /// <returns><see langword="true"/> , если файл успешно добавлен в архив. Иначе - <see langword="false"/> .</returns>
    public async Task<bool> AddFileToArchive<T>(string zipPath, T fileToAdd, string fileName, FileFormatEnum format)
    {
      try
      {
        var archiveEncryption = new ArchiveEncryption();
        return await archiveEncryption.ExecuteSecureOperation<bool>(async tempPath =>
        {
          return AddFileToArchiveInternal(tempPath, fileToAdd, fileName, format);
        },
        zipPath);
      }
      catch (Exception ex)
      {
        LogError($"Ошибка при добавлении файла: {ex.Message}");
        return false;
      }
    }

    /// <summary>
    /// Добавляет файлы в расшифрованный архив.
    /// </summary>
    /// <typeparam name="T">Тип добавляемого объекта.</typeparam>
    /// <param name="zipPath">Путь к архиву, в который добавляется файл.</param>
    /// <param name="fileToAdd">Объект, который необходимо добавить в архив.</param>
    /// <param name="fileName">Имя файла, под которым объект будет сохранен в архиве.</param>
    /// <param name="format">Формат файла для сохранения объекта.</param>
    /// <returns>
    /// <see langword="true"/> - если файл успешно добавлен в архив;
    /// <see langword="false"/> - если файл с таким именем уже существует или произошла ошибка при добавлении.
    /// </returns>
    public bool AddFileToArchiveInternal<T>(string zipPath, T fileToAdd, string fileName, FileFormatEnum format)
    {
      using (ZipArchive archive = ZipFile.Open(zipPath, ZipArchiveMode.Update))
      {
        ZipArchiveEntry foundEntry = archive.GetEntry(fileName);
        if (foundEntry == null)
        {
          ZipArchiveEntry entry = archive.CreateEntry(fileName);
          using (StreamWriter writer = new StreamWriter(entry.Open()))
          {
            CreateFile(fileToAdd, format, writer);
          }
          return archive.GetEntry(fileName) != null;
        }
        else
        {
          Show(Status.Error, "Файл с таким именем уже существует в архиве!", "Ошибка!", MessageBoxButton.OK);
          LogInformation("Файл с таким именем уже существует в архиве");
          return false;
        }
      }
    }

    #endregion

    /// <summary>
    /// Удаляет файл из архива, удаляет информацию о нем из индекса, пересчитывает контрольную сумму.
    /// </summary>
    /// <param name="path">Путь к архиву.</param>
    /// <param name="fileName">Название файла, который необходимо удалить.</param>
    /// <returns>
    /// <see langword="true"/> - если файл успешно добавлен в архив;
    /// <see langword="false"/> - если файл с таким именем уже существует или произошла ошибка при добавлении.
    /// </returns>
    public async Task<bool> DeleteFileFromArchive(string path, string fileName)
    {
      try
      {
        var archiveEncryption = new ArchiveEncryption();
        return await archiveEncryption.ExecuteSecureOperation<bool>(async tempPath =>
        {
          if (fileName == ArchiveSettings.YamlName || fileName == ArchiveSettings.IndexName)
          {
            LogWarning("Попытка удаления системного файла");
            return false;
          }

          var opkEditor = new OpkEditor();
          var indexEditor = new IndexEditor();

          if (!opkEditor.DeleteOpk(tempPath, fileName))
          {
            LogWarning($"Не удалось удалить файл {fileName}");
            return false;
          }

          indexEditor.DeleteDataFromIndex(tempPath, fileName);

          if (!ControlSum.RewriteYamlControlSumInternal(tempPath))
          {
            return false;
          }

          using (ZipArchive archive = ZipFile.Open(tempPath, ZipArchiveMode.Update))
          {
            return archive.GetEntry(fileName) == null;
          }
        }, path);
      }
      catch (Exception ex)
      {
        LogError($"Ошибка при удалении файла: {ex.Message}");
        throw new Exception($"Ошибка при обновлении индекса: {ex.Message}", ex);
      }
    }

    /// <summary>
    /// Обновляет файл в архиве.
    /// </summary>
    /// <param name="archivePath">Путь к архиву</param>
    /// <param name="fileName">Имя обновляемого файла.</param>
    /// <param name="newOpkFilePath">Путь к файлу для обновления.</param>
    /// <returns>Результат обновления файла.</returns>
    /// <exception cref="Exception"></exception>
    public async Task<bool> UpdateFile(string archivePath, string fileName, string newOpkFilePath)
    {
      try
      {
        if (!ValidateUpdateFiles(archivePath, fileName, newOpkFilePath))
        {
          return false;
        }

        var pkEditor = new PkEditor();

        bool canConvert = await ValidateNewFile(pkEditor, newOpkFilePath);
        if (!canConvert)
        {
          throw new InvalidOperationException("Новый файл не может быть корректно сконвертирован");
        }

        if (await DeleteFileFromArchive(archivePath, fileName))
        {
          return await pkEditor.ConvertPkToOpk(newOpkFilePath, archivePath);
        }
        return false;
      }
      catch (Exception ex)
      {
        LogError($"Ошибка при обновлении файла: {ex.Message}");
        throw;
      }
    }

    private static bool ValidateUpdateFiles(string archivePath, string fileName, string newOpkFilePath)
    {
      if (string.IsNullOrEmpty(archivePath) || string.IsNullOrEmpty(fileName) || string.IsNullOrEmpty(newOpkFilePath))
      {
        return false;
        throw new ArgumentException("Один из параметров пуст");
      }

      if (!File.Exists(archivePath))
      {
        return false;
        throw new FileNotFoundException("Архив не найден", archivePath);
      }

      if (!File.Exists(newOpkFilePath))
      {
        return false;
        throw new FileNotFoundException("Новый файл не найден", newOpkFilePath);
      }

      return true;
    }

    private async Task<bool> ValidateNewFile(PkEditor pkEditor, string newOpkFilePath)
    {
      try
      {
        string tempPath = Path.GetTempFileName();
        try
        {
          bool result = await pkEditor.ConvertPkToOpk(newOpkFilePath, tempPath);
          return result;
        }
        finally
        {
          if (File.Exists(tempPath))
          {
            File.Delete(tempPath);
          }
        }
      }
      catch
      {
        return false;
      }
    }

    #region Создание файла

    /// <summary>
    /// Создает файл указанного формата с содержимым или пустой структурой.
    /// </summary>
    /// <typeparam name="T">Тип объекта для сериализации.</typeparam>
    /// <param name="fileToAdd">Объект для записи в файл. Если null, создается пустой файл.</param>
    /// <param name="format">Формат файла.</param>
    /// <param name="writer">StreamWriter для записи данных в файл.</param>
    public void CreateFile<T>(T fileToAdd, FileFormatEnum format, StreamWriter writer)
    {
      if (fileToAdd == null)
      {
        CreateEmptyFile<T>(format, writer);
      }
      else
      {
        CreateNotEmptyFile(fileToAdd, format, writer);
      }
    }

    /// <summary>
    /// Сериализует объект в файл формата JSON или YAML.
    /// </summary>
    /// <typeparam name="T">Тип сериализуемого объекта.</typeparam>
    /// <param name="fileToAdd">Объект для сериализации. Может быть null.</param>
    /// <param name="format">Формат файла (JSON или YAML).</param>
    /// <param name="writer">StreamWriter для записи данных в файл.</param>
    private void CreateNotEmptyFile<T>(T fileToAdd, FileFormatEnum format, StreamWriter writer)
    {
      string content = string.Empty;
      switch (format)
      {
        case FileFormatEnum.Yaml:
          var serializer = new SerializerBuilder().Build();
          content = serializer.Serialize(fileToAdd);
          break;

        case FileFormatEnum.Json:
          if (fileToAdd != null)
          {
            content = JsonConvert.SerializeObject(fileToAdd, Formatting.Indented);
            content = Utilities.Encrypter.FileEncryptionManager.Encrypt(content);
          }
          else
          {
            content = JsonConvert.SerializeObject(fileToAdd, Formatting.None);
            content = Utilities.Encrypter.FileEncryptionManager.Encrypt(content);
          }

          break;
        case FileFormatEnum.Pk:
        default:
          if (fileToAdd is List<string>)
          {
            var fileStrings = fileToAdd as List<string>;
            foreach (var str in fileStrings)
              if (content != string.Empty)
              {
                content = content.Insert(content.Length - 1, $"\n{str}");
              }
              else
              {
                content = content.Insert(0, str);
              }
          }
          break;
      }
      writer.Write(content);
    }

    /// <summary>
    /// Создает пустой файл в формате JSON или YAML с соответствующей структурой.
    /// </summary>
    /// <typeparam name="T">Тип объекта, определяющий структуру создаваемого файла.</typeparam>
    /// <param name="format">Формат файла (JSON или YAML).</param>
    /// <param name="writer">StreamWriter для записи данных в файл.</param>
    public void CreateEmptyFile<T>(FileFormatEnum format, StreamWriter writer)
    {
      switch (format)
      {
        case FileFormatEnum.Yaml:
          writer.Write("{}");
          break;

        case FileFormatEnum.Json:
        default:
          if (typeof(T).IsGenericType &&
              typeof(T).GetGenericTypeDefinition() == typeof(List<>))
          {
            writer.Write("[]");
          }
          else
          {
            writer.Write("{}");
          }

          break;
      }
    }

    #endregion

    internal void SaveOpkFile(string foundOpkPath, string fileName)
    {
      SaveFileDialog saveFileDialog = new SaveFileDialog();
      saveFileDialog.FileName = fileName;
      saveFileDialog.Filter = "Pk files (*.pk)|*.pk;*.Pk|All files (*.*)|*.*";
      saveFileDialog.DefaultExt = ".pk";
      saveFileDialog.AddExtension = true;

      if (saveFileDialog.ShowDialog() == true)
      {
        string content;
        try
        {
          content = File.ReadAllText(foundOpkPath);
          string filePath = saveFileDialog.FileName;
          File.WriteAllText(filePath, content);
          LogInformation($"Файл {foundOpkPath} считан и записан.");
          Show(Status.Information, $"Файл {fileName}.pk успешно сохранен!", "Файл сохранен", MessageBoxButton.OK);
        }
        catch (Exception ex)
        {
          Console.WriteLine($"Ошибка при чтении файла: {ex.Message}");
        }
      }
    }
  }
}

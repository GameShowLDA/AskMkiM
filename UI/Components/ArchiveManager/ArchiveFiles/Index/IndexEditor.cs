using Message;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using System.IO.Compression;
using System.Windows;
using System.Windows.Controls;
using UI.Components.ArchiveManager.ArchiveFiles.ApkwArchive;
using UI.Components.ArchiveManager.Models;
using static Utilities.LoggerUtility;


namespace UI.Components.ArchiveManager.ArchiveFiles.Index
{
  public class IndexEditor
  {
    /// <summary>
    /// Записывает данные о новом opk-файле в index.json.
    /// </summary>
    /// <param name="filePath">Путь к index.json.</param>
    /// <param name="newOpkFile">Объект класса OpkFile, данные о которм необходимо записать в index.json.</param>
    public async Task WriteDataToIndex(string filePath, OpkFile newOpkFile)
    {
      try
      {
        var archiveEncryption = new ArchiveEncryption();
        await archiveEncryption.ExecuteSecureOperation<bool>(async tempPath =>
        {
          using (ZipArchive archive = ZipFile.Open(tempPath, ZipArchiveMode.Update))
          {
            JArray jsonArray = GetJsonArray(newOpkFile, archive);
            AddJObjectsToArray(newOpkFile, jsonArray);
            RewriteIndexEntry(archive, jsonArray);
          }
          return true;
        }, 
        filePath);
      }
      catch (Exception ex)
      {
        throw new Exception($"Ошибка при обновлении индекса: {ex.Message}", ex);
      }
    }

    /// <summary>
    /// Переписать данные в index.json.
    /// </summary>
    /// <param name="archive">Архив.</param>
    /// <param name="jsonArray">Массив объектов json, которые необходимо записать в index.json.</param>
    private static void RewriteIndexEntry(ZipArchive archive, JArray jsonArray)
    {
      var newEntry = archive.CreateEntry(ArchiveSettings.IndexName);
      using (var writer = new StreamWriter(newEntry.Open()))
      {
        writer.Write(jsonArray.ToString(Formatting.Indented));
      }
    }

    /// <summary>
    /// Удалить данные о файле из index.json.
    /// </summary>
    /// <param name="filePath">Путь к файлу, который необходимо удалить.</param>
    /// <param name="fileName">Имя файла.</param>
    /// <exception cref="Exception"></exception>
    public void DeleteDataFromIndex(string filePath, string fileName)
    {
      try
      {
        using (ZipArchive archive = ZipFile.Open(filePath, ZipArchiveMode.Update))
        {
          var indexEntry = archive.GetEntry(ArchiveSettings.IndexName);
          JArray jsonArray;

          if (indexEntry == null)
          {
            jsonArray = new JArray();
          }
          else
          {
            using (var reader = new StreamReader(indexEntry.Open()))
            {
              string jsonContent = reader.ReadToEnd();
              jsonArray = TryRemoveDataFromIndex(fileName, jsonContent);
            }

            indexEntry.Delete();
          }

          RewriteIndexEntry(archive, jsonArray);
        }
      }
      catch (Exception ex)
      {
        throw new Exception($"Ошибка при обновлении индекса: {ex.Message}", ex);
      }
    }

    public void DeleteDataFromIndex(string fileName)
    {
      var indexPath = Path.Combine(ArchiveSettings.ArchivePath, ArchiveSettings.IndexName);

      string jsonContent = File.ReadAllText(indexPath);
      var jsonArray = TryRemoveDataFromIndex(fileName, jsonContent);
      using (var fileStream = new FileStream(indexPath, FileMode.Create, FileAccess.Write))
      using (var writer = new StreamWriter(fileStream))
      {
        writer.Write(jsonArray.ToString(Formatting.Indented));
      }
    }

    /// <summary>
    /// Пытается удалить данные из индекса.
    /// </summary>
    /// <param name="fileName">Имя файла, который необходимо удалить.</param>
    /// <param name="jsonContent">Текст, считанный из json-файла.</param>
    /// <returns>Обновленный массив json-объектов.</returns>
    /// <exception cref="InvalidOperationException">Исключение возникает при попытке считать невалидный json файл.</exception>
    private static JArray TryRemoveDataFromIndex(string fileName, string jsonContent)
    {
      JArray jsonArray;
      try
      {
        jsonArray = GetJsonArray(jsonContent);
        var existingEntry = CheckExistingEntry(fileName, jsonArray);
        if (existingEntry == null)
        {
          throw new InvalidOperationException(
              $"Файл {fileName} не существует в индексе");
        }
        else
        {
          jsonArray.Remove(existingEntry);
        }
      }
      catch (JsonReaderException ex)
      {
        LogWarning($"Невалидный JSON в индексном файле: {ex.Message}");
        jsonArray = new JArray();
      }

      return jsonArray;
    }

    /// <summary>
    /// Добавляет объекты в массив json-объектов.
    /// </summary>
    /// <param name="newOpkFile">Объект, который необходимо добавить.</param>
    /// <param name="jsonArray">Массив json-объектов.</param>
    /// <exception cref="InvalidOperationException">Исключение, возникающее при попытке добавить в массив пустой объект.</exception>
    private static void AddJObjectsToArray(OpkFile newOpkFile, JArray jsonArray)
    {
      var newObject = JObject.FromObject(newOpkFile);
      if (!newObject.Properties().Any())
      {
        throw new InvalidOperationException("Попытка добавить пустой объект");
      }

      jsonArray.Add(newObject);
    }

    /// <summary>
    /// Получает массив объектов json из считанного файла.
    /// </summary>
    /// <param name="newOpkFile">Объект, который необходимо добавить.</param>
    /// <param name="archive">Архив.</param>
    /// <returns>Обновленный массив объектов json.</returns>
    private static JArray GetJsonArray(OpkFile newOpkFile, ZipArchive archive)
    {
      var indexEntry = archive.GetEntry(ArchiveSettings.IndexName);
      JArray jsonArray;

      if (indexEntry == null)
      {
        jsonArray = new JArray();
      }
      else
      {
        using (var reader = new StreamReader(indexEntry.Open()))
        {
          string jsonContent = reader.ReadToEnd();
          jsonArray = TryAddNewDataToIndex(newOpkFile, jsonContent);
        }
        indexEntry.Delete();
      }

      return jsonArray;
    }

    public static async Task<List<ApkArchive>> GetApkArrayAsync(string indexPath, List<ApkArchive> existingArchives)
    {
      if (File.Exists(indexPath))
      {
        using (var reader = new StreamReader(indexPath))
        {
          string content;
          try
          {
            var data = File.ReadAllText(indexPath);
            content = Utilities.Encrypter.FileEncryptionManager.Decrypt(data);
          }
          catch (Exception ex)
          {
            LogError($"Произошла ошибка: {ex}");
            throw;
          }
          try
          {
            existingArchives = JsonConvert.DeserializeObject<List<ApkArchive>>(content)
                ?? new List<ApkArchive>();
          }
          catch (JsonException)
          {
            existingArchives = new List<ApkArchive>();
            if (content == "{}" || content == string.Empty)
            {
              return existingArchives;
            }
            else
            {
              var existingArchive = JsonConvert.DeserializeObject<ApkArchive>(content);
              if (existingArchive != null)
              {
                existingArchives.Add(existingArchive);
              }
            }
          }
        }
      }
      return existingArchives;
    }

    public async Task<bool> RewriteApkwIndex(ApkArchive newApkw)
    {
      var indexPath = Path.Combine(ArchiveSettings.ArchivePath, ArchiveSettings.IndexName);
      List<ApkArchive> existingArchives = new List<ApkArchive>();

      existingArchives = await GetApkArrayAsync(indexPath, existingArchives);

      existingArchives.Add(newApkw);
      var fileEditor = new FileEditor();
      using (StreamWriter writer = new StreamWriter(indexPath, false))
      {
        fileEditor.CreateFile(existingArchives, FileFormatEnum.Json, writer);
        return true;
      }
    }

    /// <summary>
    /// Пытается добавить новые данные в index.json.
    /// </summary>
    /// <param name="newOpkFile">Объект, который необходимо добавить.</param>
    /// <param name="jsonContent">Текст, считанный из json-файла.</param>
    /// <returns>Обновленный массив json-объектов.</returns>
    /// <exception cref="InvalidOperationException">Исключение возникает при попытке считать невалидный json файл.</exception>
    private static JArray TryAddNewDataToIndex(OpkFile newOpkFile, string jsonContent)
    {
      JArray jsonArray;
      try
      {
        jsonArray = GetJsonArray(jsonContent);
        List<JToken> itemsToRemove = FindEmptyItems(jsonArray);
        RemoveEmptyItems(jsonArray, itemsToRemove);

        if (CheckExistingEntry(newOpkFile.OpkFilename, jsonArray) != null)
        {
          throw new InvalidOperationException(
              $"Файл {newOpkFile.OpkFilename} уже существует в индексе");
        }
      }
      catch (JsonReaderException ex)
      {
        LogWarning($"Невалидный JSON в индексном файле: {ex.Message}");
        jsonArray = new JArray();
      }

      return jsonArray;
    }

    /// <summary>
    /// Проверяет, существует объект в массиве или нет.
    /// </summary>
    /// <param name="fileName">Имя файла.</param>
    /// <param name="jsonArray">Массив объектов json.</param>
    /// <returns>Найденный в массиве объект или null.</returns>
    private static JToken CheckExistingEntry(string fileName, JArray jsonArray)
    {
      var result = jsonArray.FirstOrDefault(item =>
                      item["OpkFilename"]?.ToString() == fileName);
      if (result == null)
      {
        result = jsonArray.FirstOrDefault(item =>
                      item["ArchiveName"]?.ToString() == fileName);
      }
      return result;
    }

    /// <summary>
    /// Удаляет пустые элементы из массива.
    /// </summary>
    /// <param name="jsonArray">Массив объектов json.</param>
    /// <param name="itemsToRemove">Коллекция объектов, которые необходимо удалить из массива.</param>
    private static void RemoveEmptyItems(JArray jsonArray, List<JToken> itemsToRemove)
    {
      foreach (var item in itemsToRemove)
      {
        jsonArray.Remove(item);
      }

      LogInformation($"Удалено {itemsToRemove.Count} пустых объектов");
    }

    /// <summary>
    /// Находит пустые значения в json.
    /// </summary>
    /// <param name="jsonArray">Массив считанных json-объектов.</param>
    /// <returns>Массив с найденными пустыми объектами в исходном массиве.</returns>
    private static List<JToken> FindEmptyItems(JArray jsonArray)
    {
      return jsonArray.Where(token =>
                           (token.Type == JTokenType.Object && !token.Children().Any()) ||
                           token.ToString() == "{}" ||
                           token.ToString() == "null" ||
                           (token.Type == JTokenType.Object &&
                            token.Children().All(c => c.Type == JTokenType.Property &&
                                                    string.IsNullOrEmpty(c.First().ToString())))).ToList();
    }

    /// <summary>
    /// Создает массив объектов, полученных из json-файла.
    /// </summary>
    /// <param name="jsonContent">Данные, считанные из json файла.</param>
    /// <returns>Массив объектов json.</returns>
    private static JArray GetJsonArray(string jsonContent)
    {
      JArray jsonArray;
      try
      {
        jsonArray = JArray.Parse(jsonContent);
      }
      catch (JsonReaderException)
      {
        var jsonObject = JObject.Parse(jsonContent);
        jsonArray = new JArray { jsonObject };
      }

      return jsonArray;
    }


    public async Task<bool> PrintTable(string filePath, DataGrid table)
    {
      try
      {
        var archiveEncryption = new ArchiveEncryption();
        return await archiveEncryption.ExecuteSecureOperation<bool>(async tempPath =>
        {
          return await PrintTableInternal(tempPath, table);
        }, filePath
        );
      }
      catch (Exception ex)
      {
        LogError($"Ошибка при обновлении индекса: {ex.Message}");
        throw new Exception($"Ошибка при обновлении индекса: {ex.Message}", ex);
      }
    }

    public async Task<bool> PrintTableInternal(string path, DataGrid table)
    {
      using (ZipArchive archive = ZipFile.Open(path, ZipArchiveMode.Update))
      {
        var indexEntry = archive.GetEntry(ArchiveSettings.IndexName);

        if (indexEntry == null)
        {
          LogWarning("Файл индекса не найден.");
          return false;
        }
        else
        {
          return await ReadIndex(table, indexEntry);
        }
      }
    }

    public static async Task<bool> ReadIndex(DataGrid table, ZipArchiveEntry indexEntry)
    {
      List<OpkFile> data = new List<OpkFile>();
      return await ReadIndexFromArchive(table, indexEntry, data);
    }

    private static async Task<bool> ReadIndexFromArchive(DataGrid table, ZipArchiveEntry indexEntry, List<OpkFile> data)
    {
      using (StreamReader reader = new StreamReader(indexEntry.Open()))
      {
        string content = reader.ReadToEnd();
        return await TryGetDataFromIndex(table, data, content);
      }
    }

    public static async Task<bool> TryGetDataFromIndex<T>(DataGrid table, List<T> data, string content)
    {
      if (!(string.IsNullOrEmpty(content) || content == "{}" || content == "[]" || string.IsNullOrWhiteSpace(content)))
      {
        ProgressWindow progressWindow = null;
        try
        {
          await Application.Current.Dispatcher.InvokeAsync(() =>
          {
            progressWindow = new ProgressWindow();
            progressWindow.Show();
          });

          bool success = await Task.Run(() =>
          {
            try
            {
              return DeserializeIndex<T>(table, data, content);
            }
            catch (Exception ex)
            {
              Application.Current.Dispatcher.Invoke(() =>
              {
                MessageBoxCustom.Show($"Ошибка при загрузке файлов: {ex.Message}", image: MessageBoxImage.Error);
              });
              return false;
            }
          });

          return success;
        }
        finally
        {
          if (progressWindow != null)
          {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
              progressWindow.Close();
            });
          }
        }
      }
      else
      {
        LogWarning("В индексе нет записей.");
        return false;
      }
    }

    private static bool DeserializeIndex<T>(DataGrid table, List<T> data, string content)
    {
      var token = JToken.Parse(content);
      if (token is JArray)
      {
        data = JsonConvert.DeserializeObject<List<T>>(content);
      }
      else if (token is JObject)
      {
        var item = JsonConvert.DeserializeObject<T>(content);
        data = new List<T> { item };
      }
      else
      {
        throw new JsonException("Неподдерживаемый формат JSON");
      }

      LogInformation("Файлы архива считаны");


      Application.Current.Dispatcher.Invoke(() =>
      {
        table.ItemsSource = data;
      });
      return true;
    }
  }
}

using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace Utilities.FilesUtility
{
  public class JsonUtility<T>
  {
    private readonly string _filePath;
    private readonly JsonSerializerSettings _settings;
    public JsonUtility(string filePath)
    {
      _filePath = filePath;
      _settings = new JsonSerializerSettings
      {
        Converters = new List<JsonConverter> { new IPAddressConverter() },
        Formatting = Formatting.Indented
      };
    }

    /// <summary>
    /// Добавляет новую сущность в JSON файл.
    /// </summary>
    /// <param name="value">Новая сущность для добавления</param>
    public async Task CreateAsync(T value)
    {
      var entities = await ReadAsync();
      if (entities != null)
      {
        entities.Add(value);
        await RewriteAsync(entities);
      }
    }

    /// <summary>
    /// Удаляет сущность из JSON файла.
    /// </summary>
    /// <param name="value">Сущность для удаления</param>
    public async Task DeleteAsync(T value)
    {
      var entities = await ReadAsync();
      if (entities != null)
      {
        entities.Remove(value);
        await RewriteAsync(entities);
      }
    }

    /// <summary>
    /// Считывает все сущности из JSON файла асинхронно.
    /// </summary>
    /// <returns>Список всех сущностей</returns>
    public async Task<List<T>?> ReadAsync()
    {
      try
      {
        LoggerUtility.LogInformation($"Начало чтения файла JSON: {_filePath}");

        if (!File.Exists(_filePath))
        {
          LoggerUtility.LogError($"Файл JSON не найден по пути: {_filePath}");
          return new List<T>();
        }

        using (var reader = new StreamReader(_filePath))
        {
          string jsonString = await reader.ReadToEndAsync();
          return JsonConvert.DeserializeObject<List<T>>(jsonString, _settings) ?? new List<T>();
        }
      }
      catch (JsonException ex)
      {
        LoggerUtility.LogError($"Ошибка при чтении JSON конфигурации {_filePath}: {ex.Message}");
        return null;
      }
      catch (IOException ex)
      {
        LoggerUtility.LogError($"Ошибка ввода/вывода при чтении конфигурации{_filePath}: {ex.Message}");
        return null; ;
      }
      catch (UnauthorizedAccessException ex)
      {
        LoggerUtility.LogError($"Ошибка при чтении JSON конфигурации  {_filePath} : : {ex.Message}");
        return null; ;
      }
    }

    /// <summary>
    /// Перезаписывает все сущности в JSON файле.
    /// </summary>
    /// <param name="value">Список сущностей для записи</param>
    public async Task RewriteAsync(List<T> value)
    {
      string jsonString = JsonConvert.SerializeObject(value, _settings);
      using (var writer = new StreamWriter(_filePath, false, Encoding.UTF8))
      {
        await writer.WriteAsync(jsonString);
      }
    }
  }
}

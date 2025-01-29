using System.IO;
using System.Reflection;
using System.Text;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Utilities.FilesUtility
{
  /// <summary>
  /// Класс для работы с YAML файлами.
  /// Предоставляет методы для создания, чтения, удаления и перезаписи данных в YAML формате.
  /// </summary>
  /// <typeparam name="T">Тип объекта для сериализации/десериализации</typeparam>
  public class YamlUtility<T>
  {
    #region Поля.

    /// <summary>
    /// Путь к файлу YAML, с которым работает класс.
    /// </summary>
    private readonly string YamlFilePath;

    /// <summary>
    /// Сериализатор для преобразования объектов в формат YAML.
    /// </summary>
    private readonly ISerializer _serializer;

    /// <summary>
    /// Десериализатор для преобразования данных YAML в объекты.
    /// </summary>
    private readonly IDeserializer _deserializer;

    #endregion

    #region Публичные методы.

    /// <summary>
    /// Читает данные из YAML файла асинхронно.
    /// </summary>
    /// <returns>Объект типа T, содержащий данные из файла</returns>
    public async Task<T> ReadAsync()
    {
      try
      {
        LoggerUtility.LogInformation($"Начало чтения файла YAML: {YamlFilePath}");

        if (!File.Exists(YamlFilePath))
        {
          LoggerUtility.LogError($"Файл YAML не найден по пути: {YamlFilePath}");
          return default(T);
        }

        var result = Activator.CreateInstance<T>();
        var properties = typeof(T).GetProperties();

        var lines = await ReadYamlFileLinesAsync();
        LoggerUtility.LogInformation($"Прочитано {lines.Count()} строк из файла.");

        foreach (var line in lines)
        {
          ProcessYamlLine(line, properties, result);
        }

        LoggerUtility.LogInformation($"Чтение файла {YamlFilePath} завершено успешно.");
        return result;
      }
      catch (Exception ex)
      {
        LoggerUtility.LogError($"Ошибка чтения файла {YamlFilePath}: {ex}");
        return default(T);
      }
    }

    /// <summary>
    /// Перезаписывает данные в YAML файле.
    /// </summary>
    /// <param name="value">Новое значение для записи в файл</param>
    public async Task RewriteAsync(T value)
    {
      LoggerUtility.LogInformation($"Начинаем перезапись YAML файла: {YamlFilePath}");

      if (value == null)
      {
        LoggerUtility.LogError("Попытка перезаписать файл с null значением.");
        return;
      }

      try
      {
        if (!File.Exists(YamlFilePath))
        {
          using (var fileStream = File.Create(YamlFilePath)) { }
        }

        // Подготовка данных для записи
        var properties = typeof(T).GetProperties();
        var linesToWrite = new List<string>();
        foreach (var property in properties)
        {
          var propertyValue = property.GetValue(value)?.ToString() ?? "";
          var line = $"{property.Name}: \"{propertyValue}\"";
          linesToWrite.Add(line);

          // Выводим информацию в консоль о том, что собираемся записать
          Console.WriteLine($"Собираемся записать: {line}");
        }

        // Запись данных в файл
        using (var writer = new StreamWriter(YamlFilePath, false, Encoding.UTF8))
        {
          foreach (var line in linesToWrite)
          {
            await writer.WriteLineAsync(line);
          }
        }

        LoggerUtility.LogInformation($"Перезапись файла {YamlFilePath} завершена успешно.");

        // Выводим информацию в консоль о том, что записано в файл
        Console.WriteLine("Данные, записанные в файл:");
        foreach (var line in linesToWrite)
        {
          Console.WriteLine(line);
        }
      }
      catch (Exception ex)
      {
        LoggerUtility.LogError($"Ошибка при перезаписи файла {YamlFilePath}: {ex.Message}");
      }
    }

    #endregion

    #region Скрытые методы.

    /// <summary>
    /// Читает все строки из YAML файла асинхронно.
    /// </summary>
    /// <returns>Список строк файла</returns>
    private async Task<IEnumerable<string>> ReadYamlFileLinesAsync()
    {
      if (!File.Exists(YamlFilePath))
      {
        return new List<string>();
      }

      using (var reader = new StreamReader(YamlFilePath))
      {
        var content = await reader.ReadToEndAsync();
        return content.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
      }
    }

    /// <summary>
    /// Обрабатывает одну строку YAML и устанавливает соответствующее значение в объект.
    /// </summary>
    /// <param name="line">Строка из YAML файла.</param>
    /// <param name="properties">Список свойств объекта, в который будет присваиваться значение.</param>
    /// <param name="result">Объект, в который будут установлены значения из YAML.</param>
    private void ProcessYamlLine(string line, PropertyInfo[] properties, object result)
    {
      var parts = line.Split(new[] { ':' }, 2);
      if (parts.Length == 2)
      {
        var propertyName = parts[0].Trim();
        var propertyValue = parts[1].Trim().Trim('"');

        var property = properties.FirstOrDefault(p => string.Equals(p.Name, propertyName, StringComparison.OrdinalIgnoreCase));
        if (property != null)
        {
          SetPropertyValue(property, propertyValue, result);
        }
      }
    }

    /// <summary>
    /// Преобразует строковое значение в нужный тип и устанавливает его в свойство объекта.
    /// </summary>
    /// <param name="property">Свойство объекта, в которое будет установлено значение.</param>
    /// <param name="propertyValue">Значение свойства в виде строки.</param>
    /// <param name="result">Объект, в который будет установлено значение.</param>
    private void SetPropertyValue(PropertyInfo property, string propertyValue, object result)
    {
      var convertedValue = Convert.ChangeType(propertyValue, property.PropertyType);
      property.SetValue(result, convertedValue);
    }
    #endregion

    #region Конструктор.

    /// <summary>
    /// Конструктор класса YamlHelper.
    /// </summary>
    /// <param name="pathYaml">Путь к YAML файлу</param>
    public YamlUtility(string pathYaml)
    {
      YamlFilePath = pathYaml;

      _serializer = new SerializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .Build();
      _deserializer = new DeserializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .Build();
    }

    #endregion
  }
}

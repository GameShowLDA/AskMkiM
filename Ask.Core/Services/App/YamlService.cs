using System.IO;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Ask.Core.Services.App;

/// <summary>
/// Универсальный сервис для работы с YAML файлами.
/// </summary>
public sealed class YamlService<T>
  where T : class, new()
{
  private readonly string _filePath;

  private readonly ISerializer _serializer;

  private readonly IDeserializer _deserializer;

  public YamlService(string filePath)
  {
    _filePath = filePath;

    _serializer = new SerializerBuilder()
      .WithNamingConvention(CamelCaseNamingConvention.Instance)
      .Build();

    _deserializer = new DeserializerBuilder()
      .WithNamingConvention(CamelCaseNamingConvention.Instance)
      .IgnoreUnmatchedProperties()
      .Build();
  }

  /// <summary>
  /// Загружает объект из YAML файла.
  /// </summary>
  public T Load()
  {
    if (!File.Exists(_filePath))
    {
      return new T();
    }

    try
    {
      string yaml = File.ReadAllText(_filePath);

      T? result = _deserializer.Deserialize<T>(yaml);

      return result ?? new T();
    }
    catch
    {
      return new T();
    }
  }

  /// <summary>
  /// Сохраняет объект в YAML файл.
  /// </summary>
  public void Save(T value)
  {
    string? directory = Path.GetDirectoryName(_filePath);

    if (!string.IsNullOrWhiteSpace(directory))
    {
      Directory.CreateDirectory(directory);
    }

    string yaml = _serializer.Serialize(value);

    File.WriteAllText(_filePath, yaml);
  }
}
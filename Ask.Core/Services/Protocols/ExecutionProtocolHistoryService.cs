using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Metadata.Static;
using Ask.Core.Services.FilesUtility;
using System.IO;
using System.Text;

namespace Ask.Core.Services.Protocols;

/// <summary>
/// Предоставляет методы для сохранения протоколов выполнения в общий каталог истории.
/// </summary>
public static class ExecutionProtocolHistoryService
{
  private const string DefaultProtocolName = "protocol";
  private const string ExecutionProtocolExtension = ".lst";
  private const string InspectionProtocolExtension = ".rtlst";
  private static readonly UTF8Encoding Utf8NoBom = new(false);

  /// <summary>
  /// Сохраняет протокол выполнения в общий каталог истории,
  /// шифрует сохранённый файл и возвращает путь к нему.
  /// </summary>
  /// <param name="protocolName">Имя протокола.</param>
  /// <param name="messages">Коллекция сообщений, формирующих содержимое протокола.</param>
  /// <returns>
  /// Полный путь к сохранённому и зашифрованному файлу протокола.
  /// </returns>
  public static async Task<string> SaveAsync(string? protocolName, IEnumerable<ShowMessageModel> messages)
  {
    var lines = messages
      .Select(ExecutionProtocolLineFormatter.Format)
      .Where(static line => !string.IsNullOrWhiteSpace(line));

    return await SaveLinesAsync(protocolName, lines, ExecutionProtocolExtension);
  }

  /// <summary>
  /// Сохраняет итоговый протокол рядом с протоколом выполнения.
  /// </summary>
  public static Task<string> SaveInspectionAsync(
    string? protocolName,
    string protocolText,
    string? executionProtocolPath = null)
  {
    var lines = protocolText
      .Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

    string? inspectionProtocolPath = string.IsNullOrWhiteSpace(executionProtocolPath)
      ? null
      : Path.ChangeExtension(executionProtocolPath, InspectionProtocolExtension);

    return SaveLinesAsync(protocolName, lines, InspectionProtocolExtension, inspectionProtocolPath);
  }

  /// <summary>
  /// Возвращает абсолютный путь к каталогу хранения протоколов выполнения.
  /// </summary>
  /// <returns>Абсолютный путь к каталогу <c>History</c>.</returns>
  public static string GetHistoryDirectory() => Path.GetFullPath(Path.Combine("..", FileLocations.DataSaveDirectory));

  /// <summary>
  /// Возвращает путь к последнему сохранённому протоколу
  /// из каталога истории.
  /// </summary>
  /// <returns>
  /// Полный путь к последнему сохранённому протоколу
  /// либо <see langword="null"/>, если протоколы отсутствуют.
  /// </returns>
  public static string? ResolveLatestProtocolPath()
  {
    string historyDirectory = GetHistoryDirectory();
    if (!Directory.Exists(historyDirectory))
    {
      return null;
    }

    return Directory
      .EnumerateFiles(historyDirectory, "*.*", SearchOption.AllDirectories)
      .Where(static file => string.Equals(Path.GetExtension(file), ".lst", StringComparison.OrdinalIgnoreCase)
                            || string.Equals(Path.GetExtension(file), ".lstw", StringComparison.OrdinalIgnoreCase))
      .OrderByDescending(File.GetLastWriteTimeUtc)
      .FirstOrDefault();
  }

  /// <summary>
  /// Формирует уникальный путь к файлу протокола в указанном каталоге.
  /// </summary>
  /// <param name="dateDirectory">Каталог, в котором будет сохранён протокол.</param>
  /// <param name="protocolName">Имя протокола, используемое при формировании имени файла.</param>
  /// <returns>Полный путь к файлу, не совпадающий с уже существующими файлами.</returns>
  private static string BuildUniqueFilePath(string dateDirectory, string? protocolName, string extension)
  {
    string baseName = SanitizeFileName(Path.GetFileNameWithoutExtension(protocolName));
    if (string.IsNullOrWhiteSpace(baseName))
    {
      baseName = DefaultProtocolName;
    }

    string timestamp = DateTime.Now.ToString("HH-mm-ss");
    string fileName = $"{baseName}_{timestamp}{extension}";
    string filePath = Path.Combine(dateDirectory, fileName);

    int copyIndex = 1;
    while (File.Exists(filePath))
    {
      fileName = $"{baseName}_{timestamp}_{copyIndex}{extension}";
      filePath = Path.Combine(dateDirectory, fileName);
      copyIndex++;
    }

    return filePath;
  }

  private static async Task<string> SaveLinesAsync(
    string? protocolName,
    IEnumerable<string> lines,
    string extension,
    string? targetPath = null)
  {
    string historyDirectory = GetHistoryDirectory();
    string dateDirectory = Path.Combine(historyDirectory, DateTime.Now.ToString("yyyy-MM-dd"));
    Directory.CreateDirectory(dateDirectory);

    string filePath = string.IsNullOrWhiteSpace(targetPath)
      ? BuildUniqueFilePath(dateDirectory, protocolName, extension)
      : Path.GetFullPath(targetPath);
    await File.WriteAllLinesAsync(filePath, lines, Utf8NoBom);
    FileEncryptionManager.EncryptFile(filePath);
    return Path.GetFullPath(filePath);
  }

  /// <summary>
  /// Преобразует строку в корректное имя файла,
  /// заменяя недопустимые символы символом подчёркивания.
  /// </summary>
  /// <param name="value">Исходное имя файла.</param>
  /// <returns>
  /// Строка, пригодная для использования в качестве имени файла.
  /// Если исходное значение отсутствует или содержит только пробельные символы,
  /// возвращается пустая строка.
  /// </returns>
  private static string SanitizeFileName(string? value)
  {
    if (string.IsNullOrWhiteSpace(value))
    {
      return string.Empty;
    }

    var invalidChars = Path.GetInvalidFileNameChars();
    var builder = new StringBuilder(value.Length);

    foreach (char c in value.Trim())
    {
      builder.Append(invalidChars.Contains(c) ? '_' : c);
    }

    return builder.ToString();
  }

}

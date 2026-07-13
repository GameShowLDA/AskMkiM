using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Metadata.Static;
using Ask.Core.Services.FilesUtility;
using System.IO;
using System.Text;

namespace Ask.Core.Services.Protocols;

/// <summary>
/// Saves execution protocols to the common History directory.
/// </summary>
public static class ExecutionProtocolHistoryService
{
  private const string DefaultProtocolName = "protocol";
  private static readonly UTF8Encoding Utf8NoBom = new(false);

  /// <summary>
  /// Saves protocol messages to History\yyyy-MM-dd using the Name_HH-mm-ss.lst pattern.
  /// </summary>
  public static async Task<string> SaveAsync(string? protocolName, IEnumerable<ShowMessageModel> messages)
  {
    string historyDirectory = GetHistoryDirectory();
    string dateDirectory = Path.Combine(historyDirectory, DateTime.Now.ToString("yyyy-MM-dd"));
    Directory.CreateDirectory(dateDirectory);

    string filePath = BuildUniqueFilePath(dateDirectory, protocolName);
    var lines = messages
      .Select(FormatProtocolLine)
      .Where(static line => !string.IsNullOrWhiteSpace(line));

    await File.WriteAllLinesAsync(filePath, lines, Utf8NoBom);
    FileEncryptionManager.EncryptFile(filePath);
    return Path.GetFullPath(filePath);
  }

  /// <summary>
  /// Returns the absolute History directory path.
  /// </summary>
  public static string GetHistoryDirectory()
  {
    return Path.GetFullPath(Path.Combine("..", FileLocations.DataSaveDirectory));
  }

  /// <summary>
  /// Returns the latest saved protocol path from History.
  /// </summary>
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

  private static string BuildUniqueFilePath(string dateDirectory, string? protocolName)
  {
    string baseName = SanitizeFileName(Path.GetFileNameWithoutExtension(protocolName));
    if (string.IsNullOrWhiteSpace(baseName))
    {
      baseName = DefaultProtocolName;
    }

    string timestamp = DateTime.Now.ToString("HH-mm-ss");
    string fileName = $"{baseName}_{timestamp}.lst";
    string filePath = Path.Combine(dateDirectory, fileName);

    int copyIndex = 1;
    while (File.Exists(filePath))
    {
      fileName = $"{baseName}_{timestamp}_{copyIndex}.lst";
      filePath = Path.Combine(dateDirectory, fileName);
      copyIndex++;
    }

    return filePath;
  }

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

  private static string FormatProtocolLine(ShowMessageModel message)
  {
    string header = message.Header?.TrimEnd() ?? string.Empty;
    string body = message.Message?.TrimEnd() ?? string.Empty;

    bool hasHeader = !string.IsNullOrWhiteSpace(header);
    bool hasBody = !string.IsNullOrWhiteSpace(body);

    if (!hasHeader && !hasBody)
    {
      return string.Empty;
    }

    if (!hasHeader)
    {
      return body;
    }

    if (!hasBody)
    {
      return header;
    }

    string separator = header.EndsWith(' ') || body.StartsWith(' ') ? string.Empty : " ";
    return $"{header}{separator}{body}";
  }
}

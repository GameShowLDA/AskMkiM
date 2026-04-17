namespace Ask.Core.Services.FileFormats.Opk
{
  /// <summary>
  /// Представляет результат конвертации OPK-файла в PK-файл.
  /// </summary>
  public sealed class ConversionResult
  {
    /// <summary>
    /// Получает путь к исходному OPK-файлу.
    /// </summary>
    public string InputPath { get; init; } = null!;

    /// <summary>
    /// Получает путь к созданному PK-файлу.
    /// </summary>
    public string? OutputPath { get; init; }

    /// <summary>
    /// Получает путь к metadata-файлу, созданному рядом с PK-файлом.
    /// </summary>
    public string? MetadataPath { get; init; }

    /// <summary>
    /// Получает признак успешного завершения конвертации.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Получает текст ошибки, если конвертация завершилась неуспешно.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Получает количество строк, записанных в результирующий PK-файл.
    /// </summary>
    public int LinesCount { get; init; }
  }
}

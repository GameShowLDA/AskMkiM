namespace Ask.Diagnostics.Models
{
  /// <summary>
  /// Описывает дополнительный файл, включаемый в диагностический пакет.
  /// </summary>
  public sealed class CrashReportArtifact
  {
    private CrashReportArtifact(string fileName, object? content, bool isPlainText)
    {
      ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

      FileName = fileName;
      Content = content;
      IsPlainText = isPlainText;
    }

    /// <summary>
    /// Имя файла внутри диагностического пакета.
    /// </summary>
    public string FileName { get; }

    /// <summary>
    /// Содержимое файла.
    /// </summary>
    public object? Content { get; }

    /// <summary>
    /// Признак сохранения содержимого как обычного текста.
    /// </summary>
    public bool IsPlainText { get; }

    /// <summary>
    /// Создаёт текстовое вложение.
    /// </summary>
    /// <param name="fileName">Имя файла внутри диагностического пакета.</param>
    /// <param name="content">Текстовое содержимое файла.</param>
    /// <returns>Описание текстового вложения.</returns>
    public static CrashReportArtifact Text(string fileName, string content) =>
      new(fileName, content, isPlainText: true);

    /// <summary>
    /// Создаёт JSON-вложение.
    /// </summary>
    /// <param name="fileName">Имя файла внутри диагностического пакета.</param>
    /// <param name="content">Объект, сериализуемый в JSON.</param>
    /// <returns>Описание JSON-вложения.</returns>
    public static CrashReportArtifact Json(string fileName, object? content) =>
      new(fileName, content, isPlainText: false);
  }
}

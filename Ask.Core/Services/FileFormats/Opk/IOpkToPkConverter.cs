namespace Ask.Core.Services.FileFormats.Opk
{
  /// <summary>
  /// Определяет контракт конвертера OPK-файлов в PK-файлы.
  /// </summary>
  public interface IOpkToPkConverter
  {
    /// <summary>
    /// Конвертирует указанный OPK-файл в PK-файл и сохраняет результат в выбранную папку.
    /// </summary>
    /// <param name="inputPath">Путь к исходному OPK-файлу.</param>
    /// <param name="outputDirectory">Папка для сохранения результирующего PK-файла.</param>
    /// <returns>Результат выполнения конвертации.</returns>
    ConversionResult Convert(string inputPath, string outputDirectory);
  }
}

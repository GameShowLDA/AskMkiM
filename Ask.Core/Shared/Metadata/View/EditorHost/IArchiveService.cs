namespace Ask.Core.Shared.Metadata.View.EditorHost
{
  public interface IArchiveService
  {
    /// <summary>
    /// Создаёт новый пустой архив.
    /// </summary>
    void CreateNewArchive();

    /// <summary>
    /// Открывает архив по указанному пути.
    /// </summary>
    /// <param name="filePath">Абсолютный путь к файлу.</param>
    void OpenArchive();

    /// <summary>
    /// Удаляет выбранный архив.
    /// </summary>
    void DeleteArchive();
  }
}

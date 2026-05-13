namespace Ask.UI.Features.Archive.Contracts
{
  /// <summary>
  /// Определяет набор действий для управления архивами и файлами в UI.
  /// </summary>
  public interface IArchiveViewActions
  {
    /// <summary>
    /// Создаёт новый архив.
    /// </summary>
    void CreateArchive();

    /// <summary>
    /// Загружает архив из внешнего источника.
    /// </summary>
    void UploadArchive();

    /// <summary>
    /// Выгружает архивы.
    /// </summary>
    void DownloadArchives();

    /// <summary>
    /// Открывает архив, выбранный через контекстное меню.
    /// </summary>
    /// <returns>Асинхронная задача открытия архива.</returns>
    Task OpenContextArchiveAsync();

    /// <summary>
    /// Сохраняет архив, выбранный через контекстное меню.
    /// </summary>
    void SaveContextArchive();

    /// <summary>
    /// Печатает каталог выбранного архива.
    /// </summary>
    /// <returns>Асинхронная задача печати.</returns>
    Task PrintContextArchiveCatalogAsync();

    /// <summary>
    /// Добавляет файл в архив, выбранный через контекстное меню.
    /// </summary>
    void AddFileToContextArchive();

    /// <summary>
    /// Удаляет архив, выбранный через контекстное меню.
    /// </summary>
    /// <returns>Асинхронная задача удаления архива.</returns>
    Task DeleteContextArchiveAsync();

    /// <summary>
    /// Открывает файл, выбранный через контекстное меню.
    /// </summary>
    /// <returns>Асинхронная задача открытия файла.</returns>
    Task OpenContextFileAsync();

    /// <summary>
    /// Открывает выбранный файл во внешнем редакторе.
    /// </summary>
    void OpenContextFileInEditor();

    /// <summary>
    /// Копирует выбранный файл архива.
    /// </summary>
    void CopyContextFile();

    /// <summary>
    /// Вырезает выбранный файл архива.
    /// </summary>
    void CutContextFile();

    /// <summary>
    /// Вставляет файл из буфера обмена в архив.
    /// </summary>
    /// <returns>Асинхронная задача вставки.</returns>
    Task PasteContextFileAsync();

    /// <summary>
    /// Удаляет выбранный файл архива.
    /// </summary>
    /// <returns>Асинхронная задача удаления файла.</returns>
    Task DeleteContextFileAsync();

    /// <summary>
    /// Выполняет повторную проверку выбранного архива на проверке.
    /// </summary>
    /// <returns>Асинхронная задача повторной проверки.</returns>
    Task RecheckSelectedReviewArchiveAsync();

    /// <summary>
    /// Сохраняет выбранный архив на диск.
    /// </summary>
    void SaveSelectedArchiveToDisk();

    /// <summary>
    /// Добавляет файл в выбранный архив.
    /// </summary>
    void AddFileToSelectedArchive();

    /// <summary>
    /// Удаляет выбранный архив.
    /// </summary>
    /// <returns>Асинхронная задача удаления архива.</returns>
    Task DeleteSelectedArchiveAsync();

    /// <summary>
    /// Вставляет файл из буфера обмена в выбранный архив.
    /// </summary>
    /// <returns>Асинхронная задача вставки.</returns>
    Task PasteIntoSelectedArchiveAsync();

    /// <summary>
    /// Удаляет выбранный файл.
    /// </summary>
    /// <returns>Асинхронная задача удаления файла.</returns>
    Task DeleteSelectedFileAsync();

    /// <summary>
    /// Копирует выбранный файл.
    /// </summary>
    void CopySelectedFile();

    /// <summary>
    /// Вырезает выбранный файл.
    /// </summary>
    void CutSelectedFile();

    /// <summary>
    /// Вставляет файл из буфера обмена в архив выбранного файла.
    /// </summary>
    /// <returns>Асинхронная задача вставки.</returns>
    Task PasteIntoSelectedFileArchiveAsync();

    /// <summary>
    /// Конвертирует выбранный файл в формат PKW.
    /// </summary>
    void ConvertSelectedFileToPkw();

    /// <summary>
    /// Открывает выбранный файл во внешнем редакторе.
    /// </summary>
    void OpenSelectedFileInEditor();

    /// <summary>
    /// Запускает выбранный файл в исполнителе.
    /// </summary>
    /// <returns>Асинхронная задача запуска.</returns>
    Task RunSelectedFileInExecutorAsync();

    /// <summary>
    /// Выполняет повторную проверку выбранного файла на проверке.
    /// </summary>
    /// <returns>Асинхронная задача повторной проверки.</returns>
    Task RecheckSelectedReviewFileAsync();
  }
}

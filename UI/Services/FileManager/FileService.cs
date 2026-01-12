namespace UI.Services.FileManager
{
  /// <summary>
  /// Предоставляет высокоуровневый доступ к основным операциям работы с файлами:
  /// сравнением, созданием, получением информации о названии и открытием файлов.
  /// </summary>
  public class FileService
  {
    /// <summary>
    /// Сервис для сравнения файлов по содержимому или другим критериям.
    /// </summary>
    public FileCompareService Comparison { get; }

    /// <summary>
    /// Сервис для создания новых файлов и записи данных в них.
    /// </summary>
    public FileCreateService Creation { get; }

    /// <summary>
    /// Сервис для работы с именами файлов: генерация, проверка формата, переименование.
    /// </summary>
    public FileNameService Name { get; }

    /// <summary>
    /// Сервис для открытия файлов и чтения их содержимого.
    /// </summary>
    public FileOpenService Opening { get; }

    /// <summary>
    /// Инициализирует экземпляр <see cref="FileService"/> со всеми необходимыми подсервисами.
    /// </summary>
    public FileService(UI.Components.MultiEditorMethods.FileManager fileManager)
    {
      Comparison = new FileCompareService(fileManager.EditorWorkspaceModel);
      Creation = new FileCreateService(fileManager);
      Name = new FileNameService(fileManager.EditorWorkspaceModel);
      Opening = new FileOpenService(fileManager);
    }
  }
}
